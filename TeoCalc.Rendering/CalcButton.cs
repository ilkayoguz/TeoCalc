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

/// <summary>HP key cap — KeyCap.svg when loaded, procedural fallback.</summary>
public static class CalcButton
{
  private const float SkirtRatio = 1f - KeyCapGeometry.FaceEndRatio;

  private const float WellInsetRatio = 0.06f;

  private const float PressTravelRatio = 0.045f;

  private const float FaceLabelDropPx = 2f;

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
    if (!overlayOnly && drawWell && !Hp65FaceplateSvgAssets.CanDrawKeyCaps)
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

    float insetX = WellInsetRatio;
    Vector2 capMin = min + new Vector2(size.X * insetX, size.Y * WellInsetRatio);
    Vector2 capMax = max - new Vector2(size.X * insetX, size.Y * WellInsetRatio * 0.55f);
    float pressTravel = MathF.Max(scale * 1.25f, size.Y * PressTravelRatio);
    if (pressed)
    {
      capMin.Y += pressTravel;
      capMax.Y += pressTravel;
    }

    DrawCap(draw, capMin, capMax, style, kind, hovered, pressed, scale);
    DrawPrimaryLabel(draw, primary, capMin, capMax, kind, style, scale, leftAlignPrimary);

    if (!string.IsNullOrEmpty(blueOnBody))
    {
      float skirtFont = CalcFaceplateTypography.BlueSkirt(scale) * CalcKeyLabelPalette.SkirtLabelFontScale(blueOnBody);
      DrawSkirtLabel(
        draw,
        blueOnBody,
        capMin,
        capMax,
        style,
        kind,
        CalcKeyLabelPalette.SkirtLabelInk(blueOnBody, style),
        skirtFont,
        scale);
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
    CalcButtonKind kind,
    bool hovered,
    bool pressed,
    float scale)
  {
    if (kind == CalcButtonKind.EnterWide && Hp65FaceplateSvgAssets.CanDrawKeyCaps)
    {
      DrawKeyCapBezel(draw, capMin, capMax, scale, fixedRadius: true);
      DrawEnterWideCapProcedural(draw, capMin, capMax, style, pressed, scale);
      DrawGreySkirtBandIfNeeded(draw, capMin, capMax, style, scale);
      return;
    }

    if (Hp65FaceplateSvgAssets.CanDrawKeyCaps)
    {
      DrawKeyCapBezel(draw, capMin, capMax, scale);
      Hp65FaceplateSvgAssets.DrawKeyCap(draw, capMin, capMax, style);
      DrawGreySkirtBandIfNeeded(draw, capMin, capMax, style, scale);
      if (pressed)
      {
        float tintHeight = (capMax.Y - capMin.Y) * 0.38f;
        Vector2 tintMax = new(capMax.X, capMin.Y + tintHeight);
        uint shade = Rgba(0, 0, 0, 52);
        draw.AddRectFilledMultiColor(capMin, tintMax, shade, shade, Rgba(0, 0, 0, 8), Rgba(0, 0, 0, 8));
      }

      return;
    }

    (uint top, uint face, uint skirt) = Colors(style, hovered, pressed);
    float rounding = Math.Clamp(MathF.Min(capMax.X - capMin.X, capMax.Y - capMin.Y) * 0.12f, 2f * scale, 5f * scale);
    float skirtY = capMin.Y + (capMax.Y - capMin.Y) * (1f - SkirtRatio);
    Vector2 midY = new(capMin.X, capMin.Y + (capMax.Y - capMin.Y) * 0.44f);

    draw.AddRectFilled(capMin, capMax, face, rounding);
    draw.AddRectFilledMultiColor(capMin, midY, top, top, face, face);
    draw.AddRectFilled(new Vector2(capMin.X, skirtY), capMax, skirt, rounding, ImDrawFlags.RoundCornersBottom);
    DrawGreySkirtBandIfNeeded(draw, capMin, capMax, style, scale);

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
    CalcButtonStyle style,
    float scale,
    bool leftAlign)
  {
    uint ink = CalcKeyLabelPalette.PrimaryOnCap(style);

    if (kind == CalcButtonKind.EnterWide)
    {
      DrawEnterLabel(draw, capMin, capMax, scale, ink);
      return;
    }

    if (kind == CalcButtonKind.OperatorColon)
    {
      DrawFaceCenteredOperatorColon(draw, capMin, capMax, scale, ink);
      return;
    }

    DrawFaceCenteredKeyLabel(draw, text, capMin, capMax, scale, ink);
  }

  private static void DrawFaceCenteredKeyLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 capMin,
    Vector2 capMax,
    float scale,
    uint ink)
  {
    (Vector2 faceMin, Vector2 faceMax) = KeyCapGeometry.FaceRect(capMin, capMax);
    float fontSize = CalcFaceplateTypography.KeyPrimary(scale);
    HpClassicFaceplateGlyphs.LabelSize labelSize = HpClassicFaceplateGlyphs.MeasureKeyFaceLabel(text, fontSize);
    float centerX = (faceMin.X + faceMax.X) * 0.5f;
    float centerY = (faceMin.Y + faceMax.Y) * 0.5f;
    if (text is "f\u207b\u00b9" or "f⁻¹")
    {
      centerX += fontSize * 0.12f;
      centerY -= fontSize * 0.02f;
    }
    else if (text == "g")
    {
      centerY -= fontSize * 0.26f;
    }

    float x = centerX - labelSize.Width * 0.5f;
    float y = centerY - labelSize.Height * 0.5f + FaceLabelDropPx;
    HpClassicFaceplateGlyphs.DrawKeyFaceLabel(draw, new Vector2(x, y), text, fontSize, ink, scale);
  }

  private static void DrawFaceCenteredOperatorColon(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    float scale,
    uint ink)
  {
    (Vector2 faceMin, Vector2 faceMax) = KeyCapGeometry.FaceRect(capMin, capMax);
    float centerX = (faceMin.X + faceMax.X) * 0.5f;
    float centerY = (faceMin.Y + faceMax.Y) * 0.5f + FaceLabelDropPx;
    DrawColonDivideSymbol(draw, new Vector2(centerX, centerY), faceMax.X - faceMin.X, scale, ink);
  }

  private static void DrawEnterLabel(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    float scale,
    uint ink)
  {
    float fontSize = CalcFaceplateTypography.EnterPrimary(scale);
    string enterText = "ENTER";
    Vector2 textSize = CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(enterText, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, enterText);
    float arrowWidth = fontSize * 0.55f;
    float gap = scale * 3f;
    float totalWidth = textSize.X + gap + arrowWidth;
    float totalHeight = MathF.Max(textSize.Y, fontSize * 0.72f);

    (Vector2 faceMin, Vector2 faceMax) = KeyCapGeometry.FaceRect(capMin, capMax);
    float centerX = (faceMin.X + faceMax.X) * 0.5f;
    float centerY = (faceMin.Y + faceMax.Y) * 0.5f + FaceLabelDropPx;
    float startX = centerX - totalWidth * 0.5f;
    float y = centerY - totalHeight * 0.5f;

    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.DrawArialBoldTop(draw, enterText, startX, y, fontSize, ink);
    }
    else
    {
      draw.AddText(ImGui.GetFont(), fontSize, new Vector2(startX, y), ink, enterText);
    }

    float arrowCenterX = startX + textSize.X + gap + arrowWidth * 0.5f;
    float capTop = y + textSize.Y * 0.1f;
    float capBottom = y + textSize.Y * 0.88f;
    DrawInlineUpArrow(draw, arrowCenterX - arrowWidth * 0.5f, capTop, capBottom, fontSize, ink, scale);
  }

  private static float DrawInlineUpArrow(
    ImDrawListPtr draw,
    float x,
    float capTop,
    float capBottom,
    float fontSize,
    uint color,
    float scale)
  {
    float capHeight = capBottom - capTop;
    float w = fontSize * 0.3f;
    float thickness = MathF.Max(3.2f, scale * 2.25f);
    float cx = x + w * 0.5f;
    float arrowHeight = capHeight * 0.88f;
    float arrowBottom = capBottom - (capHeight - arrowHeight) * 0.5f;
    float tipY = arrowBottom - arrowHeight;
    float headH = arrowHeight * 0.38f;
    float headBottom = tipY + headH;

    draw.AddLine(new Vector2(cx, arrowBottom), new Vector2(cx, headBottom), color, thickness);
    draw.AddTriangleFilled(new Vector2(cx, tipY), new Vector2(x, headBottom), new Vector2(x + w, headBottom), color);
    return w + fontSize * 0.03f;
  }

  private static void DrawEnterWideCapProcedural(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonStyle style,
    bool pressed,
    float scale)
  {
    (uint top, uint face, uint skirt) = Colors(style, hovered: false, pressed);
    float rounding = Math.Clamp(4f * scale, 3f, 4.5f);
    float skirtY = capMin.Y + (capMax.Y - capMin.Y) * KeyCapGeometry.FaceEndRatio;
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
    else
    {
      byte highlightAlpha = 36;
      draw.AddLine(
        capMin + new Vector2(rounding * 0.6f, scale * 0.9f),
        new Vector2(capMax.X - rounding * 0.6f, capMin.Y + scale * 0.9f),
        Rgba(255, 255, 255, highlightAlpha),
        MathF.Max(1f, scale * 0.85f));
    }
  }

  private static void DrawColonDivideSymbol(ImDrawListPtr draw, Vector2 center, float capWidth, float scale, uint ink)
  {
    float symbolHeight = MathF.Min(capWidth * 0.42f, ImGui.GetFontSize() * 1.05f * scale);
    float lineWidth = MathF.Max(2f, capWidth * 0.34f);
    float dotRadius = MathF.Max(1.4f, scale * 1.5f);
    float topDotY = center.Y - symbolHeight * 0.34f;
    float lineY = center.Y;
    float bottomDotY = center.Y + symbolHeight * 0.34f;
    draw.AddCircleFilled(new Vector2(center.X, topDotY), dotRadius, ink);
    draw.AddLine(
      new Vector2(center.X - lineWidth * 0.5f, lineY),
      new Vector2(center.X + lineWidth * 0.5f, lineY),
      ink,
      MathF.Max(1.2f, scale * 1.1f));
    draw.AddCircleFilled(new Vector2(center.X, bottomDotY), dotRadius, ink);
  }

  private static void DrawKeyCapBezel(ImDrawListPtr draw, Vector2 capMin, Vector2 capMax, float scale, bool fixedRadius = false)
  {
    const float pad = 1f;
    float rounding = fixedRadius
      ? Math.Clamp(4f * scale, 3f, 4.5f)
      : Math.Clamp((capMax.X - capMin.X) * (4f / 48f), 3f, 5f);
    Vector2 bezelMin = capMin - new Vector2(pad, pad);
    Vector2 bezelMax = capMax + new Vector2(pad, pad);
    draw.AddRectFilled(bezelMin, bezelMax, CalcChassisPalette.KeyCapBezel, rounding + pad);
  }

  private static void DrawGreySkirtBandIfNeeded(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonStyle style,
    float scale)
  {
    if (style != CalcButtonStyle.Grey)
    {
      return;
    }

    (Vector2 skirtMin, Vector2 skirtMax) = KeyCapGeometry.SkirtRect(capMin, capMax);
    draw.AddRectFilled(skirtMin, skirtMax, CalcChassisPalette.GreySkirtLabelBand, 2f * scale);
  }

  private static void DrawSkirtLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonStyle style,
    CalcButtonKind kind,
    uint color,
    float fontSize,
    float scale)
  {
    (Vector2 skirtMin, Vector2 skirtMax) = KeyCapGeometry.SkirtRect(capMin, capMax);
    float skirtHeight = skirtMax.Y - skirtMin.Y;
    Vector2 skirtCenter = new((skirtMin.X + skirtMax.X) * 0.5f, skirtMin.Y + skirtHeight * 0.46f);
    if (kind == CalcButtonKind.EnterWide)
    {
      float pad = scale * 2.5f;
      HpClassicFaceplateGlyphs.LabelSize size = HpClassicFaceplateGlyphs.MeasureSkirtLabel(text, fontSize);
      Vector2 topLeft = new(
        skirtMax.X - pad - size.Width,
        skirtCenter.Y - size.Height * 0.5f);
      HpClassicFaceplateGlyphs.DrawSkirtLabel(draw, topLeft, text, fontSize, color, scale);
      return;
    }

    if (HpClassicFaceplateGlyphs.TryDrawSkirtCardSlotLabel(draw, text, skirtCenter, fontSize, color, scale))
    {
      return;
    }

    HpClassicFaceplateGlyphs.LabelSize skirtSize = HpClassicFaceplateGlyphs.MeasureSkirtLabel(text, fontSize);
    Vector2 skirtTopLeft = skirtCenter - new Vector2(skirtSize.Width * 0.5f, skirtSize.Height * 0.5f);
    HpClassicFaceplateGlyphs.DrawSkirtLabel(draw, skirtTopLeft, text, fontSize, color, scale);
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
