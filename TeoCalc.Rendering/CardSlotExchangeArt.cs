using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Stacked left/right exchange chevrons from <c>exchange-arrow-up.svg</c>.</summary>
public static class CardSlotExchangeArt
{
  private static string ArrowPath =>
    FaceplateAssetPaths.ResolveFile("HP-65", "CardSlot", "exchange-arrow-up.svg");

  public static bool IsReady =>
    Hp65FaceplateSvgAssets.CanDrawCardSlotLabels && File.Exists(ArrowPath);

  public static float MeasureWidth(float glyphHeight) =>
    glyphHeight * 0.26f;

  public static float Draw(
    ImDrawListPtr draw,
    float x,
    float centerY,
    float glyphHeight,
    float scale,
    uint color)
  {
    float chevronH = glyphHeight * 0.22f;
    float gap = glyphHeight * 0.06f;
    float w = MeasureWidth(glyphHeight);
    float top = centerY - chevronH - gap * 0.5f;
    float bottom = centerY + gap * 0.5f;

    int rasterW = Math.Max(1, (int)MathF.Ceiling(w * 4f * scale));
    int rasterH = Math.Max(1, (int)MathF.Ceiling(chevronH * 4f * scale));

    if (IsReady)
    {
      Hp65FaceplateSvgAssets.TryDrawSvgRotated(
        draw,
        new Vector2(x, top),
        new Vector2(x + w, top + chevronH),
        ArrowPath,
        rasterW,
        rasterH,
        90f,
        color);
      Hp65FaceplateSvgAssets.TryDrawSvgRotated(
        draw,
        new Vector2(x, bottom),
        new Vector2(x + w, bottom + chevronH),
        ArrowPath,
        rasterW,
        rasterH,
        -90f,
        color);
    }
    else
    {
      DrawFallbackChevron(draw, new Vector2(x + w, top + chevronH * 0.5f), chevronH * 0.38f, right: true, color);
      DrawFallbackChevron(draw, new Vector2(x, bottom + chevronH * 0.5f), chevronH * 0.38f, right: false, color);
    }

    return w;
  }

  private static void DrawFallbackChevron(ImDrawListPtr draw, Vector2 tip, float size, bool right, uint color)
  {
    if (right)
    {
      draw.AddTriangleFilled(tip, new(tip.X - size, tip.Y - size * 0.5f), new(tip.X - size, tip.Y + size * 0.5f), color);
    }
    else
    {
      draw.AddTriangleFilled(tip, new(tip.X + size, tip.Y - size * 0.5f), new(tip.X + size, tip.Y + size * 0.5f), color);
    }
  }
}
