using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Game.Explorer;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Firmware timer, key, and display orchestration for all models via <see cref="ICalcFirmwareGateway"/>.</summary>
public sealed class CalcExplorerSession : ICalcExplorerSession, IDisposable
{
  /// <summary>ImGui click completes on mouse-up; firmware needs batches before KeyUp (prefix keys).</summary>
  private const int KeySettleBatches = 40;

  private static readonly string[] ExplorerModels = TeoCalcModelCatalog.SupportedModels
    .Select(CalcModelIds.ToEngineId)
    .ToArray();

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

  public CalcExplorerSession(string engineRoot)
  {
    EngineRoot = engineRoot;
    ModelIndex = Array.FindIndex(ExplorerModels, id => id == CalcModelIds.ToEngineId(TeoCalcModelCatalog.PriorityModel));
    if (ModelIndex < 0)
    {
      ModelIndex = 0;
    }

    LoadModel(ModelIndex);
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

  /// <summary>When true, microcode watch follows the live ROM fetch address while running / stepping.</summary>
  public bool FollowRomWatch { get; set; } = true;

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
    _firmware?.SetKeyLineHeld(IsKeyHeld);
  }

  public void ReleaseMouseKey()
  {
    if (_mouseKeyHeld)
    {
      _firmware?.KeyUp();
    }

    _mouseKeyHeld = false;
    _firmware?.SetKeyLineHeld(_keyboardKeyHeld);
  }

  public void ClearShiftPreview() =>
    ShiftPreview.Clear();

  public void ToggleProgramMode()
  {
    if (!PowerOn)
    {
      return;
    }

    _firmware?.ToggleProgramMode();
  }

