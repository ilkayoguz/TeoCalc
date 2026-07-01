using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class HpCalcDisplayRenderer
{
  public static float Draw(
    ImDrawListPtr draw,
    Vector2 origin,
    float width,
    float height,
    ClassicCpu cpu,
    bool programMode)
  {
    Vector2 min = origin;
    Vector2 max = origin + new Vector2(width, height);
    uint bezel = Rgba(8, 8, 10);
    uint glass = Rgba(18, 4, 2);

    draw.AddRectFilled(min, max, bezel, 5f);
    Vector2 inset = new(4f, 4f);
    Vector2 glassMin = min + inset;
    Vector2 glassMax = max - inset;
    draw.AddRectFilled(glassMin, glassMax, glass, 3f);
    draw.AddRect(glassMin, glassMax, Rgba(48, 12, 8), 3f, ImDrawFlags.None, 1f);

    string display = ClassicDisplayFormatter.FormatXRegister(cpu, programMode);
    float fontSize = Math.Clamp(height * 0.52f, 18f, 42f);
    Vector2 textSize = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, display);
    Vector2 textPos = new(
      glassMin.X + MathF.Max(8f, (glassMax.X - glassMin.X - textSize.X) * 0.5f),
      glassMin.Y + (glassMax.Y - glassMin.Y - textSize.Y) * 0.5f);
    draw.AddText(ImGui.GetFont(), fontSize, textPos + new Vector2(1f, 1f), Rgba(120, 12, 0, 90), display);
    draw.AddText(ImGui.GetFont(), fontSize, textPos, Rgba(255, 72, 32), display);

    if (programMode)
    {
      float badgeSize = Math.Clamp(height * 0.22f, 8f, 12f);
      Vector2 badgePos = new(glassMax.X - badgeSize * 3.2f, glassMin.Y + 4f);
      draw.AddText(ImGui.GetFont(), badgeSize, badgePos, Rgba(255, 96, 48), "PRGM");
    }

    return height + 8f;
  }

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
}
