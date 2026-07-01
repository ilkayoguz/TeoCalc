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

/// <summary>HP Classic faceplate layout (HP-65 hp65_470.png reference).</summary>
public static class CalcFaceplateLayout
{
  public const int Rows = 8;

  public const int Columns = 5;

  public static readonly string[] CardSlotLabels = ["1/x", "\u221ax", "y^x", "R\u2193", "x\u2194y"];

  public static readonly Vector2 OnOffSwitchNorm = new(155f / CalcChassisGeometry.ReferenceWidth, 175f / CalcChassisGeometry.ReferenceHeight);

  public static readonly Vector2 PrgmRunSwitchNorm = new(320f / CalcChassisGeometry.ReferenceWidth, 175f / CalcChassisGeometry.ReferenceHeight);

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
      "\b" => "CLx",
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
    int maxRow = 0;
    int maxColumn = 0;
    foreach (FaceplateCell cell in cells)
    {
      maxRow = Math.Max(maxRow, cell.Row + cell.RowSpan);
      maxColumn = Math.Max(maxColumn, cell.Column + cell.ColSpan);
    }

    return new Vector2(
      maxColumn * metrics.KeyWidth + Math.Max(0, maxColumn - 1) * metrics.KeyGapH,
      metrics.CardSlotBand + metrics.GoldBand + maxRow * metrics.RowPitch);
  }
}
