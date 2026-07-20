namespace TeoCalc.Rendering.Faceplate;

/// <summary>Legacy pair view of a mode switch. Prefer <see cref="CalcSwitchSpec"/> / <see cref="CalcSwitchCatalog"/>.</summary>
public readonly record struct CalcSwitchLabels(string Left, string Right)
{
  public static CalcSwitchLabels ClassicPrgmRun { get; } = new("W/PRGM", "RUN");

  public static CalcSwitchLabels WoodstockPrgmRun { get; } = new("PRGM", "RUN");

  public static CalcSwitchLabels WoodstockAngle { get; } = new("DEG", "RAD");

  public static CalcSwitchLabels BeginEnd { get; } = new("BEGIN", "END");

  /// <summary>HP-38E/38C dual-row right switch (D.MY/M.DY over BEGIN/END).</summary>
  public static CalcSwitchLabels DateBeginEnd { get; } = new("D.MY\nBEGIN", "M.DY\nEND");

  public static CalcSwitchLabels TimerRun { get; } = new("TIMER", "RUN");

  public static CalcSwitchLabels PowerOnly { get; } = new(string.Empty, string.Empty);

  public static CalcSwitchLabels Power { get; } = new("OFF", "ON");

  public bool HasModeSwitch =>
    !string.IsNullOrEmpty(Left) || !string.IsNullOrEmpty(Right);

  public static CalcSwitchLabels ForModel(CalcModelDefinition model) =>
    ForModelId(model.Id, model.DisplayName);

  public static CalcSwitchLabels ForModelId(string modelId, string? displayName = null)
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId(modelId, displayName);
    return bank.Count > 1
      ? new CalcSwitchLabels(bank[1].LeftLabel, bank[1].RightLabel)
      : PowerOnly;
  }
}
