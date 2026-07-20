using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Faceplate switch unit:
/// <code>
///              TopLabel
/// LeftLabel [ KNOB ] RightLabel
///            BottomLabel
/// </code>
/// Size includes all four labels. One switch sits left; two+ share the row with equal track spacing.
/// </summary>
public static class CalcSwitchComponent
{
  public const float LabelGapRef = 12f;

  public const float FontSizeRef = 14f;

  public const float VerticalLabelGapRef = 9f;

  public readonly record struct Metrics(
    float Width,
    float Height,
    float LeftLabelWidth,
    float RightLabelWidth,
    float TrackColumnWidth,
    float TrackWidth,
    float FontSize,
    float AboveTrack,
    float BelowTrack);

  public readonly record struct HitLayout(
    RectF LeftLabel,
    RectF RightLabel,
    RectF TopLabel,
    RectF BottomLabel,
    RectF Track,
    RectF Knob,
    float TrackCenterX,
    float TrackCenterY);

  public static Metrics Measure(CalcSwitchSpec spec, float scale)
  {
    float fontSize = MathF.Max(11f, FontSizeRef * scale);
    float auxSize = fontSize * 0.85f;
    ImFontPtr font = ResolveFont();
    float lineGap = fontSize * 0.12f;
    float leftW = MeasureLabelWidth(font, fontSize, spec.LeftLabel);
    float rightW = MeasureLabelWidth(font, fontSize, spec.RightLabel);
    float leftH = MeasureLabelHeight(font, fontSize, spec.LeftLabel, lineGap);
    float rightH = MeasureLabelHeight(font, fontSize, spec.RightLabel, lineGap);
    float topW = MeasureLabelWidth(font, auxSize, spec.TopLabel);
    float botW = MeasureLabelWidth(font, auxSize, spec.BottomLabel);
    float trackW = Calc00dWireStyle.SwitchTrackWidthRefPx * scale;
    float knobH = Calc00dWireStyle.SwitchKnobHeightRefPx * scale;
    float gap = LabelGapRef * scale;
    float vGap = VerticalLabelGapRef * scale;
    float trackColumnW = MathF.Max(trackW, MathF.Max(topW, botW));

    float above = knobH * 0.5f;
    if (!string.IsNullOrEmpty(spec.TopLabel))
    {
      float topH = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.TopLabel).Y;
      above = knobH * 0.55f + vGap + topH;
    }

