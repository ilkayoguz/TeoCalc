using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Vector-drawn card-slot square root radical (from finalized <c>sqrt-radical.svg</c> geometry).</summary>
public static class CardSlotSqrtArt
{
  private const float ViewBarStartX = 11.85f;
  private const float ViewBoxMinX = 0f;
  private const float ViewBoxMinY = 1f;
  private const float ViewEarEndX = 32.49f;
  private const float ViewBottomY = 20f;
  private const float ViewWidth = 35.5f;
  private const float ViewHeight = 22f;
  private const float SvgStrokeWidth = 2f;
  private const float HookShiftRatio = 0.06f;
  private const int ArcSegments = 8;

  public readonly record struct RadicalLayout(float SqrtLeft, float RadicalWidth, float XLeft);

  public static bool IsReady => true;

  public static float MeasureWidth(float xHeight) =>
    xHeight * 1.22f + 1f;

  public static float VinculumWidth(float xHeight)
  {
    float radicalW = MeasureWidth(xHeight);
    return radicalW * ((ViewEarEndX - ViewBarStartX) / ViewWidth);
  }

  public static RadicalLayout LayoutForX(float labelCenterX, float xWidth, float xHeight, float fontSize)
  {
    float radicalW = MeasureWidth(xHeight);
    float barSpan = VinculumWidth(xHeight);
    float barCenter = labelCenterX + fontSize * 0.02f;
    float barLeft = barCenter - barSpan * 0.5f;
    float sqrtLeft = barLeft - radicalW * ((ViewBarStartX - ViewBoxMinX) / ViewWidth) - fontSize * HookShiftRatio;
    float xLeft = barCenter - xWidth * 0.5f;
    return new(sqrtLeft, radicalW, xLeft);
  }

  public static float Draw(
    ImDrawListPtr draw,
    float x,
    float xTop,
    float xHeight,
    float scale,
    uint color)
  {
    float xBottom = xTop + xHeight;
    float radicalW = MeasureWidth(xHeight);
    float radicalH = MathF.Max(1f, (xHeight * 1.28f - 2f) * 0.5f) + 2f;
    float bottom = xBottom + xHeight * 0.02f;
    float anchorFrac = (ViewBottomY - ViewBoxMinY) / ViewHeight;
    float top = bottom - radicalH * anchorFrac - radicalH * 0.5f;
    const float radicalHeightExtra = 2f;
    float drawRadicalH = radicalH + radicalHeightExtra;
    float thickness = MathF.Max(1.5f, SvgStrokeWidth / ViewHeight * drawRadicalH * MathF.Max(1f, scale * 0.95f));

    DrawRadicalPath(draw, x + 2f, top, radicalW, drawRadicalH, thickness, color);
    return radicalW;
  }

  private static void DrawRadicalPath(
    ImDrawListPtr draw,
    float left,
    float top,
    float width,
    float height,
    float thickness,
    uint color)
  {
    float MapX(float svgX) => left + (svgX - ViewBoxMinX) / ViewWidth * width;
    float MapY(float svgY) => top + (svgY - ViewBoxMinY) / ViewHeight * height;

    draw.PathClear();
    draw.PathLineTo(M(MapX, MapY, 3f, 12f));
    draw.PathLineTo(M(MapX, MapY, 4.31f, 12f));
    AppendSvgArc(draw, MapX, MapY, 4.31f, 12f, 1f, 1f, 0.93f, 0.65f, sweep: true);
    draw.PathLineTo(M(MapX, MapY, 8f, 20f));
    draw.PathLineTo(M(MapX, MapY, 10.85f, 4.82f));
    AppendSvgArc(draw, MapX, MapY, 10.85f, 4.82f, 1f, 1f, 1f, -0.82f, sweep: true);
    draw.PathLineTo(M(MapX, MapY, 30.5f, 4f));
    AppendSvgArc(draw, MapX, MapY, 30.5f, 4f, 1f, 1f, 1f, 0.82f, sweep: true);
    draw.PathLineTo(M(MapX, MapY, 32.49f, 7.22f));
    draw.PathStroke(color, ImDrawFlags.None, thickness);
  }

  private static Vector2 M(Func<float, float> mapX, Func<float, float> mapY, float x, float y) =>
    new(mapX(x), mapY(y));

  private static void AppendSvgArc(
    ImDrawListPtr draw,
    Func<float, float> mapX,
    Func<float, float> mapY,
    float x1,
    float y1,
    float rx,
    float ry,
    float dx,
    float dy,
    bool sweep)
  {
    float x2 = x1 + dx;
    float y2 = y1 + dy;
    if (!TrySampleSvgArc(x1, y1, x2, y2, rx, ry, sweep, out Vector2[] points))
    {
      draw.PathLineTo(M(mapX, mapY, x2, y2));
      return;
    }

    foreach (Vector2 point in points)
    {
      draw.PathLineTo(new Vector2(mapX(point.X), mapY(point.Y)));
    }
  }

  private static bool TrySampleSvgArc(
    float x1,
    float y1,
    float x2,
    float y2,
    float rx,
    float ry,
    bool sweep,
    out Vector2[] points)
  {
    points = [];
    if (rx <= 0f || ry <= 0f)
    {
      return false;
    }

    double dx2 = (x1 - x2) * 0.5;
    double dy2 = (y1 - y2) * 0.5;
    double rx2 = rx * rx;
    double ry2 = ry * ry;
    double dist = (dx2 * dx2) / rx2 + (dy2 * dy2) / ry2;
    if (dist > 1d)
    {
      double s = Math.Sqrt(dist);
      rx = (float)(rx * s);
      ry = (float)(ry * s);
      rx2 = rx * rx;
      ry2 = ry * ry;
    }

    double sign = sweep ? 1d : -1d;
    double sq = Math.Max(0d, (rx2 * ry2 - rx2 * dy2 * dy2 - ry2 * dx2 * dx2) / (rx2 * dy2 * dy2 + ry2 * dx2 * dx2));
    double coef = sign * Math.Sqrt(sq);
    double cxp = coef * (rx * dy2 / ry);
    double cyp = coef * -(ry * dx2 / rx);
    double cx = (x1 + x2) * 0.5 + cxp;
    double cy = (y1 + y2) * 0.5 + cyp;

    double a1 = Math.Atan2(y1 - cy, x1 - cx);
    double a2 = Math.Atan2(y2 - cy, x2 - cx);
    if (!sweep && a2 > a1)
    {
      a2 -= Math.PI * 2d;
    }
    else if (sweep && a2 < a1)
    {
      a2 += Math.PI * 2d;
    }

    points = new Vector2[ArcSegments];
    for (int i = 0; i < ArcSegments; i++)
    {
      double t = (i + 1d) / ArcSegments;
      double a = a1 + (a2 - a1) * t;
      points[i] = new Vector2(
        (float)(cx + rx * Math.Cos(a)),
        (float)(cy + ry * Math.Sin(a)));
    }

    return true;
  }
}
