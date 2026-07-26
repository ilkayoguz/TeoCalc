using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Game.Explorer;
using TeoCalc.Rendering.Faceplate;
using System.Text;

namespace TeoCalc.Rendering;

/// <summary>Firmware timer, key, and display orchestration for all models via <see cref="ICalcFirmwareGateway"/>.</summary>
public sealed class CalcExplorerSession : ICalcExplorerSession, IDisposable
{
  /// <summary>ImGui click completes on mouse-up; firmware needs batches before KeyUp (prefix keys).</summary>
  private const int KeySettleBatches = 40;

  private static readonly string[] ExplorerModels = [.. TeoCalcModelCatalog.SupportedModels];

  private ICalcFirmwareGateway? _firmware;

  private FirmwareDisplaySnapshot _displaySnapshot =
    new(string.Empty, Visible: false, BlankPulse: false, Revision: 0, StepCount: 0, ProgramCounter: 0);

  private FirmwareBatchSnapshot _lastBatch =
    new(
      StepCount: 0,
      ProgramCounter: 0,
      Status: 0,
      KeyBuffer: 0,
      LastHandlerId: null,
      KeyLineHeld: false,
      ActiveKey: null,
      Display: null,
      Rom: 0,
      Grp: 0,
      P: 0,
      Classic: null);

  private bool _mouseKeyHeld;

  private bool _keyboardKeyHeld;

  private int[] _faceplateSwitchIndices = [];

  private IReadOnlyList<CalcSwitchSpec> _faceplateSwitchSpecs = [];

  private bool _cardInserted;

  private string? _loadedCardPath;

  private string[]? _cardStripLabels;

  private bool[]? _cardStripLabelsEnabled;

  private TeoCardDocument? _loadedTeoCard;
  private bool _cardMetadataDirty;
  private int _cardMetadataEpoch;

  /// <summary>Last loaded/saved program codes; compared to live RAM for dirty detection.</summary>
  private byte[]? _savedProgramSnapshot;

  private const int MaxProgramUndoDepth = 32;

  private readonly List<ProgramEditSnapshot> _programUndoStack = [];

  private readonly List<ProgramEditSnapshot> _programRedoStack = [];

  /// <summary>Studio program-step breakpoints (Classic RAM indices).</summary>
  private readonly HashSet<int> _studioBreakpoints = [];
  /// <summary>
  /// After Continue from a hit, ignore this step until PTR leaves it (avoid instant re-pause).
  /// </summary>
  private int _breakpointContinueIgnoreStep = -1;

  /// <summary>W/PRGM edit: step waiting for the second key of a LBL/STO/shift pair.</summary>
  private int _wprgmPendingPrefixStep = -1;

  /// <summary>When true, the second key inserts a new RAM byte after the prefix.</summary>
  private bool _wprgmPendingInsertSecond;

  /// <summary>
  /// W/PRGM Machine LED focus: 0 = left museum box, 1 = right (after LBL/g/f/…).
  /// Line (↑/↓) only changes on arrow / click — never on a normal key.
  /// </summary>
  private int _wprgmMachineSlot;

  private static readonly float[] ExecutionSpeedSteps =
  [
    0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f,
  ];

  public static int ExecutionSpeedStepCount => ExecutionSpeedSteps.Length;

  private int _executionSpeedIndex = 2; // 1×

  public CalcExplorerSession(string engineRoot)
  {
    EngineRoot = engineRoot;
    ModelIndex = Array.FindIndex(ExplorerModels, id => id == TeoCalcModelCatalog.PriorityModel);
    if (ModelIndex < 0)
    {
      ModelIndex = 0;
    }

    LoadModel(ModelIndex);
    CalcSessionProfiles.ApplyTo(this);
  }

  public bool UsesFirmwareGateway => _firmware is not null;

  public string EngineRoot { get; }

  public string[] Models => ExplorerModels;

  public int ModelIndex { get; private set; }

  public string DisplayName => Model.DisplayName;

  public string EngineModelId => ExplorerModels[Math.Clamp(ModelIndex, 0, ExplorerModels.Length - 1)];

  public TeoCalcModelDefinition Model { get; private set; } = null!;

  public ProgramVocabulary? Vocabulary { get; private set; }

  public MicrocodeMapCatalog? Map { get; private set; }

  public MicrocodeCrossRefCatalog? CrossRef { get; private set; }

  public bool SupportsFaceplate => Vocabulary is not null;

  public bool SupportsMicrocode => Map is not null;

  public int MicrocodeScroll { get; set; }

  public int ProgramScroll { get; set; }

  public int SelectedAddress { get; set; }

  /// <summary>Selected user-program step index in Studio / explorer listing.</summary>
  public int SelectedProgramStep { get; set; }

  /// <summary>
  /// Card file <c>CodeEncoding</c> preference when loading/saving program text
  /// (<see cref="CardCodeEncoding.Mnemonic"/> or <see cref="CardCodeEncoding.Machine"/>).
  /// Studio UI always shows both encodings; clipboard copy is dual TSV.
  /// </summary>
  public string StudioCodeEncoding { get; set; } = CardCodeEncoding.Mnemonic;

  /// <summary>Transient status for Studio copy/paste / apply feedback.</summary>
  public string StudioStatusMessage { get; set; } = string.Empty;

  /// <summary>
  /// True when Classic RAM differs from the last loaded/saved card snapshot, or card
  /// metadata ([General] / [Label]) was edited in Studio.
  /// </summary>
  public bool IsProgramDirty =>
    SupportsCardProgram
    && ((_savedProgramSnapshot is not null && !ProgramMatchesSnapshot())
      || _cardMetadataDirty);

  /// <summary>
  /// User moved W/PRGM → RUN while dirty; UI should confirm Save / Discard / Cancel.
  /// </summary>
  public bool PendingLeaveProgramConfirm { get; private set; }

  /// <summary>Ctrl+S / Save when an inserted card path already exists on disk.</summary>
  public bool PendingStudioSaveConfirm { get; private set; }

  /// <summary>Ctrl+R when program RAM differs from the last loaded/saved snapshot.</summary>
  public bool PendingStudioRevertConfirm { get; private set; }

  /// <summary>When true, microcode watch follows the live ROM fetch address while running / stepping.</summary>
  public bool FollowRomWatch { get; set; } = true;

  /// <summary>
  /// When true, F10/F11 use microcode step (Debug panel open). Otherwise card-program
  /// models use Studio grain (row/key) and others use microcode.
  /// </summary>
  public bool PreferMicrocodeHotkeys { get; set; }

  public bool ExecutionPaused
  {
    get => _firmware?.ExecutionPaused ?? false;
    set
    {
      if (_firmware is not null)
      {
        _firmware.ExecutionPaused = value;
      }
    }
  }

  public bool SupportsInstructionStep =>
    _firmware?.SupportsInstructionStep ?? false;

  public bool ProgramMode
  {
    get => _firmware?.ProgramMode ?? false;
    set => _firmware?.SetProgramMode(value);
  }

  public bool PowerOn
  {
    get => _firmware?.PowerOn ?? false;
    set
    {
      if (_firmware is null)
      {
        return;
      }

      if (value)
      {
        if (!_firmware.PowerOn)
        {
          PowerOnResume();
        }
      }
      else
      {
        PowerOff();
      }
    }
  }

  public string DisplayText => _displaySnapshot.Text;

  public FirmwareDisplaySnapshot DisplaySnapshot => _displaySnapshot;

  public FirmwareBatchSnapshot LastBatch => _lastBatch;

  public ShiftPreviewController ShiftPreview { get; } = new();

  public bool IsKeyHeld => _mouseKeyHeld || _keyboardKeyHeld;

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged
  {
    add
    {
      if (_firmware is not null)
      {
        _firmware.DisplayChanged += value;
      }
    }
    remove
    {
      if (_firmware is not null)
      {
        _firmware.DisplayChanged -= value;
      }
    }
  }

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed
  {
    add
    {
      if (_firmware is not null)
      {
        _firmware.KeyProcessed += value;
      }
    }
    remove
    {
      if (_firmware is not null)
      {
        _firmware.KeyProcessed -= value;
      }
    }
  }

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged
  {
    add
    {
      if (_firmware is not null)
      {
        _firmware.KeyStateChanged += value;
      }
    }
    remove
    {
      if (_firmware is not null)
      {
        _firmware.KeyStateChanged -= value;
      }
    }
  }

  public event EventHandler<FirmwareBatchCompletedEventArgs>? BatchCompleted
  {
    add
    {
      if (_firmware is not null)
      {
        _firmware.BatchCompleted += value;
      }
    }
    remove
    {
      if (_firmware is not null)
      {
        _firmware.BatchCompleted -= value;
      }
    }
  }

  public void PowerOnResume()
  {
    _firmware?.PowerOnResume();
    SyncPowerSwitchIndicesOn();
    TryRestoreInsertedCardProgram();
    ApplyNonPowerFaceplateSwitchesToFirmware();
    // Baseline for dirty detection (empty RAM or restored card).
    CaptureSavedProgramSnapshot();
    ClearProgramEditHistory();
  }
  public void PowerOff()
  {
    _firmware?.PowerOff();
    SelectedAddress = 0;
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();
    ResetNonPowerSwitchesToInitial();
  }

  public bool IsDisplayVisible() => _displaySnapshot.Visible;

  public void EndDisplayFrame() =>
    _firmware?.EndDisplayFrame();

  public void SetKeyboardKeyHeld(bool held)
  {
    if (_keyboardKeyHeld && !held)
    {
      _firmware?.KeyUp();
    }

    _keyboardKeyHeld = held;
    // W/PRGM Studio edit must not keep the Classic key line asserted — idle batches would
    // MemoryInsert and clobber the overwrite. Visual press state stays in HeldKeyChartIndex.
    if (ProgramMode && SupportsCardProgram)
    {
      _firmware?.SetKeyLineHeld(false);
      return;
    }

    _firmware?.SetKeyLineHeld(IsKeyHeld);
  }

  public void ReleaseMouseKey()
  {
    if (_mouseKeyHeld)
    {
      _firmware?.KeyUp();
    }

    _mouseKeyHeld = false;
    if (ProgramMode && SupportsCardProgram)
    {
      _firmware?.SetKeyLineHeld(false);
      return;
    }

    _firmware?.SetKeyLineHeld(_keyboardKeyHeld);
    RunFirmwareTicks(KeySettleBatches);
  }

  public void ClearShiftPreview() =>
    ShiftPreview.Clear();

  public void ToggleProgramMode()
  {
    if (!PowerOn)
    {
      return;
    }

    ToggleProgramModeTo(!ProgramMode);
  }

  public void ToggleProgramModeTo(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    // Leaving W/PRGM with unsaved RAM edits → confirm before RUN.
    if (!programMode && IsProgramDirty)
    {
      PendingLeaveProgramConfirm = true;
      return;
    }

    PendingLeaveProgramConfirm = false;
    // Entering W/PRGM runs a firmware batch that can nudge program RAM (PTR/markers).
    // Absorb that into the clean snapshot so RUN without edits does not confirm.
    bool absorbModeSwitch = programMode && !IsProgramDirty;
    _firmware?.SetProgramMode(programMode);
    if (absorbModeSwitch)
    {
      CaptureSavedProgramSnapshot();
    }
  }

  /// <summary>Confirm dialog: leave W/PRGM without writing the card file (RAM edits kept; stays dirty).</summary>
  public void ConfirmDiscardProgramEditsAndRun()
  {
    PendingLeaveProgramConfirm = false;
    if (!PowerOn)
    {
      return;
    }

    _firmware?.SetProgramMode(false);
  }

