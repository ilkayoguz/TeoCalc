using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;
/// <summary>Footer strip: hp mark on the left, HEWLETT-PACKARD {model.Id} on the right.</summary>
public static class CalcLogoComponent
{
  public static void Draw(ImDrawListPtr draw, Vector2 stripMin, Vector2 stripMax, CalcModelDefinition model, float scale)
  {
    float height = stripMax.Y - stripMin.Y;
    float padX = 8f * scale;
    uint strip = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoStrip, model);
    uint markInk = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoMark, model);
    uint captionInk = CalcKeyColorPalette.Resolve(CalcKeyColorPalette.LogoCaption, model);

    draw.AddRectFilled(stripMin, stripMax, strip, 2f * scale);

    float textLeft = stripMin.X + padX;
    if (model.Id == "65" && Hp65FaceplateSvgAssets.TryDrawLogoMark(draw, stripMin, stripMax, scale))
    {
      textLeft = stripMin.X + stripMax.Y * 0.95f * (888f / 562f) * 1.4f + padX;
    }
    else
    {
      float markSize = height * 0.55f;
      Vector2 markCenter = new(stripMin.X + padX + markSize * 0.5f, (stripMin.Y + stripMax.Y) * 0.5f);
      draw.AddCircleFilled(markCenter, markSize * 0.5f, markInk);
      draw.AddText(
        ImGui.GetFont(),
        markSize * 0.45f,
        markCenter - new Vector2(markSize * 0.22f, markSize * 0.2f),
        0xFFFFFFFFu,
        "hp");
    }

    string caption = model.LogoCaption;
    float fontSize = height * 0.38f;
    Vector2 textSize = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, caption);
    Vector2 textPos = new(
      stripMax.X - padX - textSize.X,
      stripMin.Y + (height - textSize.Y) * 0.5f);
    draw.AddText(ImGui.GetFont(), fontSize, textPos, captionInk, caption);
  }
}
