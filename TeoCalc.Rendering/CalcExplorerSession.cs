using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

/// <summary>Panamatik <c>HPClassic</c> timer, <c>press_key</c>, and <c>ShowDisplay</c> orchestration.</summary>
public sealed class CalcExplorerSession
{
  private static readonly string[] ExplorerModels =
  [
    "HP-35", "HP-45", "HP-55", "HP-65", "HP-70", "HP-80",
    "HP-21", "HP-22", "HP-25", "HP-27", "HP-29",
    "HP-31", "HP-32", "HP-33", "HP-34", "HP-37", "HP-38",
  ];

  private readonly FirmwareGateway _firmware = new();

  private bool _mouseKeyHeld;

  private bool _keyboardKeyHeld;

  public CalcExplorerSession(string engineRoot)
  {
    EngineRoot = engineRoot;
    ModelIndex = Array.IndexOf(ExplorerModels, HpCalcModelCatalog.PriorityModel);
    if (ModelIndex < 0)
    {
      ModelIndex = 0;
    }

    LoadModel(ModelIndex);
  }

  public string EngineRoot { get; }

  public string[] Models => ExplorerModels;

  public int ModelIndex { get; private set; }

  public TeoCalcModelDefinition Model { get; private set; } = null!;

  public ClassicCpu? Cpu => _firmware.Cpu;

  public ProgramVocabulary? Vocabulary { get; private set; }

  public MicrocodeMapCatalog Map { get; private set; } = null!;

  public MicrocodeCrossRefCatalog? CrossRef { get; private set; }

  public bool SupportsCpu => Cpu is not null;

  public int MicrocodeScroll { get; set; }

  public int ProgramScroll { get; set; }

  public int SelectedAddress { get; set; }

  public bool ProgramMode
  {
    get => _firmware.ProgramMode;
    set => _firmware.SetProgramMode(value);
  }

  public bool PowerOn
  {
    get => _firmware.PowerOn;
    set => _firmware.PowerOn = value;
  }

  public string DisplayText => _firmware.DisplayText;

  public ShiftPreviewController ShiftPreview { get; } = new();

  public bool IsKeyHeld => _mouseKeyHeld || _keyboardKeyHeld;

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged
  {
    add => _firmware.DisplayChanged += value;
    remove => _firmware.DisplayChanged -= value;
  }

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed
  {
    add => _firmware.KeyProcessed += value;
    remove => _firmware.KeyProcessed -= value;
  }

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged
  {
    add => _firmware.KeyStateChanged += value;
    remove => _firmware.KeyStateChanged -= value;
  }

  public void PowerOnResume()
  {
    _firmware.PowerOnResume();
  }

  public void PowerOff()
  {
    _firmware.PowerOff();
    SelectedAddress = 0;
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();
  }

  public bool IsDisplayVisible() => _firmware.IsDisplayVisible();

  public void EndDisplayFrame() => _firmware.EndDisplayFrame();

  public void SetKeyboardKeyHeld(bool held)
  {
    _keyboardKeyHeld = held;
    _firmware.SetKeyLineHeld(IsKeyHeld);
  }

  public void ReleaseMouseKey()
  {
    _mouseKeyHeld = false;
    _firmware.SetKeyLineHeld(IsKeyHeld);
  }

  public void ClearShiftPreview() => ShiftPreview.Clear();

  public void ToggleProgramMode()
  {
    if (!PowerOn)
    {
      return;
    }

    _firmware.ToggleProgramMode();
  }

  public void ToggleProgramModeTo(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    _firmware.SetProgramMode(programMode);
  }

  public void LoadModel(int index)
  {
    ModelIndex = Math.Clamp(index, 0, ExplorerModels.Length - 1);
    string modelId = ExplorerModels[ModelIndex];
    string modelPath = Path.Combine(EngineRoot, modelId, "Model.json");
    Model = TeoCalcModelDefinition.Load(modelPath);

    ClassicCpu? cpu = string.Equals(Model.Family, "Classic", StringComparison.OrdinalIgnoreCase)
      ? ClassicCpuFactory.Create(Model, EngineRoot)
      : null;
    _firmware.AttachCpu(cpu);

    Vocabulary = null;
    if (Model.Program?.Vocabulary is { Length: > 0 } vocabularyPath)
    {
      string fullVocabularyPath = Path.Combine(EngineRoot, modelId, vocabularyPath.Replace('/', Path.DirectorySeparatorChar));
      if (File.Exists(fullVocabularyPath))
      {
        Vocabulary = ProgramVocabulary.Load(fullVocabularyPath);
      }
    }

    string mapPath = Path.Combine(EngineRoot, modelId, Model.Firmware.RomMap.Replace('/', Path.DirectorySeparatorChar));
    Map = MicrocodeMapCatalog.Load(mapPath);

    CrossRef = string.Equals(Model.Family, "Classic", StringComparison.OrdinalIgnoreCase)
      ? LoadCrossRefIfPresent(Path.Combine(EngineRoot, "Classic", "microcode.crossref.json"))
      : null;

    SelectedAddress = Cpu?.State.FetchAddress ?? 0;
    MicrocodeScroll = Math.Max(0, SelectedAddress - 8);
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    ShiftPreview.Reset();
  }

  public void Tick(float deltaSeconds)
  {
    _firmware.Tick(deltaSeconds);
  }

  public void StepCpu()
  {
    if (Cpu is null)
    {
      return;
    }

    _firmware.Step();
    SelectedAddress = Math.Max(0, Cpu.State.ProgramCounter - 1);
  }

  public void ResetCpu() => PowerOff();

  /// <summary>Panamatik <c>press_key</c> — display blank, buffer key, <c>Run()</c>.</summary>
  public void PressKey(int keyChartIndex, byte keyCode)
  {
    ShiftPreview.HandleKeyPress(keyChartIndex);
    PressKey(new FirmwareKeyCommand(keyChartIndex, keyCode));
  }

  public void PressKey(byte keyCode) =>
    PressKey(new FirmwareKeyCommand(-1, keyCode));

  public void PressKey(FirmwareKeyCommand key)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    _mouseKeyHeld = true;
    _firmware.KeyDown(key);
  }

  private static MicrocodeCrossRefCatalog? LoadCrossRefIfPresent(string path)
  {
    return File.Exists(path) ? MicrocodeCrossRefCatalog.Load(path) : null;
  }
}
