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
    StepInto();

  public void StepInto()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    _firmware.StepInto();
    SyncRomWatchFromBatch(_firmware.LastBatch);
  }

  public void StepOver()
  {
    if (_firmware is null || !PowerOn)
    {
      return;
    }

    _firmware.StepOver();
    SyncRomWatchFromBatch(_firmware.LastBatch);
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

  public void EjectCard() => ResetCardSlotState();

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
