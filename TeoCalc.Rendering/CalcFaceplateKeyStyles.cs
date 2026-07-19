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
        || ((modelId?.StartsWith("HP-3", StringComparison.OrdinalIgnoreCase) ?? false)
            && !string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase)))
    {
      return WoodstockStyle(modelId, keyChartIndex);
    }

    if (string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "35", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-35: no f/g prefixes; black function keys; white digit pad; π black.
      return keyChartIndex switch
      {
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33 or 36 or 37 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
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

  private static CalcButtonStyle WoodstockStyle(string? modelId, int keyChartIndex)
  {
    bool hp22 = string.Equals(modelId, "HP-22", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "22", StringComparison.OrdinalIgnoreCase);
    bool hp25 = string.Equals(modelId, "HP-25", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "25", StringComparison.OrdinalIgnoreCase);
    bool hp29 = string.Equals(modelId, "HP-29", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-29C", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "29", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "29C", StringComparison.OrdinalIgnoreCase);
    bool hp27 = string.Equals(modelId, "HP-27", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "27", StringComparison.OrdinalIgnoreCase);
    bool hp31 = string.Equals(modelId, "HP-31", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-31E", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "31", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "31E", StringComparison.OrdinalIgnoreCase);
    bool hp32 = string.Equals(modelId, "HP-32", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-32E", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "32", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "32E", StringComparison.OrdinalIgnoreCase);
    bool hp33 = string.Equals(modelId, "HP-33", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-33C", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-33E", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "33", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "33C", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "33E", StringComparison.OrdinalIgnoreCase);
    bool hp34 = string.Equals(modelId, "HP-34", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-34C", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "34", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "34C", StringComparison.OrdinalIgnoreCase);

    if (hp22)
    {
      return keyChartIndex switch
      {
        9 => CalcButtonStyle.Orange, // blank gold f prefix
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
    }

    if (hp25 || hp29 || hp32 || hp33)
    {
      // HP-25 / HP-29C / HP-32E / HP-33C: gold f; blue g; black rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        3 => CalcButtonStyle.Orange, // gold f CapFace
        4 => CalcButtonStyle.Blue,   // blue g CapFace
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
    }

    if (hp34)
    {
      // HP-34C: gold f; blue g; black h CapFace; black rows 1–3; white pad.
      return keyChartIndex switch
      {
        3 => CalcButtonStyle.Orange,
        4 => CalcButtonStyle.Blue,
        9 => CalcButtonStyle.Black, // h prefix letter
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
    }

    if (hp27)
    {
      // HP-27: olive rows 1–3 (incl. ENTER); gold f; black g; white pad.
      return keyChartIndex switch
      {
        3 => CalcButtonStyle.Orange,
        4 => CalcButtonStyle.Black,
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Olive,
      };
    }

    if (hp31)
    {
      // HP-31E: gold f only (index 9); black rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        9 => CalcButtonStyle.Orange, // gold f CapFace
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
    }

    // HP-21 (and default Woodstock): blue g at index 4; ENTER (10) is black like HP-22/25.
    return keyChartIndex switch
    {
      4 => CalcButtonStyle.Blue,
      >= 15 and <= 33 => CalcButtonStyle.White,
      _ => CalcButtonStyle.Black,
    };
  }
}
