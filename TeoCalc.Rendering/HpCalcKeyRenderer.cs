using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

public readonly record struct HpCalcKeyVisual(
  string Primary,
  string? GoldShift = null,
  string? BlueShift = null,
  FaceplateLabelStyle LabelStyle = FaceplateLabelStyle.Normal);

public static class HpCalcKeyRenderer
{
  private const float SkirtRatio = 0.34f;

  private const float WellInset = 2.5f;

  public static void DrawFaceplateBackground(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale = 1f)
  {
    uint baseColor = Rgba(14, 14, 16);
    uint grainColor = Rgba(22, 22, 25, 90);
    float rounding = 6f * scale;
    draw.AddRectFilled(min, max, baseColor, rounding);

    int grains = (int)Math.Clamp(120 * scale, 90, 220);
    int seed = (int)(min.X * 17 + min.Y * 31);
    for (int grain = 0; grain < grains; grain++)
    {
      seed = seed * 1664525 + 1013904223;
      float u = (seed & 0xFFFF) / 65535f;
      seed = seed * 1664525 + 1013904223;
      float v = (seed & 0xFFFF) / 65535f;
      Vector2 point = new(
        min.X + 4f + u * MathF.Max(8f, max.X - min.X - 8f),
        min.Y + 4f + v * MathF.Max(8f, max.Y - min.Y - 8f));
      draw.AddCircleFilled(point, (0.65f + (grain % 3) * 0.2f) * scale, grainColor);
    }
  }

  public static void Draw(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    in HpCalcKeyVisual visual,
    bool hovered,
    bool pressed,
    float scale = 1f)
  {
    float width = max.X - min.X;
    float height = max.Y - min.Y;
    float rounding = Math.Clamp(MathF.Min(width, height) * 0.14f, 3f * scale, 8f * scale);
    float pressOffset = pressed ? 1.25f * scale : 0f;
    float wellInset = WellInset * scale;

    uint well = Rgba(6, 6, 8);
    uint wellEdge = Rgba(2, 2, 3);
    draw.AddRectFilled(min, max, wellEdge, rounding + 1.5f);
    draw.AddRectFilled(min + new Vector2(0.5f, 0.5f), max - new Vector2(0.5f, 0.5f), well, rounding + 1f);

    Vector2 capMin = min + new Vector2(wellInset, wellInset + pressOffset);
    Vector2 capMax = max - new Vector2(wellInset, wellInset - pressOffset * 0.35f);
    if (capMax.Y - capMin.Y < 8f || capMax.X - capMin.X < 8f)
    {
      return;
    }

    uint capTop = pressed ? Rgba(34, 34, 38) : hovered ? Rgba(48, 48, 54) : Rgba(42, 42, 48);
    uint capMid = pressed ? Rgba(18, 18, 22) : Rgba(24, 24, 28);
    uint capBottom = pressed ? Rgba(12, 12, 15) : Rgba(16, 16, 20);
    Vector2 capMidY = new(capMin.X, capMin.Y + (capMax.Y - capMin.Y) * 0.42f);
    draw.AddRectFilled(capMin, capMax, capMid, rounding);
    draw.AddRectFilledMultiColor(capMin, capMidY, capTop, capTop, capMid, capMid);
    draw.AddRectFilledMultiColor(capMidY, capMax, capMid, capMid, capBottom, capBottom);

    draw.AddLine(
      capMin + new Vector2(rounding * 0.7f, 1.2f),
      new Vector2(capMax.X - rounding * 0.7f, capMin.Y + 1.2f),
      Rgba(255, 255, 255, (byte)(hovered ? 55 : 38)),
      1f);
    draw.AddLine(
      new Vector2(capMax.X - 1.1f, capMin.Y + rounding),
      new Vector2(capMax.X - 1.1f, capMax.Y - rounding * 0.5f),
      Rgba(0, 0, 0, 90),
      1f);

    float skirtY = capMin.Y + (capMax.Y - capMin.Y) * (1f - SkirtRatio);
    draw.AddRectFilled(
      new Vector2(capMin.X, skirtY),
      capMax,
      Rgba(10, 10, 13),
      rounding,
      ImDrawFlags.RoundCornersBottom);

    if (!string.IsNullOrEmpty(visual.GoldShift))
    {
      DrawCaption(draw, visual.GoldShift, min + new Vector2(width * 0.5f, -1f * scale), Rgba(214, 168, 58), 0.72f * scale);
    }

    if (visual.LabelStyle == FaceplateLabelStyle.Vertical)
    {
      DrawVerticalPrimary(draw, visual.Primary, capMin, capMax, skirtY, scale);
    }
    else
    {
      DrawPrimary(draw, visual.Primary, capMin, capMax, skirtY, scale);
    }

    if (!string.IsNullOrEmpty(visual.BlueShift))
    {
      Vector2 skirtCenter = new((capMin.X + capMax.X) * 0.5f, skirtY + (capMax.Y - skirtY) * 0.58f);
      DrawCaption(draw, visual.BlueShift, skirtCenter, Rgba(72, 168, 236), 0.78f * scale);
    }
  }

  private static void DrawPrimary(ImDrawListPtr draw, string text, Vector2 capMin, Vector2 capMax, float skirtY, float scale)
  {
    Vector2 center = new((capMin.X + capMax.X) * 0.5f, capMin.Y + (skirtY - capMin.Y) * 0.56f);
    DrawCaption(draw, text, center, Rgba(236, 236, 240), 0.92f * scale);
  }

  private static void DrawVerticalPrimary(ImDrawListPtr draw, string text, Vector2 capMin, Vector2 capMax, float skirtY, float scale)
  {
    float usableTop = capMin.Y + 6f * scale;
    float usableBottom = skirtY - 4f * scale;
    float step = (usableBottom - usableTop) / MathF.Max(1, text.Length);
    float fontSize = ImGui.GetFontSize() * 0.82f * scale;
    for (int index = 0; index < text.Length; index++)
    {
      string letter = text[index].ToString();
      Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, letter);
      Vector2 pos = new(
        (capMin.X + capMax.X - size.X) * 0.5f,
        usableTop + step * index + (step - size.Y) * 0.5f);
      draw.AddText(ImGui.GetFont(), fontSize, pos, Rgba(236, 236, 240), letter);
    }
  }

  private static void DrawCaption(ImDrawListPtr draw, string text, Vector2 center, uint color, float scale)
  {
    if (string.IsNullOrEmpty(text))
    {
      return;
    }

    float fontSize = ImGui.GetFontSize() * scale;
    Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    Vector2 pos = center - size * 0.5f;
    draw.AddText(ImGui.GetFont(), fontSize, pos, color, text);
  }

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
}
