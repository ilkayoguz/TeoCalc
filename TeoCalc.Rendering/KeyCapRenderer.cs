using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Procedural Key2.svg cap (body / top / face) using palette tones — no SVG rasterization.</summary>
public static class KeyCapRenderer
{
  private const float KeyWidth = 48f;

  private const float KeyHeight = 38f;

  /// <summary>Key2 top band height (px at 48×38).</summary>
  private const float TopHeightRatio = 22.8f / KeyHeight;

  /// <summary>Key2 face inset from cap top.</summary>
  private const float FaceTopRatio = 2.5f / KeyHeight;

  /// <summary>Key2 corner radius (px at 48 wide).</summary>
  private const float CornerRadiusRatio = 2.4f / KeyWidth;

  public static void Draw(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    KeyCapPalette palette,
    bool hovered,
    bool pressed,
    float scale,
    bool fixedFaceRadius = false)
  {
    float width = capMax.X - capMin.X;
    float height = capMax.Y - capMin.Y;
    float radiusWidth = fixedFaceRadius ? height * (KeyWidth / KeyHeight) : width;
    float rx = Math.Clamp(radiusWidth * CornerRadiusRatio, 2f * scale, 4.5f * scale);

    draw.AddRectFilled(capMin, capMax, palette.Skirt, rx);

    Vector2 topMax = capMin + new Vector2(width, height * TopHeightRatio);
    draw.AddRectFilled(capMin, topMax, palette.Top, rx, ImDrawFlags.RoundCornersTop);

    Vector2 faceMin = capMin + new Vector2(1f, height * FaceTopRatio);
    Vector2 faceMax = capMin + new Vector2(width - 1f, height * KeyCapGeometry.FaceLabelEndRatio);
    draw.AddRectFilled(faceMin, faceMax, palette.Face, rx);

    if (pressed)
    {
      float tintHeight = height * 0.38f;
      Vector2 tintMax = new(capMax.X, capMin.Y + tintHeight);
      uint shade = Rgba(0, 0, 0, 52);
      draw.AddRectFilledMultiColor(capMin, tintMax, shade, shade, Rgba(0, 0, 0, 8), Rgba(0, 0, 0, 8));
    }
    else if (hovered)
    {
      draw.AddLine(
        capMin + new Vector2(rx * 0.6f, scale * 0.9f),
        new Vector2(capMax.X - rx * 0.6f, capMin.Y + scale * 0.9f),
        Rgba(255, 255, 255, 50),
        MathF.Max(1f, scale * 0.85f));
    }
    else
    {
      draw.AddLine(
        capMin + new Vector2(rx * 0.6f, scale * 0.9f),
        new Vector2(capMax.X - rx * 0.6f, capMin.Y + scale * 0.9f),
        Rgba(255, 255, 255, 36),
        MathF.Max(1f, scale * 0.85f));
    }
  }

  public static void DrawBezel(ImDrawListPtr draw, Vector2 capMin, Vector2 capMax, float scale, bool fixedRadius = false)
  {
    float pad = MathF.Max(1.25f, scale * 1.1f);
    float rounding = fixedRadius
      ? Math.Clamp(4f * scale, 3f, 4.5f)
      : Math.Clamp((capMax.X - capMin.X) * (4f / KeyWidth), 3f, 5f);
    Vector2 bezelMin = capMin - new Vector2(pad, pad);
    Vector2 bezelMax = capMax + new Vector2(pad, pad);
    draw.AddRectFilled(bezelMin, bezelMax, CalcChassisPalette.KeyCapBezel, rounding + pad * 0.5f);
  }

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
}
