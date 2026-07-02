namespace TeoCalc.Rendering;

public static class CalcKeyLabelPalette
{
  /// <summary>Gold shift labels on body above keys — matches KeyCap gold face paint.</summary>
  public static uint GoldOnBody => CalcChassisPalette.KeyCapGoldFace;

  /// <summary>Blue shift labels on key skirt — visible on dark cap skirts.</summary>
  public static uint BlueOnSkirt => CalcChassisPalette.BlueLabel;

  /// <summary>White comparison / shift labels on black key skirts (DSP row).</summary>
  public static uint WhiteOnSkirt => CalcChassisPalette.KeyText;

  public static uint SkirtLabelInk(string? label, CalcButtonStyle style) =>
    style == CalcButtonStyle.Black && IsWhiteSkirtLabel(label)
      ? WhiteOnSkirt
      : BlueOnSkirt;

  public static float SkirtLabelFontScale(string? label)
  {
    if (IsWhiteSkirtLabel(label))
    {
      return 1.08f;
    }

    if (label is "x\u2194y")
    {
      return 1.12f;
    }

    if (label is not null && CompactBlueSkirtLabels.Contains(label))
    {
      return 0.93f;
    }

    return 1f;
  }

  private static readonly HashSet<string> CompactBlueSkirtLabels = new(StringComparer.Ordinal)
  {
    "DEG",
    "RAD",
    "GRD",
    "DEL",
    "ABS",
    "NOP",
    "LST X",
    "DSZ",
  };

  public static bool IsWhiteSkirtLabel(string? label) =>
    label is "x\u2260y" or "x\u2264y" or "x=y" or "x>y";

  public static uint PrimaryOnCap(CalcButtonStyle style) =>
    style switch
    {
      CalcButtonStyle.Blue => CalcChassisPalette.KeyCapDarkText,
      CalcButtonStyle.Black => CalcChassisPalette.KeyText,
      _ => CalcChassisPalette.KeyCapDarkText,
    };

  public static uint SkirtOnCap(CalcButtonStyle style) =>
    style is CalcButtonStyle.Black or CalcButtonStyle.Blue
      ? CalcChassisPalette.KeyText
      : CalcChassisPalette.KeyCapDarkText;
}
