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
    KeyLegendVisual legacy,
    CalcButtonStyle capStyle,
    CalcButtonKind kind,
    CalcModelDefinition? model = null)
  {
    CalcModelDefinition bindings = model ?? CalcModelCatalog.Hp65;
    CalcLabelAnchor fAnchor = CalcModifierPlacement.PrimaryAnchor(bindings, CalcModifierKey.F);
    CalcLabelAnchor gAnchor = CalcModifierPlacement.PrimaryAnchor(bindings, CalcModifierKey.G);
    bool dualCapAbove = fAnchor == CalcLabelAnchor.CapAbove
      && gAnchor == CalcLabelAnchor.CapAbove
      && !string.IsNullOrEmpty(legacy.GoldShift)
      && !string.IsNullOrEmpty(legacy.BlueShift);
    bool dualCapBelow = fAnchor == CalcLabelAnchor.CapBelow
      && gAnchor == CalcLabelAnchor.CapBelow
      && !string.IsNullOrEmpty(legacy.GoldShift)
      && !string.IsNullOrEmpty(legacy.BlueShift);
    bool spaceSavingCapAbove = dualCapAbove
      && CalcCapAboveComposite.IsSpaceSavingDualInk(legacy.GoldShift, legacy.BlueShift);
    // HP-67 CapBelow composites: trig SIN+^-1 and unit conversions R→/←P (same as CapAbove dual-ink).
    bool spaceSavingCapBelow = dualCapBelow
      && CalcCapAboveComposite.IsSpaceSavingDualInk(legacy.GoldShift, legacy.BlueShift);
    bool splitDualBand = (dualCapAbove && !spaceSavingCapAbove)
      || (dualCapBelow && !spaceSavingCapBelow);
    List<CalcKeyAnnotation> annotations = [];
    if (!string.IsNullOrEmpty(legacy.GoldShift))
    {
      CalcLabelAlign goldAlign = splitDualBand || !string.IsNullOrEmpty(legacy.GoldShiftRight)
        ? CalcLabelAlign.Left
        : CalcLabelAlign.Center;
      annotations.Add(CalcModifierPlacement.Annotate(
        bindings,
        CalcModifierKey.F,
        legacy.GoldShift,
        align: goldAlign));
    }

    if (!string.IsNullOrEmpty(legacy.GoldShiftRight))
    {
      annotations.Add(CalcModifierPlacement.Annotate(
        bindings,
        CalcModifierKey.F,
        legacy.GoldShiftRight,
        align: CalcLabelAlign.Right));
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
      CalcLabelAlign blueAlign = splitDualBand ? CalcLabelAlign.Right : CalcLabelAlign.Center;
      annotations.Add(CalcModifierPlacement.Annotate(
        bindings,
        CalcModifierKey.G,
        legacy.BlueShift,
        align: blueAlign));
    }

    if (!string.IsNullOrEmpty(legacy.BlackShift))
    {
      annotations.Add(CalcModifierPlacement.Annotate(bindings, CalcModifierKey.H, legacy.BlackShift));
    }

    return new CalcKeyVisual
    {
      CapFace = legacy.Primary,
      CapStyle = capStyle,
      Kind = kind,
      Annotations = annotations,
      CapSkirtInkOverride = !string.IsNullOrEmpty(legacy.BlackShift)
        ? CalcKeyLabelPalette.HShiftSkirtInk(capStyle, bindings.Id)
        : null,
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
    float scale,
    CalcModelDefinition? model = null)
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
    // When the model reserves CapAbove (Measure always budgets LabelAboveRef), every row must
    // shrink CapFace by that band — even digit rows with no legends — or CapFace heights diverge.
    // CapBelow-only models (HP-67) still skip an empty CapAbove gutter.
    if (model is not null)
    {
      if (CalcModifierPlacement.ReservesCapAboveBand(model))
      {
        maxAbove = MathF.Max(maxAbove, CalcKeyPanelComponent.LabelAboveRef * scale);
      }

      if (CalcModifierPlacement.ReservesCapBelowBand(model))
      {
        maxBelow = MathF.Max(maxBelow, CalcKeyPanelComponent.LabelBelowRef * scale);
      }
    }
    else if (CalcModernBody.IsActive)
    {
      if (maxAbove > 0f)
      {
        maxAbove = MathF.Max(maxAbove, CalcKeyPanelComponent.LabelAboveRef * scale);
      }

      if (maxBelow > 0f)
      {
        maxBelow = MathF.Max(maxBelow, CalcKeyPanelComponent.LabelBelowRef * scale);
      }
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

  // CapBelow is the primary shift band on HP-67 (no CapAbove); match CapAbove budget.
  private const float BelowBandScale = 1.05f;

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
    bool interactive = true,
    bool skipText = false)
  {
    if (skipText)
    {
      return CalcButton.Draw(
        draw,
        id,
        capMin,
        capMax,
        visual.CapStyle,
        visual.Kind,
        primary: string.Empty,
        goldOnBody: null,
        blueOnBody: null,
        scale,
        leftAlignPrimary,
        drawWell,
        forcePressed: forcePressed,
        interactive: interactive,
        skipText: true);
    }

    bool drewSpaceSavingDualInkAbove = TryDrawSpaceSavingDualInkCapAbove(
      draw,
      visual,
      model,
      slotMin,
      slotMax,
      capMin.Y,
      scale);
    bool drewSpaceSavingDualInkBelow = TryDrawSpaceSavingDualInkCapBelow(
      draw,
      visual,
      model,
      slotMin,
      slotMax,
      capMax.Y,
      scale);

    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (string.IsNullOrEmpty(annotation.Text))
      {
        continue;
      }

      if (annotation.Anchor == CalcLabelAnchor.CapAbove)
      {
        if (drewSpaceSavingDualInkAbove
            && annotation.Modifier is CalcModifierKey.F or CalcModifierKey.G)
        {
          continue;
        }

        uint ink = visual.CapAboveInkOverride
          ?? CalcFaceplateTheme.ResolveAnnotation(annotation.Modifier, annotation.Anchor, model);
        DrawBodyBandLabel(
          draw,
          annotation.Text,
          slotMin,
          slotMax,
          capMin.Y,
          above: true,
          ink,
          scale,
          annotation.Align);
      }
      else if (annotation.Anchor == CalcLabelAnchor.CapBelow)
      {
        if (drewSpaceSavingDualInkBelow
            && annotation.Modifier is CalcModifierKey.F or CalcModifierKey.G)
        {
          continue;
        }

        uint ink = CalcFaceplateTheme.ResolveAnnotation(annotation.Modifier, annotation.Anchor, model);
        DrawBodyBandLabel(
          draw,
          annotation.Text,
          slotMin,
          slotMax,
          capMax.Y,
          above: false,
          ink,
          scale,
          annotation.Align);
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
      skirtInk = skirt.Modifier == CalcModifierKey.H
        ? CalcKeyLabelPalette.HShiftSkirtInk(visual.CapStyle, model.Id)
        : CalcModernBody.IsActive
          ? CalcKeyLabelPalette.SkirtLabelInk(skirt.Text, visual.CapStyle, model.Id)
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

  private static CalcKeyAnnotation? FindCapAbove(CalcKeyVisual visual, CalcModifierKey modifier)
  {
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor == CalcLabelAnchor.CapAbove
          && annotation.Modifier == modifier
          && !string.IsNullOrEmpty(annotation.Text))
      {
        return annotation;
      }
    }

    return null;
  }

  private static CalcKeyAnnotation? FindCapBelow(CalcKeyVisual visual, CalcModifierKey modifier)
  {
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor == CalcLabelAnchor.CapBelow
          && annotation.Modifier == modifier
          && !string.IsNullOrEmpty(annotation.Text))
      {
        return annotation;
      }
    }

    return null;
  }

  /// <summary>
  /// Dual-ink CapAbove composites: trig −1, H.MS±, or unit conversion arrow stack.
  /// </summary>
  private static bool TryDrawSpaceSavingDualInkCapAbove(
    ImDrawListPtr draw,
    CalcKeyVisual visual,
    CalcModelDefinition model,
    Vector2 slotMin,
    Vector2 slotMax,
    float capEdgeY,
    float scale)
  {
    CalcKeyAnnotation? gold = FindCapAbove(visual, CalcModifierKey.F);
    CalcKeyAnnotation? blue = FindCapAbove(visual, CalcModifierKey.G);
    if (gold is null || blue is null)
    {
      return false;
    }

    string goldText = gold.Value.Text;
    string blueText = blue.Value.Text;
    uint goldInk = visual.CapAboveInkOverride
      ?? CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapAbove, model);
    uint blueInk = CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.G, CalcLabelAnchor.CapAbove, model);
    float fontSize = CalcFaceplateTypography.GoldShift(scale);

    if (CalcCapAboveComposite.IsSpaceSavingInverse(goldText, blueText))
    {
      ClassicFaceplateGlyphs.LabelSize baseSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(goldText, fontSize);
      float gap = fontSize * 0.04f;
      float superW = ClassicFaceplateGlyphs.MeasureInverseSuffixWidth(fontSize);
      float totalW = baseSize.Width + gap + superW;
      float x = slotMin.X + ((slotMax.X - slotMin.X) - totalW) * 0.5f;
      float y = capEdgeY - baseSize.Height - scale * 1.2f;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), goldText, fontSize, goldInk, scale);
      ClassicFaceplateGlyphs.DrawInverseSuffix(
        draw,
        x + baseSize.Width + gap,
        y,
        fontSize,
        blueInk,
        scale);
      return true;
    }

    if (CalcCapAboveComposite.IsSpaceSavingHmsPlusMinus(goldText, blueText))
    {
      const string hms = "H.MS";
      ClassicFaceplateGlyphs.LabelSize baseSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(hms, fontSize);
      float gap = fontSize * 0.08f;
      float signW = ClassicFaceplateGlyphs.MeasureHmsSignStackWidth(fontSize);
      float totalW = baseSize.Width + gap + signW;
      float x = slotMin.X + ((slotMax.X - slotMin.X) - totalW) * 0.5f;
      float y = capEdgeY - baseSize.Height - scale * 1.2f;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), hms, fontSize, goldInk, scale);
      ClassicFaceplateGlyphs.DrawHmsSignStack(
        draw,
        x + baseSize.Width + gap,
        y,
        fontSize,
        goldInk,
        blueInk,
        scale);
      return true;
    }

    if (CalcCapAboveComposite.TryParseUnitConversionPair(goldText, blueText, out string left, out string right))
    {
      ClassicFaceplateGlyphs.LabelSize leftSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(left, fontSize);
      ClassicFaceplateGlyphs.LabelSize rightSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(right, fontSize);
      float gap = fontSize * 0.06f;
      float arrowW = ClassicFaceplateGlyphs.MeasureDualInkConversionArrowWidth(fontSize);
      float totalW = leftSize.Width + gap + arrowW + gap + rightSize.Width;
      float bandH = MathF.Max(leftSize.Height, rightSize.Height);
      float x = slotMin.X + ((slotMax.X - slotMin.X) - totalW) * 0.5f;
      float y = capEdgeY - bandH - scale * 1.2f;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), left, fontSize, goldInk, scale);
      x += leftSize.Width + gap;
      // Upper → blue (g), lower ← gold (f) — color-coded conversion direction markers.
      ClassicFaceplateGlyphs.DrawDualInkConversionArrows(
        draw,
        x,
        y,
        fontSize,
        rightArrowInk: blueInk,
        leftArrowInk: goldInk,
        scale);
      x += arrowW + gap;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), right, fontSize, blueInk, scale);
      return true;
    }

    return false;
  }

  /// <summary>
  /// HP-67 CapBelow space-saving dual-ink: trig SIN/COS/TAN + ^-1, and unit conversions R→/←P.
  /// </summary>
  private static bool TryDrawSpaceSavingDualInkCapBelow(
    ImDrawListPtr draw,
    CalcKeyVisual visual,
    CalcModelDefinition model,
    Vector2 slotMin,
    Vector2 slotMax,
    float capEdgeY,
    float scale)
  {
    CalcKeyAnnotation? gold = FindCapBelow(visual, CalcModifierKey.F);
    CalcKeyAnnotation? blue = FindCapBelow(visual, CalcModifierKey.G);
    if (gold is null || blue is null)
    {
      return false;
    }

    string goldText = gold.Value.Text;
    string blueText = blue.Value.Text;
    uint goldInk = CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapBelow, model);
    uint blueInk = CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.G, CalcLabelAnchor.CapBelow, model);
    float fontSize = CalcFaceplateTypography.GoldShift(scale) * 0.92f;
    float y = capEdgeY + scale * 1.2f;

    if (CalcCapAboveComposite.IsSpaceSavingInverse(goldText, blueText))
    {
      ClassicFaceplateGlyphs.LabelSize baseSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(goldText, fontSize);
      float gap = fontSize * 0.04f;
      float superW = ClassicFaceplateGlyphs.MeasureInverseSuffixWidth(fontSize);
      float totalW = baseSize.Width + gap + superW;
      float x = slotMin.X + ((slotMax.X - slotMin.X) - totalW) * 0.5f;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), goldText, fontSize, goldInk, scale);
      ClassicFaceplateGlyphs.DrawInverseSuffix(
        draw,
        x + baseSize.Width + gap,
        y,
        fontSize,
        blueInk,
        scale);
      return true;
    }

    if (CalcCapAboveComposite.TryParseUnitConversionPair(goldText, blueText, out string left, out string right))
    {
      ClassicFaceplateGlyphs.LabelSize leftSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(left, fontSize);
      ClassicFaceplateGlyphs.LabelSize rightSize =
        ClassicFaceplateGlyphs.MeasureBodyLabel(right, fontSize);
      float gap = fontSize * 0.06f;
      float arrowW = ClassicFaceplateGlyphs.MeasureDualInkConversionArrowWidth(fontSize);
      float totalW = leftSize.Width + gap + arrowW + gap + rightSize.Width;
      float x = slotMin.X + ((slotMax.X - slotMin.X) - totalW) * 0.5f;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), left, fontSize, goldInk, scale);
      x += leftSize.Width + gap;
      // Upper → blue (g), lower ← gold (f) — same as CapAbove unit conversions.
      ClassicFaceplateGlyphs.DrawDualInkConversionArrows(
        draw,
        x,
        y,
        fontSize,
        rightArrowInk: blueInk,
        leftArrowInk: goldInk,
        scale);
      x += arrowW + gap;
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), right, fontSize, blueInk, scale);
      return true;
    }

    return false;
  }

  private static void DrawBodyBandLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 slotMin,
    Vector2 slotMax,
    float capEdgeY,
    bool above,
    uint ink,
    float scale,
    CalcLabelAlign align = CalcLabelAlign.Center)
  {
    float fontSize = above
      ? CalcFaceplateTypography.GoldShift(scale)
      : CalcFaceplateTypography.GoldShift(scale) * 0.92f;

    // CapFace / ENTER-row gold already use vector glyphs for √ / →; CapAbove must too
    // (Arial Bold atlas is Latin-1 + π only, so those codepoints become "?").
    ClassicFaceplateGlyphs.LabelSize textSize = ClassicFaceplateGlyphs.MeasureBodyLabel(text, fontSize);
    float inset = align is CalcLabelAlign.Left or CalcLabelAlign.Right
      ? CalcLabelAlignMetrics.DualBandInset(scale)
      : scale * 3f;
    float x = align switch
    {
      CalcLabelAlign.Left => slotMin.X + inset,
      CalcLabelAlign.Right => slotMax.X - inset - textSize.Width,
      _ => slotMin.X + ((slotMax.X - slotMin.X) - textSize.Width) * 0.5f,
    };
    float y = above
      ? capEdgeY - textSize.Height - scale * 1.2f
      : capEdgeY + scale * 1.2f;

    ClassicFaceplateGlyphs.DrawBodyLabel(draw, new Vector2(x, y), text, fontSize, ink, scale);
  }
}
