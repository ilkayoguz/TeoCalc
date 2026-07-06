namespace TeoCalc.Rendering.Faceplate;

/// <summary>Delegates to the active <see cref="CalcFaceplateTheme"/>.</summary>
public static class CalcKeyColorPalette
{
  public static CalcColorToken LabelOnDarkSurface { get; } = CalcColorToken.From(CalcFaceplateTokens.LabelOnDarkSurfaceColor);

  public static CalcColorToken LabelOnLightCap { get; } = CalcColorToken.From(CalcFaceplateTokens.LabelOnLightCapColor);

  public static CalcColorToken LabelOnDarkCap { get; } = CalcColorToken.From(CalcFaceplateTokens.LabelOnDarkCapColor);

  public static CalcColorToken ModifierFOnCapAbove { get; } = CalcColorToken.From(CalcFaceplateTokens.ModifierFCapAboveColor);

  public static CalcColorToken ModifierGOnCapSkirt { get; } = CalcColorToken.From(CalcFaceplateTokens.ModifierGCapSkirtColor);

  public static CalcColorToken ModifierHOnCapFace { get; } = CalcColorToken.From(CalcFaceplateTokens.ModifierHCapFaceColor);

  public static CalcColorToken LogoMark { get; } = CalcColorToken.From(CalcFaceplateTokens.LogoMarkColor);

  public static CalcColorToken LogoCaption { get; } = CalcColorToken.From(CalcFaceplateTokens.LogoCaptionColor);

  public static CalcColorToken LogoStrip { get; } = CalcColorToken.From(CalcFaceplateTokens.LogoStripColor);

  public static uint Resolve(CalcColorToken token, CalcModelDefinition? model = null) =>
    CalcFaceplateTheme.Resolve(token, model);
}
