namespace TeoCalc.Game.Launcher;

public readonly record struct CalculatorLauncherItem(
  string CatalogModelId,
  string EngineModelId,
  string DisplayName,
  string ProductLabel,
  bool CanOpen,
  string Status);
