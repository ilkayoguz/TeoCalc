namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Maps prefix modifiers (F/G/H/…) to label slots (CapAbove/CapFace/CapSkirt/CapBelow).
/// Placement is per-model via <see cref="CalcModelDefinition.AnnotationStyles"/> — not hardcoded.
/// </summary>
public static class CalcModifierPlacement
{
  /// <summary>Classic HP-65/67 style: F above, F-inverse on face, G on skirt.</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> ClassicFg { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.F, CalcLabelAnchor.CapFace, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.G, CalcLabelAnchor.CapSkirt, CalcKeyColorPalette.ModifierGOnCapSkirt),
  ];

  /// <summary>
  /// HP-35: CapAbove legends only (no f/g). Ink is cream/white on black and blue keys — not gold.
  /// </summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> Hp35WhiteCapAbove { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.LabelOnDarkCap),
  ];

  /// <summary>HP-34C: f CapAbove left (gold), g CapAbove right (blue), h CapSkirt (black).</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> SpiceFgh { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.G, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierGOnCapSkirt),
    new(CalcModifierKey.H, CalcLabelAnchor.CapSkirt, CalcKeyColorPalette.ModifierHOnCapFace),
  ];

  public static IReadOnlyList<CalcModifierAnnotationStyle> StylesOrDefault(CalcModelDefinition model) =>
    model.AnnotationStyles.Count > 0 ? model.AnnotationStyles : ClassicFg;

  /// <summary>Primary slot for a modifier (first binding in the model list).</summary>
  public static CalcLabelAnchor PrimaryAnchor(CalcModelDefinition model, CalcModifierKey modifier)
  {
    foreach (CalcModifierAnnotationStyle style in StylesOrDefault(model))
    {
      if (style.Modifier == modifier)
      {
        return style.Anchor;
      }
    }

    return CalcLabelAnchor.CapAbove;
  }

  /// <summary>
  /// Resolve slot for <paramref name="modifier"/>. If <paramref name="requested"/> is set and
  /// the model binds that pair, use it; otherwise fall back to the modifier's primary slot.
  /// </summary>
  public static CalcLabelAnchor ResolveAnchor(
    CalcModelDefinition model,
    CalcModifierKey modifier,
    CalcLabelAnchor? requested = null)
  {
    IReadOnlyList<CalcModifierAnnotationStyle> styles = StylesOrDefault(model);
    if (requested is { } want)
    {
      foreach (CalcModifierAnnotationStyle style in styles)
      {
        if (style.Modifier == modifier && style.Anchor == want)
        {
          return want;
        }
      }
    }

    return PrimaryAnchor(model, modifier);
  }

  public static bool TryGetInkToken(
    CalcModelDefinition model,
    CalcModifierKey modifier,
    CalcLabelAnchor anchor,
    out CalcColorToken ink)
  {
    foreach (CalcModifierAnnotationStyle style in StylesOrDefault(model))
    {
      if (style.Modifier == modifier && style.Anchor == anchor)
      {
        ink = style.Ink;
        return true;
      }
    }

    foreach (CalcModifierAnnotationStyle style in StylesOrDefault(model))
    {
      if (style.Modifier == modifier)
      {
        ink = style.Ink;
        return true;
      }
    }

    ink = default;
    return false;
  }

  public static CalcKeyAnnotation Annotate(
    CalcModelDefinition model,
    CalcModifierKey modifier,
    string text,
    CalcLabelAnchor? requestedAnchor = null,
    CalcLabelAlign align = CalcLabelAlign.Center) =>
    new(modifier, ResolveAnchor(model, modifier, requestedAnchor), text, align);
}
