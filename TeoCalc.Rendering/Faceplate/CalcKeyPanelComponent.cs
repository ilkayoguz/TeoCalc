using System.Numerics;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Keypad band on the face band. Full rows use a column grid; sparse rows keep original
/// key sizes and distribute leftover space as equal gaps (L/R edges stay on the band).
/// Slot height = TopLabel + cap (Label + CapSkirt etek) + BottomLabel.
/// Column count follows the cell map (5 for Classic, 7 for HP-01, 6 for HP-19C, …).
/// </summary>
public static class CalcKeyPanelComponent
{
  /// <summary>
  /// Gap from the switch panel to the keypad slot. Negative to offset the keypad's reserved
  /// top gutter + first-row top-label band (10 + 12), so the top row's caps sit as close to the
  /// switch as the other insets. Kept at -Gutter so the label band just touches the switch bottom.
  /// </summary>
  public const float GapBelowSwitchRef = -10f;

  public const float GapAboveLogoRef = 8f;

  /// <summary>Gutter between keys and for top/bottom keypad padding.</summary>
  public const float GutterRef = 10f;

  /// <summary>Full cap height (face Label + CapSkirt etek). Outside labels are extra.</summary>
  public const float PreferredCapHeightRef = 58f;

  public const float PreferredCellWidthRef = 88f;

  /// <summary>TopLabel band above the cap (f / CapAbove).</summary>
  public const float LabelAboveRef = 14f;

  /// <summary>BottomLabel band below the cap (CapBelow only). CapSkirt is inside the cap.</summary>
  public const float LabelBelowRef = 0f;

  /// <summary>Classic HP-65 column count (reference); prefer <see cref="CountColumns"/>.</summary>
  public const int Columns = 5;

  public readonly record struct PanelMetrics(
    float Width,
    float Height,
    float CellWidth,
    float CellHeight,
    float CapHeight,
    float LabelAbove,
    float LabelBelow,
    float Gutter,
    int ColumnCount);

  public static int CountRows(IReadOnlyList<FaceplateCell> cells)
  {
    int extent = 0;
    foreach (FaceplateCell cell in cells)
    {
      extent = Math.Max(extent, cell.Row + Math.Max(1, cell.RowSpan));
    }

    return Math.Max(1, extent);
  }

  public static int CountColumns(IReadOnlyList<FaceplateCell> cells)
  {
    int extent = 0;
    foreach (FaceplateCell cell in cells)
    {
      extent = Math.Max(extent, cell.Column + Math.Max(1, cell.ColSpan));
    }

    return Math.Max(1, extent);
  }

  /// <summary>Row pitch = above label + cap + below label.</summary>
  public static float RowPitch(PanelMetrics metrics) =>
    metrics.LabelAbove + metrics.CapHeight + metrics.LabelBelow;

  public static PanelMetrics Measure(
    IReadOnlyList<FaceplateCell> cells,
    int skipTopRows = 0,
    bool includeTopGutter = true)
  {
    int rows = Math.Max(0, CountRows(cells) - Math.Max(0, skipTopRows));
    int cols = CountColumns(cells);
    float g = GutterRef;
    float cellW = PreferredCellWidthRef;
    float capH = PreferredCapHeightRef;
    float above = LabelAboveRef;
    float below = LabelBelowRef;
    float rowH = above + capH + below;
    float width = cols * cellW + (cols - 1) * g;
    float topGutter = includeTopGutter ? g : 0f;
    float height = rows == 0
      ? 0f
      : topGutter + g + rows * rowH + (rows - 1) * g;
    return new PanelMetrics(width, height, cellW, rowH, capH, above, below, g, cols);
  }

  public static RectF ResolveSlotRef(float bandLeft, float bandTop, PanelMetrics metrics) =>
    new(bandLeft, bandTop, metrics.Width, metrics.Height);

  /// <summary>
  /// Full-width rows keep column geometry; shorter rows are justified L→R across the band.
  /// <paramref name="skipTopRows"/> omits leading rows (e.g. A–E owned by the card plate).
  /// </summary>
  public static Dictionary<int, RectF> BuildKeySlots(
    RectF panel,
    IReadOnlyList<FaceplateCell> cells,
    PanelMetrics metrics,
    int skipTopRows = 0,
    bool includeTopGutter = true)
  {
    Dictionary<int, RectF> slots = new();
    if (cells.Count == 0)
    {
      return slots;
    }

    float g = metrics.Gutter;
    float rowPitch = RowPitch(metrics);
    float originY = panel.Y + (includeTopGutter ? g : 0f);
    int skip = Math.Max(0, skipTopRows);

    foreach (IGrouping<int, FaceplateCell> rowGroup in cells.GroupBy(cell => cell.Row).OrderBy(group => group.Key))
    {
      int visualRow = rowGroup.Key - skip;
      if (visualRow < 0)
      {
        continue;
      }

      List<FaceplateCell> rowCells = rowGroup.OrderBy(cell => cell.Column).ToList();
      float rowY = originY + visualRow * (rowPitch + g);
      PlaceRow(slots, rowCells, panel.X, rowY, panel.Width, metrics, g);
    }

    return slots;
  }

  private static void PlaceRow(
    Dictionary<int, RectF> slots,
    List<FaceplateCell> rowCells,
    float rowLeft,
    float rowY,
    float rowWidth,
    PanelMetrics metrics,
    float g)
  {
    int units = 0;
    foreach (FaceplateCell cell in rowCells)
    {
      units += Math.Max(1, cell.ColSpan);
    }

    bool sparse = units < metrics.ColumnCount;
    if (!sparse)
    {
      foreach (FaceplateCell cell in rowCells)
      {
        float spanW = metrics.CellWidth * cell.ColSpan + g * Math.Max(0, cell.ColSpan - 1);
        float spanH = RowSlotHeight(metrics, cell.RowSpan, g);
        float cx = rowLeft + cell.Column * (metrics.CellWidth + g);
        slots[cell.KeyChartIndex] = new RectF(cx, rowY, spanW, spanH);
      }

      return;
    }

    // Sparse row: keep original key widths; distribute leftover space as equal inter-key gaps
    // so the first key stays on the left edge and the last on the right.
    float totalKeyW = 0f;
    Span<float> widths = stackalloc float[rowCells.Count];
    for (int i = 0; i < rowCells.Count; i++)
    {
      FaceplateCell cell = rowCells[i];
      int span = Math.Max(1, cell.ColSpan);
      widths[i] = metrics.CellWidth * span + g * Math.Max(0, span - 1);
      totalKeyW += widths[i];
    }

    int gapCount = Math.Max(1, rowCells.Count - 1);
    float free = MathF.Max(0f, rowWidth - totalKeyW);
    float interGap = free / gapCount;

    float x = rowLeft;
    for (int i = 0; i < rowCells.Count; i++)
    {
      FaceplateCell cell = rowCells[i];
      float spanH = RowSlotHeight(metrics, cell.RowSpan, g);
      slots[cell.KeyChartIndex] = new RectF(x, rowY, widths[i], spanH);
      x += widths[i] + interGap;
    }
  }

  private static float RowSlotHeight(PanelMetrics metrics, int rowSpan, float g)
  {
    int span = Math.Max(1, rowSpan);
    return metrics.LabelAbove
      + metrics.CapHeight * span
      + metrics.LabelBelow
      + g * Math.Max(0, span - 1);
  }
}
