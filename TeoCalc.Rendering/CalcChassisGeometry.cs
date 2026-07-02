using System.Numerics;

namespace TeoCalc.Rendering;

/// <summary>HP-65 chassis proportions from Body.svg and faceplate-d03-layout.json (409×861).</summary>
public static class CalcChassisGeometry
{
  public const float ReferenceWidth = BodyFaceplateLayout.ReferenceWidth;

  public const float ReferenceHeight = BodyFaceplateLayout.ReferenceHeight;

  public static CalcChassisMetrics Fit(Vector2 available)
  {
    BodyFaceplateLayout.EnsureLoaded();
    float scale = Math.Min(available.X / ReferenceWidth, available.Y / ReferenceHeight);
    scale = Math.Clamp(scale, 0.85f, 2.8f);
    return new CalcChassisMetrics(scale);
  }
}

public readonly record struct CalcChassisMetrics(float Scale)
{
  public float Width => CalcChassisGeometry.ReferenceWidth * Scale;

  public float Height => CalcChassisGeometry.ReferenceHeight * Scale;

  public float FooterHeight
  {
    get
    {
      BodyFaceplateLayout.EnsureLoaded();
      return BodyFaceplateLayout.BrandPlate.Height * Scale;
    }
  }

  public RectF DisplayRect(Vector2 origin) => ScaleRect(origin, BodyFaceplateLayout.DisplayWindow);

  public RectF KeypadPanelRect(Vector2 origin) => ScaleRect(origin, BodyFaceplateLayout.KeypadPanel);

  public RectF KeyRect(Vector2 origin, int keyChartIndex)
  {
    if (!BodyFaceplateLayout.TryGetKeyRect(keyChartIndex, out RectF rect))
    {
      return default;
    }

    return ScaleRect(origin, rect);
  }

  public float GoldBandForKey(int keyChartIndex) => BodyFaceplateLayout.GoldBandHeight(keyChartIndex) * Scale;

  public RectF SwitchTrackRect(Vector2 origin) => ScaleRect(origin, BodyFaceplateLayout.SwitchTrack);

  public Vector2 OnOffSwitchCenter(Vector2 origin) => ScalePoint(origin, BodyFaceplateLayout.OnOffSwitchKnob);

  public Vector2 PrgmRunSwitchCenter(Vector2 origin) => ScalePoint(origin, BodyFaceplateLayout.PrgmRunSwitchKnob);

  public RectF CardSlotBandRect(Vector2 origin) => ScaleRect(origin, BodyFaceplateLayout.CardSlotBand);

  public bool TryGetCardSlotColumn(Vector2 origin, int column, out RectF slotRect)
  {
    slotRect = default;
    if (column < 0 || column >= CalcFaceplateLayout.Columns)
    {
      return false;
    }

    if (!BodyFaceplateLayout.TryGetKeyRect(column, out RectF key))
    {
      return false;
    }

    RectF band = BodyFaceplateLayout.CardSlotBand;
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
