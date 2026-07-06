namespace TeoCalc.Rendering;

public readonly record struct CalculatorLauncherEntry(
  string ModelId,
  string DisplayName,
  string TeoCalcModelId,
  string TeoCalcStatus,
  bool CanOpenTeoCalc,
  ReferenceCalculatorEntry? Reference)
{
  public bool CanOpenReference => Reference?.CanOpen == true;

  public string ReferenceStatus => Reference?.Status ?? "Reference pending";
}
