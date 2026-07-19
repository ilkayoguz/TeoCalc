using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

public sealed class CalcKeyVisual
{
  public required string CapFace { get; init; }

  public CalcButtonStyle CapStyle { get; init; } = CalcButtonStyle.Black;

  public CalcButtonKind Kind { get; init; } = CalcButtonKind.Standard;

  public IReadOnlyList<CalcKeyAnnotation> Annotations { get; init; } = [];

  public uint? CapFaceInkOverride { get; init; }

  public uint? CapSkirtInkOverride { get; init; }

  /// <summary>When set (e.g. shift preview swapped primary onto CapAbove), overrides theme gold/blue ink.</summary>
  public uint? CapAboveInkOverride { get; init; }

  public static CalcKeyVisual FromLegacy(
    HpCalcKeyVisual legacy,
    CalcButtonStyle capStyle,
    CalcButtonKind kind,
    CalcModelDefinition? model = null)
  {
    CalcModelDefinition bindings = model ?? CalcModelCatalog.Hp65;
    List<CalcKeyAnnotation> annotations = [];
    if (!string.IsNullOrEmpty(legacy.GoldShift))
    {
      annotations.Add(CalcModifierPlacement.Annotate(bindings, CalcModifierKey.F, legacy.GoldShift));
    }

    if (!string.IsNullOrEmpty(legacy.GoldInverseShift))
    {
      annotations.Add(CalcModifierPlacement.Annotate(
        bindings,
        CalcModifierKey.F,
        legacy.GoldInverseShift,
        CalcLabelAnchor.CapFace));
    }

    if (!string.IsNullOrEmpty(legacy.BlueShift))
    {
      annotations.Add(CalcModifierPlacement.Annotate(bindings, CalcModifierKey.G, legacy.BlueShift));
    }

    return new CalcKeyVisual
    {
      CapFace = legacy.Primary,
      CapStyle = capStyle,
      Kind = kind,
      Annotations = annotations,
    };
  }
}

public readonly record struct CalcKeyMetrics(
  float AboveBandHeight,
  float BelowBandHeight,
  Vector2 CapMin,
  Vector2 CapMax);

public static class CalcKeyRowLayout
{
  public static void ApplyRowBands(
    IReadOnlyList<CalcKeyVisual> visuals,
    Vector2[] slotMins,
    Vector2[] slotMaxs,
    Vector2[] capMins,
    Vector2[] capMaxs,
    float scale)
  {
    if (visuals.Count == 0)
    {
      return;
    }

    float maxAbove = 0f;
    float maxBelow = 0f;
    for (int i = 0; i < visuals.Count; i++)
    {
      CalcKeyMetrics metrics = CalcKeyComponent.Measure(slotMins[i], slotMaxs[i], visuals[i], scale);
      maxAbove = MathF.Max(maxAbove, metrics.AboveBandHeight);
      maxBelow = MathF.Max(maxBelow, metrics.BelowBandHeight);
    }

    // Empty CapAbove keys share the row’s top band so gold labels stay aligned.
    // CapSkirt lives on the key etek (inside CapHeight); CapBelow alone needs the bottom band.
    if (CalcModernBody.IsActive)
    {
      maxAbove = MathF.Max(maxAbove, CalcKeyPanelComponent.LabelAboveRef * scale);
    }

    for (int i = 0; i < visuals.Count; i++)
    {
      capMins[i] = new Vector2(slotMins[i].X, slotMins[i].Y + maxAbove);
      capMaxs[i] = new Vector2(slotMaxs[i].X, slotMaxs[i].Y - maxBelow);
    }
  }
}

public static class CalcKeyComponent
{
  // Reserve nearly full GoldShift height so CapAbove glyphs are not clipped.
  private const float AboveBandScale = 1.05f;

  private const float BelowBandScale = 0.42f;

  public static CalcKeyMetrics Measure(Vector2 slotMin, Vector2 slotMax, CalcKeyVisual visual, float scale)
  {
    // TopLabel (CapAbove) and BottomLabel (CapBelow) sit outside the cap.
    // BelowLabel (CapSkirt / g) sits on the key etek — inside CapHeight.
    float above = HasAnchor(visual, CalcLabelAnchor.CapAbove)
      ? CalcFaceplateTypography.GoldShift(scale) * AboveBandScale
      : 0f;
    float below = HasAnchor(visual, CalcLabelAnchor.CapBelow)
      ? CalcFaceplateTypography.GoldShift(scale) * BelowBandScale
      : 0f;

    Vector2 capMin = new(slotMin.X, slotMin.Y + above);
    Vector2 capMax = new(slotMax.X, slotMax.Y - below);
    return new CalcKeyMetrics(above, below, capMin, capMax);
  }

