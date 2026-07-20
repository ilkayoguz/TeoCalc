using System.Linq;

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

  /// <summary>HP-45: single gold f CapAbove (no blue g / CapSkirt).</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> ClassicGoldOnly { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierFOnCapAbove),
  ];

  /// <summary>HP-55: f CapAbove left (gold), g CapAbove right (blue); no CapSkirt.</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> ClassicDualCapAbove { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.G, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierGOnCapSkirt),
  ];

  /// <summary>HP-34C: f CapAbove left (gold), g CapAbove right (blue), h CapSkirt (black).</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> SpiceFgh { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.G, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierGOnCapSkirt),
    new(CalcModifierKey.H, CalcLabelAnchor.CapSkirt, CalcKeyColorPalette.ModifierHOnCapFace),
  ];

  /// <summary>
  /// HP-67: no CapAbove — f CapBelow (gold), g CapBelow (blue), h CapSkirt (black ink).
  /// Dual CapBelow uses left gold / right blue, except space-saving composites
  /// (trig SIN+^-1; unit conversions R→/←P).
  /// </summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> ClassicHp67Fgh { get; } =
  [
    new(CalcModifierKey.F, CalcLabelAnchor.CapBelow, CalcKeyColorPalette.ModifierFOnCapAbove),
    new(CalcModifierKey.G, CalcLabelAnchor.CapBelow, CalcKeyColorPalette.ModifierGOnCapSkirt),
    new(CalcModifierKey.H, CalcLabelAnchor.CapSkirt, CalcKeyColorPalette.ModifierHOnCapFace),
  ];

  /// <summary>HP-70: no shift keys / no CapAbove annotations.</summary>
  public static IReadOnlyList<CalcModifierAnnotationStyle> None { get; } = [];

  public static IReadOnlyList<CalcModifierAnnotationStyle> StylesOrDefault(CalcModelDefinition model)
  {
    if (model.ModifierKeys.Count == 0)
    {
      return model.AnnotationStyles;
    }

    return model.AnnotationStyles.Count > 0 ? model.AnnotationStyles : ClassicFg;
  }

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

  /// <summary>True when the model prints any legend on CapAbove (reserves top label band).</summary>
  public static bool ReservesCapAboveBand(CalcModelDefinition model) =>
    StylesOrDefault(model).Any(style => style.Anchor == CalcLabelAnchor.CapAbove);

  /// <summary>True when the model prints any legend on CapBelow (reserves bottom label band).</summary>
  public static bool ReservesCapBelowBand(CalcModelDefinition model) =>
    StylesOrDefault(model).Any(style => style.Anchor == CalcLabelAnchor.CapBelow);

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