  /// <summary>Confirm dialog: cancel leaving W/PRGM (stay in program mode).</summary>
  public void CancelLeaveProgramConfirm() =>
    PendingLeaveProgramConfirm = false;

  /// <summary>Open Studio save confirm (Overwrite / Save As / Cancel) when an inserted path exists.</summary>
  public void RequestStudioSaveConfirm() =>
    PendingStudioSaveConfirm = true;

  public void CancelStudioSaveConfirm() =>
    PendingStudioSaveConfirm = false;

  /// <summary>
  /// Request revert-to-snapshot confirm when dirty; otherwise report status / no-op.
  /// </summary>
  public void RequestStudioRevertConfirm()
  {
    if (!SupportsCardProgram)
    {
      StudioStatusMessage = "Program memory not available.";
      return;
    }

    if (_savedProgramSnapshot is null)
    {
      StudioStatusMessage = "Nothing to revert (no loaded/saved snapshot).";
      return;
    }

    if (!IsProgramDirty)
    {
      StudioStatusMessage = "Program matches last saved snapshot.";
      return;
    }

    PendingStudioRevertConfirm = true;
  }

  public void CancelStudioRevertConfirm() =>
    PendingStudioRevertConfirm = false;

  /// <summary>
  /// Confirm dialog: save to <paramref name="path"/> then switch to RUN.
  /// Returns false when save fails (stays in W/PRGM; <see cref="PendingLeaveProgramConfirm"/> cleared).
  /// </summary>
  public bool TryConfirmSaveProgramEditsAndRun(string path, out string? error)
  {
    PendingLeaveProgramConfirm = false;
    if (!TrySaveCardProgram(path, out error))
    {
      return false;
    }

    _firmware?.SetProgramMode(false);
    return true;
  }
  public void EnsureFaceplateSwitches(IReadOnlyList<CalcSwitchSpec> specs)
  {
    _faceplateSwitchSpecs = specs;
    if (_faceplateSwitchIndices.Length == specs.Count)
    {
      return;
    }

    _faceplateSwitchIndices = new int[specs.Count];
    for (int i = 0; i < specs.Count; i++)
    {
      _faceplateSwitchIndices[i] = specs[i].ClampIndex(specs[i].InitialIndex);
    }
  }

  public int GetFaceplateSwitchIndex(int switchIndex, CalcSwitchSpec spec)
  {
    if ((uint)switchIndex >= (uint)_faceplateSwitchIndices.Length)
    {
      return spec.ClampIndex(spec.InitialIndex);
    }

    int index = _faceplateSwitchIndices[switchIndex];
    if (spec.IsPower && spec.PositionCount == 2)
    {
      index = PowerOn ? 1 : 0;
      _faceplateSwitchIndices[switchIndex] = index;
    }
    else     if (spec.IsPower && !PowerOn)
    {
      index = 0;
      _faceplateSwitchIndices[switchIndex] = 0;
    }
    else if (IsTwoPositionProgramRunSwitch(spec))
    {
      // Keep knob aligned with firmware when leave-PRGM is blocked by dirty confirm.
      index = ProgramMode ? 0 : 1;
      _faceplateSwitchIndices[switchIndex] = index;
    }

    return spec.ClampIndex(index);
  }

  private static bool IsTwoPositionProgramRunSwitch(CalcSwitchSpec spec) =>
    !spec.IsPower
    && spec.PositionCount == 2
    && string.Equals(spec.RightLabel, "RUN", StringComparison.OrdinalIgnoreCase);

  public float GetFaceplateSwitchNorm(int switchIndex, CalcSwitchSpec spec) =>
    spec.NormForIndex(GetFaceplateSwitchIndex(switchIndex, spec));

  public void SetFaceplateSwitchIndex(int switchIndex, CalcSwitchSpec spec, int positionIndex)
  {
    EnsureFaceplateSwitchesSize(switchIndex + 1);
    positionIndex = spec.ClampIndex(positionIndex);
    _faceplateSwitchIndices[switchIndex] = positionIndex;
    ApplyFaceplateSwitchToFirmware(spec, positionIndex);
  }

  public void AdvanceFaceplateSwitch(int switchIndex, CalcSwitchSpec spec)
  {
    int current = GetFaceplateSwitchIndex(switchIndex, spec);
    SetFaceplateSwitchIndex(switchIndex, spec, spec.NextIndex(current));
  }

  private void EnsureFaceplateSwitchesSize(int count)
  {
    if (_faceplateSwitchIndices.Length >= count)
    {
      return;
    }

    int[] next = new int[count];
    Array.Copy(_faceplateSwitchIndices, next, _faceplateSwitchIndices.Length);
    _faceplateSwitchIndices = next;
  }

  private void ResetNonPowerSwitchesToInitial()
  {
    int n = Math.Min(_faceplateSwitchIndices.Length, _faceplateSwitchSpecs.Count);
    for (int i = 0; i < n; i++)
    {
      CalcSwitchSpec spec = _faceplateSwitchSpecs[i];
      _faceplateSwitchIndices[i] = spec.IsPower
        ? 0
        : spec.ClampIndex(spec.InitialIndex);
    }
  }

  private void SyncPowerSwitchIndicesOn()
  {
    int n = Math.Min(_faceplateSwitchIndices.Length, _faceplateSwitchSpecs.Count);
    for (int i = 0; i < n; i++)
    {
      CalcSwitchSpec spec = _faceplateSwitchSpecs[i];
      if (!spec.IsPower)
      {
        continue;
      }

      if (spec.PositionCount == 2)
      {
        _faceplateSwitchIndices[i] = 1;
      }
      else if (_faceplateSwitchIndices[i] <= 0)
      {
        _faceplateSwitchIndices[i] = spec.ClampIndex(spec.InitialIndex > 0 ? spec.InitialIndex : spec.PositionCount - 1);
      }
    }
  }

  private void ApplyNonPowerFaceplateSwitchesToFirmware()
  {
    if (!PowerOn)
    {
      return;
    }

    int n = Math.Min(_faceplateSwitchIndices.Length, _faceplateSwitchSpecs.Count);
    for (int i = 0; i < n; i++)
    {
      CalcSwitchSpec spec = _faceplateSwitchSpecs[i];
      if (spec.IsPower)
      {
        continue;
      }

      ApplyModeSwitchToFirmware(spec, GetFaceplateSwitchIndex(i, spec));
    }
  }

  private void ApplyFaceplateSwitchToFirmware(CalcSwitchSpec spec, int positionIndex)
  {
    if (spec.IsPower)
    {
      if (positionIndex <= 0)
      {
        PowerOff();
        return;
      }

      bool wasOn = PowerOn;
      if (!wasOn)
      {
        PowerOnResume();
      }
      else
      {
        SyncPowerSwitchIndicesOn();
      }

      if (spec.PositionCount == 3)
      {
        // OFF · PRGM · RUN — mid is program mode.
        ToggleProgramModeTo(positionIndex == 1);
      }

      return;
    }

    if (!PowerOn)
    {
      return;
    }

    ApplyModeSwitchToFirmware(spec, positionIndex);
  }

  private void ApplyModeSwitchToFirmware(CalcSwitchSpec spec, int positionIndex)
  {
    if (spec.PositionCount == 2)
    {
      // Left = program / DEG / BEGIN / … ; right = run / RAD / END.
      ToggleProgramModeTo(positionIndex == 0);
      return;
    }

    // 3-pos mode: left & mid → program-ish, right → run.
    ToggleProgramModeTo(positionIndex <= 1);
  }

  public IReadOnlyList<string> LoadWarnings { get; private set; } = [];

  public void LoadModel(int index)
  {
    DisposeFirmware();

    ModelIndex = Math.Clamp(index, 0, ExplorerModels.Length - 1);
    string explorerModelId = ExplorerModels[ModelIndex];
    string engineModelFolder = CalcModelIds.ToEngineId(explorerModelId);
    string modelPath = Path.Combine(EngineRoot, engineModelFolder, "Model.json");
    Model = File.Exists(modelPath)
      ? TeoCalcModelDefinition.Load(modelPath)
      : CreatePlaceholderModel(explorerModelId);

    _firmware = CalcFirmwareGatewayLocator.CreateGateway(engineModelFolder);
    _firmware.DisplayChanged += OnFirmwareDisplayChanged;
    _firmware.BatchCompleted += OnFirmwareBatchCompleted;
    _faceplateSwitchIndices = [];
    _faceplateSwitchSpecs = [];
    ResetCardSlotState();
    ClearStudioBreakpoints();

    Vocabulary = null;
    if (Model.Program?.Vocabulary is { Length: > 0 } vocabularyPath)
    {
      string fullVocabularyPath = Path.Combine(
        EngineRoot,
        engineModelFolder,
        vocabularyPath.Replace('/', Path.DirectorySeparatorChar));
      if (File.Exists(fullVocabularyPath))
      {
        Vocabulary = ProgramVocabulary.Load(fullVocabularyPath);
      }
    }

    Map = null;
    CrossRef = null;
    if (!string.IsNullOrWhiteSpace(Model.Firmware.RomMap))
    {
      string mapPath = Path.Combine(
        EngineRoot,
        engineModelFolder,
        Model.Firmware.RomMap.Replace('/', Path.DirectorySeparatorChar));
      if (File.Exists(mapPath))
      {
        Map = MicrocodeMapCatalog.Load(mapPath);
        CrossRef = string.Equals(Model.Family, "Classic", StringComparison.OrdinalIgnoreCase)
          ? LoadCrossRefIfPresent(Path.Combine(EngineRoot, "Classic", "microcode.crossref.json"))
          : null;
      }
    }

    SelectedAddress = _firmware.LastBatch.ProgramCounter;
    MicrocodeScroll = RomWatchFollowScroll.CenterOn(
      SelectedAddress,
      Map?.WordCount ?? 0);
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();

    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(Model, explorerModelId);
    CalcFaceplateThemeState.ApplyForModel(faceplateModel);
    LoadWarnings = [.. CalcFirmwareGatewayLocator.AssetWarnings(engineModelFolder)];
  }

  public void Tick(float deltaSeconds) =>
    _firmware?.Tick(deltaSeconds * ExecutionSpeed);

  /// <summary>Free-run speed multiplier (0.25× … 16×). Affects firmware Tick only.</summary>
  public float ExecutionSpeed => ExecutionSpeedSteps[_executionSpeedIndex];

  public int ExecutionSpeedIndex => _executionSpeedIndex;

  public string ExecutionSpeedLabel => FormatExecutionSpeedLabel(_executionSpeedIndex);

  public static string FormatExecutionSpeedLabel(int index)
  {
    float speed = ExecutionSpeedSteps[Math.Clamp(index, 0, ExecutionSpeedSteps.Length - 1)];
    return $"{speed:0.##}x";
  }

  public void SetExecutionSpeedIndex(int index) =>
    _executionSpeedIndex = Math.Clamp(index, 0, ExecutionSpeedSteps.Length - 1);

  public void NudgeExecutionSpeed(int delta)
  {
    _executionSpeedIndex = Math.Clamp(
      _executionSpeedIndex + delta,
      0,
      ExecutionSpeedSteps.Length - 1);
  }

  public void StepCpu() =>
    StepMicrocodeInto();

  /// <summary>
  /// F11: Studio keystroke / FC element when card program and microcode hotkeys off;
  /// otherwise microcode into.
  /// </summary>
  public void StepInto()
  {
    if (!PreferMicrocodeHotkeys && SupportsCardProgram)
    {
      StepStudioKey();
      return;
    }

    StepMicrocodeInto();
  }