    float below = knobH * 0.5f;
    if (!string.IsNullOrEmpty(spec.BottomLabel))
    {
      float botH = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.BottomLabel).Y;
      below = knobH * 0.55f + vGap + botH;
    }

    // Dual-row side legends (HP-38E D.MY/BEGIN) need vertical room beyond the knob.
    float sideHalf = MathF.Max(leftH, rightH) * 0.5f;
    above = MathF.Max(above, sideHalf);
    below = MathF.Max(below, sideHalf);

    float width = leftW + gap + trackColumnW + gap + rightW;
    return new Metrics(width, above + below, leftW, rightW, trackColumnW, trackW, fontSize, above, below);
  }

  /// <summary>Draw one switch. <paramref name="leftX"/> is the left edge of left+track+right.</summary>
  public static void Draw(
    ImDrawListPtr draw,
    float leftX,
    float trackCenterY,
    float scale,
    CalcSwitchSpec spec,
    float positionNorm,
    bool modernChrome = true,
    bool skipText = false)
  {
    Metrics m = Measure(spec, scale);
    float gap = LabelGapRef * scale;
    float vGap = VerticalLabelGapRef * scale;
    ImFontPtr font = ResolveFont();
    uint ink = modernChrome ? Calc00dWireStyle.SwitchLabelInk : CalcChassisPalette.SwitchLabel;
    float auxSize = m.FontSize * 0.85f;

    float x = leftX;
    if (!skipText && m.LeftLabelWidth > 0.5f)
    {
      DrawStackedSideLabel(draw, font, m.FontSize, ink, spec.LeftLabel, x, trackCenterY, rightAligned: true);
    }

    x += m.LeftLabelWidth + gap;
    float trackCenterX = x + m.TrackColumnWidth * 0.5f;
    DrawTrackAndKnob(draw, new Vector2(trackCenterX, trackCenterY), scale, positionNorm, modernChrome);

    if (skipText)
    {
      return;
    }

    if (!string.IsNullOrEmpty(spec.TopLabel))
    {
      Vector2 topSize = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.TopLabel);
      float topY = trackCenterY - Calc00dWireStyle.SwitchKnobHeightRefPx * scale * 0.55f - vGap - topSize.Y;
      draw.AddText(font, auxSize, new Vector2(trackCenterX - topSize.X * 0.5f, topY), ink, spec.TopLabel);
    }

    if (!string.IsNullOrEmpty(spec.BottomLabel))
    {
      Vector2 botSize = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.BottomLabel);
      float botY = trackCenterY + Calc00dWireStyle.SwitchKnobHeightRefPx * scale * 0.55f + vGap;
      draw.AddText(font, auxSize, new Vector2(trackCenterX - botSize.X * 0.5f, botY), ink, spec.BottomLabel);
    }

    x += m.TrackColumnWidth + gap;
    if (m.RightLabelWidth > 0.5f)
    {
      DrawStackedSideLabel(draw, font, m.FontSize, ink, spec.RightLabel, x, trackCenterY, rightAligned: false);
    }
  }

  /// <summary>
  /// 1 switch → left-aligned in the slot.
  /// 2+ switches → track centers spaced evenly across the slot (avoids right-shift from wide left labels).
  /// </summary>
  public static void DrawRow(
    ImDrawListPtr draw,
    RectF rowSlot,
    float trackCenterY,
    float scale,
    IReadOnlyList<CalcSwitchSpec> specs,
    Func<int, CalcSwitchSpec, float> positionNormForIndex,
    bool modernChrome = true,
    bool skipText = false)
  {
    if (specs.Count == 0)
    {
      return;
    }

    Span<Metrics> metrics = stackalloc Metrics[specs.Count];
    for (int i = 0; i < specs.Count; i++)
    {
      metrics[i] = Measure(specs[i], scale);
    }

    for (int i = 0; i < specs.Count; i++)
    {
      float leftX = ComponentLeftX(rowSlot, specs.Count, metrics, i, scale);
      Draw(draw, leftX, trackCenterY, scale, specs[i], positionNormForIndex(i, specs[i]), modernChrome, skipText);
    }
  }

  /// <summary>Legacy entry: vertically centers the track in <paramref name="rowSlot"/>.</summary>
  public static void DrawRow(
    ImDrawListPtr draw,
    RectF rowSlot,
    float scale,
    IReadOnlyList<CalcSwitchSpec> specs,
    Func<int, CalcSwitchSpec, float> positionNormForIndex,
    bool modernChrome = true)
  {
    float trackCenterY = rowSlot.Min.Y + rowSlot.Height * 0.5f;
    if (specs.Count > 0)
    {
      float maxAbove = 0f;
      float maxBelow = 0f;
      for (int i = 0; i < specs.Count; i++)
      {
        Metrics m = Measure(specs[i], scale);
        maxAbove = MathF.Max(maxAbove, m.AboveTrack);
        maxBelow = MathF.Max(maxBelow, m.BelowTrack);
      }

      float bandH = maxAbove + maxBelow;
      float bandTop = rowSlot.Min.Y + MathF.Max(0f, (rowSlot.Height - bandH) * 0.5f);
      trackCenterY = bandTop + maxAbove;
    }

    DrawRow(draw, rowSlot, trackCenterY, scale, specs, positionNormForIndex, modernChrome);
  }

  public static float TrackCenterX(RectF rowSlot, float scale, IReadOnlyList<CalcSwitchSpec> specs, int index)
  {
    Span<Metrics> metrics = stackalloc Metrics[specs.Count];
    for (int i = 0; i < specs.Count; i++)
    {
      metrics[i] = Measure(specs[i], scale);
    }

    float leftX = ComponentLeftX(rowSlot, specs.Count, metrics, index, scale);
    Metrics m = metrics[index];
    return leftX + m.LeftLabelWidth + LabelGapRef * scale + m.TrackColumnWidth * 0.5f;
  }

  public static HitLayout BuildHitLayout(
    RectF rowSlot,
    float trackCenterY,
    float scale,
    IReadOnlyList<CalcSwitchSpec> specs,
    int index,
    float positionNorm)
  {
    Span<Metrics> metrics = stackalloc Metrics[specs.Count];
    for (int i = 0; i < specs.Count; i++)
    {
      metrics[i] = Measure(specs[i], scale);
    }

    CalcSwitchSpec spec = specs[index];
    Metrics m = metrics[index];
    float gap = LabelGapRef * scale;
    float vGap = VerticalLabelGapRef * scale;
    float leftX = ComponentLeftX(rowSlot, specs.Count, metrics, index, scale);
    float trackCenterX = leftX + m.LeftLabelWidth + gap + m.TrackColumnWidth * 0.5f;
    ImFontPtr font = ResolveFont();
    float auxSize = m.FontSize * 0.85f;

    float lineGap = m.FontSize * 0.12f;
    RectF leftLabel = default;
    if (m.LeftLabelWidth > 0.5f)
    {
      float leftH = MeasureLabelHeight(font, m.FontSize, spec.LeftLabel, lineGap);
      leftLabel = new RectF(leftX, trackCenterY - leftH * 0.5f, m.LeftLabelWidth, leftH);
    }

    float trackW = Calc00dWireStyle.SwitchTrackWidthRefPx * scale;
    float trackH = Calc00dWireStyle.SwitchTrackHeightRefPx * scale;
    float knobW = Calc00dWireStyle.SwitchKnobWidthRefPx * scale;
    float knobH = Calc00dWireStyle.SwitchKnobHeightRefPx * scale;
    positionNorm = Math.Clamp(positionNorm, 0f, 1f);
    RectF track = new(trackCenterX - trackW * 0.5f, trackCenterY - trackH * 0.5f, trackW, trackH);
    float knobX = track.X + positionNorm * (trackW - knobW);
    RectF knob = new(knobX, trackCenterY - knobH * 0.5f, knobW, knobH);

    RectF topLabel = default;
    if (!string.IsNullOrEmpty(spec.TopLabel))
    {
      Vector2 topSize = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.TopLabel);
      float topY = trackCenterY - knobH * 0.55f - vGap - topSize.Y;
      topLabel = new RectF(trackCenterX - topSize.X * 0.5f, topY, topSize.X, topSize.Y);
    }

    RectF botLabel = default;
    if (!string.IsNullOrEmpty(spec.BottomLabel))
    {
      Vector2 botSize = font.CalcTextSizeA(auxSize, float.MaxValue, 0f, spec.BottomLabel);
      float botY = trackCenterY + knobH * 0.55f + vGap;
      botLabel = new RectF(trackCenterX - botSize.X * 0.5f, botY, botSize.X, botSize.Y);
    }

    RectF rightLabel = default;
    if (m.RightLabelWidth > 0.5f)
    {
      float rightX = leftX + m.LeftLabelWidth + gap + m.TrackColumnWidth + gap;
      float rightH = MeasureLabelHeight(font, m.FontSize, spec.RightLabel, lineGap);
      rightLabel = new RectF(rightX, trackCenterY - rightH * 0.5f, m.RightLabelWidth, rightH);
    }

    return new HitLayout(leftLabel, rightLabel, topLabel, botLabel, track, knob, trackCenterX, trackCenterY);
  }

  public static SwitchLabelSlot HitTest(Vector2 mouse, in HitLayout layout)
  {
    if (Contains(mouse, layout.Knob))
    {
      return SwitchLabelSlot.Knob;
    }

    if (Contains(mouse, layout.LeftLabel))
    {
      return SwitchLabelSlot.Left;
    }

    if (Contains(mouse, layout.RightLabel))
    {
      return SwitchLabelSlot.Right;
    }

    if (Contains(mouse, layout.TopLabel))
    {
      return SwitchLabelSlot.Top;
    }

    if (Contains(mouse, layout.BottomLabel))
    {
      return SwitchLabelSlot.Bottom;
    }

    if (Contains(mouse, layout.Track))
    {
      return SwitchLabelSlot.Track;
    }

    return SwitchLabelSlot.None;
  }

  public static int IndexForTrackClick(CalcSwitchSpec spec, in HitLayout layout, Vector2 mouse)
  {
    float t = (mouse.X - layout.Track.X) / MathF.Max(1f, layout.Track.Width);
    if (spec.PositionCount == 2)
    {
      return t < 0.5f ? 0 : 1;
    }

    if (t < 1f / 3f)
    {
      return 0;
    }

    return t < 2f / 3f ? 1 : 2;
  }

  private static bool Contains(Vector2 point, RectF rect) =>
    rect.Width > 0.5f
    && rect.Height > 0.5f
    && point.X >= rect.X
    && point.X <= rect.Max.X
    && point.Y >= rect.Y
    && point.Y <= rect.Max.Y;

  private static float ComponentLeftX(RectF rowSlot, int count, Span<Metrics> metrics, int index, float scale)
  {
    if (count <= 1)
    {
      return rowSlot.Min.X;
    }

    // Even track-center spacing across the row — independent of left/right label widths.
    float trackCenterX = rowSlot.Min.X + rowSlot.Width * (index + 0.5f) / count;
    Metrics m = metrics[index];
    return trackCenterX - m.LeftLabelWidth - LabelGapRef * scale - m.TrackColumnWidth * 0.5f;
  }

  private static void DrawTrackAndKnob(
    ImDrawListPtr draw,
    Vector2 center,
    float scale,
    float positionNorm,
    bool modernChrome)
  {
    float trackW = Calc00dWireStyle.SwitchTrackWidthRefPx * scale;
    float trackH = Calc00dWireStyle.SwitchTrackHeightRefPx * scale;
    float knobW = Calc00dWireStyle.SwitchKnobWidthRefPx * scale;
    float knobH = Calc00dWireStyle.SwitchKnobHeightRefPx * scale;
    positionNorm = Math.Clamp(positionNorm, 0f, 1f);

    Vector2 trackMin = new(center.X - trackW * 0.5f, center.Y - trackH * 0.5f);
    Vector2 trackMax = new(center.X + trackW * 0.5f, center.Y + trackH * 0.5f);

    if (modernChrome)
    {
      draw.AddRectFilled(trackMin, trackMax, Calc00dWireStyle.SwitchTrackFill, Calc00dWireStyle.SwitchTrackRadiusRef * scale, ImDrawFlags.RoundCornersAll);
      float knobX = trackMin.X + positionNorm * (trackW - knobW);
      Vector2 knobMin = new(knobX, center.Y - knobH * 0.5f);
      Vector2 knobMax = knobMin + new Vector2(knobW, knobH);
      float knobR = Calc00dWireStyle.SwitchKnobRadiusRef * scale;
      draw.AddRectFilled(knobMin, knobMax, Calc00dWireStyle.SwitchKnobFill, knobR, ImDrawFlags.RoundCornersAll);
      DrawKnurls(draw, knobMin, knobMax, scale);
      draw.AddRect(
        knobMin,
        knobMax,
        Calc00dWireStyle.SwitchKnobEdge,
        knobR,
        ImDrawFlags.RoundCornersAll,
        Calc00dWireStyle.Px(Calc00dWireStyle.SwitchKnobEdgeWidthRef, scale));
      return;
    }

    uint track = CalcChassisPalette.SwitchTrack;
    uint edge = CalcChassisPalette.KeyWellEdge;
    draw.AddRectFilled(trackMin, trackMax, track, trackH * 0.5f);
    draw.AddRect(trackMin, trackMax, edge, trackH * 0.5f, ImDrawFlags.None, scale * 0.55f);
    float kx = trackMin.X + positionNorm * (trackW - knobW);
    Vector2 kMin = new(kx, center.Y - knobH * 0.5f);
    Vector2 kMax = kMin + new Vector2(knobW, knobH);
    draw.AddRectFilled(kMin, kMax, CalcChassisPalette.SwitchKnob, 2f * scale);
  }

  private static void DrawKnurls(ImDrawListPtr draw, Vector2 knobMin, Vector2 knobMax, float scale)
  {
    float pitch = MathF.Max(1.2f, Calc00dWireStyle.SwitchKnurlPitchRef * scale);
    float insetX = scale * 1.6f;
    float insetY = scale * 1.2f;
    int i = 0;
    for (float x = knobMin.X + insetX; x <= knobMax.X - insetX; x += pitch, i++)
    {
      uint color = (i & 1) == 0 ? 0x66FFFFFFu : 0x55000000u;
      draw.AddLine(
        new Vector2(x, knobMin.Y + insetY),
        new Vector2(x, knobMax.Y - insetY),
        color,
        MathF.Max(1f, scale * 0.55f));
    }
  }

  private static ImFontPtr ResolveFont() =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.ArialBold
      : (CalcFaceplateFonts.IsReady ? CalcFaceplateFonts.SansBold : ImGui.GetFont());

  private static float MeasureLabelWidth(ImFontPtr font, float fontSize, string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return 0f;
    }

    float maxW = 0f;
    foreach (string line in SplitLines(text))
    {
      maxW = MathF.Max(maxW, font.CalcTextSizeA(fontSize, float.MaxValue, 0f, line).X);
    }

    return maxW;
  }

  private static float MeasureLabelHeight(ImFontPtr font, float fontSize, string text, float lineGap)
  {
    if (string.IsNullOrEmpty(text))
    {
      return 0f;
    }

    string[] lines = SplitLines(text);
    float lineH = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, lines[0]).Y;
    return lines.Length * lineH + MathF.Max(0, lines.Length - 1) * lineGap;
  }

  /// <summary>
  /// Side legends: single line centered on the track, or dual-row stack
  /// (HP-38E: D.MY above BEGIN / M.DY above END), right-edge aligned on the left side.
  /// </summary>
  private static void DrawStackedSideLabel(
    ImDrawListPtr draw,
    ImFontPtr font,
    float fontSize,
    uint ink,
    string text,
    float columnX,
    float trackCenterY,
    bool rightAligned)
  {
    string[] lines = SplitLines(text);
    float lineGap = fontSize * 0.12f;
    float lineH = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, lines[0]).Y;
    float totalH = lines.Length * lineH + MathF.Max(0, lines.Length - 1) * lineGap;
    float y = trackCenterY - totalH * 0.5f;
    float columnW = 0f;
    foreach (string line in lines)
    {
      columnW = MathF.Max(columnW, font.CalcTextSizeA(fontSize, float.MaxValue, 0f, line).X);
    }

    foreach (string line in lines)
    {
      Vector2 size = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, line);
      float x = rightAligned ? columnX + columnW - size.X : columnX;
      draw.AddText(font, fontSize, new Vector2(x, y), ink, line);
      y += lineH + lineGap;
    }
  }

  private static string[] SplitLines(string text) =>
    text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
