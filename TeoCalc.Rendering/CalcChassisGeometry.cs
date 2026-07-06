using System.Numerics;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Calculator chassis proportions from a <see cref="CalcBodyLayout"/>.</summary>
public static class CalcChassisGeometry
{
  public static CalcChassisMetrics Fit(Vector2 available, CalcBodyLayout layout)
  {
    float scale = Math.Min(available.X / layout.ReferenceWidth, available.Y / layout.ReferenceHeight);
    scale = Math.Clamp(scale, 0.85f, 2.8f);
    return new CalcChassisMetrics(layout, scale);
  }

  public static CalcChassisMetrics FitHp65(Vector2 available) =>
    Fit(available, Hp65CalcBodyLayout.Instance);
}

public readonly record struct CalcChassisMetrics(CalcBodyLayout Layout, float Scale)
{
  public float Width => Layout.ReferenceWidth * Scale;

  public float Height => Layout.ReferenceHeight * Scale;

  public float FooterHeight => Layout.LogoSlot.Height * Scale;

  public RectF DisplayRect(Vector2 origin) => ScaleRect(origin, Layout.DisplaySlot);

  public RectF KeypadPanelRect(Vector2 origin) => ScaleRect(origin, Layout.KeypadSlot);

  public RectF LogoRect(Vector2 origin) => ScaleRect(origin, Layout.LogoSlot);

  public RectF KeyRect(Vector2 origin, int keyChartIndex)
  {
    if (!Layout.TryGetKeySlot(keyChartIndex, out RectF rect))
    {
      return default;
    }

    return ScaleRect(origin, rect);
  }

  public float GoldBandForKey(int keyChartIndex) =>
    string.Equals(Layout.Id, Hp65CalcBodyLayout.LayoutId, StringComparison.OrdinalIgnoreCase)
      ? BodyFaceplateLayout.GoldBandHeight(keyChartIndex) * Scale
      : 12f * Scale;

  public RectF SwitchTrackRect(Vector2 origin) => ScaleRect(origin, Layout.SwitchSlot);

  public Vector2 OnOffSwitchCenter(Vector2 origin) => ScalePoint(origin, Layout.OnOffSwitchCenter);

  public Vector2 PrgmRunSwitchCenter(Vector2 origin) => ScalePoint(origin, Layout.PrgmRunSwitchCenter);

  public RectF CardSlotBandRect(Vector2 origin) =>
    Layout.CardSlotBand is { } band ? ScaleRect(origin, band) : default;

  public bool TryGetCardSlotColumn(Vector2 origin, int column, out RectF slotRect)
  {
    slotRect = default;
    if (column < 0 || column >= CalcFaceplateLayout.Columns)
    {
      return false;
    }

    if (!Layout.TryGetKeySlot(column, out RectF key))
    {
      return false;
    }

    if (Layout.CardSlotBand is not { } band)
    {
      return false;
    }

    slotRect = ScaleRect(origin, new RectF(key.X, band.Y, key.Width, band.Height));
    return true;
  }

  private RectF ScaleRect(Vector2 origin, RectF rect) => new(
    origin.X + rect.X * Scale,
    origin.Y + rect.Y * Scale,
    rect.Width * Scale,
    rect.Height * Scale);

  private Vector2 ScalePoint(Vector2 origin, Vector2 point) => new(
    origin.X + point.X * Scale,
    origin.Y + point.Y * Scale);
}

public readonly record struct RectF(float X, float Y, float Width, float Height)
{
  public Vector2 Min => new(X, Y);

  public Vector2 Max => new(X + Width, Y + Height);

  public Vector2 Size => new(Width, Height);
}