  public void ToggleProgramModeTo(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    _firmware?.SetProgramMode(programMode);
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
    else if (spec.IsPower && !PowerOn)
    {
      index = 0;
      _faceplateSwitchIndices[switchIndex] = 0;
    }

    return spec.ClampIndex(index);
  }

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
    MicrocodeScroll = Math.Max(0, SelectedAddress - 8);
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();

    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(Model, explorerModelId);
    CalcFaceplateThemeState.ApplyForModel(faceplateModel);
    LoadWarnings = [.. CalcFirmwareGatewayLocator.AssetWarnings(engineModelFolder)];
  }

  public void Tick(float deltaSeconds) =>
    _firmware?.Tick(deltaSeconds);

  public void StepCpu() =>
    StepMicrocodeInto();

  /// <summary>
  /// F11 when card program: one Classic keystroke / FC element. Else microcode into.
  /// </summary>
  public void StepInto()
  {
    if (SupportsCardProgram)
    {
      StepStudioKey();
      return;
    }

    StepMicrocodeInto();
  }

  /// <summary>
  /// F10 when card program: one Code row / FC box. Else microcode over.
  /// </summary>
  public void StepOver()
  {
    if (SupportsCardProgram)
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

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      LoadedTeoCard?.Program.Steps);
    if (rows.Count == 0)
    {
      return;
    }

    int ptr = cpu.Program.PointerPosition();
    int rowIdx = FindStudioRowIndex(rows, ptr);

    if (StudioPaneSync.Focus == StudioPaneSync.StudioFocus.Flowchart)
    {
      StepStudioFlowchartBox(cpu, rows, ptr);
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

    cpu.Program.SeekPointer(rows[next].Index);
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
    int ptr)
  {
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      EngineModelId,
      CardStripLabels);
    int nodeId = StudioFlowchartGraph.FindNodeIdForStep(graph, ptr);
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
      _ = TryWrapRoutineEnd(cpu, forceFromRow: node.FirstStep >= 0 ? node.FirstStep : ptr);
      return;
    }

    // Next node in routine order (by FirstStep).
    int bestId = -1;
    int bestStep = int.MaxValue;
    int after = node.LastStep >= 0 ? node.LastStep : ptr;
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

    cpu.Program.SeekPointer(bestStep);
    SyncStudioToPointer(cpu);
  }

  private static int FindStudioRowIndex(IReadOnlyList<StudioListingView.Row> rows, int ptr)
  {
    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].ContainsIndex(ptr) || rows[i].Index == ptr)
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

    cpu.Program.SeekPointer(lblIndex);
    SyncStudioToPointer(cpu);
    return true;
  }

  private void SyncStudioToPointer(ClassicCpu cpu)
  {
    int ptr = cpu.Program.PointerPosition();
    SelectedProgramStep = ptr;
    StudioPaneSync.OnFlowchartSelected(ptr);
    StudioPaneSync.OnCodeSelected(ptr);
    if (_firmware is not null)
    {
      SyncRomWatchFromBatch(_firmware.LastBatch);
    }
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

  public void ContinueExecution() =>
    _firmware?.ContinueExecution();

  public string CaptureDebugDump() =>
    _firmware?.CaptureDebugDump() ?? "No firmware gateway.";

  public FirmwareDebugRegisters? TryGetDebugRegisters() =>
    _firmware?.TryGetDebugRegisters();

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
    MicrocodeScroll = Math.Max(0, address - 6);
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
    ShiftPreview.HandleKeyPress(keyChartIndex, Model.Family, Model.Model);
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

    _mouseKeyHeld = true;
    _firmware?.KeyDown(key);
    RunFirmwareTicks(KeySettleBatches);
  }

  private void SettleAfterCardImport()
  {
    ApplyNonPowerFaceplateSwitchesToFirmware();
    RunFirmwareTicks(KeySettleBatches);
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

  /// <summary>Move Classic PTR to <paramref name="stepIndex"/> (Studio “Set start point”).</summary>
  public bool TrySetProgramStartStep(int stepIndex)
  {
    if (_firmware is not ClassicFirmwareGateway { Cpu: { } cpu })
    {
      return false;
    }

    int last = cpu.Program.LastContentIndex();
    int target = Math.Clamp(stepIndex, 1, last);
    // Prefer landing on a real instruction (not NOP filler) when the selection is past content.
    while (target > 1 && cpu.Program.ReadCode(target) == 0)
    {
      target--;
    }

    cpu.Program.SeekPointer(target);
    SelectedProgramStep = target;
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

    if (!_firmware.TryExportCardProgram(out _, out double[] registers))
    {
      error = "Could not read current program/registers.";
      return false;
    }

    int capacity = CardProgramCapacity;
    byte[] merged = new byte[capacity];
    int count = Math.Min(capacity, pasted.Count);
    for (int i = 0; i < count; i++)
    {
      merged[i] = pasted[i];
    }

    if (!_firmware.TryImportCardProgram(merged, registers))
    {
      error = "Could not apply pasted program.";
      return false;
    }

    if (pasted.Count > capacity)
    {
      StudioStatusMessage = $"Pasted {capacity} of {pasted.Count} steps (capacity).";
    }
    else
    {
      StudioStatusMessage = $"Pasted {pasted.Count} step(s).";
    }

    SelectedProgramStep = Math.Clamp(SelectedProgramStep, 0, Math.Max(0, count - 1));
    return true;
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

  public string? CardTitle => _loadedTeoCard?.Title;

  public string? CardDescription => _loadedTeoCard?.Description;

  public string? CardUsage => _loadedTeoCard?.Usage;

  public string? CardRunHint => _loadedTeoCard?.RunHint;

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
  }

  private void MarkCardInserted(string path, TeoCardDocument? teoCard = null)
  {
    _cardInserted = true;
    _loadedCardPath = path;
    _loadedTeoCard = teoCard;
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

  public bool TrySaveCardProgram(string path, out string? error)
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

    try
    {
      if (IsCardTextPath(path))
      {
        if (!TryBuildT6xDocument(codes, registers, out T6xDocument t6x, out error))
        {
          return false;
        }

        T6xCardFormat.WriteFile(path, t6x);
        MarkCardInserted(path, T6xCardFormat.ToTeoCardDocument(t6x));
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
        MarkCardInserted(path, T6xCardFormat.ToTeoCardDocument(t6x));
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
        MarkCardInserted(path, teo);
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

  private void OnFirmwareBatchCompleted(object? sender, FirmwareBatchCompletedEventArgs args) =>
    SyncRomWatchFromBatch(args.Snapshot);
}
