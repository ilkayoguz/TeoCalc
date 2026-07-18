namespace TeoCalc.Rendering;

public static class Hp19CFaceplateLayout
{
  public const int Rows = 6;

  public const int Columns = 6;

  /// <summary>Blank chart indices (KeyCode 0) sit in the last column of rows 2–5.</summary>
  private static readonly HashSet<int> BlankKeys = [17, 23, 29, 35];

  public static readonly FaceplateCell[] PhysicalCells =
    Enumerable.Range(0, 36)
      .Where(index => !BlankKeys.Contains(index))
      .Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .ToArray();
}
