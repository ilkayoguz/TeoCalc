using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Panamatik;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Panamatik emulator timer, press_key, and ShowDisplay orchestration for all models.</summary>
public sealed class CalcExplorerSession : IDisposable
{
  private static readonly string[] ExplorerModels = HpCalcModelCatalog.SupportedModels
    .Select(MapCatalogModelToExplorer)
    .ToArray();

  private PanamatikFirmwareGateway? _firmware;

  private IPanamatikEngine? _panamatikEngine;

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
      Flags: ClassicCpuFlags.None,
      BranchOffset: 0,
      KeyInputState: ClassicKeyInputState.Idle,
      KeyAvailable: false,
      KeysToRomAddressCount: 0,
      BufferToRomAddressCount: 0);

  private bool _mouseKeyHeld;

  private bool _keyboardKeyHeld;

  private int[] _faceplateSwitchIndices = [];

  private IReadOnlyList<CalcSwitchSpec> _faceplateSwitchSpecs = [];

  public CalcExplorerSession(string engineRoot)
  {
    EngineRoot = engineRoot;
    ModelIndex = Array.FindIndex(ExplorerModels, id => id == MapCatalogModelToExplorer(HpCalcModelCatalog.PriorityModel));
    if (ModelIndex < 0)
    {
      ModelIndex = 0;
    }

    LoadModel(ModelIndex);
  }

  public bool UsesPanamatikEngine => _panamatikEngine is not null;

  public string EngineRoot { get; }

  public string[] Models => ExplorerModels;

  public int ModelIndex { get; private set; }

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
    DisposePanamatikEngine();

    ModelIndex = Math.Clamp(index, 0, ExplorerModels.Length - 1);
    string explorerModelId = ExplorerModels[ModelIndex];
    string panamatikModelId = MapExplorerModelToPanamatik(explorerModelId);
    string engineModelFolder = MapExplorerModelToEngineFolder(explorerModelId);
    string modelPath = Path.Combine(EngineRoot, engineModelFolder, "Model.json");
    Model = File.Exists(modelPath)
      ? TeoCalcModelDefinition.Load(modelPath)
      : CreatePlaceholderModel(explorerModelId);

    _panamatikEngine = PanamatikEngineFactory.Create(panamatikModelId);
    _firmware = new PanamatikFirmwareGateway(_panamatikEngine);
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

    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(Model.DisplayName);
    CalcFaceplateThemeState.ApplyForModel(faceplateModel);
    LoadWarnings = BuildLoadWarnings(explorerModelId, engineModelFolder, panamatikModelId);
  }

  private static List<string> BuildLoadWarnings(
    string explorerModelId,
    string engineModelFolder,
    string panamatikModelId)
  {
    List<string> warnings = [];
    warnings.AddRange(PanamatikEngineFactory.GetAssetWarnings(panamatikModelId));
    return warnings;
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
    DisposePanamatikEngine();

  private void DisposePanamatikEngine()
  {
    if (_firmware is not null)
    {
      _firmware.DisplayChanged -= OnFirmwareDisplayChanged;
      _firmware.BatchCompleted -= OnFirmwareBatchCompleted;
      _firmware.Dispose();
      _firmware = null;
    }

    _panamatikEngine = null;
  }

  public void ResetCpu() =>
    PowerOff();

  public void PressKey(int keyChartIndex, byte keyCode)
  {
    ShiftPreview.HandleKeyPress(keyChartIndex);
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

  private static TeoCalcModelDefinition CreatePlaceholderModel(string modelId) =>
    new()
    {
      Model = modelId,
      DisplayName = modelId,
      Family = InferFamily(modelId),
      Program = new TeoCalcModelProgram
      {
        Vocabulary = "Program/program.vocabulary.json",
      },
    };

  private static string InferFamily(string modelId) =>
    modelId.ToUpperInvariant() switch
    {
      "HP-01" => "HP01",
      "HP-19C" => "HP19C",
      "HP-67" => "Classic",
      "HP-35" or "HP-45" or "HP-55" or "HP-65" or "HP-70" or "HP-80" => "Classic",
      var id when id.StartsWith("HP-3", StringComparison.Ordinal) && id is not "HP-35" => "Spice",
      var id when id.StartsWith("HP-2", StringComparison.Ordinal) => "Woodstock",
      _ => "Panamatik",
    };

  private static string MapCatalogModelToExplorer(string catalogModelId) =>
    catalogModelId switch
    {
      "HP-29C" => "HP-29",
      "HP-31E" => "HP-31",
      "HP-32E" => "HP-32",
      "HP-34C" => "HP-34",
      "HP-37E" => "HP-37",
      "HP-38E" => "HP-38",
      _ => catalogModelId,
    };

  private static string MapExplorerModelToPanamatik(string explorerModelId) =>
    explorerModelId switch
    {
      "HP-29" => "HP-29",
      "HP-31" => "HP-31",
      "HP-32" => "HP-32",
      "HP-34" => "HP-34",
      "HP-37" => "HP-37",
      "HP-38" => "HP-38",
      _ => explorerModelId,
    };

  private static string MapExplorerModelToEngineFolder(string explorerModelId) =>
    explorerModelId;

  private static MicrocodeCrossRefCatalog? LoadCrossRefIfPresent(string path) =>
    File.Exists(path) ? MicrocodeCrossRefCatalog.Load(path) : null;

  private void OnFirmwareDisplayChanged(object? sender, FirmwareDisplayChangedEventArgs args) =>
    _displaySnapshot = args.Snapshot;

  private void OnFirmwareBatchCompleted(object? sender, FirmwareBatchCompletedEventArgs args) =>
    _lastBatch = args.Snapshot;
}
