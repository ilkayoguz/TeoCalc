namespace TeoCalc.Game.Explorer;

/// <summary>Renderer-agnostic session surface for explorer presenters.</summary>
public interface ICalcExplorerSession
{
  string DisplayName { get; }

  string EngineModelId { get; }

  string[] Models { get; }

  int ModelIndex { get; }

  bool PowerOn { get; set; }

  bool ProgramMode { get; set; }

  string DisplayText { get; }

  bool IsDisplayVisible();

  void LoadModel(int index);

  void Tick(float deltaSeconds);

  void PowerOnResume();

  void PowerOff();
}
