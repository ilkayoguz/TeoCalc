namespace TeoCalc.Rendering;

public static class Teo01FaceplateLayout
{
  public const int Rows = 4;

  public const int Columns = 7;

  public static readonly FaceplateCell[] PhysicalCells =
    Enumerable.Range(0, 28)
      .Select(index => new FaceplateCell(index, index / Columns, index % Columns))
      .ToArray();
}
