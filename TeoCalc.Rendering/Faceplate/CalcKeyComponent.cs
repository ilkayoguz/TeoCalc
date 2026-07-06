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

  public static CalcKeyVisual FromLegacy(
    HpCalcKeyVisual legacy,
    CalcButtonStyle capStyle,
    CalcButtonKind kind)
  {
    List<CalcKeyAnnotation> annotations = [];
    if (!string.IsNullOrEmpty(legacy.GoldShift))
    {
      annotations.Add(new CalcKeyAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapAbove, legacy.GoldShift));
    }

    if (!string.IsNullOrEmpty(legacy.GoldInverseShift))
    {
      annotations.Add(new CalcKeyAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapFace, legacy.GoldInverseShift));
    }

    if (!string.IsNullOrEmpty(legacy.BlueShift))
    {
      annotations.Add(new CalcKeyAnnotation(CalcModifierKey.G, CalcLabelAnchor.CapSkirt, legacy.BlueShift));
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

    for (int i = 0; i < visuals.Count; i++)
    {
      capMins[i] = new Vector2(slotMins[i].X, slotMins[i].Y + maxAbove);
      capMaxs[i] = new Vector2(slotMaxs[i].X, slotMaxs[i].Y - maxBelow);
    }
  }
}

public static class CalcKeyComponent
{
  private const float AboveBandScale = 0.72f;

  private const float BelowBandScale = 0.34f;

  public static CalcKeyMetrics Measure(Vector2 slotMin, Vector2 slotMax, CalcKeyVisual visual, float scale)
  {
    float above = HasAnchor(visual, CalcLabelAnchor.CapAbove)
      ? CalcFaceplateTypography.GoldShift(scale) * AboveBandScale
      : 0f;
    float below = HasAnchor(visual, CalcLabelAnchor.CapBelow)
      ? CalcFaceplateTypography.BlueSkirt(scale) * BelowBandScale
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
    CalcKeyMetrics metrics = Measure(slotMin, slotMax, visual, scale);

    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor is not (CalcLabelAnchor.CapAbove or CalcLabelAnchor.CapBelow)
          || string.IsNullOrEmpty(annotation.Text))
      {
        continue;
      }

      uint ink = CalcFaceplateTheme.ResolveAnnotation(annotation.Modifier, annotation.Anchor, model);
      if (annotation.Anchor == CalcLabelAnchor.CapAbove)
      {
        DrawBodyBandLabel(
          draw,
          annotation.Text,
          slotMin,
          slotMax,
          metrics.CapMin.Y,
          above: true,
          ink,
          scale);
      }
      else
      {
        DrawBodyBandLabel(
          draw,
          annotation.Text,
          slotMin,
          slotMax,
          metrics.CapMax.Y,
          above: false,
          ink,
          scale);
      }
    }

    string? skirtLabel = FindAnnotationText(visual, CalcLabelAnchor.CapSkirt);
    string capFace = visual.CapFace;
    foreach (CalcKeyAnnotation annotation in visual.Annotations)
    {
      if (annotation.Anchor == CalcLabelAnchor.CapFace && !string.IsNullOrEmpty(annotation.Text))
      {
        capFace = annotation.Text;
        break;
      }
    }

    return CalcButton.Draw(
      draw,
      id,
      metrics.CapMin,
      metrics.CapMax,
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
      skirtInkOverride: visual.CapSkirtInkOverride
        ?? (skirtLabel is not null
          ? CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.G, CalcLabelAnchor.CapSkirt, model)
          : null));
  }

  private static bool HasAnchor(CalcKeyVisual visual, CalcLabelAnchor anchor) =>
    visual.Annotations.Any(annotation => annotation.Anchor == anchor && !string.IsNullOrEmpty(annotation.Text));

  private static string? FindAnnotationText(CalcKeyVisual visual, CalcLabelAnchor anchor) =>
    visual.Annotations.FirstOrDefault(annotation => annotation.Anchor == anchor).Text is { Length: > 0 } text
      ? text
      : null;

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
      : CalcFaceplateTypography.BlueSkirt(scale) * CalcKeyLabelPalette.BlueSkirtFontScale(text);

    Vector2 textSize = CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

    float x = slotMin.X + ((slotMax.X - slotMin.X) - textSize.X) * 0.5f;
    float y = above
      ? capEdgeY - textSize.Y - scale * 1.5f
      : capEdgeY + scale * 1.5f;

    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, y, fontSize, ink);
    }
    else
    {
      draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), ink, text);
    }
  }
}
