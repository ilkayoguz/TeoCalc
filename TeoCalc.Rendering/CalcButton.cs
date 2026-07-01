using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

public enum CalcButtonStyle
{
  Black,
  Grey,
  Orange,
  Blue,
}

/// <summary>Procedural HP key cap — interactive faceplate button component (no SVG).</summary>
public static class CalcButton
{
  private const float SkirtRatio = 0.36f;

  private const float WellInsetRatio = 0.06f;

  private const float PressTravelRatio = 0.045f;

  public static CalcButtonStyle StyleForKeyIndex(int keyChartIndex)
  {
    return keyChartIndex switch
    {
      10 or 11 => CalcButtonStyle.Orange,
      14 => CalcButtonStyle.Blue,
      >= 20 and <= 37 => CalcButtonStyle.Grey,
      15 or 16 or 17 or 18 or 19 => CalcButtonStyle.Grey,
      38 => CalcButtonStyle.Black,
      _ => CalcButtonStyle.Black,
    };
  }

  public static bool Draw(
    ImDrawListPtr draw,
    string id,
    Vector2 min,
    Vector2 max,
    CalcButtonStyle style,
    CalcButtonKind kind,
    string primary,
    string? goldOnBody,
    string? blueOnBody,
    float scale,
    bool leftAlignPrimary = false,
    bool drawWell = true,
    bool overlayOnly = false)
  {
    if (!overlayOnly && drawWell)
    {
      DrawWell(draw, min, max, scale);
    }

    Vector2 size = max - min;
    ImGui.SetCursorScreenPos(min);
    bool clicked = ImGui.InvisibleButton(id, size);
    bool hovered = ImGui.IsItemHovered();
    bool pressed = ImGui.IsItemActive();

    if (overlayOnly)
    {
      return clicked;
    }

    Vector2 capMin = min + size * WellInsetRatio;
    Vector2 capMax = max - size * new Vector2(WellInsetRatio, WellInsetRatio * 0.55f);
    float pressTravel = MathF.Max(scale * 1.25f, size.Y * PressTravelRatio);
    if (pressed)
    {
      capMin.Y += pressTravel;
      capMax.Y += pressTravel;
    }

    DrawCap(draw, capMin, capMax, style, hovered, pressed, scale);
    DrawPrimaryLabel(draw, primary, capMin, capMax, kind, scale, leftAlignPrimary);

    if (!string.IsNullOrEmpty(blueOnBody))
    {
      float skirtY = capMin.Y + (capMax.Y - capMin.Y) * (1f - SkirtRatio);
      DrawBodyLabel(
        draw,
        blueOnBody,
        new Vector2((capMin.X + capMax.X) * 0.5f, skirtY + (capMax.Y - skirtY) * 0.55f),
        CalcChassisPalette.BlueLabel,
        scale * 0.58f);
    }

    return clicked;
  }

  public static void DrawWell(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale)
  {
    float rounding = Math.Clamp(MathF.Min(max.X - min.X, max.Y - min.Y) * 0.1f, 2.5f * scale, 6f * scale);
    draw.AddRectFilled(min, max, CalcChassisPalette.KeyWellEdge, rounding + scale);
    draw.AddRectFilled(min + new Vector2(scale * 0.5f), max - new Vector2(scale * 0.5f), CalcChassisPalette.KeyWell, rounding);
  }

  private static void DrawCap(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonStyle style,
    bool hovered,
    bool pressed,
    float scale)
  {
    (uint top, uint face, uint skirt) = Colors(style, hovered, pressed);
    float rounding = Math.Clamp(MathF.Min(capMax.X - capMin.X, capMax.Y - capMin.Y) * 0.12f, 2f * scale, 5f * scale);
    float skirtY = capMin.Y + (capMax.Y - capMin.Y) * (1f - SkirtRatio);
    Vector2 midY = new(capMin.X, capMin.Y + (capMax.Y - capMin.Y) * 0.44f);

    draw.AddRectFilled(capMin, capMax, face, rounding);
    draw.AddRectFilledMultiColor(capMin, midY, top, top, face, face);
    draw.AddRectFilled(new Vector2(capMin.X, skirtY), capMax, skirt, rounding, ImDrawFlags.RoundCornersBottom);

    if (pressed)
    {
      float tintHeight = (capMax.Y - capMin.Y) * 0.38f;
      Vector2 tintMax = new(capMax.X, capMin.Y + tintHeight);
      uint shade = Rgba(0, 0, 0, 52);
      draw.AddRectFilledMultiColor(capMin, tintMax, shade, shade, Rgba(0, 0, 0, 8), Rgba(0, 0, 0, 8));
    }

    if (!pressed)
    {
      byte highlightAlpha = (byte)(hovered ? 50 : 36);
      draw.AddLine(
        capMin + new Vector2(rounding * 0.6f, scale * 0.9f),
        new Vector2(capMax.X - rounding * 0.6f, capMin.Y + scale * 0.9f),
        Rgba(255, 255, 255, highlightAlpha),
        MathF.Max(1f, scale * 0.85f));
    }
  }

