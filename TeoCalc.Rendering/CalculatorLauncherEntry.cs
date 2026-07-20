namespace TeoCalc.Rendering;

public readonly record struct CalculatorLauncherEntry(
  string ModelId,
  string DisplayName,
  string ProductLabel,
  string TeoCalcModelId,
  string TeoCalcStatus,
  bool CanOpenTeoCalc,
  ReferenceCalculatorEntry? Reference)
{
  public bool CanOpenReference => Reference?.CanOpen == true;

  public string ReferenceStatus => Reference?.Status ?? "Reference pending";

  /// <summary>Launcher tile caption — product label (T-65), not engine folder id (HP-65).</summary>
  public string LauncherLabel =>
    string.IsNullOrWhiteSpace(ProductLabel) ? DisplayName : ProductLabel;
}
