namespace TeoCalc.Rendering;

public static class CalcFaceplateKeyStyles
{
  public static CalcButtonStyle StyleForKey(string family, string? modelId, int keyChartIndex)
  {
    // HP-01: dark keys; Δ (gold shift) painted orange so the modifier reads clearly.
    if (string.Equals(family, "HP01", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "HP-01", StringComparison.OrdinalIgnoreCase))
    {
      return keyChartIndex == 24 ? CalcButtonStyle.Orange : CalcButtonStyle.Black;
    }

    if (string.Equals(family, "Woodstock", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Spice", StringComparison.OrdinalIgnoreCase)
        || (modelId?.StartsWith("HP-2", StringComparison.OrdinalIgnoreCase) ?? false)
        || (modelId?.StartsWith("HP-3", StringComparison.OrdinalIgnoreCase) ?? false))
    {
      return WoodstockStyle(keyChartIndex);
    }

    return CalcButton.StyleForKeyIndex(keyChartIndex);
  }

  private static CalcButtonStyle WoodstockStyle(int keyChartIndex) =>
    keyChartIndex switch
    {
      4 => CalcButtonStyle.Blue,
      10 or 11 => CalcButtonStyle.Grey,
      >= 15 and <= 33 => CalcButtonStyle.White,
      _ => CalcButtonStyle.Black,
    };
}
