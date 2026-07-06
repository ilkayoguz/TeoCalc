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
          _firmware.PowerOnResume();
        }
      }
      else
      {
        _firmware.PowerOff();
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

  public void PowerOnResume() =>
    _firmware?.PowerOnResume();

  public void PowerOff()
  {
    _firmware?.PowerOff();
    SelectedAddress = 0;
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();
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
      Family = "Panamatik",
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
