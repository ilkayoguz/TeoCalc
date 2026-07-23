using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcKeyLabelPalette
{
  /// <summary>Gold shift labels on body above keys — matches KeyCap gold face paint.</summary>
  public static uint GoldOnBody => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ModifierFCapAboveColor);

  public static uint BlueOnSkirt => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ModifierGCapSkirtColor);

  public static uint SkirtBlueDark => CalcFaceplateTheme.Resolve(CalcFaceplateTokens.SkirtBlueDarkColor);

  public static uint GoldOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue or CalcButtonStyle.DarkGrey
      ? GoldOnBody
      : CalcChassisPalette.KeyOrangeSkirt;

  public static uint BlueOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue or CalcButtonStyle.DarkGrey
      ? BlueOnSkirt
      : SkirtBlueDark;

  /// <summary>T-27 prints g-shift legends in black ink (g prefix key is black, not blue).</summary>
  public static bool UsesBlackGShiftSkirtInk(string? modelId) =>
    CalcModelIds.IsEngine(modelId, "T-27");

  /// <summary>
  /// T-67 CapSkirt (h) is always black ink — including Olive body keys.
  /// CapFace olive stays light; skirt-only override.
  /// </summary>
  public static bool UsesBlackHShiftSkirtInk(string? modelId) =>
    CalcModelIds.IsEngine(modelId, "T-67");

  /// <summary>
  /// HP-34C h CapSkirt: light ink on black/blue keys, dark ink on white/orange (same contrast as CapFace).
  /// Do not force KeyCapDarkText — that is black-on-black on Spice black keys.
  /// HP-67: always <see cref="CalcChassisPalette.KeyCapDarkText"/> (see <see cref="UsesBlackHShiftSkirtInk"/>).
  /// </summary>
  public static uint HShiftSkirtInk(CalcButtonStyle style) => HShiftSkirtInk(style, modelId: null);

  public static uint HShiftSkirtInk(CalcButtonStyle style, string? modelId) =>
    UsesBlackHShiftSkirtInk(modelId)
      ? CalcChassisPalette.KeyCapDarkText
      : SkirtOnCap(style);

  public static uint SkirtLabelInk(string? label, CalcButtonStyle style) =>
    SkirtLabelInk(label, style, modelId: null);

  public static uint SkirtLabelInk(string? label, CalcButtonStyle style, string? modelId)
  {
    if (UsesBlackGShiftSkirtInk(modelId))
    {
      return CalcChassisPalette.KeyCapDarkText;
    }

    if (IsProgramRowComparisonLabel(label))
    {
      return style is CalcButtonStyle.Black or CalcButtonStyle.Blue or CalcButtonStyle.DarkGrey
        ? BlueOnSkirt
        : SkirtBlueDark;
    }

    return style is CalcButtonStyle.Black or CalcButtonStyle.Blue or CalcButtonStyle.DarkGrey
      ? BlueOnSkirt
      : SkirtBlueDark;
  }

  /// <summary>CapFace ink when a g-shift label is promoted during blue shift preview.</summary>
  public static uint GShiftPreviewFaceInk(CalcButtonStyle style, string? modelId) =>
    UsesBlackGShiftSkirtInk(modelId) ? CalcChassisPalette.KeyCapDarkText : BlueOnCap(style);

  // CapSkirt font stays smaller than CapFace; mild per-label ticks only.
  private const float SkirtScaleBase = 1.0f;
  private const float SkirtAsciiTick = 0.02f;
  private const float SkirtMathTick = 0.08f;

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
    || (label is not null && label.Length <= 4 && ClassicFaceplateGlyphs.IsPlainArialSkirtLabel(label));

  public static bool IsProgramRowComparisonLabel(string? label) =>
    label is "x\u2260y" or "x\u2264y" or "x\u2265y" or "x=y" or "x>y";

  public static uint PrimaryOnCap(CalcButtonStyle style) =>
    style switch
    {
      CalcButtonStyle.Blue => CalcChassisPalette.KeyCapDarkText,
      CalcButtonStyle.Black => CalcChassisPalette.KeyText,
      CalcButtonStyle.DarkGrey => CalcChassisPalette.KeyText,
      // Olive (HP-67 body) / Cement (HP-80 SlateGray #708090) are light-on-dark like Black.
      CalcButtonStyle.Olive => CalcChassisPalette.KeyText,
      CalcButtonStyle.Cement => CalcChassisPalette.KeyText,
      CalcButtonStyle.White => CalcChassisPalette.KeyCapDarkText,
      CalcButtonStyle.LightGrey => CalcChassisPalette.KeyCapDarkText,
      _ => CalcChassisPalette.KeyCapDarkText,
    };

  public static uint SkirtOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue or CalcButtonStyle.DarkGrey
      or CalcButtonStyle.Olive or CalcButtonStyle.Cement
      ? CalcChassisPalette.KeyText
      : CalcChassisPalette.KeyCapDarkText;
}
