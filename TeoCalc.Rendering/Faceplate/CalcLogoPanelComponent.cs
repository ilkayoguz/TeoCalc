using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Bottom logo plate: Teo mark | centered "Teo © 2026" | model id (T-65).
/// Caption is not width-stretched.
/// </summary>
public static class CalcLogoPanelComponent
{
  /// <summary>Original 00d logo band height before modern shortening.</summary>
  public const float BaseHeightRef = 77f;

  /// <summary>Logo panel height — shortened twice by ¼ from base (keeps 9/16). Mark stays square.</summary>
  public static float HeightRef => BaseHeightRef * 9f / 16f;

  public static RectF ResolveSlotRef(float bandLeft, float bandWidth, float topY) =>
    new(bandLeft, topY, bandWidth, HeightRef);

  public static void Draw(
    ImDrawListPtr draw,
    RectF logo,
    float scale,
    CalcModelDefinition model,
    bool skipText,
    out RectF markHit)
  {
    float radius = Calc00dWireStyle.SwitchPanelRadiusRef * scale;
    DrawBrushedAluminumPlate(draw, logo, radius, scale);

    float padX = logo.Width * 0.03f;
    float padY = logo.Height * 0.12f;
    float markSize = MathF.Max(8f * scale, logo.Height - padY * 2f);
    Vector2 markMin = new(logo.Min.X + padX, logo.Min.Y + (logo.Height - markSize) * 0.5f);
    Vector2 markMax = markMin + new Vector2(markSize, markSize);
    markHit = new RectF(markMin.X, markMin.Y, markSize, markSize);
    // Direct Svg.Skia render of TeoMark.svg (supersampled texture).
    if (!CalcModernSvgAssets.TryDrawTeoMark(draw, markMin, markMax)
        && !CalcModernSvgAssets.TryDrawHpMark(draw, markMin, markMax))
    {
      draw.AddRectFilled(markMin, markMax, 0xFF3A3A3A, MathF.Max(2f, scale));
    }

    if (skipText)
    {
      return;
    }

    float dividerPadY = logo.Height * 0.18f;
    float leftDividerX = markMax.X + padX * 0.55f;
    DrawDivider(draw, leftDividerX, logo.Min.Y + dividerPadY, logo.Max.Y - dividerPadY, scale);

    float fontSize = logo.Height * 0.42f;
    uint ink = Calc00dWireStyle.LogoCaptionInk;
    string modelLabel = model.ProductLabel;
    float modelWidth = MeasureLabel(modelLabel, fontSize);
    float modelRight = logo.Max.X - padX;
    float modelLeft = modelRight - modelWidth;

    float rightDividerX = modelLeft - padX * 0.55f;
    if (rightDividerX > leftDividerX + padX)
    {
      DrawDivider(draw, rightDividerX, logo.Min.Y + dividerPadY, logo.Max.Y - dividerPadY, scale);
    }

    float centerZoneLeft = leftDividerX + padX * 0.45f;
    float centerZoneRight = rightDividerX - padX * 0.45f;
    if (centerZoneRight <= centerZoneLeft)
    {
      centerZoneRight = modelLeft - padX * 0.35f;
    }

    DrawCenteredLabel(draw, model.LogoCaption, centerZoneLeft, centerZoneRight, logo.Min.Y, logo.Max.Y, fontSize, ink);
    DrawLabelRight(draw, modelLabel, modelRight, logo.Min.Y, logo.Max.Y, fontSize, ink);
  }

  public static void Draw(ImDrawListPtr draw, RectF logo, float scale, CalcModelDefinition model, bool skipText = false) =>
    Draw(draw, logo, scale, model, skipText, out _);

  private static void DrawDivider(ImDrawListPtr draw, float x, float y0, float y1, float scale)
  {
    // Soft embedded groove: thin shadow + adjacent highlight, both near plate tone.
    float thick = Calc00dWireStyle.Px(Calc00dWireStyle.LogoDividerWidthRef, scale);
    float offset = MathF.Max(1f, thick * 0.85f);
    draw.AddLine(
      new Vector2(x, y0),
      new Vector2(x, y1),
      Calc00dWireStyle.LogoDivider,
      thick);
    draw.AddLine(
      new Vector2(x + offset, y0),
      new Vector2(x + offset, y1),
      Calc00dWireStyle.LogoDividerHighlight,
      MathF.Max(1f, thick * 0.75f));
  }

  private static void DrawCenteredLabel(
    ImDrawListPtr draw,
    string text,
    float zoneLeft,
    float zoneRight,
    float bandTop,
    float bandBottom,
    float fontSize,
    uint ink)
  {
    float width = MeasureLabel(text, fontSize);
    float zoneMid = (zoneLeft + zoneRight) * 0.5f;
    float x = zoneMid - width * 0.5f;
    DrawLabelAt(draw, text, x, bandTop, bandBottom, fontSize, ink);
  }

  private static void DrawLabelRight(
    ImDrawListPtr draw,
    string text,
    float rightEdge,
    float bandTop,
    float bandBottom,
    float fontSize,
    uint ink)
  {
    float width = MeasureLabel(text, fontSize);
    DrawLabelAt(draw, text, rightEdge - width, bandTop, bandBottom, fontSize, ink);
  }

  private static void DrawLabelAt(
    ImDrawListPtr draw,
    string text,
    float x,
    float bandTop,
    float bandBottom,
    float fontSize,
    uint ink)
  {
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.FontInkBounds bounds = CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize);
      float plateMidY = (bandTop + bandBottom) * 0.5f;
      float topY = plateMidY - bounds.InkMidY;
      CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, topY, fontSize, ink);
      return;
    }

    ImFontPtr font = ImGui.GetFont();
    Vector2 measure = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float y = (bandTop + bandBottom - measure.Y) * 0.5f;
    draw.AddText(font, fontSize, new Vector2(x, y), ink, text);
  }

  private static float MeasureLabel(string text, float fontSize) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize).X
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text).X;

  private static void DrawBrushedAluminumPlate(ImDrawListPtr draw, RectF logo, float radius, float scale)
  {
    uint edge = Calc00dWireStyle.LogoStripEdge;
    uint center = Calc00dWireStyle.LogoStripCenter;

    draw.AddRectFilled(logo.Min, logo.Max, center, radius, ImDrawFlags.RoundCornersAll);
    draw.AddRect(logo.Min, logo.Max, edge, radius, ImDrawFlags.RoundCornersAll, MathF.Max(1f, scale * 1.1f));

    draw.PushClipRect(logo.Min, logo.Max, true);
    int lines = (int)Math.Clamp(logo.Height / MathF.Max(0.45f, scale * 0.45f), 12, 40);
    float inset = MathF.Max(1f, radius * 0.35f);
    for (int i = 0; i < lines; i++)
    {
      float t = i / (float)Math.Max(1, lines - 1);
      float y = logo.Min.Y + t * logo.Height;
      uint hi = (uint)(0x18 + (i * 17 % 20)) << 24 | 0x00FFFFFFu;
      uint lo = (uint)(0x14 + (i * 13 % 18)) << 24;
      uint color = (i & 1) == 0 ? hi : lo;
      float thick = (i % 5 == 0) ? MathF.Max(1f, scale * 0.45f) : MathF.Max(1f, scale * 0.22f);
      draw.AddLine(
        new Vector2(logo.Min.X + inset, y),
        new Vector2(logo.Max.X - inset, y),
        color,
        thick);
    }

    draw.PopClipRect();
  }
}
