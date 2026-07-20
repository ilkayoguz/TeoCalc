using System.Numerics;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

public enum FaceplateLabelStyle
{
  Normal,
  Vertical,
}

public readonly record struct FaceplateCell(
  int KeyChartIndex,
  int Row,
  int Column,
  int RowSpan = 1,
  int ColSpan = 1,
  FaceplateLabelStyle LabelStyle = FaceplateLabelStyle.Normal);

/// <summary>HP Classic faceplate cell map; geometry from Body.svg layout JSON.</summary>
public static class CalcFaceplateLayout
{
  public const int Rows = 8;

  public const int Columns = 5;

  public static readonly string[] CardSlotLabels = ["1/x", "\u221ax", "y^x", "R\u2193", "x\u2194y"];

  public static Vector2 OnOffSwitchNorm
  {
    get
    {
      BodyFaceplateLayout.EnsureLoaded();
      Vector2 center = BodyFaceplateLayout.OnOffSwitchCenter;
      return new(center.X / BodyFaceplateLayout.ReferenceWidth, center.Y / BodyFaceplateLayout.ReferenceHeight);
    }
  }

  public static Vector2 PrgmRunSwitchNorm
  {
    get
    {
      BodyFaceplateLayout.EnsureLoaded();
      Vector2 center = BodyFaceplateLayout.PrgmRunSwitchCenter;
      return new(center.X / BodyFaceplateLayout.ReferenceWidth, center.Y / BodyFaceplateLayout.ReferenceHeight);
    }
  }

  private static readonly FaceplateCell[] ClassicPhysicalCells =
  [
    ..Enumerable.Range(0, 15).Select(index => new FaceplateCell(index, index / Columns, index % Columns)),
    new(15, 3, 0, ColSpan: 2),
    new(17, 3, 2),
    new(18, 3, 3),
    new(19, 3, 4),
    ..Enumerable.Range(20, 19).Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .Where(cell => cell.KeyChartIndex is not (24 or 29 or 34 or 39)),
  ];

  public static int ToIndex(int row, int column) => row * Columns + column;

