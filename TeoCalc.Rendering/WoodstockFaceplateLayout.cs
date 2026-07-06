using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

public static class WoodstockFaceplateLayout
{
  public const int Rows = 7;

  public const int Columns = 5;

  private static readonly FaceplateCell[] Hp21PhysicalCells =
  [
    new(0, 0, 0), new(1, 0, 1), new(2, 0, 2), new(3, 0, 3), new(4, 0, 4),
    new(5, 1, 0), new(6, 1, 1), new(7, 1, 2), new(8, 1, 3), new(9, 1, 4),
    new(10, 2, 0, ColSpan: 2), new(12, 2, 2), new(13, 2, 3), new(14, 2, 4),
    new(15, 3, 0), new(16, 3, 1), new(17, 3, 2), new(18, 3, 3),
    new(20, 4, 0), new(21, 4, 1), new(22, 4, 2), new(23, 4, 3),
    new(25, 5, 0), new(26, 5, 1), new(27, 5, 2), new(28, 5, 3),
    new(30, 6, 0), new(31, 6, 1), new(32, 6, 2), new(33, 6, 3),
  ];

  public static IReadOnlyList<FaceplateCell> GetPhysicalCells(string? modelId = null) =>
    Hp21PhysicalCells;
}
