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

    if (string.Equals(family, "HP19C", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "HP-19C", StringComparison.OrdinalIgnoreCase))
    {
      return Hp19CStyle(keyChartIndex);
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

  /// <summary>HP-19C: black top/side, white digit pad, orange f, blue g.</summary>
  private static CalcButtonStyle Hp19CStyle(int keyChartIndex) =>
    keyChartIndex switch
    {
      5 => CalcButtonStyle.Orange,
      11 => CalcButtonStyle.Blue,
      12 or 13 or 14 or 15 => CalcButtonStyle.White,
      18 or 19 or 20 or 21 => CalcButtonStyle.White,
      24 or 25 or 26 or 27 => CalcButtonStyle.White,
      30 or 31 or 32 or 33 => CalcButtonStyle.White,
      _ => CalcButtonStyle.Black,
    };

  private static CalcButtonStyle WoodstockStyle(int keyChartIndex) =>
    keyChartIndex switch
    {
      4 => CalcButtonStyle.Blue,
      10 or 11 => CalcButtonStyle.Grey,
      >= 15 and <= 33 => CalcButtonStyle.White,
      _ => CalcButtonStyle.Black,
    };
}
