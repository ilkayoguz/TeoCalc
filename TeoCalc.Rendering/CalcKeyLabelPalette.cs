using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcKeyLabelPalette
{
  /// <summary>Gold shift labels on body above keys — matches KeyCap gold face paint.</summary>
  public static uint GoldOnBody => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ModifierFCapAboveColor);

  public static uint BlueOnSkirt => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ModifierGCapSkirtColor);

  public static uint SkirtBlueDark => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.SkirtBlueDarkColor);

  public static uint GoldOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue
      ? GoldOnBody
      : CalcChassisPalette.KeyOrangeSkirt;

  public static uint BlueOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue
      ? BlueOnSkirt
      : SkirtBlueDark;

  public static uint SkirtLabelInk(string? label, CalcButtonStyle style)
  {
    if (IsProgramRowComparisonLabel(label))
    {
      return style is CalcButtonStyle.Black or CalcButtonStyle.Blue
        ? BlueOnSkirt
        : SkirtBlueDark;
    }

    return style is CalcButtonStyle.Black or CalcButtonStyle.Blue
      ? BlueOnSkirt
      : SkirtBlueDark;
  }

  // Blue skirt tiers: base + 1 tick (ASCII) or + 2 ticks (math glyphs), kept close in size.
  private const float SkirtScaleBase = 1.20f;
  private const float SkirtAsciiTick = 0.04f;
  private const float SkirtMathTick = 0.30f;

  public static float BlueSkirtFontScale(string? label)
  {
    if (IsMathBlueSkirtLabel(label))
    {
      return SkirtScaleBase + SkirtMathTick;
    }

    if (IsAsciiBlueSkirtLabel(label))
    {
      return SkirtScaleBase + SkirtAsciiTick;
    }

    return SkirtScaleBase;
  }

  private static bool IsMathBlueSkirtLabel(string? label) =>
    IsProgramRowComparisonLabel(label)
    || label is "1/x"
      or "y^x"
      or "x\u2194y"
      or "R\u2191"
      or "R\u2193"
      or "\u03c0"
      or "n!"
      or "ABS"
      or "LST X";

  private static bool IsAsciiBlueSkirtLabel(string? label) =>
    label is "DEG" or "RAD" or "GRD" or "DEL" or "NOP" or "DSZ"
    || (label is not null && label.Length <= 4 && HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel(label));

  public static bool IsProgramRowComparisonLabel(string? label) =>
    label is "x\u2260y" or "x\u2264y" or "x=y" or "x>y";

  public static uint PrimaryOnCap(CalcButtonStyle style) =>
    style switch
    {
      CalcButtonStyle.Blue => CalcChassisPalette.KeyCapDarkText,
      CalcButtonStyle.Black => CalcChassisPalette.KeyText,
      CalcButtonStyle.White => CalcChassisPalette.KeyCapDarkText,
      _ => CalcChassisPalette.KeyCapDarkText,
    };

  public static uint SkirtOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue
      ? CalcChassisPalette.KeyText
      : CalcChassisPalette.KeyCapDarkText;
}
