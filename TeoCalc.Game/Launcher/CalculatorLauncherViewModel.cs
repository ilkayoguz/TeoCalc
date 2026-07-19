namespace TeoCalc.Game.Launcher;

public sealed class CalculatorLauncherViewModel
{
  public IReadOnlyList<CalculatorLauncherItem> Items { get; init; } = [];

  public int SelectedIndex { get; set; }

  public string StatusLine { get; set; } = string.Empty;

  public CalculatorLauncherItem? SelectedItem =>
    SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
}
