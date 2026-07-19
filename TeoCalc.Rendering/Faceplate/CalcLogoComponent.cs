using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Logo strip: brand mark + Teo caption.</summary>
public static class CalcLogoComponent
{
  public static void Draw(ImDrawListPtr draw, Vector2 stripMin, Vector2 stripMax, CalcModelDefinition model, float scale) =>
    DrawProceduralStrip(draw, stripMin, stripMax, model, scale);

  private static void DrawProceduralStrip(
    ImDrawListPtr draw,
    Vector2 stripMin,
    Vector2 stripMax,
    CalcModelDefinition model,
    float scale)
  {
    float height = stripMax.Y - stripMin.Y;
    float width = stripMax.X - stripMin.X;
    float padX = width * 0.04f;

    uint stripBase = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoStrip, model);
    uint markMetal = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoMark, model);
    uint captionInk = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoCaption, model);

    draw.AddRectFilled(stripMin, stripMax, stripBase, 2f * scale);
    DrawBrushedMetal(draw, stripMin, stripMax, scale, stripBase);

    float logoRight = DrawMonochromeHpLogo(draw, stripMin, stripMax, scale, markMetal);

    float dividerX = logoRight + padX * 0.35f;
    draw.AddLine(
      new Vector2(dividerX, stripMin.Y + height * 0.14f),
      new Vector2(dividerX, stripMax.Y - height * 0.14f),
      Darken(captionInk, 0.15f, 0x88),
      scale);

    CalcChassisRenderer.DrawBrandPlateText(
      draw,
      dividerX + padX * 0.45f,
      stripMin,
      stripMax,
      model.LogoCaption,
      captionInk,
      width * 0.03f);
  }

  private static float DrawMonochromeHpLogo(
    ImDrawListPtr draw,
    Vector2 stripMin,
    Vector2 stripMax,
    float scale,
    uint markMetal)
  {
    float height = stripMax.Y - stripMin.Y;
    float padX = (stripMax.X - stripMin.X) * 0.04f;
    float markSize = height * 0.76f;
    Vector2 center = new(stripMin.X + padX + markSize * 0.5f, (stripMin.Y + stripMax.Y) * 0.5f);
    float radius = markSize * 0.5f;

    uint rim = Darken(markMetal, 0.22f);
    uint face = Lighten(markMetal, 0.18f);
    uint hpInk = Darken(markMetal, 0.42f);

    draw.AddCircleFilled(center, radius + scale * 0.8f, rim);
    draw.AddCircleFilled(center, radius, face);

    float fontSize = markSize * 0.36f;
    ImFontPtr font = CalcFaceplateFonts.IsArialBoldReady ? CalcFaceplateFonts.ArialBold : ImGui.GetFont();
    Vector2 textSize = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, "hp");
    draw.AddText(font, fontSize, center - textSize * 0.5f, hpInk, "hp");

    return center.X + radius;
  }

  private static void DrawBrushedMetal(ImDrawListPtr draw, Vector2 stripMin, Vector2 stripMax, float scale, uint baseColor)
  {
    float height = stripMax.Y - stripMin.Y;
    int lines = (int)Math.Clamp(height / (1.0f * scale), 16, 40);
    for (int line = 0; line < lines; line++)
    {
      float y = stripMin.Y + line / (float)Math.Max(1, lines - 1) * height;
      uint color = line % 2 == 0 ? 0x38FFFFFFu : 0x28000000u;
      draw.AddLine(new Vector2(stripMin.X, y), new Vector2(stripMax.X, y), color, scale * 0.35f);
    }
  }

  private static uint Lighten(uint color, float amount, byte alpha = 0xFF) =>
    MixColor(color, 0xFFFFFFFF, amount, alpha);

  private static uint Darken(uint color, float amount, byte alpha = 0xFF) =>
    MixColor(color, 0xFF000000, amount, alpha);

  private static uint MixColor(uint color, uint target, float amount, byte alpha)
  {
    amount = Math.Clamp(amount, 0f, 1f);
    byte r = (byte)(((color >> 16) & 0xFF) + ((((target >> 16) & 0xFF) - ((color >> 16) & 0xFF)) * amount));
    byte g = (byte)(((color >> 8) & 0xFF) + ((((target >> 8) & 0xFF) - ((color >> 8) & 0xFF)) * amount));
    byte b = (byte)((color & 0xFF) + (((target & 0xFF) - (color & 0xFF)) * amount));
    return ((uint)alpha << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
  }
}
