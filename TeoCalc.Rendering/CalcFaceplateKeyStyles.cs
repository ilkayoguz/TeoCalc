namespace TeoCalc.Rendering;

public static class CalcFaceplateKeyStyles
{
  public static CalcButtonStyle StyleForKey(string family, string? modelId, int keyChartIndex) =>
    string.Equals(family, "Woodstock", StringComparison.OrdinalIgnoreCase)
      || string.Equals(family, "Spice", StringComparison.OrdinalIgnoreCase)
      || (modelId?.StartsWith("HP-2", StringComparison.OrdinalIgnoreCase) ?? false)
      || (modelId?.StartsWith("HP-3", StringComparison.OrdinalIgnoreCase) ?? false)
      ? WoodstockStyle(keyChartIndex)
      : CalcButton.StyleForKeyIndex(keyChartIndex);

  private static CalcButtonStyle WoodstockStyle(int keyChartIndex) =>
    keyChartIndex switch
    {
      4 => CalcButtonStyle.Blue,
      10 or 11 => CalcButtonStyle.Grey,
      >= 15 and <= 33 => CalcButtonStyle.White,
      _ => CalcButtonStyle.Black,
    };
}
