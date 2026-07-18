using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public enum CalcButtonStyle
{
  Black,
  Grey,
  White,
  Orange,
  Blue,
}

/// <summary>HP key cap — procedural Key2 geometry; labels drawn at runtime.</summary>
public static class CalcButton
{
  private const float WellInsetRatio = 0.024f;

  /// <summary>Panamatik reference caps — slight inset leaves room for bezel + gold shift labels.</summary>
  private const float KeyCapLayoutScale = 1.02f;

  private static float CapLayoutScale => KeyCapLayoutScale;

  private const float PressTravelRatio = 0.045f;

  private const float FaceLabelDropPx = 0f;

  public static CalcButtonStyle StyleForKeyIndex(int keyChartIndex)
  {
    return keyChartIndex switch
    {
      10 or 11 => CalcButtonStyle.Orange,
      14 => CalcButtonStyle.Blue,
      12 or 13 => CalcButtonStyle.Grey,
      21 or 22 or 23 or 26 or 27 or 28 or 31 or 32 or 33 or 36 or 37 => CalcButtonStyle.White,
      >= 15 and <= 19 => CalcButtonStyle.Grey,
      >= 20 and <= 35 => CalcButtonStyle.Grey,
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
    bool overlayOnly = false,
    bool forcePressed = false,
    bool interactive = true,
    uint? primaryInkOverride = null,
    uint? skirtInkOverride = null)
  {
    if (!overlayOnly && CalcModernBody.IsActive)
    {
      return DrawModernFlat(
        draw,
        id,
        min,
        max,
        style,
        kind,
        primary,
        blueOnBody,
        scale,
        leftAlignPrimary,
        forcePressed,
        interactive,
        primaryInkOverride,
        skirtInkOverride);
    }

    if (!overlayOnly && drawWell && !Hp65FaceplateSvgAssets.CanDrawKeyCaps)
    {
      DrawWell(draw, min, max, scale);
    }

    Vector2 size = max - min;
    bool clicked = false;
    bool hovered = false;
    bool pressed = forcePressed;
    if (interactive)
    {
      ImGui.SetCursorScreenPos(min);
      clicked = ImGui.InvisibleButton(id, size);
      hovered = ImGui.IsItemHovered();
      pressed = ImGui.IsItemActive() || forcePressed;
    }

    if (overlayOnly)
    {
      return clicked;
    }

    Vector2 center = (min + max) * 0.5f;
    Vector2 half = size * 0.5f;
    float insetBottom = WellInsetRatio * 0.38f;
    Vector2 capHalf = new(
      half.X * CapLayoutScale * (1f - WellInsetRatio * 2f),
      half.Y * CapLayoutScale * (1f - WellInsetRatio - insetBottom));
    Vector2 capMin = center - capHalf;
    Vector2 capMax = center + capHalf;
    float pressTravel = MathF.Max(scale * 1.25f, size.Y * PressTravelRatio);
    if (pressed)
    {
      capMin.Y += pressTravel;
      capMax.Y += pressTravel;
    }

    DrawCap(draw, capMin, capMax, style, kind, hovered, pressed, scale);
    CalcKeyCapComponent cap = new() { CapMin = capMin, CapMax = capMax };
    DrawPrimaryLabel(draw, primary, cap, kind, style, scale, leftAlignPrimary, primaryInkOverride);

    if (!string.IsNullOrEmpty(blueOnBody))
    {
      float skirtFont = CalcFaceplateTypography.BlueSkirt(scale) * CalcKeyLabelPalette.BlueSkirtFontScale(blueOnBody);
      DrawSkirtLabel(
        draw,
        blueOnBody,
        cap,
        style,
        kind,
        skirtInkOverride ?? CalcKeyLabelPalette.SkirtLabelInk(blueOnBody, style),
        skirtFont,
        scale);
    }

    return clicked;
  }

  private static bool DrawModernFlat(
    ImDrawListPtr draw,
    string id,
    Vector2 min,
    Vector2 max,
    CalcButtonStyle style,
    CalcButtonKind kind,
    string primary,
    string? blueOnBody,
    float scale,
    bool leftAlignPrimary,
    bool forcePressed,
    bool interactive,
    uint? primaryInkOverride,
    uint? skirtInkOverride)
  {
    Vector2 size = max - min;
    bool clicked = false;
    bool hovered = false;
    bool pressed = forcePressed;
    if (interactive)
    {
      ImGui.SetCursorScreenPos(min);
      clicked = ImGui.InvisibleButton(id, size);
      hovered = ImGui.IsItemHovered();
      pressed = ImGui.IsItemActive() || forcePressed;
    }

    float inset = MathF.Min(size.X, size.Y) * 0.05f;
    Vector2 capMin = min + new Vector2(inset);
    Vector2 capMax = max - new Vector2(inset);
    if (pressed)
    {
      float travel = MathF.Max(scale * 0.8f, size.Y * 0.03f);
      capMin.Y += travel;
      capMax.Y += travel;
    }

    float radius = Math.Clamp(MathF.Min(capMax.X - capMin.X, capMax.Y - capMin.Y) * 0.14f, 3f * scale, 10f * scale);
    KeyCapPalette tones = KeyCapPalette.ForStyle(style, hovered, pressed);
    draw.AddRectFilled(capMin, capMax, tones.Face, radius);

    // CapSkirt etek uses palette skirt tone (same Top/Face/Skirt system as Retro).
    float skirtTop = capMin.Y + (capMax.Y - capMin.Y) * KeyCapGeometry.BottomTopRatio;
    draw.PushClipRect(new Vector2(capMin.X, skirtTop), capMax, true);
    draw.AddRectFilled(capMin, capMax, tones.Skirt, radius);
    draw.PopClipRect();

    if (hovered || pressed)
    {
      draw.AddRect(capMin, capMax, CalcChassisPalette.KeyHighlight, radius, ImDrawFlags.None, scale * 0.5f);
    }
    else
    {
      draw.AddRect(capMin, capMax, CalcChassisPalette.KeyCapBezel, radius, ImDrawFlags.None, scale * 0.35f);
    }

    CalcKeyCapComponent cap = new() { CapMin = capMin, CapMax = capMax };
    DrawPrimaryLabel(draw, primary, cap, kind, style, scale, leftAlignPrimary, primaryInkOverride);

    if (!string.IsNullOrEmpty(blueOnBody))
    {
      float skirtFont = CalcFaceplateTypography.BlueSkirt(scale) * CalcKeyLabelPalette.BlueSkirtFontScale(blueOnBody);
      DrawSkirtLabel(
        draw,
        blueOnBody,
        cap,
        style,
        kind,
        skirtInkOverride ?? CalcKeyLabelPalette.SkirtLabelInk(blueOnBody, style),
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
    CalcKeyCapComponent cap = new() { CapMin = capMin, CapMax = capMax };
    cap.DrawCap(draw, style, hovered, pressed, scale, kind);
  }

  private static void DrawPrimaryLabel(
    ImDrawListPtr draw,
    string text,
    CalcKeyCapComponent cap,
    CalcButtonKind kind,
    CalcButtonStyle style,
    float scale,
    bool leftAlign,
    uint? inkOverride)
  {
    uint ink = inkOverride ?? CalcKeyLabelPalette.PrimaryOnCap(style);

    if (kind == CalcButtonKind.EnterWide)
    {
      DrawEnterLabel(draw, cap.CapMin, cap.CapMax, scale, ink);
      return;
    }

    if (kind == CalcButtonKind.OperatorColon)
    {
      DrawFaceCenteredOperatorColon(draw, cap.FaceBand.Min, cap.FaceBand.Max, scale, ink);
      return;
    }

    DrawFaceCenteredKeyLabel(draw, text, cap.FaceBand.Min, cap.FaceBand.Max, scale, ink);
  }

  private static void DrawFaceCenteredKeyLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 faceMin,
    Vector2 faceMax,
    float scale,
    uint ink)
  {
    if (text == "\u00b7")
    {
      DrawKeyFaceDot(draw, faceMin, faceMax, scale, ink);
      return;
    }

    if (text is "+" or "-" or "\u00d7")
    {
      DrawKeyFaceOperator(draw, text, faceMin, faceMax, scale, ink);
      return;
    }

    float fontSize = PrimaryFontSize(text, scale);
    HpClassicFaceplateGlyphs.DrawKeyFaceLabelInRect(draw, faceMin, faceMax, text, fontSize, ink, scale);
  }

  private static void DrawKeyFaceDot(ImDrawListPtr draw, Vector2 faceMin, Vector2 faceMax, float scale, uint ink)
  {
    Vector2 center = (faceMin + faceMax) * 0.5f;
    float radius = MathF.Min(faceMax.X - faceMin.X, faceMax.Y - faceMin.Y) * 0.13f;
    draw.AddCircleFilled(center, MathF.Max(radius, scale * 2.3f), ink, 24);
  }

  private static float PrimaryFontSize(string text, float scale) =>
    text switch
    {
      "\u00b7" or "\u00f7" => CalcFaceplateTypography.KeyOperator(scale),
      "+" or "-" or "\u00d7" => CalcFaceplateTypography.KeyOperator(scale),
      _ when text.Length == 1 && char.IsDigit(text[0]) => CalcFaceplateTypography.KeyDigit(scale),
      _ when text.Length == 1 && char.IsLetter(text[0]) => CalcFaceplateTypography.KeyLetter(scale),
      _ => CalcFaceplateTypography.KeyPrimary(scale),
    };

  private static void DrawFaceCenteredOperatorColon(
    ImDrawListPtr draw,
    Vector2 faceMin,
    Vector2 faceMax,
    float scale,
    uint ink)
  {
    float centerX = (faceMin.X + faceMax.X) * 0.5f;
    float centerY = (faceMin.Y + faceMax.Y) * 0.5f;
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
    float arrowWidth = fontSize * 0.38f;
    float gap = scale * 5f;
    float totalWidth = textSize.X + gap + arrowWidth;
    float totalHeight = MathF.Max(textSize.Y, fontSize * 0.72f);

    (Vector2 faceMin, Vector2 faceMax) = KeyCapGeometry.FaceLabelRect(capMin, capMax);
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
    float capTop = y + textSize.Y * 0.22f;
    float capBottom = y + textSize.Y * 0.78f;
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
    float w = fontSize * 0.28f;
    DrawSvgArrowUp(draw, new Vector2(x + w * 0.5f, (capTop + capBottom) * 0.5f), MathF.Min(capHeight, fontSize * 0.55f), color);
    return w + fontSize * 0.03f;
  }

  private static void DrawSvgArrowUp(ImDrawListPtr draw, Vector2 center, float size, uint color)
  {
    float s = size / 32f;
    Vector2 p(float x, float y) => center + new Vector2((x - 16f) * s, (y - 16f) * s);
    draw.AddTriangleFilled(p(16f, 0f), p(4f, 10f), p(28f, 10f), color);
    draw.AddQuadFilled(p(12f, 10f), p(20f, 10f), p(20f, 32f), p(12f, 32f), color);
  }

  private static void DrawKeyFaceOperator(
    ImDrawListPtr draw,
    string op,
    Vector2 faceMin,
    Vector2 faceMax,
    float scale,
    uint ink)
  {
    Vector2 center = (faceMin + faceMax) * 0.5f;
    float fw = faceMax.X - faceMin.X;
    float fh = faceMax.Y - faceMin.Y;
    float stroke = MathF.Max(2.6f, scale * 2.5f);

    switch (op)
    {
      case "-":
        float minusW = fw * 0.5f;
        draw.AddLine(
          center + new Vector2(-minusW * 0.5f, 0f),
          center + new Vector2(minusW * 0.5f, 0f),
          ink,
          stroke);
        break;
      case "+":
        float plusSize = MathF.Min(fw, fh) * 0.58f;
        draw.AddLine(
          center + new Vector2(-plusSize * 0.5f, 0f),
          center + new Vector2(plusSize * 0.5f, 0f),
          ink,
          stroke);
        draw.AddLine(
          center + new Vector2(0f, -plusSize * 0.5f),
          center + new Vector2(0f, plusSize * 0.5f),
          ink,
          stroke);
        break;
      case "\u00d7":
        float xSize = CalcFaceplateTypography.KeyOperator(scale) * 1.22f;
        HpClassicFaceplateGlyphs.DrawKeyFaceMultiply(draw, center, xSize, ink, scale);
        break;
    }
  }

  private static void DrawColonDivideSymbol(ImDrawListPtr draw, Vector2 center, float capWidth, float scale, uint ink)
  {
    float symbolHeight = MathF.Min(capWidth * 0.52f, ImGui.GetFontSize() * 1.28f * scale);
    float lineWidth = MathF.Max(2f, capWidth * 0.4f);
    float dotRadius = MathF.Max(1.6f, scale * 1.75f);
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

  private static void DrawSkirtLabel(
    ImDrawListPtr draw,
    string text,
    CalcKeyCapComponent cap,
    CalcButtonStyle style,
    CalcButtonKind kind,
    uint color,
    float fontSize,
    float scale)
  {
    (Vector2 bottomMin, Vector2 bottomMax) = cap.BottomBand;
    float bottomHeight = bottomMax.Y - bottomMin.Y;
    // CapSkirt slot size: shrink-to-fit only (never grow past band); keep below CapFace.
    fontSize = MathF.Min(fontSize, bottomHeight * 0.82f);
    // Slight upward bias so ink clears the cap lip.
    Vector2 bottomNudge = new(0f, -scale * 0.35f);
    if (kind == CalcButtonKind.EnterWide)
    {
      float pad = scale * 2.5f;
      Vector2 topLeft = CalcFaceplateFonts.ArialBoldTopLeftForBand(bottomMin + bottomNudge, bottomMax + bottomNudge, text, fontSize, verticalBiasRatio: 0f);
      CalcFaceplateFonts.FontInkBounds ink = CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize);
      topLeft.X = bottomMax.X - pad - (ink.Left + ink.Width);
      HpClassicFaceplateGlyphs.DrawArialBoldGlyph(draw, text, topLeft.X, topLeft.Y, fontSize, color);
      return;
    }

    HpClassicFaceplateGlyphs.DrawSkirtLabelInRect(draw, bottomMin + bottomNudge, bottomMax + bottomNudge, text, fontSize, color, scale);
  }
}
