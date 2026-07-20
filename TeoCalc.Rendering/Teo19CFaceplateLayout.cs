namespace TeoCalc.Rendering;

public static class Teo19CFaceplateLayout
{
  public const int Rows = 6;

  public const int Columns = 6;

  /// <summary>
  /// Blank chart slots (KeyCode 0) plus the second half of wide ENTER (index 7 shares keycode with 6).
  /// </summary>
  private static readonly HashSet<int> OmittedKeys = [7, 17, 23, 29, 35];

  public static readonly FaceplateCell[] PhysicalCells = BuildCells();

  private static FaceplateCell[] BuildCells()
  {
    List<FaceplateCell> cells = [];
    for (int index = 0; index < Rows * Columns; index++)
    {
      if (OmittedKeys.Contains(index))
      {
        continue;
      }

      int row = index / Columns;
      int col = index % Columns;

      // Wide ENTER↑ occupies columns 0–1 on row 1.
      if (index == 6)
      {
        cells.Add(new FaceplateCell(6, 1, 0, ColSpan: 2));
        continue;
      }

      if (index is >= 8 and <= 11)
      {
        // Shift left by one column because ENTER ate column 1.
        cells.Add(new FaceplateCell(index, 1, index - 6));
        continue;
      }

      cells.Add(new FaceplateCell(index, row, col));
    }

    return cells.ToArray();
  }
}
