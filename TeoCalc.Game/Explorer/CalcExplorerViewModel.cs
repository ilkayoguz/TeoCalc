namespace TeoCalc.Game.Explorer;

public sealed class CalcExplorerViewModel
{
  public string DisplayName { get; set; } = string.Empty;

  public string EngineModelId { get; set; } = string.Empty;

  public string ProductLabel { get; set; } = string.Empty;

  public bool PowerOn { get; set; }

  public bool ProgramMode { get; set; }

  public string DisplayText { get; set; } = string.Empty;

  public bool IsDisplayVisible { get; set; }

  public string StatusLine { get; set; } = string.Empty;
}
