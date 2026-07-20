namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Space-saving dual-ink CapAbove/CapBelow composites (HP-34C trig −1; HP-55 H.MS± and unit
/// conversions; HP-67 CapBelow R→/←P). When matched, Gold/Blue share one centered band instead of
/// left/right split legends.
/// </summary>
public static class CalcCapAboveComposite
{
  public const char ArrowRight = '\u2192';
  public const char ArrowLeft = '\u2190';

  public static bool IsInverseSuffixOnly(string? text) =>
    text is "^-1" or "\u207b\u00b9" or "⁻¹" or "-1";

  /// <summary>True when CapAbove should draw gold base + blue −1 as one dual-ink unit (not left/right split).</summary>
  public static bool IsSpaceSavingInverse(string? gold, string? blue) =>
    !string.IsNullOrEmpty(gold) && IsInverseSuffixOnly(blue);

  /// <summary>
  /// HP-55 ENTER: gold <c>H.MS</c> with stacked gold <c>+</c> / blue <c>−</c> beside it.
  /// JSON: Gold=<c>H.MS</c>, Blue=<c>+-</c> (also accepts legacy <c>H.MS+</c>/<c>H.MS-</c>).
  /// </summary>
  public static bool IsSpaceSavingHmsPlusMinus(string? gold, string? blue) =>
    (gold is "H.MS" && IsHmsSignPairSuffix(blue))
    || (gold is "H.MS+" && blue is "H.MS-");

  /// <summary>
  /// HP-55 digit-pad unit conversions: Gold=<c>in→</c>, Blue=<c>←mm</c> (Finseth pairs).
  /// Drawn as gold left unit + dual-ink arrow stack + blue right unit.
  /// </summary>
  public static bool IsSpaceSavingUnitConversion(string? gold, string? blue) =>
    TryParseUnitConversionPair(gold, blue, out _, out _);

  /// <summary>Any CapAbove/CapBelow dual-ink composite that must not left/right-split.</summary>
  public static bool IsSpaceSavingDualInk(string? gold, string? blue) =>
    IsSpaceSavingInverse(gold, blue)
    || IsSpaceSavingHmsPlusMinus(gold, blue)
    || IsSpaceSavingUnitConversion(gold, blue);

  public static bool TryParseUnitConversionPair(
    string? gold,
    string? blue,
    out string leftUnit,
    out string rightUnit)
  {
    leftUnit = string.Empty;
    rightUnit = string.Empty;
    if (string.IsNullOrEmpty(gold) || string.IsNullOrEmpty(blue))
    {
      return false;
    }

    if (gold.Length < 2 || gold[^1] != ArrowRight)
    {
      return false;
    }

    if (blue.Length < 2 || blue[0] != ArrowLeft)
    {
      return false;
    }

    leftUnit = gold[..^1];
    rightUnit = blue[1..];
    return leftUnit.Length > 0 && rightUnit.Length > 0;
  }

  /// <summary>g-preview face text when Blue is only <c>^-1</c> — compose with Gold base.</summary>
  public static string ComposeInversePreviewFace(string goldBase, string blueSuffix) =>
    IsInverseSuffixOnly(blueSuffix) ? goldBase + "^-1" : blueSuffix;

  public static string ComposeHmsPlusMinusPreviewFace(string? gold, string? blue, bool blueShift) =>
    blueShift ? "H.MS-" : "H.MS+";

  public static string ComposeUnitConversionPreviewFace(string? gold, string? blue, bool blueShift)
  {
    if (!TryParseUnitConversionPair(gold, blue, out string left, out string right))
    {
      return blueShift ? blue ?? string.Empty : gold ?? string.Empty;
    }

    return blueShift ? $"{ArrowLeft}{right}" : $"{left}{ArrowRight}";
  }

  private static bool IsHmsSignPairSuffix(string? blue) =>
    blue is "+-" or "+/-" or "+/−" or "\u00b1" or "±";
}