  private static (uint Top, uint Face, uint Skirt) Colors(CalcButtonStyle style, bool hovered, bool pressed)
  {
    byte bump = (byte)(hovered && !pressed ? 10 : 0);
    byte drop = (byte)(pressed ? 16 : 0);
    byte skirtDrop = (byte)(pressed ? 18 : 0);
    return style switch
    {
      CalcButtonStyle.Grey => (
        Sub(Add(CalcChassisPalette.KeyGreyTop, bump), drop),
        Sub(CalcChassisPalette.KeyGreyFace, drop),
        Sub(CalcChassisPalette.KeyGreySkirt, skirtDrop)),
      CalcButtonStyle.Orange => (
        Sub(Add(CalcChassisPalette.KeyOrangeTop, bump), drop),
        Sub(CalcChassisPalette.KeyOrangeFace, drop),
        Sub(CalcChassisPalette.KeyOrangeSkirt, skirtDrop)),
      CalcButtonStyle.Blue => (
        Sub(Add(CalcChassisPalette.KeyBlueTop, bump), drop),
        Sub(CalcChassisPalette.KeyBlueFace, drop),
        Sub(CalcChassisPalette.KeyBlueSkirt, skirtDrop)),
      _ => (
        Sub(Add(CalcChassisPalette.KeyBlackTop, bump), drop),
        Sub(CalcChassisPalette.KeyBlackFace, drop),
        Sub(CalcChassisPalette.KeyBlackSkirt, skirtDrop)),
    };
  }

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;

  private static void DrawPrimaryLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonKind kind,
    float scale,
    bool leftAlign)
  {
    float skirtY = capMin.Y + (capMax.Y - capMin.Y) * (1f - SkirtRatio);
    float centerY = capMin.Y + (skirtY - capMin.Y) * 0.52f;

    if (kind == CalcButtonKind.EnterWide)
    {
      DrawEnterLabel(draw, capMin, capMax, skirtY, scale, leftAlign);
      return;
    }

    if (kind == CalcButtonKind.OperatorColon)
    {
      DrawColonDivideSymbol(draw, new Vector2((capMin.X + capMax.X) * 0.5f, centerY), capMax.X - capMin.X, scale);
      return;
    }

    float fontSize = ImGui.GetFontSize() * 0.88f * scale;
    Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float x = leftAlign ? capMin.X + scale * 5f : (capMin.X + capMax.X - size.X) * 0.5f;
    float y = centerY - size.Y * 0.5f;
    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), CalcChassisPalette.KeyText, text);
  }

  private static void DrawEnterLabel(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    float skirtY,
    float scale,
    bool leftAlign)
  {
    float fontSize = ImGui.GetFontSize() * 0.78f * scale;
    string enterText = "ENTER";
    Vector2 textSize = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, enterText);
    float arrowWidth = fontSize * 0.55f;
    float gap = scale * 3f;
    float totalWidth = textSize.X + gap + arrowWidth;
    float startX = leftAlign
      ? capMin.X + scale * 5f
      : (capMin.X + capMax.X - totalWidth) * 0.5f;
    float y = capMin.Y + (skirtY - capMin.Y - textSize.Y) * 0.52f;
    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(startX, y), CalcChassisPalette.KeyText, enterText);

    float arrowCenterX = startX + textSize.X + gap + arrowWidth * 0.5f;
    float arrowBaseY = y + textSize.Y * 0.72f;
    float arrowHeight = fontSize * 0.72f;
    Vector2 tip = new(arrowCenterX, y + textSize.Y * 0.08f);
    Vector2 left = new(arrowCenterX - arrowWidth * 0.45f, arrowBaseY);
    Vector2 right = new(arrowCenterX + arrowWidth * 0.45f, arrowBaseY);
    draw.AddTriangleFilled(tip, left, right, CalcChassisPalette.KeyText);
    draw.AddLine(left, right, CalcChassisPalette.KeyText, MathF.Max(1f, scale * 0.9f));
  }

  private static void DrawColonDivideSymbol(ImDrawListPtr draw, Vector2 center, float capWidth, float scale)
  {
    float symbolHeight = MathF.Min(capWidth * 0.42f, ImGui.GetFontSize() * 1.05f * scale);
    float lineWidth = MathF.Max(2f, capWidth * 0.34f);
    float dotRadius = MathF.Max(1.4f, scale * 1.5f);
    float topDotY = center.Y - symbolHeight * 0.34f;
    float lineY = center.Y;
    float bottomDotY = center.Y + symbolHeight * 0.34f;
    draw.AddCircleFilled(new Vector2(center.X, topDotY), dotRadius, CalcChassisPalette.KeyText);
    draw.AddLine(
      new Vector2(center.X - lineWidth * 0.5f, lineY),
      new Vector2(center.X + lineWidth * 0.5f, lineY),
      CalcChassisPalette.KeyText,
      MathF.Max(1.2f, scale * 1.1f));
    draw.AddCircleFilled(new Vector2(center.X, bottomDotY), dotRadius, CalcChassisPalette.KeyText);
  }

  private static void DrawBodyLabel(ImDrawListPtr draw, string text, Vector2 center, uint color, float scale)
  {
    float fontSize = ImGui.GetFontSize() * scale;
    Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    draw.AddText(ImGui.GetFont(), fontSize, center - size * 0.5f, color, text);
  }

  private static uint Add(uint color, byte amount)
  {
    byte r = (byte)Math.Min(255, (color & 0xFF) + amount);
    byte g = (byte)Math.Min(255, ((color >> 8) & 0xFF) + amount);
    byte b = (byte)Math.Min(255, ((color >> 16) & 0xFF) + amount);
  byte a = (byte)(color >> 24);
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }

  private static uint Sub(uint color, byte amount)
  {
    byte r = (byte)Math.Max(0, (color & 0xFF) - amount);
    byte g = (byte)Math.Max(0, ((color >> 8) & 0xFF) - amount);
    byte b = (byte)Math.Max(0, ((color >> 16) & 0xFF) - amount);
    byte a = (byte)(color >> 24);
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }
}
