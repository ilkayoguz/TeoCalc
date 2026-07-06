using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

public sealed class CalcThemePack
{
  public required string Id { get; init; }

  public required string DisplayName { get; init; }

  public required ThemePalette Palette { get; init; }
}
