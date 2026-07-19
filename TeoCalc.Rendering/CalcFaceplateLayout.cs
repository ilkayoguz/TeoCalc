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

  /// <summary>HP-35/45/55/70 share Classic map but omit blank top-right key (chart index 4).</summary>
  private static readonly FaceplateCell[] ClassicPhysicalCellsNoTopRight =
    ClassicPhysicalCells.Where(cell => cell.KeyChartIndex != 4).ToArray();

  public static int ToIndex(int row, int column) => row * Columns + column;

  public static IReadOnlyList<FaceplateCell> GetPhysicalCells(string family, string? modelId = null)
  {
    if (IsClassicSparseTopRight(modelId))
    {
      return ClassicPhysicalCellsNoTopRight;
    }

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

  private static bool IsClassicSparseTopRight(string? modelId) =>
    modelId is not null
    && modelId.ToUpperInvariant() is "HP-35" or "HP-45" or "HP-55" or "HP-70"
      or "35" or "45" or "55" or "70";

  public static string LabelForKey(
    ProgramKeyEntry key,
    ProgramVocabulary? vocabulary,
    string? family = null,
    string? modelId = null)
  {
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

    if (string.Equals(family, "Classic", StringComparison.OrdinalIgnoreCase)
        && IsHp35(modelId))
    {
      string? hp35 = Hp35LabelFromIndex(key.Index, key.Char);
      if (hp35 is not null)
      {
        return hp35;
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

  /// <summary>
  /// HP-35 CapFace (Finseth hp35a). Index-based: chart reuses chars (c=CLR/cos, r=1/x/RCL).
  /// Classic x^y (not y^x). No shift legends.
  /// </summary>
  private static string? Hp35LabelFromIndex(int index, string charValue) =>
    index switch
    {
      0 => "x^y",
      1 => "log",
      2 => "ln",
      3 => "e^x",
      4 => "CLR",
      5 => "\u221ax",
      6 => "arc",
      7 => "sin",
      8 => "cos",
      9 => "tan",
      10 => "1/x",
      11 => "x\u2194y",
      12 => "R\u2193",
      13 => "STO",
      14 => "RCL",
      15 or 16 => "ENTER",
      17 => "CHS",
      18 => "EEX",
      19 => "CLX",
      38 => "\u03c0",
      _ => charValue switch
      {
        "*" => "\u00d7",
        "/" => "\u00f7",
        "." => "\u00b7",
        "-" or "+" => charValue,
        _ when charValue.Length == 1 && char.IsDigit(charValue[0]) => charValue,
        _ => null,
      },
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
