using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public sealed class CalcExplorerSession
{
  private static readonly string[] ExplorerModels =
  [
    "HP-35", "HP-45", "HP-55", "HP-65", "HP-70", "HP-80",
    "HP-21", "HP-22", "HP-25", "HP-27", "HP-29",
    "HP-31", "HP-32", "HP-33", "HP-34", "HP-37", "HP-38",
  ];

  public const int KeyRunSteps = 200;

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
  }

  public void StepCpu()
  {
    if (Cpu is null)
    {
      return;
    }

    if (Cpu.StepCount == 0 && Cpu.State.ProgramCounter == 0)
    {
      Cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    }

    Cpu.Step();
    SelectedAddress = Math.Max(0, Cpu.State.ProgramCounter - 1);
  }

  public void ResetCpu()
  {
    if (Cpu is null)
    {
      return;
    }

    Cpu.Reset();
    SelectedAddress = 0;
  }

  public void PressKeyAndRun(byte keyCode, int steps = KeyRunSteps)
  {
    if (Cpu is null)
    {
      return;
    }

    if (Cpu.StepCount == 0 && Cpu.State.ProgramCounter == 0)
    {
      Cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    }

    Cpu.PressKey(keyCode);
    for (int step = 0; step < steps; step++)
    {
      Cpu.Step();
    }

    SelectedAddress = Math.Max(0, Cpu.State.ProgramCounter - 1);
  }

  private static MicrocodeCrossRefCatalog? LoadCrossRefIfPresent(string path)
  {
    return File.Exists(path) ? MicrocodeCrossRefCatalog.Load(path) : null;
  }
}
