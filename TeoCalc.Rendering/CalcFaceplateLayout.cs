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

/// <summary>HP Classic faceplate cell map; CapFace text lives in key.faceplate.json.</summary>
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
        || string.Equals(modelId, "HP-67", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Classic", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Hp67", StringComparison.OrdinalIgnoreCase))
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
        || string.Equals(family, "HP19C", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Hp19", StringComparison.OrdinalIgnoreCase))
    {
      return Hp19CFaceplateLayout.PhysicalCells;
    }

    return Enumerable.Range(0, Rows * Columns)
      .Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .Where(cell => cell.KeyChartIndex is >= 0)
      .ToArray();
  }

  /// <summary>
  /// CapFace from key.faceplate.json when present; otherwise digit-pad / chart-char fallback.
  /// Model-specific remaps live in JSON — not in C#.
  /// </summary>
  public static string LabelForKey(
    ProgramKeyEntry key,
    ProgramVocabulary? vocabulary,
    string? family = null,
    string? modelId = null)
  {
    _ = vocabulary;
    _ = family;

    if (!string.IsNullOrWhiteSpace(modelId)
        && ClassicKeyFaceplateLegend.TryGetEntryPublic(modelId, key.Index) is { CapFace: { } capFace })
    {
      return capFace;
    }

    // Family-only callers (tests): map to engine catalog id for CapFace lookup.
    string? inferredId = InferModelId(family, modelId);
    if (!string.IsNullOrWhiteSpace(inferredId)
        && !string.Equals(inferredId, modelId, StringComparison.OrdinalIgnoreCase)
        && ClassicKeyFaceplateLegend.TryGetEntryPublic(inferredId, key.Index) is { CapFace: { } inferredCap })
    {
      return inferredCap;
    }

    if (key.KeyCode == 0)
    {
      return string.Empty;
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
    _ = family;
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

  /// <summary>Shared digit-pad / chart-char fallback when CapFace is absent from JSON.</summary>
  public static string? PrimaryLabelFromChar(string charValue)
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

  private static string? InferModelId(string? family, string? modelId)
  {
    if (!string.IsNullOrWhiteSpace(modelId))
    {
      return modelId;
    }

    // Only unambiguous single-model families.
    return family switch
    {
      "HP01" => "HP-01",
      "HP19C" => "HP-19C",
      _ => null,
    };
  }

  public static Vector2 PanelSize(IReadOnlyList<FaceplateCell> cells, CalcChassisMetrics metrics)
  {
    _ = metrics;
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
