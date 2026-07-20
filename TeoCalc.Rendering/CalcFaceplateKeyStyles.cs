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
      // Classic HP-35: black rows 1–3 except CLR [1,5]=index 4 blue;
      // row 2 cols 2–5 (arc sin cos tan) dark grey; √x [2,1] stays black;
      // blue ENTER row + blue arithmetic; white digits / . / π.
      return keyChartIndex switch
      {
        4 => CalcButtonStyle.Blue, // CLR (1-based row1 col5)
        6 or 7 or 8 or 9 => CalcButtonStyle.DarkGrey, // arc sin cos tan (row2 cols 2–5)
        15 or 17 or 18 or 19 => CalcButtonStyle.Blue, // ENTER, CHS, EEX, CLX
        20 or 25 or 30 or 35 => CalcButtonStyle.Blue, // − + × ÷
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 or 38 => CalcButtonStyle.White, // digits, ·, π
        _ => CalcButtonStyle.Black,
      };
    }

    if (string.Equals(modelId, "HP-45", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "45", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-45: gold prefix; black trig; dark grey powers/roots/%;
      // grey ENTER + stack/arithmetic; white digits · Σ+.
      return keyChartIndex switch
      {
        4 => CalcButtonStyle.Orange, // gold f
        7 or 8 or 9 => CalcButtonStyle.Black, // SIN COS TAN
        0 or 1 or 2 or 3 or 5 or 6 or 14 => CalcButtonStyle.DarkGrey, // 1/x ln e^x FIX x² →P %
        15 or 17 or 18 or 19 => CalcButtonStyle.Grey, // ENTER CHS EEX CLX
        10 or 11 or 12 or 13 => CalcButtonStyle.Grey, // x↔y R↓ STO RCL
        20 or 25 or 30 or 35 => CalcButtonStyle.Grey, // − + × ÷
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 or 38 => CalcButtonStyle.White, // digits, ·, Σ+
        _ => CalcButtonStyle.Black,
      };
    }

    if (string.Equals(modelId, "HP-55", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "55", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-55 (T-55): light grey rows 1–2 + ENTER + ops; olive BST/SST;
      // gold f / blue g; black GTO + R/S; white digit pad.
      return keyChartIndex switch
      {
        4 or 9 => CalcButtonStyle.Olive, // BST, SST
        10 => CalcButtonStyle.Orange, // f
        11 => CalcButtonStyle.Blue, // g
        14 or 38 => CalcButtonStyle.Black, // GTO, R/S
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 => CalcButtonStyle.White, // digits, ·
        _ => CalcButtonStyle.LightGrey,
      };
    }

    if (string.Equals(modelId, "HP-67", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "HP-67BE", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "67", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "67BE", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-67: orange f / blue g / black h; olive body; white digit pad + R/S.
      return keyChartIndex switch
      {
        10 => CalcButtonStyle.Orange, // f
        11 => CalcButtonStyle.Blue, // g
        14 => CalcButtonStyle.Black, // h
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 or 38 => CalcButtonStyle.White, // digits, ·, R/S
        _ => CalcButtonStyle.Olive,
      };
    }

    if (string.Equals(modelId, "HP-70", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "70", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-70 (T-70): black TVM row; dark grey finance/stack; orange ENTER/CHS + ops; white pad.
      return keyChartIndex switch
      {
        5 or 6 or 7 or 8 => CalcButtonStyle.DarkGrey, // INT % Δ% y^x
        10 or 11 or 12 or 13 => CalcButtonStyle.DarkGrey, // x↔y R↓ STO K
        15 or 17 => CalcButtonStyle.Orange, // ENTER CHS
        18 or 19 => CalcButtonStyle.DarkGrey, // M M+
        20 or 25 or 30 or 35 => CalcButtonStyle.Orange, // − + × ÷
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 or 38 => CalcButtonStyle.White, // digits, ·, CLx
        _ => CalcButtonStyle.Black, // n i PMT PV FV CLR DSP
      };
    }

    if (string.Equals(modelId, "HP-80", StringComparison.OrdinalIgnoreCase)
        || string.Equals(modelId, "80", StringComparison.OrdinalIgnoreCase))
    {
      // Classic HP-80 (T-80): cement only [2,2]–[2,5] (% TL SOD DAY) + [3,4]–[3,5] (y^x x̄);
      // face SlateGray #708090 from Catalog HP-80.jpg (TL/SOD/DAY). Former cement slots → LightGrey.
      return keyChartIndex switch
      {
        5 => CalcButtonStyle.Orange, // gold prefix
        6 or 7 or 8 or 9 => CalcButtonStyle.Cement, // % TL SOD DAY
        13 or 14 => CalcButtonStyle.Cement, // y^x x̄
        10 or 11 or 12 => CalcButtonStyle.LightGrey, // x↔y R↓ STO
        15 or 17 or 18 or 19 => CalcButtonStyle.LightGrey, // SAVE RCL CHS CLX
        20 or 25 or 30 or 35 => CalcButtonStyle.LightGrey, // − + × ÷
        21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33
          or 36 or 37 or 38 => CalcButtonStyle.White, // digits, ·, Σ+
        _ => CalcButtonStyle.Black, // n i PMT PV FV
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
    bool hp37 = string.Equals(modelId, "HP-37", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-37E", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "37", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "37E", StringComparison.OrdinalIgnoreCase);
    bool hp38 = string.Equals(modelId, "HP-38", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "HP-38E", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "38", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "38E", StringComparison.OrdinalIgnoreCase);

    if (hp22)
    {
      // HP-22: gold f; dark grey rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        9 => CalcButtonStyle.Orange, // blank gold f prefix
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.DarkGrey,
      };
    }

    if (hp25 || hp29)
    {
      // HP-25 / HP-29C: gold f; blue g; dark grey rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        3 => CalcButtonStyle.Orange, // gold f CapFace
        4 => CalcButtonStyle.Blue,   // blue g CapFace
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.DarkGrey,
      };
    }

    if (hp32 || hp33)
    {
      // HP-32E / HP-33E: gold f; blue g; black rows 1–3 (incl. ENTER); white pad.
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
      // HP-34C: gold f; blue g; dark grey h + rows 1–3; white pad.
      return keyChartIndex switch
      {
        3 => CalcButtonStyle.Orange,
        4 => CalcButtonStyle.Blue,
        9 => CalcButtonStyle.DarkGrey, // h prefix letter
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.DarkGrey,
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

    if (hp31 || hp37)
    {
      // HP-31E / HP-37E: gold f only (index 9); black rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        9 => CalcButtonStyle.Orange, // gold f CapFace
        >= 15 and <= 33 => CalcButtonStyle.White,
        _ => CalcButtonStyle.Black,
      };
    }

    if (hp38)
    {
      // HP-38E: gold f at 8; blue g at 9; black rows 1–3 (incl. ENTER); white pad.
      return keyChartIndex switch
      {
        8 => CalcButtonStyle.Orange, // gold f CapFace
        9 => CalcButtonStyle.Blue,   // blue g CapFace
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