  /// <summary>
  /// F10: Studio Code row / FC box when card program and microcode hotkeys off;
  /// otherwise microcode over.
  /// </summary>
  public void StepOver()
  {
    if (!PreferMicrocodeHotkeys && SupportsCardProgram)
    {
      StepStudioLine();
      return;
    }

    StepMicrocodeOver();
  }

  /// <summary>True microcode single-step (Debug panel).</summary>
  public void StepMicrocodeInto()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    _firmware.StepInto();
    SyncRomWatchFromBatch(_firmware.LastBatch);
  }

  /// <summary>True microcode step-over (Debug panel).</summary>
  public void StepMicrocodeOver()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    _firmware.StepOver();
    SyncRomWatchFromBatch(_firmware.LastBatch);
  }

  /// <summary>True microcode step-out (Debug panel / Shift+F11).</summary>
  public void StepMicrocodeOut()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    _firmware.StepOut();
    SyncRomWatchFromBatch(_firmware.LastBatch);
  }

  /// <summary>
  /// F10 / Studio Over: one Code listing row or one FC box.
  /// Uses SeekPointer (not SST key) so one keypress always advances one grain.
  /// </summary>
  public void StepStudioLine()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      StepMicrocodeOver();
      return;
    }

    ExecutionPaused = true;
    if (!TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) || lines.Count == 0)
    {
      StepStudioKey();
      return;
    }

    IReadOnlyList<StudioListingView.Row> rows = BuildStudioListingRows(lines);
    if (rows.Count == 0)
    {
      return;
    }

    int highlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
    int rowIdx = FindStudioRowIndex(rows, highlight);

    if (StudioPaneSync.Focus == StudioPaneSync.StudioFocus.Flowchart)
    {
      StepStudioFlowchartBox(cpu, rows, highlight);
      return;
    }

    if (rowIdx < 0)
    {
      cpu.Program.AdvancePointer();
      SyncStudioToPointer(cpu);
      return;
    }

    // On RTN / R/S row: one F10 returns to that routine’s LBL (do not enter next label).
    if (IsStudioExitRow(rows[rowIdx]))
    {
      _ = TryWrapRoutineEnd(cpu, forceFromRow: rows[rowIdx].Index);
      return;
    }

    int next = rowIdx + 1;
    if (next >= rows.Count)
    {
      return;
    }

    SeekPointerForHighlight(cpu, rows[next].Index);
    SyncStudioToPointer(cpu);
  }

  /// <summary>
  /// F11 / Studio Step: one Classic keystroke (one RAM slot / one element inside an FC box).
  /// </summary>
  public void StepStudioKey()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      StepMicrocodeInto();
      return;
    }

    ExecutionPaused = true;
    // Already sitting just after RTN → wrap to LBL on the next keystroke step.
    if (TryWrapRoutineEnd(cpu, forceFromRow: -1))
    {
      return;
    }

    cpu.Program.AdvancePointer();
    SyncStudioToPointer(cpu);
  }

  /// <summary>Legacy alias — keystroke step.</summary>
  public void StepStudioVisible() => StepStudioKey();

  private void StepStudioFlowchartBox(
    ClassicCpu cpu,
    IReadOnlyList<StudioListingView.Row> rows,
    int highlightStep)
  {
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      EngineModelId,
      CardStripLabels,
      omitStripFilters: !ProgramMode);
    int nodeId = StudioFlowchartGraph.FindNodeIdForStep(graph, highlightStep);
    if (nodeId < 0)
    {
      cpu.Program.AdvancePointer();
      SyncStudioToPointer(cpu);
      return;
    }

    StudioFlowchartGraph.Node node = graph.Nodes[nodeId];
    if (node.Kind == StudioFlowchartGraph.NodeKind.End
        || (node.FirstStep >= 0
            && IsClassicRoutineEnd(cpu.Program.ReadCode(Math.Max(1, node.LastStep)))))
    {
      _ = TryWrapRoutineEnd(cpu, forceFromRow: node.FirstStep >= 0 ? node.FirstStep : highlightStep);
      return;
    }

    // Next node in routine order (by FirstStep).
    int bestId = -1;
    int bestStep = int.MaxValue;
    int after = node.LastStep >= 0 ? node.LastStep : highlightStep;
    foreach (StudioFlowchartGraph.Node other in graph.Nodes)
    {
      if (other.RoutineId != node.RoutineId || other.FirstStep <= after)
      {
        continue;
      }

      if (other.FirstStep < bestStep)
      {
        bestStep = other.FirstStep;
        bestId = other.Id;
      }
    }

    if (bestId < 0)
    {
      _ = TryWrapRoutineEnd(cpu, forceFromRow: after);
      return;
    }

    SeekPointerForHighlight(cpu, bestStep);
    SyncStudioToPointer(cpu);
  }

  private static int FindStudioRowIndex(IReadOnlyList<StudioListingView.Row> rows, int ptr)
  {
    // Prefer exact Index — fused Single rows can ContainsIndex() a neighbor's step.
    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].Index == ptr)
      {
        return i;
      }
    }

    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].ContainsIndex(ptr))
      {
        return i;
      }
    }

    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].Index >= ptr)
      {
        return i;
      }
    }

    return rows.Count > 0 ? rows.Count - 1 : -1;
  }

  private static bool IsStudioExitRow(StudioListingView.Row row)
  {
    string m = row.DisplayMnemonic.Trim();
    return string.Equals(m, "RTN", StringComparison.OrdinalIgnoreCase)
      || string.Equals(m, "R/S", StringComparison.OrdinalIgnoreCase)
      || string.Equals(row.Mnemonic.Trim(), "RTN", StringComparison.OrdinalIgnoreCase)
      || string.Equals(row.Mnemonic.Trim(), "R/S", StringComparison.OrdinalIgnoreCase);
  }

  private bool TryWrapRoutineEnd(ClassicCpu cpu, int forceFromRow)
  {
    int ptr = cpu.Program.PointerPosition();
    int from = forceFromRow >= 0 ? forceFromRow : ptr - 1;
    if (forceFromRow < 0)
    {
      if (ptr <= 1 || !IsClassicRoutineEnd(cpu.Program.ReadCode(ptr - 1)))
      {
        return false;
      }

      from = ptr - 1;
    }

    if (!TryFindRoutineLabelIndex(cpu, from, out int lblIndex))
    {
      return false;
    }

    SeekPointerForHighlight(cpu, lblIndex);
    SyncStudioToPointer(cpu);
    return true;
  }

  /// <summary>
  /// Place Classic PTR so Studio ▶ highlights <paramref name="instructionIndex"/>
  /// (marker sits just before that opcode; PTR slot itself is skipped in the listing).
  /// </summary>
  private static void SeekPointerForHighlight(ClassicCpu cpu, int instructionIndex)
  {
    int last = cpu.Program.LastContentIndex();
    int target = Math.Clamp(instructionIndex, 1, last);
    while (target > 1 && cpu.Program.ReadCode(target) == 0)
    {
      target--;
    }

    int seekTo = Math.Max(1, target - 1);
    cpu.Program.SeekPointer(seekTo);
  }

  private void SyncStudioToPointer(ClassicCpu cpu, bool syncFaceplateLed = true)
  {
    int selected = cpu.Program.PointerPosition();
    if (TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) && lines.Count > 0)
    {
      IReadOnlyList<StudioListingView.Row> rows = BuildStudioListingRows(lines);
      int highlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
      if (highlight >= 0)
      {
        selected = highlight;
      }
    }

    // Only yank Code/FC scroll when ▶ actually moves. W/PRGM fires BatchCompleted
    // every tick; FollowPointer on a stable PTR fights manual scroll (RUN does not).
    bool ptrMoved = selected != SelectedProgramStep;
    SelectedProgramStep = selected;
    if (ptrMoved)
    {
      StudioPaneSync.FollowPointer(selected);
    }

    if (_firmware is not null)
    {
      SyncRomWatchFromBatch(_firmware.LastBatch);
    }

    // Studio seek / F10 / F11 paint A/B; skip after firmware SST so micro-step LED stays authentic.
    if (syncFaceplateLed)
    {
      SyncFaceplateProgramLed(cpu);
    }
  }

  /// <summary>
  /// W/PRGM: load museum codes for the Studio ▶ row into A/B and refresh the faceplate LED.
  /// </summary>
  private void SyncFaceplateProgramLed(ClassicCpu cpu, int? stepOverride = null)
  {
    if (!ProgramMode || _firmware is not ClassicFirmwareGateway gateway)
    {
      return;
    }

    if (!TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) || lines.Count == 0)
    {
      return;
    }

    IReadOnlyList<StudioListingView.Row> rows = BuildStudioListingRows(lines);
    if (rows.Count == 0)
    {
      return;
    }

    int step = stepOverride ?? SelectedProgramStep;
    int rowIdx = FindStudioRowIndex(rows, step);
    if (rowIdx < 0)
    {
      return;
    }

    string museum = StudioMuseumKeycodes.FormatMachineDisplay(rows[rowIdx], EngineModelId);
    // Prefix waiting for second key: show only the left museum box (e.g. "23"), slot=1.
    if (_wprgmPendingPrefixStep == step && _wprgmMachineSlot == 1)
    {
      string[] parts = museum.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length > 0)
      {
        museum = parts[0];
      }
    }

    ClassicWprgmLedSync.ApplyMuseumText(cpu.State.Registers, museum);
    cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    gateway.SyncDisplayFromCpu();
  }

  private static bool IsClassicRoutineEnd(byte code) =>
    code is 42 /* RTN */ or 34 /* R/S */;

  private static bool TryFindRoutineLabelIndex(ClassicCpu cpu, int fromIndex, out int labelIndex)
  {
    labelIndex = -1;
    for (int i = Math.Min(fromIndex, cpu.Program.LastContentIndex()); i >= 1; i--)
    {
      if (cpu.Program.ReadCode(i) != ClassicProgramCodes.Label)
      {
        continue;
      }

      labelIndex = i;
      return true;
    }

    return false;
  }

  public void BreakExecution() =>
    ExecutionPaused = true;

  public void ContinueExecution()
  {
    if (_firmware is ClassicFirmwareGateway { Cpu: { } cpu }
        && TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines)
        && lines.Count > 0)
    {
      IReadOnlyList<StudioListingView.Row> rows = BuildStudioListingRows(lines);
      int highlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
      _breakpointContinueIgnoreStep = highlight >= 0 ? highlight : cpu.Program.PointerPosition();
    }
    else
    {
      _breakpointContinueIgnoreStep = -1;
    }

    _firmware?.ContinueExecution();
  }

  /// <summary>Toggle a Studio breakpoint at <paramref name="stepIndex"/> (Classic program step).</summary>
  public bool ToggleStudioBreakpoint(int stepIndex)
  {
    if (!SupportsCardProgram || stepIndex < 1)
    {
      return false;
    }

    if (!_studioBreakpoints.Add(stepIndex))
    {
      _studioBreakpoints.Remove(stepIndex);
      return false;
    }

    return true;
  }

  /// <summary>Toggle breakpoint at the current Studio selection (or live PTR).</summary>
  public bool ToggleStudioBreakpointAtSelection()
  {
    int step = SelectedProgramStep > 0
      ? SelectedProgramStep
      : (_firmware is ClassicFirmwareGateway { Cpu: { } cpu }
        ? cpu.Program.PointerPosition()
        : -1);
    return ToggleStudioBreakpoint(step);
  }

  public bool HasStudioBreakpoint(int stepIndex) =>
    stepIndex > 0 && _studioBreakpoints.Contains(stepIndex);

  /// <summary>True if any RAM index spanned by this listing row has a breakpoint.</summary>
  public bool HasStudioBreakpointOnRow(StudioListingView.Row row)
  {
    foreach (int step in _studioBreakpoints)
    {
      if (row.ContainsIndex(step))
      {
        return true;
      }
    }

    return false;
  }

  public void ClearStudioBreakpoints()
  {
    _studioBreakpoints.Clear();
    _breakpointContinueIgnoreStep = -1;
  }

  public string CaptureDebugDump()
  {
    string baseDump = _firmware?.CaptureDebugDump() ?? "No firmware gateway.";
    StringBuilder sb = new(baseDump);
    sb.AppendLine();
    AppendRomSliceToDump(sb, radius: 16);
    sb.AppendLine();
    sb.AppendLine("## User program");
    string listing = FormatProgramListingText();
    sb.AppendLine(string.IsNullOrWhiteSpace(listing) ? "(empty)" : listing.TrimEnd());
    return sb.ToString();
  }

  private void AppendRomSliceToDump(StringBuilder sb, int radius)
  {
    sb.AppendLine("## ROM around PC");
    if (Map is null)
    {
      sb.AppendLine("(no microcode map)");
      return;
    }

    int pc = Math.Max(0, LastBatch.ProgramCounter);
    int first = Math.Max(0, pc - radius);
    int last = Math.Min(Map.WordCount - 1, pc + radius);
    for (int address = first; address <= last; address++)
    {
      MicrocodeMapEntry? entry = Map.TryGetAddress(address);
      if (entry is null)
      {
        continue;
      }

      string mark = address == pc ? ">" : " ";
      sb.AppendLine(
        $"{mark}{entry.AddressHex}  {entry.RomWordHex}  {entry.Mnemonic,-8}  {entry.HandlerId}");
    }
  }

  public FirmwareDebugRegisters? TryGetDebugRegisters() =>
    _firmware?.TryGetDebugRegisters();

  public bool TrySetDebugRegister(string name, string digitsHex, out string? error)
  {
    if (_firmware is null)
    {
      error = "No firmware gateway.";
      return false;
    }

    return _firmware.TrySetDebugRegister(name, digitsHex, out error);
  }

  public FirmwareCallStackSnapshot? TryGetCallStack() =>
    _firmware?.TryGetCallStack();

  public void Dispose() =>
    DisposeFirmware();

  private void SyncRomWatchFromBatch(FirmwareBatchSnapshot batch)
  {
    _lastBatch = batch;
    if (!FollowRomWatch)
    {
      return;
    }

    int address = Math.Max(0, batch.ProgramCounter);
    SelectedAddress = address;
    int wordCount = Map?.WordCount ?? 0;
    MicrocodeScroll = RomWatchFollowScroll.Adjust(MicrocodeScroll, address, wordCount);
  }

  private void DisposeFirmware()
  {
    if (_firmware is not null)
    {
      _firmware.DisplayChanged -= OnFirmwareDisplayChanged;
      _firmware.BatchCompleted -= OnFirmwareBatchCompleted;
      if (_firmware is IDisposable disposable)
      {
        disposable.Dispose();
      }

      _firmware = null;
    }
  }

  public void ResetCpu() =>
    PowerOff();

  public void PressKey(int keyChartIndex, byte keyCode)
  {
    // In W/PRGM, f/g/LBL are program prefixes (second museum box), not RUN shift preview.
    if (ProgramMode && SupportsCardProgram)
    {
      ShiftPreview.Clear();
    }
    else
    {
      ShiftPreview.HandleKeyPress(keyChartIndex, Model.Family, Model.Model);
    }

    PressKey(new FirmwareKeyCommand(keyChartIndex, keyCode));
  }

  public void PressKey(byte keyCode) =>
    PressKey(new FirmwareKeyCommand(-1, keyCode));

  public void PressKey(FirmwareKeyCommand key)
  {
    if (!PowerOn)
    {
      return;
    }

    const byte sstKeyCode = 40;
    const byte bspKeyCode = 56; // faceplate BSP (chart 19)

    // W/PRGM program-entry keys must NEVER hit firmware MemoryInsert — that shifts/drops the
    // tail and advances PTR (keyboard 2 then 3 becomes two lines; calc key wipes below).
    if (ProgramMode
        && SupportsCardProgram
        && key.KeyCode != sstKeyCode
        && key.KeyCode != bspKeyCode)
    {
      if (TryStudioWprgmEditKey(key))
      {
        // Do NOT hold the firmware key line. PressCharacter / faceplate release paths keep
        // IsKeyHeld true across ticks; Classic ROM would then MemoryInsert and overwrite
        // (or drop) the Studio write — looks like the program tail vanished.
        _mouseKeyHeld = false;
        if (_firmware is ClassicFirmwareGateway { Cpu: { } cpuClear })
        {
          cpuClear.State.KeyAvailable = false;
          cpuClear.State.KeyBuffer = 0;
        }

        _firmware?.SetKeyLineHeld(false);
        return;
      }

      _mouseKeyHeld = false;
      _firmware?.SetKeyLineHeld(false);
      StudioStatusMessage = "Select a Code line first (↑/↓), then press keys.";
      return;
    }

    _mouseKeyHeld = true;
    _firmware?.KeyDown(key);
    RunFirmwareTicks(KeySettleBatches);

    if (ProgramMode
        && (key.KeyCode == sstKeyCode || key.KeyCode == bspKeyCode)
        && _firmware is ClassicFirmwareGateway { Cpu: { } cpuNav })
    {
      ClearWprgmPendingPrefix();
      SyncStudioToPointer(cpuNav, syncFaceplateLed: false);
    }
  }

  /// <summary>
  /// W/PRGM Studio edit: write into the selected RAM step without firmware MemoryInsert.
  /// Stay on the same Code line until ↑/↓. Prefix keys (LBL, g, f, …) move only to the
  /// right museum box on this line — never auto-advance to the next row.
  /// </summary>
  private bool TryStudioWprgmEditKey(FirmwareKeyCommand key)
  {
    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      return false;
    }

    int step = SelectedProgramStep;
    if (step <= 0)
    {
      step = cpu.Program.PointerPosition() + 1;
    }

    if (step <= 0 || step >= cpu.Program.MemLength - 1)
    {
      return false;
    }

    int ptr = cpu.Program.PointerPosition();
    byte atPtr = ptr > 0 ? cpu.Program.ReadCode(ptr) : (byte)0;
    bool onMarker = atPtr is ClassicProgramCodes.Pointer
        or ClassicProgramCodes.Start
        or ClassicProgramCodes.Mark;
    int afterPtr = ptr + 1;
    byte afterPtrCode = afterPtr < cpu.Program.MemLength ? cpu.Program.ReadCode(afterPtr) : (byte)0;
    bool writeIntoFreeSlot = onMarker
        && afterPtr > 0
        && afterPtr < cpu.Program.MemLength - 1
        && afterPtrCode == 0
        && (SelectedProgramStep <= 0 || step == ptr || step == afterPtr);

    if (writeIntoFreeSlot)
    {
      PushProgramUndoSnapshot();
      cpu.Program.WriteCode(afterPtr, key.KeyCode);
      cpu.Program.Cleanup(10);
      StayOnStudioEditLine(cpu, afterPtr, FormatProgramCode(key.KeyCode), oldRamSpan: 1);
      return true;
    }

    // Resolve selection without SeekPointer — seeking mid-edit shifts PTR through RAM and
    // makes the listing look like it advanced/wiped. ↑/↓ / click own seeking.
    step = Math.Clamp(step, 1, Math.Max(1, cpu.Program.LastContentIndex()));
    byte existing = cpu.Program.ReadCode(step);
    if (existing is ClassicProgramCodes.Start
        or ClassicProgramCodes.Pointer
        or ClassicProgramCodes.Mark)
    {
      if (afterPtr > 0 && afterPtr < cpu.Program.MemLength - 1)
      {
        PushProgramUndoSnapshot();
        cpu.Program.WriteCode(afterPtr, key.KeyCode);
        cpu.Program.Cleanup(10);
        StayOnStudioEditLine(cpu, afterPtr, FormatProgramCode(key.KeyCode), oldRamSpan: 1);
        return true;
      }

      return false;
    }

    PushProgramUndoSnapshot();

    // Second keystroke → right museum box on this same line (LBL→A, g→4, …).
    if (_wprgmPendingPrefixStep == step && _wprgmMachineSlot == 1)
    {
      if (_wprgmPendingInsertSecond)
      {
        InsertProgramByteAfter(cpu, step, key.KeyCode);
      }
      else
      {
        cpu.Program.WriteCode(step + 1, key.KeyCode);
      }

      cpu.Program.Cleanup(10);
      StayOnStudioEditLine(cpu, step, secondComplete: true);
      return true;
    }

    int oldRamSpan = 1;
    if (TryGetStudioListingRows(out IReadOnlyList<StudioListingView.Row> rows)
        && TryFindStudioRow(rows, step, out StudioListingView.Row row))
    {
      oldRamSpan = Math.Max(1, StudioListingRowRamSpan(row));
    }

    cpu.Program.WriteCode(step, key.KeyCode);
    string mnemonic = FormatProgramCode(key.KeyCode);
    if (!StudioMuseumPrefix.NeedsSecondToken(mnemonic) && oldRamSpan >= 2)
    {
      for (int extra = oldRamSpan - 1; extra > 0; extra--)
      {
        cpu.Program.DeleteAt(step + 1);
      }
    }

    cpu.Program.Cleanup(10);
    StayOnStudioEditLine(cpu, step, mnemonic, oldRamSpan);
    return true;
  }

  /// <summary>
  /// Keep ▶ / selection on <paramref name="step"/>; optionally arm the right museum box.
  /// Does not SeekPointer (no auto “next line”).
  /// </summary>
  private void StayOnStudioEditLine(
    ClassicCpu cpu,
    int step,
    string? firstMnemonic = null,
    int oldRamSpan = 1,
    bool secondComplete = false)
  {
    SelectedProgramStep = step;
    if (secondComplete)
    {
      ClearWprgmPendingPrefix();
    }
    else if (firstMnemonic is not null && StudioMuseumPrefix.NeedsSecondToken(firstMnemonic))
    {
      _wprgmPendingPrefixStep = step;
      _wprgmPendingInsertSecond = oldRamSpan < 2;
      _wprgmMachineSlot = 1;
    }
    else
    {
      ClearWprgmPendingPrefix();
    }

    SyncFaceplateProgramLed(cpu, step);
  }

  /// <summary>Insert one program byte after <paramref name="step"/> without MemLength end drop.</summary>
  private static void InsertProgramByteAfter(ClassicCpu cpu, int step, byte code)
  {
    int last = Math.Min(cpu.Program.LastContentIndex() + 1, cpu.Program.MemLength - 2);
    for (int i = last; i > step; i--)
    {
      cpu.Program.WriteCode(i + 1, cpu.Program.ReadCode(i));
    }

    cpu.Program.WriteCode(step + 1, code);
  }

  private void ClearWprgmPendingPrefix()
  {
    _wprgmPendingPrefixStep = -1;
    _wprgmPendingInsertSecond = false;
    _wprgmMachineSlot = 0;
  }

  private void SettleAfterCardImport()
  {
    ApplyNonPowerFaceplateSwitchesToFirmware();
    RunFirmwareTicks(KeySettleBatches);
    CaptureSavedProgramSnapshot();
    ClearProgramEditHistory();
  }

  private void CaptureSavedProgramSnapshot()
  {
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      _savedProgramSnapshot = null;
      return;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      return;
    }

    _savedProgramSnapshot = codes;
  }

  private bool ProgramMatchesSnapshot()
  {
    if (_savedProgramSnapshot is null
        || _firmware is null
        || !_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      return true;
    }

    if (codes.Length != _savedProgramSnapshot.Length)
    {
      return false;
    }

    for (int i = 0; i < codes.Length; i++)
    {
      if (codes[i] != _savedProgramSnapshot[i])
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Classic power-off clears program RAM; re-import the inserted card so Studio/FC stay in sync.
  /// </summary>
  private void TryRestoreInsertedCardProgram()
  {
    if (!_cardInserted || _firmware is null || !_firmware.SupportsCardProgram || Vocabulary is null)
    {
      return;
    }

    if (_loadedTeoCard is not null)
    {
      T6xDocument t6x = T6xCardFormat.FromTeoCardDocument(_loadedTeoCard);
      if (_firmware is Teo67FirmwareGateway hp67)
      {
        Teo67CardSnapshot snapshot = T6xCardFormat.ToTeo67Snapshot(t6x, Teo67CardProgramIo.ResolveMnemonic);
        if (hp67.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers))
        {
          SettleAfterCardImport();
        }
      }
      else
      {
        ClassicCardSnapshot classic = T6xCardFormat.ToClassicSnapshot(
          t6x,
          mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
        if (_firmware.TryImportCardProgram(classic.ProgramCodes, classic.Registers))
        {
          SettleAfterCardImport();
        }
      }

      return;
    }

    if (!string.IsNullOrEmpty(_loadedCardPath) && File.Exists(_loadedCardPath))
    {
      _ = TryLoadCardProgram(_loadedCardPath, out _);
    }
  }

  private void RunFirmwareTicks(int batches)
  {
    // T-01 uses a 10ms timer; 40×50ms settle would burn its ~2s display hold (200 batches).
    float delta = _firmware is Teo01FirmwareGateway ? 0.01f : 0.05f;
    for (int i = 0; i < batches; i++)
    {
      _firmware?.Tick(delta);
    }
  }

  public bool SupportsCardProgram =>
    _firmware?.SupportsCardProgram == true;

  /// <summary>True when this session uses ACT card packing (T-67), not Classic (T-65).</summary>
  public bool UsesActCardProgram =>
    _firmware is Teo67FirmwareGateway;

  /// <summary>
  /// Live user-program listing from card/RAM export — shared model for Studio editor and explorer.
  /// </summary>
  public bool TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines)
  {
    lines = [];
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      return false;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      return false;
    }

    lines = ClassicProgramListing.ToList(codes, FormatProgramCode);
    return true;
  }

  /// <summary>
  /// Select a Studio listing step. In W/PRGM this is the current edit line (LED);
  /// in RUN it only moves the selection highlight (dbl-click / Set start still seeks).
  /// </summary>
  public bool TrySelectStudioProgramLine(int stepIndex)
  {
    if (!SupportsCardProgram)
    {
      return false;
    }

    ClearWprgmPendingPrefix();
    if (ProgramMode)
    {
      // Do not SeekPointer on every ↑/↓ — bubbling PTR through RAM reshuffles indices so
      // the next arrow can land two visual rows away. Seek stays on Set start / F10 / F11.
      if (stepIndex < 1)
      {
        return false;
      }

      SelectedProgramStep = stepIndex;
      StudioPaneSync.FollowPointer(stepIndex);
      if (_firmware is ClassicFirmwareGateway { Cpu: { } cpu })
      {
        SyncFaceplateProgramLed(cpu, stepIndex);
      }

      return true;
    }

    SelectedProgramStep = stepIndex;
    StudioPaneSync.OnCodeSelected(stepIndex);
    StudioPaneSync.FollowPointer(stepIndex);
    return true;
  }

  /// <summary>Move Classic PTR so Studio ▶ highlights <paramref name="stepIndex"/>.</summary>
  public bool TrySetProgramStartStep(int stepIndex)
  {
    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      return false;
    }

    ClearWprgmPendingPrefix();
    int last = Math.Max(1, cpu.Program.LastContentIndex());
    int target = Math.Clamp(stepIndex, 1, last);
    // Keep mid-program NOP / empty lines selectable (W/PRGM Ins). Do not walk back to
    // the previous non-zero opcode — trailing filler is already excluded by LastContentIndex.
    int seekTo = Math.Max(1, target - 1);
    cpu.Program.SeekPointer(seekTo);
    SelectedProgramStep = target;
    StudioPaneSync.FollowPointer(target);
    if (_firmware is not null)
    {
      SyncRomWatchFromBatch(_firmware.LastBatch);
    }

    SyncFaceplateProgramLed(cpu, target);
    return true;
  }

  /// <summary>Live DATA registers from firmware RAM (updates after RUN STO/RCL).</summary>
  public bool TryGetLiveRegisters(out IReadOnlyList<double> registers)
  {
    registers = [];
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      return false;
    }

    if (!_firmware.TryExportCardProgram(out _, out double[] regs))
    {
      return false;
    }

    registers = regs;
    return true;
  }

  /// <summary>Write DATA registers into firmware RAM (keeps current program bytes).</summary>
  public bool TrySetLiveRegisters(IReadOnlyList<double> registers)
  {
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      return false;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      return false;
    }

    return _firmware.TryImportCardProgram(codes, registers);
  }

  public string FormatProgramListingText()
  {
    if (!TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      return string.Empty;
    }

    // Studio shows both encodings; copy dual TSV without runtime START/PTR markers.
    return UserProgramClipboard.FormatDual(StudioListingView.FilterForClipboard(lines));
  }

  /// <summary>Dual TSV for the Studio listing row under the current selection (single-line copy).</summary>
  public string FormatSelectedProgramListingForClipboard()
  {
    if (!TryGetStudioListingRows(out IReadOnlyList<StudioListingView.Row> rows)
        || !TryFindStudioRow(rows, SelectedProgramStep, out StudioListingView.Row row)
        || !TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      return string.Empty;
    }

    int span = StudioListingRowRamSpan(row);
    List<ClassicProgramLine> selected = [];
    foreach (ClassicProgramLine line in lines)
    {
      if (line.Index >= row.Index && line.Index < row.Index + span)
      {
        selected.Add(line);
      }
    }

    return UserProgramClipboard.FormatDual(StudioListingView.FilterForClipboard(selected));
  }

  /// <summary>
  /// Replace user program steps from clipboard text. Dual TSV uses the machine column;
  /// otherwise auto-detects mnemonic then machine. Registers are preserved.
  /// </summary>
  public bool TryPasteProgramListing(string text, out string? error)
  {
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Program memory not available for this engine.";
      return false;
    }

    if (!UserProgramClipboard.TryParseAuto(
          text,
          ResolveProgramMnemonic,
          out List<byte> pasted,
          out error))
    {
      return false;
    }

    if (!TryApplyProgramCodes(pasted, out error))
    {
      return false;
    }

    if (pasted.Count > CardProgramCapacity)
    {
      StudioStatusMessage = $"Pasted {CardProgramCapacity} of {pasted.Count} steps (capacity).";
    }
    else
    {
      StudioStatusMessage = $"Pasted {pasted.Count} step(s).";
    }

    return true;
  }

  /// <summary>Copy current listing row to clipboard text, then delete that row.</summary>
  public bool TryCutSelectedProgramLine(out string clipboardText, out string? error)
  {
    clipboardText = FormatSelectedProgramListingForClipboard();
    if (string.IsNullOrWhiteSpace(clipboardText))
    {
      error = "Nothing to cut.";
      return false;
    }

    return TryDeleteProgramLineAtSelection(out error);
  }

  /// <summary>
  /// Replace user program RAM with <paramref name="codes"/> (registers preserved).
  /// Used by Studio paste and the W/PRGM dual-pane Apply.
  /// </summary>
  public bool TryApplyProgramCodes(IReadOnlyList<byte> codes, out string? error) =>
    TryApplyProgramCodesCore(codes, pushUndo: true, out error);

  /// <summary>Insert Classic NOP (0) at the selected RAM step; shifts the tail.</summary>
  public bool TryInsertEmptyProgramLineAtSelection(out string? error)
  {
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Program memory not available for this engine.";
      return false;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      error = "Could not read current program.";
      return false;
    }

    int capacity = codes.Length;
    int at = Math.Clamp(SelectedProgramStep, 0, capacity - 1);
    if (StudioListingView.IsRuntimeMarker(codes[at]))
    {
      // Prefer inserting after START/PTR into the first user step slot.
      at = Math.Min(capacity - 1, Math.Max(2, at + 1));
    }

    PushProgramUndoSnapshot();
    for (int i = capacity - 1; i > at; i--)
    {
      codes[i] = codes[i - 1];
    }

    codes[at] = 0;
    if (!TryApplyProgramCodesCore(codes, pushUndo: false, out error))
    {
      PopProgramUndoSnapshotDiscarded();
      return false;
    }

    SelectedProgramStep = at;
    StudioPaneSync.FollowPointer(at);
    StudioStatusMessage = $"Inserted NOP at step {at}.";
    return true;
  }

  /// <summary>
  /// Delete the Studio listing row under selection (RAM span for that painted row).
  /// </summary>
  public bool TryDeleteProgramLineAtSelection(out string? error)
  {
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Program memory not available for this engine.";
      return false;
    }

    if (!TryGetStudioListingRows(out IReadOnlyList<StudioListingView.Row> rows)
        || !TryFindStudioRow(rows, SelectedProgramStep, out StudioListingView.Row row))
    {
      error = "No program line selected.";
      return false;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      error = "Could not read current program.";
      return false;
    }

    int at = row.Index;
    int span = StudioListingRowRamSpan(row);
    if (span <= 0 || at < 0 || at + span > codes.Length)
    {
      error = "Cannot delete selection.";
      return false;
    }

    if (StudioListingView.IsRuntimeMarker(codes[at]))
    {
      error = "Cannot delete runtime marker.";
      return false;
    }

    PushProgramUndoSnapshot();
    for (int i = at; i + span < codes.Length; i++)
    {
      codes[i] = codes[i + span];
    }

    for (int i = codes.Length - span; i < codes.Length; i++)
    {
      codes[i] = 0;
    }

    if (!TryApplyProgramCodesCore(codes, pushUndo: false, out error))
    {
      PopProgramUndoSnapshotDiscarded();
      return false;
    }

    // Keep selection on the row that slid into `at` (same visual slot); else previous row.
    if (!TryGetStudioListingRows(out rows) || rows.Count == 0)
    {
      SelectedProgramStep = Math.Max(1, at);
    }
    else
    {
      int rowIndex = -1;
      for (int i = 0; i < rows.Count; i++)
      {
        if (rows[i].Index == at || rows[i].ContainsIndex(at))
        {
          rowIndex = i;
          break;
        }
      }

      if (rowIndex < 0)
      {
        for (int i = rows.Count - 1; i >= 0; i--)
        {
          if (rows[i].Index < at)
          {
            rowIndex = i;
            break;
          }
        }
      }

      rowIndex = Math.Clamp(rowIndex < 0 ? 0 : rowIndex, 0, rows.Count - 1);
      SelectedProgramStep = rows[rowIndex].Index;
    }

    StudioPaneSync.FollowPointer(SelectedProgramStep);
    if (ProgramMode && _firmware is ClassicFirmwareGateway { Cpu: { } cpuLed })
    {
      SyncFaceplateProgramLed(cpuLed, SelectedProgramStep);
    }

    StudioStatusMessage = $"Deleted step {at}.";
    return true;
  }

  public bool TryUndoProgramEdit(out string? error)
  {
    error = null;
    if (_programUndoStack.Count == 0)
    {
      error = "Nothing to undo.";
      return false;
    }

    if (!TryCaptureLiveProgramEditSnapshot(out ProgramEditSnapshot current))
    {
      error = "Could not read current program.";
      return false;
    }

    ProgramEditSnapshot prior = _programUndoStack[^1];
    _programUndoStack.RemoveAt(_programUndoStack.Count - 1);
    _programRedoStack.Add(current);
    TrimProgramEditStack(_programRedoStack);

    if (!TryApplyProgramCodesCore(prior.Codes, pushUndo: false, out error))
    {
      return false;
    }

    SelectedProgramStep = prior.SelectedStep;
    StudioPaneSync.FollowPointer(SelectedProgramStep);
    StudioStatusMessage = "Undo.";
    return true;
  }

  public bool TryRedoProgramEdit(out string? error)
  {
    error = null;
    if (_programRedoStack.Count == 0)
    {
      error = "Nothing to redo.";
      return false;
    }

    if (!TryCaptureLiveProgramEditSnapshot(out ProgramEditSnapshot current))
    {
      error = "Could not read current program.";
      return false;
    }

    ProgramEditSnapshot next = _programRedoStack[^1];
    _programRedoStack.RemoveAt(_programRedoStack.Count - 1);
    _programUndoStack.Add(current);
    TrimProgramEditStack(_programUndoStack);

    if (!TryApplyProgramCodesCore(next.Codes, pushUndo: false, out error))
    {
      return false;
    }

    SelectedProgramStep = next.SelectedStep;
    StudioPaneSync.FollowPointer(SelectedProgramStep);
    StudioStatusMessage = "Redo.";
    return true;
  }

  /// <summary>Reload RAM from the last loaded/saved snapshot (not a file re-read).</summary>
  public bool TryRevertProgramToSnapshot(out string? error)
  {
    error = null;
    PendingStudioRevertConfirm = false;
    if (_savedProgramSnapshot is null)
    {
      error = "Nothing to revert (no loaded/saved snapshot).";
      return false;
    }

    PushProgramUndoSnapshot();
    if (!TryApplyProgramCodesCore(_savedProgramSnapshot, pushUndo: false, out error))
    {
      PopProgramUndoSnapshotDiscarded();
      return false;
    }

    CaptureSavedProgramSnapshot();
    StudioStatusMessage = "Reverted to last saved snapshot.";
    return true;
  }

  /// <summary>
  /// Up / Down / Home / End / PgUp / PgDn among Studio listing rows.
  /// In W/PRGM the new selection becomes the current line immediately (no SeekPointer).
  /// </summary>
  public bool TryNavigateProgramSelection(StudioProgramNav nav)
  {
    if (!SupportsCardProgram
        || !TryGetStudioListingRows(out IReadOnlyList<StudioListingView.Row> rows)
        || rows.Count == 0)
    {
      return false;
    }

    ClearWprgmPendingPrefix();

    int rowIndex = FindStudioRowIndex(rows, SelectedProgramStep);
    if (rowIndex < 0)
    {
      rowIndex = 0;
    }

    const int page = 10;
    int next = nav switch
    {
      StudioProgramNav.Home => 0,
      StudioProgramNav.End => rows.Count - 1,
      StudioProgramNav.PageUp => Math.Max(0, rowIndex - page),
      StudioProgramNav.PageDown => Math.Min(rows.Count - 1, rowIndex + page),
      StudioProgramNav.Up => Math.Max(0, rowIndex - 1),
      StudioProgramNav.Down => Math.Min(rows.Count - 1, rowIndex + 1),
      _ => rowIndex,
    };

    // Always land on the row's first RAM index (not a mid-pair address).
    return TrySelectStudioProgramLine(rows[next].Index);
  }

  private bool TryApplyProgramCodesCore(
    IReadOnlyList<byte> codes,
    bool pushUndo,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(codes);
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Program memory not available for this engine.";
      return false;
    }

    if (!_firmware.TryExportCardProgram(out _, out double[] registers))
    {
      error = "Could not read current program/registers.";
      return false;
    }

    if (pushUndo)
    {
      PushProgramUndoSnapshot();
    }

    int capacity = CardProgramCapacity;
    byte[] merged = new byte[capacity];
    int count = Math.Min(capacity, codes.Count);
    for (int i = 0; i < count; i++)
    {
      merged[i] = codes[i];
    }

    if (!_firmware.TryImportCardProgram(merged, registers))
    {
      if (pushUndo)
      {
        PopProgramUndoSnapshotDiscarded();
      }

      error = "Could not apply program.";
      return false;
    }

    SelectedProgramStep = Math.Clamp(SelectedProgramStep, 0, Math.Max(0, capacity - 1));
    return true;
  }

  private void PushProgramUndoSnapshot()
  {
    if (!TryCaptureLiveProgramEditSnapshot(out ProgramEditSnapshot snap))
    {
      return;
    }

    _programUndoStack.Add(snap);
    TrimProgramEditStack(_programUndoStack);
    _programRedoStack.Clear();
  }

  private void PopProgramUndoSnapshotDiscarded()
  {
    if (_programUndoStack.Count > 0)
    {
      _programUndoStack.RemoveAt(_programUndoStack.Count - 1);
    }
  }

  private bool TryCaptureLiveProgramEditSnapshot(out ProgramEditSnapshot snapshot)
  {
    snapshot = default;
    if (_firmware is null
        || !_firmware.SupportsCardProgram
        || !_firmware.TryExportCardProgram(out byte[] codes, out _))
    {
      return false;
    }

    snapshot = new ProgramEditSnapshot((byte[])codes.Clone(), SelectedProgramStep);
    return true;
  }

  private void ClearProgramEditHistory()
  {
    _programUndoStack.Clear();
    _programRedoStack.Clear();
  }

  private static void TrimProgramEditStack(List<ProgramEditSnapshot> stack)
  {
    while (stack.Count > MaxProgramUndoDepth)
    {
      stack.RemoveAt(0);
    }
  }

  private bool TryGetStudioListingRows(out IReadOnlyList<StudioListingView.Row> rows)
  {
    rows = [];
    if (!TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      return false;
    }

    rows = BuildStudioListingRows(lines);
    return rows.Count > 0;
  }

  /// <summary>
  /// Studio Code/FC rows. In W/PRGM, strip-omit filters are off so a mid-edit LBL/g/f
  /// cannot hide the rest of the program from the listing.
  /// </summary>
  public IReadOnlyList<StudioListingView.Row> BuildStudioListingRows(
    IReadOnlyList<ClassicProgramLine> lines) =>
    StudioListingView.Build(
      lines,
      StudioCardAuthoringSteps,
      omitStripFilters: !ProgramMode);

  private static bool TryFindStudioRow(
    IReadOnlyList<StudioListingView.Row> rows,
    int step,
    out StudioListingView.Row row)
  {
    // Prefer exact Index match — fused Single rows report StepSpan>1 for # display
    // but the next row still starts at Index+1; ContainsIndex would steal that selection.
    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].Index == step)
      {
        row = rows[i];
        return true;
      }
    }

    int iContain = FindStudioRowIndex(rows, step);
    if (iContain < 0)
    {
      row = default;
      return false;
    }

    row = rows[iContain];
    return true;
  }

  /// <summary>RAM bytes covered by a painted Studio row (not display keystroke span).</summary>
  private static int StudioListingRowRamSpan(StudioListingView.Row row)
  {
    int span = 1;
    if (row.SecondCode.HasValue)
    {
      span++;
    }

    if (row.ThirdCode.HasValue)
    {
      span++;
    }

    return span;
  }

  private readonly record struct ProgramEditSnapshot(byte[] Codes, int SelectedStep);

  /// <summary>Public hook for Studio dual-pane editor (same as private listing format).</summary>
  public string FormatProgramCodeForEditor(byte code) => FormatProgramCode(code);

  /// <summary>Public hook for Studio dual-pane editor mnemonic → byte.</summary>
  public byte? ResolveProgramMnemonicForEditor(string mnemonic) =>
    ResolveProgramMnemonic(mnemonic);

  /// <summary>Keys-pane completion candidates (vocabulary / ACT table).</summary>
  public IReadOnlyList<string> EnumerateProgramMnemonics()
  {
    if (UsesActCardProgram)
    {
      return Teo67CardProgramIo.EnumerateMnemonics();
    }

    if (Vocabulary is null)
    {
      return [];
    }

    List<string> list = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    foreach (ProgramStepEntry step in Vocabulary.Steps)
    {
      string mnemonic = step.Mnemonic?.Trim() ?? string.Empty;
      if (mnemonic.Length == 0
          || mnemonic.StartsWith('#')
          || string.Equals(mnemonic, "PTR", StringComparison.OrdinalIgnoreCase)
          || string.Equals(mnemonic, "NOP", StringComparison.OrdinalIgnoreCase) && step.Code == 0)
      {
        continue;
      }

      if (seen.Add(mnemonic))
      {
        list.Add(mnemonic);
      }
    }

    list.Sort(StringComparer.OrdinalIgnoreCase);
    return list;
  }

  /// <summary>Machine-pane completion candidates (museum display strings).</summary>
  public IReadOnlyList<string> EnumerateMachineCompletionTokens()
  {
    List<string> list = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    foreach (string mnemonic in EnumerateProgramMnemonics())
    {
      if (ResolveProgramMnemonic(mnemonic) is not byte code)
      {
        continue;
      }

      string museum = StudioMuseumKeycodes.FormatMachineDisplay(code, mnemonic, EngineModelId);
      if (string.IsNullOrWhiteSpace(museum) || museum.StartsWith('#'))
      {
        continue;
      }

      if (seen.Add(museum))
      {
        list.Add(museum);
      }

      string[] parts = museum.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length > 0 && seen.Add(parts[0]))
      {
        list.Add(parts[0]);
      }
    }

    list.Sort(StringComparer.OrdinalIgnoreCase);
    return list;
  }

  private string FormatProgramCode(byte code) =>
    UsesActCardProgram
      ? Teo67CardProgramIo.FormatMnemonic(code)
      : ClassicCardProgramIo.FormatMnemonic(Vocabulary, code);

  private byte? ResolveProgramMnemonic(string mnemonic) =>
    UsesActCardProgram
      ? Teo67CardProgramIo.ResolveMnemonic(mnemonic)
      : ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic);

  public string CardProgramExtension =>
    UsesActCardProgram ? T6xDocument.Extension67 : T6xDocument.Extension65;

  public int CardProgramCapacity =>
    UsesActCardProgram ? Teo67CardProgramIo.ProgramCapacity : ClassicCardProgramIo.ProgramCapacity;

  public IReadOnlyList<string> PrintLines =>
    _firmware?.PrintLines ?? [];

  /// <summary>True after a successful card load/save — faceplate strip shows inserted state.</summary>
  public bool CardInserted => _cardInserted;

  public string? LoadedCardPath => _loadedCardPath;

  /// <summary>Strip captions when a card is inserted (falls back to blank columns in the component).</summary>
  public IReadOnlyList<string>? CardStripLabels => _cardStripLabels;

  /// <summary>False when a caption has no matching <c>LBL A</c>…<c>LBL E</c> subroutine.</summary>
  public IReadOnlyList<bool>? CardStripLabelsEnabled => _cardStripLabelsEnabled;

  /// <summary>Metadata when the loaded file carries TeoCard fields (null if unavailable).</summary>
  public TeoCardDocument? LoadedTeoCard => _loadedTeoCard;

  /// <summary>
  /// Card <c>[Code]</c> authoring steps for Studio listing filters (strip A–E omit).
  /// Kept while dirty so a single RAM edit does not rebuild Code/FC with a different omit set.
  /// </summary>
  public IReadOnlyList<string>? StudioCardAuthoringSteps => _loadedTeoCard?.Program.Steps;

  public string? CardTitle => _loadedTeoCard?.Title;

  public string? CardDescription => _loadedTeoCard?.Description;

  public string? CardUsage => _loadedTeoCard?.Usage;

  public string? CardRunHint => _loadedTeoCard?.RunHint;

  public string? CardAuthor => _loadedTeoCard?.Author;

  public string? CardCategory => _loadedTeoCard?.Category;

  public string? CardProfile => _loadedTeoCard?.Profile;

  public bool IsCardMetadataDirty => _cardMetadataDirty;

  /// <summary>Bumps when card metadata is replaced from disk / eject (Studio Card tab resync).</summary>
  public int CardMetadataEpoch => _cardMetadataEpoch;

  /// <summary>Editable card metadata for Studio Card tab (creates a shell if none loaded).</summary>
  public CardMetadataFields GetCardMetadataFields()
  {
    EnsureCardMetadataShell();
    return CardMetadataFields.FromDocument(_loadedTeoCard, StudioCodeEncoding);
  }

  /// <summary>
  /// Apply Studio Card-tab edits into the in-memory card document (persisted on Save).
  /// Updates faceplate strip captions when Labels change.
  /// </summary>
  public bool TryApplyCardMetadata(CardMetadataFields fields, out string? error)
  {
    error = null;
    ArgumentNullException.ThrowIfNull(fields);
    if (!SupportsCardProgram)
    {
      error = "Program memory not available for this engine.";
      return false;
    }

    string encoding;
    try
    {
      encoding = CardCodeEncoding.Normalize(fields.CodeEncoding);
    }
    catch (FormatException ex)
    {
      error = ex.Message;
      return false;
    }

    DateTimeOffset? created = _loadedTeoCard?.Created;
    if (!string.IsNullOrWhiteSpace(fields.Created))
    {
      if (!DateTimeOffset.TryParse(
            fields.Created.Trim(),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal
              | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsed))
      {
        error = "Created timestamp is invalid (use ISO-8601, e.g. 2026-07-25T12:00:00Z).";
        return false;
      }

      created = parsed;
    }

    EnsureCardMetadataShell();
    TeoCardDocument prior = _loadedTeoCard!;
    string[] labels = TeoCardProgramFormat.NormalizeStripLabels(fields.Labels);
    string[] hints = TeoCardProgramFormat.NormalizeStripLabels(fields.LabelHints);

    TeoCardDocument next = new()
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = string.IsNullOrWhiteSpace(prior.Model) ? EngineModelId : prior.Model,
      InteropMagic = prior.InteropMagic,
      Profile = NullIfBlank(fields.Profile),
      Title = NullIfBlank(fields.Title),
      Description = NullIfBlank(fields.Description),
      Usage = NullIfBlank(fields.Usage),
      Category = NullIfBlank(fields.Category),
      RunHint = NullIfBlank(fields.RunHint),
      Author = NullIfBlank(fields.Author),
      Labels = labels.ToList(),
      LabelHints = hints.ToList(),
      Program = new TeoCardProgramSection
      {
        CodeEncoding = encoding,
        Steps = prior.Program.Steps,
      },
      Data = prior.Data,
      Created = created,
      Modified = DateTimeOffset.UtcNow,
    };

    if (CardMetadataEquals(prior, next) && string.Equals(StudioCodeEncoding, encoding, StringComparison.Ordinal))
    {
      return true;
    }

    _loadedTeoCard = next;
    StudioCodeEncoding = encoding;
    RefreshStripFromLoadedCard();
    _cardMetadataDirty = true;
    StudioStatusMessage = "Card info updated.";
    return true;
  }

  private void EnsureCardMetadataShell()
  {
    if (_loadedTeoCard is not null || !SupportsCardProgram)
    {
      return;
    }

    _loadedTeoCard = new TeoCardDocument
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = EngineModelId,
      Profile = EngineModelId,
      Program = new TeoCardProgramSection
      {
        CodeEncoding = CardCodeEncoding.Normalize(StudioCodeEncoding),
        Steps = [],
      },
      Data = new TeoCardDataSection
      {
        Registers = [],
      },
      Labels = ["", "", "", "", ""],
      LabelHints = ["", "", "", "", ""],
      Created = DateTimeOffset.UtcNow,
      Modified = DateTimeOffset.UtcNow,
    };
  }

  private void RefreshStripFromLoadedCard()
  {
    if (_loadedTeoCard is null)
    {
      return;
    }

    CardStripPresentation strip = ClassicCardStripLabels.HasAnyLabel(_loadedTeoCard.Labels)
      ? ClassicCardStripLabels.Resolve(_loadedTeoCard.Labels, _loadedTeoCard.Program.Steps)
      : ClassicCardStripLabels.Resolve(
          ClassicCardStripLabels.InferFromSteps(_loadedTeoCard.Program.Steps),
          _loadedTeoCard.Program.Steps);
    _cardStripLabels = strip.Captions;
    _cardStripLabelsEnabled = strip.Enabled;
  }

  private static string? NullIfBlank(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static bool CardMetadataEquals(TeoCardDocument a, TeoCardDocument b)
  {
    if (!string.Equals(a.Title, b.Title, StringComparison.Ordinal)
        || !string.Equals(a.Description, b.Description, StringComparison.Ordinal)
        || !string.Equals(a.Usage, b.Usage, StringComparison.Ordinal)
        || !string.Equals(a.Category, b.Category, StringComparison.Ordinal)
        || !string.Equals(a.RunHint, b.RunHint, StringComparison.Ordinal)
        || !string.Equals(a.Author, b.Author, StringComparison.Ordinal)
        || !string.Equals(a.Profile, b.Profile, StringComparison.Ordinal)
        || !string.Equals(a.Program.CodeEncoding, b.Program.CodeEncoding, StringComparison.Ordinal)
        || a.Created != b.Created)
    {
      return false;
    }

    if (a.Labels.Count != b.Labels.Count || a.LabelHints.Count != b.LabelHints.Count)
    {
      return false;
    }

    for (int i = 0; i < a.Labels.Count; i++)
    {
      if (!string.Equals(a.Labels[i], b.Labels[i], StringComparison.Ordinal))
      {
        return false;
      }
    }

    for (int i = 0; i < a.LabelHints.Count; i++)
    {
      if (!string.Equals(a.LabelHints[i], b.LabelHints[i], StringComparison.Ordinal))
      {
        return false;
      }
    }

    return true;
  }

  public void EjectCard()
  {
    ResetCardSlotState();
    // Card metadata is cleared, but Classic RAM still holds the ejected program —
    // reboot into firmware no-card defaults so Studio/FC match the empty slot.
    if (PowerOn)
    {
      PowerOff();
      PowerOnResume();
    }
  }

  private void ResetCardSlotState()
  {
    _cardInserted = false;
    _loadedCardPath = null;
    _cardStripLabels = null;
    _cardStripLabelsEnabled = null;
    _loadedTeoCard = null;
    _cardMetadataDirty = false;
    _cardMetadataEpoch++;
    _savedProgramSnapshot = null;
    PendingLeaveProgramConfirm = false;
    PendingStudioSaveConfirm = false;
    PendingStudioRevertConfirm = false;
    ClearProgramEditHistory();
  }

  private void MarkCardInserted(string path, TeoCardDocument? teoCard = null)
  {
    _cardInserted = true;
    _loadedCardPath = path;
    _loadedTeoCard = teoCard;
    _cardMetadataDirty = false;
    _cardMetadataEpoch++;
    if (teoCard?.Program.CodeEncoding is { Length: > 0 } encoding)
    {
      try
      {
        StudioCodeEncoding = CardCodeEncoding.Normalize(encoding);
      }
      catch (FormatException)
      {
        // Keep current Studio encoding when card metadata is unexpected.
      }
    }

    CardStripPresentation strip = ResolveStripPresentation(path, teoCard);
    _cardStripLabels = strip.Captions;
    _cardStripLabelsEnabled = strip.Enabled;
  }

  private CardStripPresentation ResolveStripPresentation(string path, TeoCardDocument? teoCard)
  {
    if (teoCard is not null)
    {
      if (ClassicCardStripLabels.HasAnyLabel(teoCard.Labels))
      {
        return ClassicCardStripLabels.Resolve(teoCard.Labels, teoCard.Program.Steps);
      }

      if (teoCard.Program.Steps.Count > 0)
      {
        return ClassicCardStripLabels.Resolve(
          ClassicCardStripLabels.InferFromSteps(teoCard.Program.Steps),
          teoCard.Program.Steps);
      }
    }

    return InferStripPresentationFromLegacyFile(path);
  }

  private CardStripPresentation InferStripPresentationFromLegacyFile(string path)
  {
    if (Vocabulary is null)
    {
      return new CardStripPresentation();
    }

    try
    {
      if (CuveSoftCardPlistFormat.IsCuveSoftCardPath(path))
      {
        CuveSoftCardPlistSnapshot cuveSoft = CuveSoftCardPlistFormat.ReadFile(path);
        ClassicCardSnapshot cuveSoftClassic = CuveSoftCardPlistFormat.ToClassicSnapshot(cuveSoft);
        return ClassicCardStripLabels.ResolveFromClassicSnapshot(
          cuveSoftClassic,
          code => ClassicCardProgramIo.FormatMnemonic(Vocabulary, code),
          cuveSoft.Labels);
      }

      if (TeoCardProgramFormat.IsTeoCardPath(path))
      {
        TeoCardDocument teoJson = TeoCardProgramFormat.ReadFile(path);
        if (ClassicCardStripLabels.HasAnyLabel(teoJson.Labels))
        {
          return ClassicCardStripLabels.Resolve(teoJson.Labels, teoJson.Program.Steps);
        }

        if (teoJson.Program.Steps.Count > 0)
        {
          return ClassicCardStripLabels.Resolve(
            ClassicCardStripLabels.InferFromSteps(teoJson.Program.Steps),
            teoJson.Program.Steps);
        }
      }

      if (IsCardTextPath(path))
      {
        T6xDocument t6x = T6xCardFormat.ReadFile(path);
        TeoCardDocument teo = T6xCardFormat.ToTeoCardDocument(t6x);
        if (ClassicCardStripLabels.HasAnyLabel(teo.Labels))
        {
          return ClassicCardStripLabels.Resolve(teo.Labels, teo.Program.Steps);
        }

        if (teo.Program.Steps.Count > 0)
        {
          return ClassicCardStripLabels.Resolve(
            ClassicCardStripLabels.InferFromSteps(teo.Program.Steps),
            teo.Program.Steps);
        }
      }

      return new CardStripPresentation();
    }
    catch
    {
      return new CardStripPresentation();
    }
  }

  public bool TrySaveCardProgram(string path, out string? error) =>
    TrySaveCardProgram(path, writeBackup: false, out error);

  /// <param name="writeBackup">When true and <paramref name="path"/> exists, copy it to <c>path.bak</c> first.</param>
  public bool TrySaveCardProgram(string path, bool writeBackup, out string? error)
  {
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Card program I/O is not available for this model.";
      return false;
    }

    if (!_firmware.TryExportCardProgram(out byte[] codes, out double[] registers))
    {
      error = "Failed to export program memory.";
      return false;
    }

    // Saving a copy of firmware / empty-slot RAM must not mark the file as inserted —
    // power cycle must reload ROM defaults, not this export.
    bool markInserted = _cardInserted;

    try
    {
      if (writeBackup && File.Exists(path))
      {
        File.Copy(path, path + ".bak", overwrite: true);
      }

      if (IsCardTextPath(path))
      {
        if (!TryBuildT6xDocument(codes, registers, out T6xDocument t6x, out error))
        {
          return false;
        }

        T6xCardFormat.WriteFile(path, t6x);
        if (markInserted)
        {
          MarkCardInserted(path, T6xCardFormat.ToTeoCardDocument(t6x));
        }
        else
        {
          _loadedTeoCard = T6xCardFormat.ToTeoCardDocument(t6x);
          _cardMetadataDirty = false;
          _cardMetadataEpoch++;
        }

        CaptureSavedProgramSnapshot();
        return true;
      }

      if (CuveSoftCardPlistFormat.IsCuveSoftCardPath(path))
      {
        if (_firmware is Teo67FirmwareGateway)
        {
          error = "CuveSoft (.xml) export is only supported for T-65.";
          return false;
        }

        if (Vocabulary is null)
        {
          error = "Program vocabulary is not available.";
          return false;
        }

        if (!TryBuildT6xDocument(codes, registers, out T6xDocument t6x, out error))
        {
          return false;
        }

        CuveSoftCardPlistSnapshot plist = CuveSoftCardPlistFormat.FromT6xDocument(
          t6x,
          mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
        CuveSoftCardPlistFormat.WriteFile(path, plist);
        if (markInserted)
        {
          MarkCardInserted(path, T6xCardFormat.ToTeoCardDocument(t6x));
        }
        else
        {
          _loadedTeoCard = T6xCardFormat.ToTeoCardDocument(t6x);
          _cardMetadataDirty = false;
          _cardMetadataEpoch++;
        }

        CaptureSavedProgramSnapshot();
        return true;
      }

      if (TeoCardProgramFormat.IsTeoCardPath(path))
      {
        if (!TryBuildT6xDocument(codes, registers, out T6xDocument t6x, out error))
        {
          return false;
        }

        TeoCardDocument teo = T6xCardFormat.ToTeoCardDocument(t6x);
        TeoCardProgramFormat.WriteFile(path, teo);
        if (markInserted)
        {
          MarkCardInserted(path, teo);
        }
        else
        {
          _loadedTeoCard = teo;
          _cardMetadataDirty = false;
          _cardMetadataEpoch++;
        }

        CaptureSavedProgramSnapshot();
        return true;
      }

      error =
        $"Unsupported card file extension '{Path.GetExtension(path)}'. " +
        "Save as .t65/.t67, or Export as CuveSoft (.xml) / Teo (.json).";
      return false;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }
  public bool TryLoadCardProgram(string path, out string? error)
  {
    error = null;
    if (_firmware is null || !_firmware.SupportsCardProgram)
    {
      error = "Card program I/O is not available for this model.";
      return false;
    }

    try
    {
      if (CuveSoftCardPlistFormat.IsCuveSoftCardPath(path))
      {
        return TryLoadCuveSoftCardProgram(path, out error);
      }

      if (TeoCardProgramFormat.IsTeoCardPath(path))
      {
        return TryLoadTeoCardProgram(path, out error);
      }

      if (IsCardTextPath(path))
      {
        return TryLoadT6xCardProgram(path, out error);
      }

      error =
        $"Unsupported card file extension '{Path.GetExtension(path)}'. " +
        "Use .t65/.t67, CuveSoft (.xml/.plist/.rpn65), or Teo (.json).";
      return false;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }

  private bool TryBuildT6xDocument(
    byte[] codes,
    double[] registers,
    out T6xDocument document,
    out string? error)
  {
    document = null!;
    error = null;

    if (_firmware is Teo67FirmwareGateway hp67Save)
    {
      Teo67CardModeSnapshot? mode = null;
      if (hp67Save.TryExportCardMode(out Teo67CardMode exported))
      {
        mode = new Teo67CardModeSnapshot(
          exported.Angle,
          exported.Display,
          exported.Digits,
          exported.FlagsHi,
          exported.FlagsLo);
      }

      Teo67CardSnapshot actSnapshot = new(codes, registers, mode);
      document = T6xCardFormat.FromTeo67Snapshot(
        actSnapshot,
        Teo67CardProgramIo.FormatMnemonic,
        _loadedTeoCard);
      return true;
    }

    if (Vocabulary is null)
    {
      error = "Program vocabulary is not available.";
      return false;
    }

    ClassicCardSnapshot classicSnapshot = new(codes, registers);
    string engineModelId = ExplorerModels[ModelIndex];
    TeoCardDocument teo = TeoCardProgramFormat.FromClassicSnapshot(
      classicSnapshot,
      code => ClassicCardProgramIo.FormatMnemonic(Vocabulary, code),
      engineModelId,
      _loadedTeoCard);
    document = T6xCardFormat.FromTeoCardDocument(teo);
    return true;
  }

  private bool TryLoadCuveSoftCardProgram(string path, out string? error)
  {
    error = null;
    if (Vocabulary is null)
    {
      error = "Program vocabulary is not available.";
      return false;
    }

    CuveSoftCardPlistSnapshot snapshot = CuveSoftCardPlistFormat.ReadFile(path);
    TeoCardDocument document = CuveSoftCardPlistFormat.ToTeoCardDocument(
      snapshot,
      code => ClassicCardProgramIo.FormatMnemonic(Vocabulary, code));
    string engineModelId = ExplorerModels[ModelIndex];
    if (!TeoCardProgramFormat.ModelMatches(document.Model, engineModelId, Model.Model))
    {
      error = $"Card model '{document.Model}' does not match active calculator '{engineModelId}'.";
      return false;
    }

    if (_firmware is Teo67FirmwareGateway)
    {
      error = "CuveSoft (.xml) import for HP-67 is not supported.";
      return false;
    }

    ClassicCardSnapshot classic = CuveSoftCardPlistFormat.ToClassicSnapshot(snapshot);
    if (!_firmware!.TryImportCardProgram(classic.ProgramCodes, classic.Registers))
    {
      error = "Failed to import program memory.";
      return false;
    }

    MarkCardInserted(path, document);
    SettleAfterCardImport();
    return true;
  }

  private bool TryLoadT6xCardProgram(string path, out string? error)
  {
    error = null;
    if (Vocabulary is null)
    {
      error = "Program vocabulary is not available.";
      return false;
    }

    T6xDocument t6x = T6xCardFormat.ReadFile(path);
    return TryImportT6xDocument(path, t6x, out error);
  }

  private bool TryLoadTeoCardProgram(string path, out string? error)
  {
    error = null;
    if (Vocabulary is null)
    {
      error = "Program vocabulary is not available.";
      return false;
    }

    TeoCardDocument document = TeoCardProgramFormat.ReadFile(path);
    T6xDocument t6x = T6xCardFormat.FromTeoCardDocument(document);
    return TryImportT6xDocument(path, t6x, out error);
  }

  private bool TryImportT6xDocument(string path, T6xDocument t6x, out string? error)
  {
    error = null;
    string engineModelId = ExplorerModels[ModelIndex];
    if (!T6xCardFormat.TargetCpuMatches(t6x.TargetCpu, engineModelId, Model.Model))
    {
      error = $"Card TargetCpu '{t6x.TargetCpu}' does not match active calculator '{engineModelId}'.";
      return false;
    }

    if (_firmware is Teo67FirmwareGateway hp67)
    {
      Teo67CardSnapshot snapshot = T6xCardFormat.ToTeo67Snapshot(t6x, Teo67CardProgramIo.ResolveMnemonic);
      if (!hp67.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers))
      {
        error = "Failed to import program memory.";
        return false;
      }

      TeoCardDocument document = T6xCardFormat.ToTeoCardDocument(t6x);
      MarkCardInserted(path, document);
      SettleAfterCardImport();
      return true;
    }

    ClassicCardSnapshot classic = T6xCardFormat.ToClassicSnapshot(
      t6x,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary!, mnemonic));
    if (!_firmware!.TryImportCardProgram(classic.ProgramCodes, classic.Registers))
    {
      error = "Failed to import program memory.";
      return false;
    }

    TeoCardDocument teoDocument = T6xCardFormat.ToTeoCardDocument(t6x);
    MarkCardInserted(path, teoDocument);
    SettleAfterCardImport();
    return true;
  }

  private static bool IsCardTextPath(string path) =>
    T6xCardFormat.IsCardTextPath(path);

  public void ClearPrintLines() =>
    _firmware?.ClearPrintLines();

  public void AppendTestPrint(string line) =>
    _firmware?.AppendTestPrint(line);

  private static TeoCalcModelDefinition CreatePlaceholderModel(string modelId) =>
    new()
    {
      Model = modelId,
      DisplayName = modelId,
      Family = CalcModelIds.InferFamily(modelId),
      Program = new TeoCalcModelProgram
      {
        Vocabulary = "Program/program.vocabulary.json",
      },
    };

  private static MicrocodeCrossRefCatalog? LoadCrossRefIfPresent(string path) =>
    File.Exists(path) ? MicrocodeCrossRefCatalog.Load(path) : null;

  private void OnFirmwareDisplayChanged(object? sender, FirmwareDisplayChangedEventArgs args) =>
    _displaySnapshot = args.Snapshot;

  private void OnFirmwareBatchCompleted(object? sender, FirmwareBatchCompletedEventArgs args)
  {
    SyncRomWatchFromBatch(args.Snapshot);
    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      return;
    }

    // W/PRGM: do not yank Studio ▶ on every idle batch — edit selection / overwrite owns it.
    // SST and BSP call SyncStudioToPointer from PressKey instead.
    // Re-paint museum LED after each tick: firmware ShowDisplay overwrites A/B and would
    // otherwise blank the faceplate right after a Studio overwrite / seek.
    if (ProgramMode)
    {
      SyncRomWatchFromBatch(args.Snapshot);
      SyncFaceplateProgramLed(cpu);
      return;
    }

    // RUN: pause when ▶ lands on a Studio breakpoint (batch-grain; may overshoot slightly).
    if (ExecutionPaused || _studioBreakpoints.Count == 0)
    {
      return;
    }

    int ptr = cpu.Program.PointerPosition();
    int hit = ptr;
    if (TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) && lines.Count > 0)
    {
      IReadOnlyList<StudioListingView.Row> rows = BuildStudioListingRows(lines);
      int highlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
      if (highlight >= 0)
      {
        hit = highlight;
      }
    }

    if (hit == _breakpointContinueIgnoreStep)
    {
      return;
    }

    _breakpointContinueIgnoreStep = -1;
    if (!HasStudioBreakpoint(hit))
    {
      return;
    }

    ExecutionPaused = true;
    SyncStudioToPointer(cpu);
  }
}

/// <summary>Studio listing selection navigation (arrows / Home / End / page).</summary>
public enum StudioProgramNav : byte
{
  Home = 0,
  End = 1,
  PageUp = 2,
  PageDown = 3,
  Up = 4,
  Down = 5,
}
