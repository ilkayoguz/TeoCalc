using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Bottom logo plate (brushed aluminum + hp mark + caption). Keys must not overlap this band.</summary>
public static class CalcLogoPanelComponent
{
  /// <summary>Original 00d logo band height before modern shortening.</summary>
  public const float BaseHeightRef = 77f;

  /// <summary>Logo panel height — shortened twice by ¼ from base (keeps 9/16). Mark stays square.</summary>
  public static float HeightRef => BaseHeightRef * 9f / 16f;

  public static RectF ResolveSlotRef(float bandLeft, float bandWidth, float topY) =>
    new(bandLeft, topY, bandWidth, HeightRef);

  public static void Draw(ImDrawListPtr draw, RectF logo, float scale, CalcModelDefinition model)
  {
    // Match display/switch panel rounding.
    float radius = Calc00dWireStyle.SwitchPanelRadiusRef * scale;
    DrawBrushedAluminumPlate(draw, logo, radius, scale);

    float padX = logo.Width * 0.03f;
    float padY = logo.Height * 0.12f;
    // Square mark — aspect locked to height (⅓ panel), not stretched with the plate width.
    float markSize = MathF.Max(8f * scale, logo.Height - padY * 2f);
    Vector2 markMin = new(logo.Min.X + padX, logo.Min.Y + (logo.Height - markSize) * 0.5f);
    Vector2 markMax = markMin + new Vector2(markSize, markSize);
    CalcModernSvgAssets.TryDrawHpMark(draw, markMin, markMax);

    float dividerX = markMax.X + padX * 0.55f;
    float dividerPadY = logo.Height * 0.18f;
    draw.AddLine(
      new Vector2(dividerX, logo.Min.Y + dividerPadY),
      new Vector2(dividerX, logo.Max.Y - dividerPadY),
      Calc00dWireStyle.LogoDivider,
      Calc00dWireStyle.Px(Calc00dWireStyle.LogoDividerWidthRef, scale));

    DrawCaption(draw, logo, dividerX + padX * 0.55f, model.LogoCaption);
  }

  private static void DrawCaption(ImDrawListPtr draw, RectF logo, float textLeft, string caption)
  {
    float textRight = logo.Max.X - logo.Width * 0.03f;
    float fontSize = logo.Height * 0.52f;
    uint ink = Calc00dWireStyle.LogoCaptionInk;

    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.FontInkBounds bounds = CalcFaceplateFonts.MeasureArialBoldInk(caption, fontSize);
      float plateMidY = (logo.Min.Y + logo.Max.Y) * 0.5f;
      float topY = plateMidY - bounds.InkMidY;
      CalcFaceplateFonts.DrawArialBoldStretchedToWidth(draw, caption, textLeft, textRight, topY, fontSize, ink);
      return;
    }

    ImFontPtr font = ImGui.GetFont();
    Vector2 measure = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, caption);
    float y = (logo.Min.Y + logo.Max.Y - measure.Y) * 0.5f;
    draw.AddText(font, fontSize, new Vector2(textLeft, y), ink, caption);
  }

  private static void DrawBrushedAluminumPlate(ImDrawListPtr draw, RectF logo, float radius, float scale)
  {
    uint edge = Calc00dWireStyle.LogoStripEdge;
    uint center = Calc00dWireStyle.LogoStripCenter;

    // Solid rounded fill first — MultiColor fills would square the corners.
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