  public static IReadOnlyList<FaceplateCell> GetPhysicalCells(string family, string? modelId = null)
  {
    if (string.Equals(modelId, "HP-65", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Classic", StringComparison.OrdinalIgnoreCase))
    {
      return ClassicPhysicalCells;
    }

    if (string.Equals(modelId, "HP-21", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Woodstock", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Spice", StringComparison.OrdinalIgnoreCase))
    {
      return WoodstockFaceplateLayout.GetPhysicalCells(modelId);
    }

    if (string.Equals(modelId, "HP-01", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "HP01", StringComparison.OrdinalIgnoreCase))
    {
      return Hp01FaceplateLayout.PhysicalCells;
    }

    if (string.Equals(modelId, "HP-19C", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "HP19C", StringComparison.OrdinalIgnoreCase))
    {
      return Hp19CFaceplateLayout.PhysicalCells;
    }

    return Enumerable.Range(0, Rows * Columns)
      .Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .Where(cell => cell.KeyChartIndex is >= 0)
      .ToArray();
  }

  public static string LabelForKey(
    ProgramKeyEntry key,
    ProgramVocabulary? vocabulary,
    string? family = null,
    string? modelId = null)
  {
    if (string.Equals(family, "Classic", StringComparison.OrdinalIgnoreCase))
    {
      if (IsHp35(modelId))
      {
        string? hp35 = Hp35LabelFromIndex(key.Index, key.Char);
        if (hp35 is not null)
        {
          return hp35;
        }
      }

      if (IsHp45(modelId))
      {
        string? hp45 = Hp45LabelFromIndex(key.Index, key.Char);
        if (hp45 is not null)
        {
          return hp45;
        }
      }

      if (IsHp55(modelId))
      {
        string? hp55 = Hp55LabelFromIndex(key.Index, key.Char);
        if (hp55 is not null)
        {
          return hp55;
        }
      }

      if (IsHp67(modelId))
      {
        string? hp67 = Hp67LabelFromIndex(key.Index, key.Char);
        if (hp67 is not null)
        {
          return hp67;
        }
      }

      if (IsHp70(modelId))
      {
        string? hp70 = Hp70LabelFromIndex(key.Index, key.Char);
        if (hp70 is not null)
        {
          return hp70;
        }
      }

      if (IsHp80(modelId))
      {
        string? hp80 = Hp80LabelFromIndex(key.Index, key.Char);
        if (hp80 is not null)
        {
          return hp80;
        }
      }
    }

    if (key.KeyCode == 0)
    {
      return string.Empty;
    }

    if (string.Equals(family, "Woodstock", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Spice", StringComparison.OrdinalIgnoreCase))
    {
      string? woodstock = WoodstockLabelFromChar(key.Char, modelId);
      if (woodstock is not null)
      {
        return woodstock;
      }
    }

    if (string.Equals(family, "HP01", StringComparison.OrdinalIgnoreCase))
    {
      string? hp01 = Hp01LabelFromChar(key.Char);
      if (hp01 is not null)
      {
        return hp01;
      }
    }

    if (string.Equals(family, "HP19C", StringComparison.OrdinalIgnoreCase))
    {
      string? hp19 = Hp19CLabelFromChar(key.Char);
      if (hp19 is not null)
      {
        return hp19;
      }
    }

    string? faceplate = PrimaryLabelFromChar(key.Char);
    if (faceplate is not null)
    {
      return faceplate;
    }

    if (key.Char == "\0")
    {
      return string.Empty;
    }

    return key.Char;
  }

  public static CalcButtonKind ButtonKindForKey(ProgramKeyEntry key, FaceplateCell cell, string? family = null)
  {
    if (cell.ColSpan >= 2 && key.Char == "\r")
    {
      return CalcButtonKind.EnterWide;
    }

    // Chart char "/" is the divide key — Classic + HP-01 operator-row use colon-obelus art.
    // HP-01 date slash is a different chart char (";") labeled "/".
    if (key.Char == "/")
    {
      return CalcButtonKind.OperatorColon;
    }

    return CalcButtonKind.Standard;
  }

  private static string? PrimaryLabelFromChar(string charValue)
  {
    return charValue switch
    {
      "a" => "A",
      "b" => "B",
      "c" => "C",
      "d" => "D",
      "e" => "E",
      "p" => "DSP",
      "o" => "GTO",
      "l" => "LBL",
      "q" => "RTN",
      "t" => "SST",
      "f" => "f",
      "h" => "f\u207b\u00b9",
      "s" => "STO",
      "r" => "RCL",
      "g" => "g",
      "\r" => "ENTER",
      "n" => "CHS",
      "x" => "EEX",
      "\b" => "CLX",
      "*" => "\u00d7",
      "/" => "\u00f7",
      " " => "R/S",
      "." => "\u00b7",
      "-" or "+" => charValue,
      _ when charValue.Length == 1 && char.IsDigit(charValue[0]) => charValue,
      _ => null,
    };
  }

  private static string? WoodstockLabelFromChar(string charValue, string? modelId = null)
  {
    bool hp21 = string.Equals(modelId, "HP-21", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "21", StringComparison.OrdinalIgnoreCase);
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

    // HP-21 Owner's Handbook: blank CapFace on blue prefix; DSP (not R/S); CLx spelling.
    if (hp21)
    {
      string? hp21Label = charValue switch
      {
        "g" => string.Empty,
        " " => "DSP",
        "\b" => "CLx",
        _ => null,
      };
      if (hp21Label is not null)
      {
        return hp21Label;
      }
    }

    // HP-22 Finseth hp22a / Owner's Handbook: financial top row; blank gold f; Σ+; CLX.
    if (hp22)
    {
      string? hp22Label = charValue switch
      {
        "n" => "n",
        "i" => "i",
        "t" => "PMT",
        "p" => "PV",
        "v" => "FV",
        "f" => string.Empty,
        "h" => "CHS",
        "#" => "\u03a3+",
        "\b" => "CLX",
        _ => null,
      };
      if (hp22Label is not null)
      {
        return hp22Label;
      }
    }

    // HP-25 Finseth hp25a / Owner's Handbook: SST/BST/GTO; visible f/g CapFace; Σ+; CLX; R/S.
    if (hp25)
    {
      string? hp25Label = charValue switch
      {
        "t" => "SST",
        "b" => "BST",
        "o" => "GTO",
        "f" => "f",
        "g" => "g",
        "y" => "x\u2194y",
        "d" => "R\u2193",
        "s" => "STO",
        "r" => "RCL",
        "#" => "\u03a3+",
        "\b" => "CLX",
        " " => "R/S",
        _ => null,
      };
      if (hp25Label is not null)
      {
        return hp25Label;
      }
    }

    // HP-29C Finseth hp29c / Owner's Handbook: SST/GSB/GTO; visible f/g CapFace; Σ+ on row2; CLX; R/S.
    if (hp29)
    {
      string? hp29Label = charValue switch
      {
        "t" => "SST",
        "b" => "GSB",
        "o" => "GTO",
        "f" => "f",
        "g" => "g",
        "y" => "x\u2194y",
        "d" => "R\u2193",
        "s" => "STO",
        "r" => "RCL",
        "e" => "\u03a3+",
        "\b" => "CLX",
        " " => "R/S",
        _ => null,
      };
      if (hp29Label is not null)
      {
        return hp29Label;
      }
    }

    // HP-27 Finseth hp27a / Owner's Handbook: ŷ/x̄/%/f/g; y^x; Σ+; CLX; visible f/g CapFace.
    if (hp27)
    {
      string? hp27Label = charValue switch
      {
        "y" => "y\u0302",
        "x" => "x\u0305",
        "%" => "%",
        "f" => "f",
        "g" => "g",
        "c" => "x\u2194y",
        "d" => "R\u2193",
        "s" => "STO",
        "r" => "RCL",
        "p" => "y^x",
        "n" => "CHS",
        "e" => "EEX",
        "#" => "\u03a3+",
        "\b" => "CLX",
        _ => null,
      };
      if (hp27Label is not null)
      {
        return hp27Label;
      }
    }

    // HP-31E Finseth hp31e / Owner's Handbook: √x 1/x y^x e^x LN; gold f only (no g); % not R/S.
    if (hp31)
    {
      string? hp31Label = charValue switch
      {
        "k" => "\u221ax",
        "i" => "1/x",
        "c" => "y^x",
        "t" => "e^x",
        "g" => "LN",
        "y" => "x\u2194y",
        "d" => "R\u2193",
        "e" => "STO",
        "s" => "RCL",
        "r" => "f",
        "\b" => "CLX",
        " " => "%",
        _ => null,
      };
      if (hp31Label is not null)
      {
        return hp31Label;
      }
    }

    // HP-32E Finseth hp32e / Owner's Handbook: √x 1/x y^x f g; Σ+; CHS EEX CLX; % (not R/S).
    // Panamatik chart chars stay for binding; CapFace remaps p/v→f/g and f→Σ+.
    if (hp32)
    {
      string? hp32Label = charValue switch
      {
        "n" => "\u221ax",
        "i" => "1/x",
        "t" => "y^x",
        "p" => "f",
        "v" => "g",
        "y" => "x\u2194y",
        "d" => "R\u2193",
        "s" => "STO",
        "r" => "RCL",
        "f" => "\u03a3+",
        "h" => "CHS",
        "%" => "EEX",
        "\b" => "CLX",
        "#" => "%",
        _ => null,
      };
      if (hp32Label is not null)
      {
        return hp32Label;
      }
    }

    // HP-33C Finseth hp33e (same keyboard as 33E) / Owner's Handbook: SST GSB GTO f g; Σ+; R/S.
    if (hp33)
    {
      string? hp33Label = charValue switch
      {
        "t" => "SST",
        "b" => "GSB",
        "o" => "GTO",
        "f" => "f",
        "g" => "g",
        "y" => "x\u2194y",
        "d" => "R\u2193",
        "s" => "STO",
        "r" => "RCL",
        "#" => "\u03a3+",
        "\b" => "CLX",
        " " => "R/S",
        _ => null,
      };
      if (hp33Label is not null)
      {
        return hp33Label;
      }
    }

    // HP-34C Finseth hp34c / Owner's Handbook: A B GSB f g; h CapFace; GTO not R↓ on row2.
    if (hp34)
    {
      string? hp34Label = charValue switch
      {
        "y" => "A",
        "x" => "B",
        "%" => "GSB",
        "f" => "f",
        "g" => "g",
        "c" => "x\u2194y",
        "d" => "GTO",
        "s" => "STO",
        "r" => "RCL",
        "p" => "h",
        "n" => "CHS",
        "e" => "EEX",
        "\b" => "CLX",
        "#" => "R/S",
        _ => null,
      };
      if (hp34Label is not null)
      {
        return hp34Label;
      }
    }

    // HP-37E Finseth hp37e / Owner's Handbook: n i PV PMT FV; STO RCL % %T f; CHS x↔y CLX; Σ+ (no EEX/R/S).
    // Panamatik chart chars stay for binding; CapFace remaps t/p/v and row2/row3/%/#.
    if (hp37)
    {
      string? hp37Label = charValue switch
      {
        "n" => "n",
        "i" => "i",
        "t" => "PV",
        "p" => "PMT",
        "v" => "FV",
        "y" => "STO",
        "d" => "RCL",
        "s" => "%",
        "r" => "%T",
        "f" => "f",
        "h" => "CHS",
        "%" => "x\u2194y",
        "\b" => "CLX",
        "#" => "\u03a3+",
        _ => null,
      };
      if (hp37Label is not null)
      {
        return hp37Label;
      }
    }

    // HP-38E Finseth hp38e / Owner's Handbook: n i PV PMT FV; STO RCL % f g; CHS x↔y CL X; R/S.
    // Panamatik chart: r→f, f→g CapFace letters (indices 8/9).
    if (hp38)
    {
      string? hp38Label = charValue switch
      {
        "n" => "n",
        "i" => "i",
        "t" => "PV",
        "p" => "PMT",
        "v" => "FV",
        "y" => "STO",
        "d" => "RCL",
        "s" => "%",
        "r" => "f",
        "f" => "g",
        "h" => "CHS",
        "%" => "x\u2194y",
        "\b" => "CL X",
        "#" => "R/S",
        _ => null,
      };
      if (hp38Label is not null)
      {
        return hp38Label;
      }
    }

    return charValue switch
    {
      "k" => "1/x",
      "i" => "SIN",
      "c" => "COS",
      "t" => "TAN",
      "g" => "g",
      "y" => "x\u2194y",
      "d" => "R\u2193",
      "e" => "e^x",
      "w" => "DSP",
      "#" => "DSP",
      "%" => "%",
      _ => null,
    };
  }

  private static bool IsHp35(string? modelId) =>
    string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "35", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp45(string? modelId) =>
    string.Equals(modelId, "HP-45", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "45", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp55(string? modelId) =>
    string.Equals(modelId, "HP-55", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "55", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp67(string? modelId) =>
    string.Equals(modelId, "HP-67", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-67BE", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "67", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "67BE", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp70(string? modelId) =>
    string.Equals(modelId, "HP-70", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "70", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp80(string? modelId) =>
    string.Equals(modelId, "HP-80", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "80", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// HP-35 CapFace only. Rows 1–3 + CHS/EEX/CLX legends live on CapAbove (key.faceplate.json Gold).
  /// Index-based: chart reuses chars (c=CLR/cos, r=1/x/RCL). Classic x^y (not y^x).
  /// </summary>
  private static string? Hp35LabelFromIndex(int index, string charValue) =>
    index switch
    {
      // CapAbove-only function keys (blank CapFace).
      >= 0 and <= 14 => string.Empty,
      17 or 18 or 19 => string.Empty,
      15 or 16 => "ENTER",
      38 => "\u03c0",
      _ => ClassicDigitPadCapFace(charValue),
    };

  /// <summary>
  /// HP-45 CapFace (Finseth hp45a / Owner's Handbook). Primary on key; gold CapAbove in JSON.
  /// Index 4 = gold prefix (blank CapFace). Chart char "p" at 38 is Σ+, not DSP.
  /// </summary>
  private static string? Hp45LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "1/x",
      1 => "ln",
      2 => "e^x",
      3 => "FIX",
      4 => string.Empty, // gold prefix
      5 => "x\u00b2",
      6 => "\u2192P",
      7 => "SIN",
      8 => "COS",
      9 => "TAN",
      10 => "x\u2194y",
      11 => "R\u2193",
      12 => "STO",
      13 => "RCL",
      14 => "%",
      15 or 16 => "ENTER",
      17 => "CHS",
      18 => "EEX",
      19 => "CL X",
      38 => "\u03a3+",
      _ => ClassicDigitPadCapFace(charValue),
    };

  /// <summary>
  /// HP-55 CapFace (Finseth hp55a). Primary on key; dual CapAbove f/g in JSON.
  /// Index 4 = BST (KeyCode 0 scancode). f/g CapFace at 10/11.
  /// </summary>
  private static string? Hp55LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "\u03a3+",
      1 => "y^x",
      2 => "1/x",
      3 => "%",
      4 => "BST",
      5 => "y\u0302",
      6 => "x\u2194y",
      7 => "R\u2193",
      8 => "FIX",
      9 => "SST",
      10 => "f",
      11 => "g",
      12 => "STO",
      13 => "RCL",
      14 => "GTO",
      15 or 16 => "ENTER",
      17 => "CHS",
      18 => "EEX",
      19 => "CL X",
      38 => "R/S",
      _ => ClassicDigitPadCapFace(charValue),
    };

  /// <summary>
  /// HP-67 CapFace (Finseth hp67a). Primary on key; f/g CapBelow + h CapSkirt in JSON (no CapAbove).
  /// Chart: f,g,STO,RCL,h (not HP-65 f,h,STO,RCL,g). Do not map h→f⁻¹.
  /// </summary>
  private static string? Hp67LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "A",
      1 => "B",
      2 => "C",
      3 => "D",
      4 => "E",
      5 => "\u03a3+",
      6 => "GTO",
      7 => "DSP",
      8 => "(i)",
      9 => "SST",
      10 => "f",
      11 => "g",
      12 => "STO",
      13 => "RCL",
      14 => "h",
      15 or 16 => "ENTER",
      17 => "CHS",
      18 => "EEX",
      19 => "CL X",
      38 => "R/S",
      _ => ClassicDigitPadCapFace(charValue),
    };

  /// <summary>
  /// HP-70 CapFace (Finseth hp70a). No shift keys. Index 4 = FV (KeyCode 0 restore).
  /// </summary>
  private static string? Hp70LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "n",
      1 => "i",
      2 => "PMT",
      3 => "PV",
      4 => "FV",
      5 => "INT",
      6 => "%",
      7 => "\u0394%",
      8 => "y^x",
      9 => "CLR",
      10 => "x\u2194y",
      11 => "R\u2193",
      12 => "STO",
      13 => "K",
      14 => "DSP",
      15 or 16 => "ENTER",
      17 => "CHS",
      18 => "M",
      19 => "M+",
      38 => "CL X",
      _ => ClassicDigitPadCapFace(charValue),
    };

  /// <summary>
  /// HP-80 CapFace (Finseth hp80a / HP Journal). Gold blank CapFace at index 5; SAVE not ENTER.
  /// Panamatik chart chars are HP-45 clones — labels are index-based.
  /// </summary>
  private static string? Hp80LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "n",
      1 => "i",
      2 => "PMT",
      3 => "PV",
      4 => "FV",
      5 => string.Empty, // gold prefix
      6 => "%",
      7 => "TL",
      8 => "SOD",
      9 => "DAY",
      10 => "x\u2194y",
      11 => "R\u2193",
      12 => "STO",
      13 => "y^x",
      14 => "x\u0305",
      15 or 16 => "SAVE",
      17 => "RCL",
      18 => "CHS",
      19 => "CL X",
      38 => "\u03a3+",
      _ => ClassicDigitPadCapFace(charValue),
    };

  private static string? ClassicDigitPadCapFace(string charValue) =>
    charValue switch
    {
      "*" => "\u00d7",
      "/" => "\u00f7",
      "." => "\u00b7",
      "-" or "+" => charValue,
      _ when charValue.Length == 1 && char.IsDigit(charValue[0]) => charValue,
      _ => null,
    };

  /// <summary>
  /// HP-01 face legends (Owner's Guide keyboard summary).
  /// Panamatik <c>Char</c> codes stay for firmware/chart binding; this maps them to printed labels.
  /// Grid: R 0 1 2 3 4 S / . 5 6 7 8 9 C / : + - × ÷ = p / D / A Δ M % T
  /// </summary>
  private static string? Hp01LabelFromChar(string charValue) =>
    charValue switch
    {
      "R" => "R",
      "S" => "S",
      "P" => "p", // PM
      "D" => "D",
      "T" => "T",
      "A" => "A", // Alarm
      "M" => "M", // Memory
      "C" => "%", // Percent (Panamatik chart char C at index 26)
      "a" => "C", // Clear (Panamatik chart char a at index 13)
      "d" => "\u2206", // Shift / store (∆)
      ":" => ":",
      ";" => "/", // Date field separator (row 4 slash)
      "*" => "\u00d7",
      "/" => "\u00f7", // Operator-row classic ÷
      "=" => "=",
      "." => ".",
      _ => null,
    };

  /// <summary>
  /// HP-19C CapFace legends. Chart chars stay for firmware; printed labels differ from Classic.
  /// </summary>
  private static string? Hp19CLabelFromChar(string charValue) =>
    charValue switch
    {
      "y" => "x\u2194y",
      "d" => "R\u2193",
      "b" => "GSB",
      "o" => "GTO",
      "t" => "SST",
      "e" => "\u03a3+",
      "p" => "PRx",
      _ => null,
    };

  public static Vector2 PanelSize(IReadOnlyList<FaceplateCell> cells, CalcChassisMetrics metrics)
  {
    BodyFaceplateLayout.EnsureLoaded();
    float maxX = 0f;
    float maxY = 0f;
    RectF band = BodyFaceplateLayout.CardSlotBand;
    foreach (FaceplateCell cell in cells)
    {
      if (!BodyFaceplateLayout.TryGetKeyRect(cell.KeyChartIndex, out RectF rect))
      {
        continue;
      }

      maxX = Math.Max(maxX, rect.X + rect.Width);
      maxY = Math.Max(maxY, rect.Y + rect.Height);
    }

    return new Vector2(maxX - band.X, maxY - band.Y + band.Height);
  }
}
