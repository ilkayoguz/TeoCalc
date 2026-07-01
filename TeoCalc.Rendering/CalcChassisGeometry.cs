using System.Numerics;

namespace TeoCalc.Rendering;

/// <summary>HP-65 chassis proportions from hp65_470.png (470×870) and HP65_470.kml.</summary>
public static class CalcChassisGeometry
{
  public const float ReferenceWidth = 470f;

  public const float ReferenceHeight = 870f;

  // KML DISPLAY 68,54 410,96
  public static readonly Vector4 DisplayNorm = new(68f / ReferenceWidth, 54f / ReferenceHeight, 342f / ReferenceWidth, 42f / ReferenceHeight);

  // KML BUTTONS 91,258 378,780
  public static readonly Vector4 KeypadNorm = new(91f / ReferenceWidth, 258f / ReferenceHeight, 287f / ReferenceWidth, 522f / ReferenceHeight);

  public const float KeyWidthPx = 47f;

  public const float KeyHeightPx = 57f;

  public const float KeyGapHPx = 10f;

  public const float KeyGapVPx = 8f;

  public const float CardSlotBandPx = 12f;

  public const float GoldBandPx = 14f;

  public const float KeypadInsetPx = 5f;

  public const float EnterColSpan = 2f;

  public const float FooterHeightPx = 36f;

  public const float SliderBandTopPx = 96f;

  public const float SliderBandBottomPx = 258f;

  public static CalcChassisMetrics Fit(Vector2 available)
  {
    float scale = Math.Min(available.X / ReferenceWidth, available.Y / ReferenceHeight);
    scale = Math.Clamp(scale, 0.85f, 2.8f);
    return new CalcChassisMetrics(scale);
  }
}

public readonly record struct CalcChassisMetrics(float Scale)
{
  public float Width => CalcChassisGeometry.ReferenceWidth * Scale;

  public float Height => CalcChassisGeometry.ReferenceHeight * Scale;

  public float KeyWidth => CalcChassisGeometry.KeyWidthPx * Scale;

  public float KeyHeight => CalcChassisGeometry.KeyHeightPx * Scale;

  public float KeyGapH => CalcChassisGeometry.KeyGapHPx * Scale;

  public float KeyGapV => CalcChassisGeometry.KeyGapVPx * Scale;

  public float GoldBand => CalcChassisGeometry.GoldBandPx * Scale;

  public float CardSlotBand => CalcChassisGeometry.CardSlotBandPx * Scale;

  public float KeypadInset => CalcChassisGeometry.KeypadInsetPx * Scale;

  public float FooterHeight => CalcChassisGeometry.FooterHeightPx * Scale;

  public RectF DisplayRect(Vector2 origin)
  {
    return NormRect(origin, CalcChassisGeometry.DisplayNorm, Scale);
  }

  public RectF KeypadRect(Vector2 origin)
  {
    return NormRect(origin, CalcChassisGeometry.KeypadNorm, Scale);
  }

  public float RowPitch => KeyHeight + KeyGapV;

  public Vector2 CellSize(FaceplateCell cell)
  {
    float width = cell.ColSpan * KeyWidth + (cell.ColSpan - 1) * KeyGapH;
    float height = cell.RowSpan * KeyHeight + (cell.RowSpan - 1) * KeyGapV;
    return new Vector2(width, height);
  }

  public Vector2 CellOrigin(RectF keypad, FaceplateCell cell)
  {
    float x = keypad.X + KeypadInset + cell.Column * (KeyWidth + KeyGapH);
    float y = keypad.Y + CardSlotBand + GoldBand + cell.Row * RowPitch;
    return new Vector2(x, y);
  }

  private static RectF NormRect(Vector2 origin, Vector4 norm, float scale)
  {
    return new RectF(
      origin.X + norm.X * CalcChassisGeometry.ReferenceWidth * scale,
      origin.Y + norm.Y * CalcChassisGeometry.ReferenceHeight * scale,
      norm.Z * CalcChassisGeometry.ReferenceWidth * scale,
      norm.W * CalcChassisGeometry.ReferenceHeight * scale);
  }
}

public readonly record struct RectF(float X, float Y, float Width, float Height)
{
  public Vector2 Min => new(X, Y);

  public Vector2 Max => new(X + Width, Y + Height);

  public Vector2 Size => new(Width, Height);
}
