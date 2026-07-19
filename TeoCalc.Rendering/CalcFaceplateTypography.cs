using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Label sizes tuned against Body.svg reference (409 px wide faceplate).</summary>
public static class CalcFaceplateTypography
{
  public static float CardSlot(float scale) =>
    Ref(31f, scale, 2.9f);

  // CapAbove gold legends — closer to key-face size (was ~9.5 / too timid vs 14–16 face).
  public static float GoldShift(float scale) =>
    Ref(12.25f, scale, 1.28f);

  public static float GoldShiftSmall(float scale) =>
    Ref(10.5f, scale, 1.12f);

  public static float KeyPrimary(float scale) =>
    Ref(14.5f, scale, 1.34f);

  public static float KeyDigit(float scale) =>
    Ref(16.5f, scale, 1.48f);

  public static float KeyLetter(float scale) =>
    Ref(16f, scale, 1.44f);

  public static float KeyOperator(float scale) =>
    Ref(18.5f, scale, 1.62f);

  public static float EnterPrimary(float scale) =>
    Ref(15f, scale, 1.38f);

  public static float BlueSkirt(float scale) =>
    Ref(11.5f, scale, 1.05f);

  public static float SwitchLabel(float scale) =>
    16.5f * scale;

  public static float BrandPlate(float plateHeight) =>
    MathF.Max(plateHeight * 0.5f, 11f);

  public static void DrawBoldText(ImDrawListPtr draw, string text, Vector2 pos, float fontSize, uint color)
  {
    ImFontPtr font = ImGui.GetFont();
    float stroke = MathF.Max(0.65f, fontSize * 0.045f);
    draw.AddText(font, fontSize, pos, color, text);
    draw.AddText(font, fontSize, pos + new Vector2(stroke, 0f), color, text);
    draw.AddText(font, fontSize, pos + new Vector2(0f, stroke * 0.35f), color, text);
  }

  private static float Ref(float referencePx, float scale, float imguiMultiplier)
  {
    float fromRef = referencePx * scale;
    float fromFont = ImGui.GetFontSize() * imguiMultiplier * scale;
    return MathF.Max(fromRef, fromFont);
  }
}
