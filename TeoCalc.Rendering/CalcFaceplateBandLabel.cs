using System.Numerics;

namespace TeoCalc.Rendering;

/// <summary>Band-centered label layout from painted ink bounds — no per-key tuning.</summary>
public static class CalcFaceplateBandLabel
{
  public readonly record struct LayoutBox(float Left, float Top, float Width, float Height)
  {
    public float MidX => Left + Width * 0.5f;

    public float MidY => Top + Height * 0.5f;
  }

  public static Vector2 BandCenter(Vector2 bandMin, Vector2 bandMax) =>
    KeyCapGeometry.BandCenter(bandMin, bandMax);

  public static Vector2 TopLeftForBand(Vector2 bandMin, Vector2 bandMax, LayoutBox box)
  {
    Vector2 center = BandCenter(bandMin, bandMax);
    return new(center.X - box.MidX, center.Y - box.MidY);
  }

  public static Vector2 TopLeftForBandInk(Vector2 bandMin, Vector2 bandMax, CalcFaceplateFonts.FontInkBounds ink) =>
    TopLeftForBand(bandMin, bandMax, FromInk(ink));

  public static LayoutBox FromInk(CalcFaceplateFonts.FontInkBounds ink) =>
    new(ink.Left, ink.Top, ink.Width, ink.Height);

  public static LayoutBox Union(LayoutBox a, LayoutBox b)
  {
    if (a.Width <= 0f || a.Height <= 0f)
    {
      return b;
    }

    if (b.Width <= 0f || b.Height <= 0f)
    {
      return a;
    }

    float left = MathF.Min(a.Left, b.Left);
    float top = MathF.Min(a.Top, b.Top);
    float right = MathF.Max(a.Left + a.Width, b.Left + b.Width);
    float bottom = MathF.Max(a.Top + a.Height, b.Top + b.Height);
    return new LayoutBox(left, top, right - left, bottom - top);
  }

  public static LayoutBox Offset(LayoutBox box, float dx, float dy) =>
    new(box.Left + dx, box.Top + dy, box.Width, box.Height);

  public static LayoutBox BoxAt(float left, float top, float width, float height) =>
    new(left, top, width, height);

  public static LayoutBox CenteredRow(float width, float height, float rowMidY) =>
    new(0f, rowMidY - height * 0.5f, width, height);
}
