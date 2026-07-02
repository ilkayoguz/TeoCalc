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

  public static readonly string[] CardSlotLabels = ["1/x", "\u221ax", "R\u2191", "R\u2193", "x\u2194y"];

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

    return Enumerable.Range(0, Rows * Columns)
      .Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .Where(cell => cell.KeyChartIndex is >= 0)
      .ToArray();
  }

  public static string LabelForKey(ProgramKeyEntry key, ProgramVocabulary? vocabulary)
  {
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

  public static CalcButtonKind ButtonKindForKey(ProgramKeyEntry key, FaceplateCell cell)
  {
    if (cell.ColSpan >= 2 && key.Char == "\r")
    {
      return CalcButtonKind.EnterWide;
    }

    return key.Char switch
    {
      "/" => CalcButtonKind.OperatorColon,
      _ => CalcButtonKind.Standard,
    };
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
      "-" or "+" or "." => charValue,
      _ when charValue.Length == 1 && char.IsDigit(charValue[0]) => charValue,
      _ => null,
    };
  }

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
