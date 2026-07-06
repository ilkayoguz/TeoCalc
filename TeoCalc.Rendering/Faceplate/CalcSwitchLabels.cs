namespace TeoCalc.Rendering.Faceplate;

public readonly record struct CalcSwitchLabels(string Left, string Right)
{
  public static CalcSwitchLabels ClassicPrgmRun { get; } = new("W/PRGM", "RUN");

  public static CalcSwitchLabels WoodstockAngle { get; } = new("DEG", "RAD");
}
