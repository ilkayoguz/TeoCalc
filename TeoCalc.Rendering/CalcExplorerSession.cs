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

  public const int KeyRunSteps = 200;

  private const float RunTickSeconds = 0.01f;

  private float _runAccumulator;

  private string _displayText = string.Empty;

  private bool _displayBlankPulse;

  private bool _mouseKeyHeld;

  private bool _keyboardKeyHeld;

  private int _keyHoldBatchesRemaining;

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

  public ClassicCpu? Cpu { get; private set; }

  public ProgramVocabulary? Vocabulary { get; private set; }

  public MicrocodeMapCatalog Map { get; private set; } = null!;

  public MicrocodeCrossRefCatalog? CrossRef { get; private set; }

  public bool SupportsCpu => Cpu is not null;

  public int MicrocodeScroll { get; set; }

  public int ProgramScroll { get; set; }

  public int SelectedAddress { get; set; }

  public bool ProgramMode { get; set; }

  public bool PowerOn { get; set; }

  public string DisplayText => _displayText;

  public ShiftPreviewMode ShiftPreview { get; private set; }

  public bool IsKeyHeld => _mouseKeyHeld || _keyboardKeyHeld;

  public void PowerOnResume()
  {
    PowerOn = true;
    RunInstructionBatch(KeyRunSteps);
  }

  public void PowerOff()
  {
    if (Cpu is null)
    {
      PowerOn = false;
      return;
    }

    Cpu.Reset();
    SelectedAddress = 0;
    PowerOn = false;
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    _keyHoldBatchesRemaining = 0;
    _displayText = string.Empty;
    _displayBlankPulse = false;
    ShiftPreview = ShiftPreviewMode.None;
  }

  public bool IsDisplayVisible() =>
    PowerOn && !_displayBlankPulse && _displayText.Length > 0;

  public void EndDisplayFrame()
  {
    _displayBlankPulse = false;
  }

  public void SetKeyboardKeyHeld(bool held) => _keyboardKeyHeld = held;

  public void ReleaseMouseKey() => _mouseKeyHeld = false;

  public void ClearShiftPreview() => ShiftPreview = ShiftPreviewMode.None;

  public void ToggleProgramMode()
  {
    if (!PowerOn)
    {
      return;
    }

    ProgramMode = !ProgramMode;
    RunInstructionBatch(KeyRunSteps);
  }

  public void ToggleProgramModeTo(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    ProgramMode = programMode;
    RunInstructionBatch(KeyRunSteps);
  }

  public void LoadModel(int index)
  {
    ModelIndex = Math.Clamp(index, 0, ExplorerModels.Length - 1);
    string modelId = ExplorerModels[ModelIndex];
    string modelPath = Path.Combine(EngineRoot, modelId, "Model.json");
    Model = TeoCalcModelDefinition.Load(modelPath);

    Cpu = string.Equals(Model.Family, "Classic", StringComparison.OrdinalIgnoreCase)
      ? ClassicCpuFactory.Create(Model, EngineRoot)
      : null;

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
    _runAccumulator = 0f;
    PowerOn = false;
    _displayText = string.Empty;
    _displayBlankPulse = false;
    _mouseKeyHeld = false;
    _keyboardKeyHeld = false;
    _keyHoldBatchesRemaining = 0;
    ShiftPreview = ShiftPreviewMode.None;
  }

  public void Tick(float deltaSeconds)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    _runAccumulator += deltaSeconds;
    while (_runAccumulator >= RunTickSeconds)
    {
      RunInstructionBatch(KeyRunSteps);
      _runAccumulator -= RunTickSeconds;
    }
  }

  public void StepCpu()
  {
    if (Cpu is null)
    {
      return;
    }

    Cpu.Step();
    SelectedAddress = Math.Max(0, Cpu.State.ProgramCounter - 1);
  }

  public void ResetCpu() => PowerOff();

  /// <summary>Panamatik <c>press_key</c> — display blank, buffer key, <c>Run()</c>.</summary>
  public void PressKey(int keyChartIndex, byte keyCode)
  {
    UpdateShiftPreview(keyChartIndex);
    PressKey(keyCode);
  }

  public void PressKey(byte keyCode)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    Cpu.State.Flags &= ~ClassicCpuFlags.DisplayOn;
    _displayBlankPulse = true;
    _displayText = string.Empty;
    Cpu.PressKey(keyCode);
    _mouseKeyHeld = true;
    _keyHoldBatchesRemaining = Math.Max(_keyHoldBatchesRemaining, 12);
    RunInstructionBatch(KeyRunSteps);
  }

  private void UpdateShiftPreview(int keyChartIndex)
  {
    ShiftPreviewMode requested = keyChartIndex switch
    {
      10 => ShiftPreviewMode.Gold,
      11 => ShiftPreviewMode.GoldInverse,
      14 => ShiftPreviewMode.Blue,
      _ => ShiftPreviewMode.None,
    };

    ShiftPreview = requested == ShiftPreview ? ShiftPreviewMode.None : requested;
  }

  private void RunInstructionBatch(int steps)
  {
    if (Cpu is null)
    {
      return;
    }

    bool keyHeld = IsKeyHeld || _keyHoldBatchesRemaining > 0;
    for (int step = 0; step < steps; step++)
    {
      Cpu.Step();
      if (keyHeld)
      {
        Cpu.State.Status |= 1;
      }

      if (ProgramMode)
      {
        Cpu.State.Status |= 8;
      }
    }
    if (_keyHoldBatchesRemaining > 0)
    {
      _keyHoldBatchesRemaining--;
    }

    RefreshDisplayFromCpu();
  }

  private void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      _displayText = string.Empty;
      return;
    }

    string? text = ClassicFirmwareDisplay.TryBuildLedText(
      Cpu.State,
      ProgramMode,
      Cpu.Program.EndState);

    if (text is not null)
    {
      _displayText = text;
      _displayBlankPulse = false;
      return;
    }

    if (_displayBlankPulse)
    {
      _displayText = string.Empty;
    }
  }

  private static MicrocodeCrossRefCatalog? LoadCrossRefIfPresent(string path)
  {
    return File.Exists(path) ? MicrocodeCrossRefCatalog.Load(path) : null;
  }
}
