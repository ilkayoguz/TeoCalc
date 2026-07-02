using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Gold annotations above the ENTER row (PREFIX, CLEAR rule, STK/REG/PRGM).</summary>
public static class CalcEnterRowLabels
{
  public static bool IsEnterRowKey(int keyChartIndex) => keyChartIndex is 15 or 17 or 18 or 19;

  public static void Draw(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    RectF enterRect = metrics.KeyRect(origin, 15);
    RectF chsRect = metrics.KeyRect(origin, 17);
    RectF eexRect = metrics.KeyRect(origin, 18);
    RectF clxRect = metrics.KeyRect(origin, 19);
    if (enterRect.Width <= 0f || chsRect.Width <= 0f)
    {
      return;
    }

    float rowBand = metrics.GoldBandForKey(17);
    float subFont = CalcFaceplateTypography.GoldShiftSmall(metrics.Scale);
    float labelRowY = enterRect.Y - rowBand * 0.08f;
    float gap = subFont * 0.55f + metrics.Scale * 2.5f;
    float clearRowY = labelRowY - gap - metrics.Scale * 1.8f;

    DrawClearRule(draw, enterRect, chsRect, clxRect, clearRowY, subFont, metrics.Scale);
    DrawGoldTextCentered(draw, "PREFIX", enterRect, subFont, labelRowY);
    DrawGoldTextCentered(draw, "STK", chsRect, subFont, labelRowY);
    DrawGoldTextCentered(draw, "REG", eexRect, subFont, labelRowY);
    DrawGoldTextCentered(draw, "PRGM", clxRect, subFont, labelRowY);
  }

  private static void DrawGoldTextCentered(ImDrawListPtr draw, string text, RectF anchor, float fontSize, float centerY)
  {
    Vector2 size = MeasureArial(text, fontSize);
    float x = anchor.X + (anchor.Width - size.X) * 0.5f;
    float y = centerY - size.Y * 0.5f;
    DrawArialTop(draw, text, x, y, fontSize, CalcKeyLabelPalette.GoldOnBody);
  }

  private static void DrawClearRule(
    ImDrawListPtr draw,
    RectF enterRect,
    RectF chsRect,
    RectF clxRect,
    float clearCenterY,
    float fontSize,
    float scale)
  {
    const string clearWord = "CLEAR";
    Vector2 clearSize = MeasureArial(clearWord, fontSize);
    float textCenterX = chsRect.X + chsRect.Width * 0.5f;
    float sidePad = fontSize * 0.34f + scale * 4.5f;
    float lineLeft = enterRect.X + scale * 3f;
    float lineRight = clxRect.Max.X - scale * 3f;
    uint color = CalcChassisPalette.GoldRule;
    float thickness = MathF.Max(1f, scale * 1f);
    float drop = scale * 3.4f;
    float cornerR = MathF.Max(1.5f, scale * 1.4f);

    float textCenterY = clearCenterY - scale * 2.2f;
    float textY = textCenterY - clearSize.Y * 0.5f;
    float lineY = textCenterY;
    float leftEndX = textCenterX - clearSize.X * 0.5f - sidePad;
    float rightStartX = textCenterX + clearSize.X * 0.5f + sidePad;

    DrawArialTop(
      draw,
      clearWord,
      textCenterX - clearSize.X * 0.5f,
      textY,
      fontSize,
      CalcKeyLabelPalette.GoldOnBody);

    DrawClearRuleSide(draw, lineLeft, lineY, leftEndX, lineY, drop, cornerR, color, thickness, fromLeft: true);
    DrawClearRuleSide(draw, lineRight, lineY, rightStartX, lineY, drop, cornerR, color, thickness, fromLeft: false);
  }

  private static void DrawClearRuleSide(
    ImDrawListPtr draw,
    float outerX,
    float lineY,
    float innerX,
    float innerY,
    float drop,
    float cornerR,
    uint color,
    float thickness,
    bool fromLeft)
  {
    float vertBottom = lineY + drop;
    draw.PathClear();
    if (fromLeft)
    {
      draw.PathLineTo(new Vector2(outerX, vertBottom));
      draw.PathLineTo(new Vector2(outerX, lineY + cornerR));
      draw.PathBezierQuadraticCurveTo(new Vector2(outerX, lineY), new Vector2(outerX + cornerR, lineY));
      draw.PathLineTo(new Vector2(innerX, innerY));
    }
    else
    {
      draw.PathLineTo(new Vector2(innerX, innerY));
      draw.PathLineTo(new Vector2(outerX - cornerR, lineY));
      draw.PathBezierQuadraticCurveTo(new Vector2(outerX, lineY), new Vector2(outerX, lineY + cornerR));
      draw.PathLineTo(new Vector2(outerX, vertBottom));
    }

    draw.PathStroke(color, ImDrawFlags.RoundCornersAll, thickness);
  }

  private static Vector2 MeasureArial(string text, float fontSize) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

  private static void DrawArialTop(ImDrawListPtr draw, string text, float x, float y, float fontSize, uint color)
  {
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, y, fontSize, color);
      return;
    }

    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), color, text);
  }
}