  public static bool Draw(
    ImDrawListPtr draw,
    string id,
    Vector2 slotMin,
    Vector2 slotMax,
    CalcKeyVisual visual,
    CalcModelDefinition model,
    float scale,
    bool leftAlignPrimary = false,
    bool drawWell = true,
    bool forcePressed = false,
    bool interactive = true)
  {
    CalcKeyMetrics measured = Measure(slotMin, slotMax, visual, scale);
    return DrawAtCapBounds(
      draw,
      id,
      slotMin,
      slotMax,
      measured.CapMin,
      measured.CapMax,
      visual,
      model,
      scale,
      leftAlignPrimary,
      drawWell,
      forcePressed,
      interactive);
  }

  public static bool DrawAtCapBounds(
    ImDrawListPtr draw,
    string id,
    Vector2 slotMin,
    Vector2 slotMax,
    Vector2 capMin,
    Vector2 capMax,
    CalcKeyVisual visual,
    CalcModelDefinition model,
    float scale,
    bool leftAlignPrimary = false,
    bool drawWell = true,
    bool forcePressed = false,
    bool interactive = true)
  {
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (string.IsNullOrEmpty(annotation.Text))
      {
        continue;
      }

      if (annotation.Anchor == CalcLabelAnchor.CapAbove)
      {
        uint ink = visual.CapAboveInkOverride
          ?? CalcFaceplateTheme.ResolveAnnotation(annotation.Modifier, annotation.Anchor, model);
        DrawBodyBandLabel(draw, annotation.Text, slotMin, slotMax, capMin.Y, above: true, ink, scale);
      }
      else if (annotation.Anchor == CalcLabelAnchor.CapBelow)
      {
        uint ink = CalcFaceplateTheme.ResolveAnnotation(annotation.Modifier, annotation.Anchor, model);
        DrawBodyBandLabel(draw, annotation.Text, slotMin, slotMax, capMax.Y, above: false, ink, scale);
      }
    }

    // CapSkirt → key etek; CapFace modifier text may replace primary when present.
    CalcKeyAnnotation? skirtAnnotation = FindAnnotation(visual, CalcLabelAnchor.CapSkirt);
    string? skirtLabel = skirtAnnotation?.Text;
    string capFace = visual.CapFace;
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor == CalcLabelAnchor.CapFace && !string.IsNullOrEmpty(annotation.Text))
      {
        capFace = annotation.Text;
        break;
      }
    }

    uint? skirtInk = visual.CapSkirtInkOverride;
    if (skirtInk is null && skirtAnnotation is { } skirt)
    {
      skirtInk = CalcModernBody.IsActive
        ? CalcKeyLabelPalette.SkirtLabelInk(skirt.Text, visual.CapStyle)
        : CalcFaceplateTheme.ResolveAnnotation(skirt.Modifier, skirt.Anchor, model);
    }

    return CalcButton.Draw(
      draw,
      id,
      capMin,
      capMax,
      visual.CapStyle,
      visual.Kind,
      capFace,
      goldOnBody: null,
      blueOnBody: skirtLabel,
      scale,
      leftAlignPrimary,
      drawWell,
      forcePressed: forcePressed,
      interactive: interactive,
      primaryInkOverride: visual.CapFaceInkOverride,
      skirtInkOverride: skirtInk);
  }

  private static bool HasAnchor(CalcKeyVisual visual, CalcLabelAnchor anchor) =>
    visual.Annotations.Any(annotation => annotation.Anchor == anchor && !string.IsNullOrEmpty(annotation.Text));

  private static CalcKeyAnnotation? FindAnnotation(CalcKeyVisual visual, CalcLabelAnchor anchor)
  {
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor == anchor && !string.IsNullOrEmpty(annotation.Text))
      {
        return annotation;
      }
    }

    return null;
  }

  private static void DrawBodyBandLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 slotMin,
    Vector2 slotMax,
    float capEdgeY,
    bool above,
    uint ink,
    float scale)
  {
    float fontSize = above
      ? CalcFaceplateTypography.GoldShift(scale)
      : CalcFaceplateTypography.GoldShift(scale) * 0.92f;

    // CapFace / ENTER-row gold already use vector glyphs for √ / →; CapAbove must too
    // (Arial Bold atlas is Latin-1 + π only, so those codepoints become "?").
    HpClassicFaceplateGlyphs.LabelSize textSize = HpClassicFaceplateGlyphs.MeasureBodyLabel(text, fontSize);
    float x = slotMin.X + ((slotMax.X - slotMin.X) - textSize.Width) * 0.5f;
    float y = above
      ? capEdgeY - textSize.Height - scale * 1.2f
      : capEdgeY + scale * 1.2f;

    HpClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), text, fontSize, ink, scale);
  }
}
