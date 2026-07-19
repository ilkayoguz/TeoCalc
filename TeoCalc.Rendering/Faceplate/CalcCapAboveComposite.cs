namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// HP-34C space-saving CapAbove: shared gold base (SIN/COS/TAN) + blue superscript −1.
/// Finseth hp34c lists g as <c>-1</c> above the same SIN/COS/TAN band — not a separate full-word right legend.
/// </summary>
public static class CalcCapAboveComposite
{
  public static bool IsInverseSuffixOnly(string? text) =>
    text is "^-1" or "\u207b\u00b9" or "⁻¹" or "-1";

  /// <summary>True when CapAbove should draw gold base + blue −1 as one dual-ink unit (not left/right split).</summary>
  public static bool IsSpaceSavingInverse(string? gold, string? blue) =>
    !string.IsNullOrEmpty(gold) && IsInverseSuffixOnly(blue);

  /// <summary>g-preview face text when Blue is only <c>^-1</c> — compose with Gold base.</summary>
  public static string ComposeInversePreviewFace(string goldBase, string blueSuffix) =>
    IsInverseSuffixOnly(blueSuffix) ? goldBase + "^-1" : blueSuffix;
}
