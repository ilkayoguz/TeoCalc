using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Game.Explorer;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Firmware timer, key, and display orchestration for all models via <see cref="ICalcFirmwareGateway"/>.</summary>
public sealed class CalcExplorerSession : ICalcExplorerSession, IDisposable
{
  private static readonly string[] ExplorerModels = HpCalcModelCatalog.SupportedModels
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

  public CalcExplorerSession(string engineRoot)
  {
    EngineRoot = engineRoot;
    ModelIndex = Array.FindIndex(ExplorerModels, id => id == CalcModelIds.ToEngineId(HpCalcModelCatalog.PriorityModel));
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
    _keyboardKeyHeld = held;
    _firmware?.SetKeyLineHeld(IsKeyHeld);
  }

  public void ReleaseMouseKey()
  {
    _mouseKeyHeld = false;
    _firmware?.SetKeyLineHeld(IsKeyHeld);
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

  public void StepCpu()
  {
    if (_firmware is null)
    {
      return;
    }

    _firmware.Step();
    SelectedAddress = Math.Max(0, _firmware.LastBatch.ProgramCounter);
  }

  public void Dispose() =>
    DisposeFirmware();

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
  }

  public bool SupportsCardProgram =>
    _firmware?.SupportsCardProgram == true;

  public IReadOnlyList<string> PrintLines =>
    _firmware?.PrintLines ?? [];

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
      Hp65CardSnapshot snapshot = new(codes, registers);
      Hp65CardProgramFormat.WriteFile(
        path,
        snapshot,
        code => ClassicCardProgramIo.FormatMnemonic(Vocabulary, code));
      return true;
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
      Hp65CardSnapshot snapshot = Hp65CardProgramFormat.ReadFile(
        path,
        mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
      if (!_firmware.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers))
      {
        error = "Failed to import program memory.";
        return false;
      }

      return true;
    }
    catch (Exception ex)
    {
      error = ex.Message;
      return false;
    }
  }

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
    _lastBatch = args.Snapshot;
}
