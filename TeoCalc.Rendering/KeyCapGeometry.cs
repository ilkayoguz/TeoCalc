using System.Numerics;

namespace TeoCalc.Rendering;

/// <summary>Key2.svg label zones on the 48x38 cap slot.</summary>
public static class KeyCapGeometry
{
  private const float ViewWidth = 48f;

  private const float ViewHeight = 38f;

  /// <summary>Face label band starts where the Face layer begins (Key2 y=2.5).</summary>
  public const float FaceLabelTopRatio = 2.5f / ViewHeight;

  /// <summary>Face label band ends where Bottom begins (Key2 y=25.3).</summary>
  public const float FaceLabelEndRatio = 25.3f / ViewHeight;

  /// <summary>Bottom label band top (Key2 face base).</summary>
  public const float BottomTopRatio = 25.3f / ViewHeight;

  private const float SideInsetRatio = 2f / ViewWidth;

  private const float BottomLipInsetRatio = 4f / ViewHeight;

  /// <summary>Legacy face paint end; kept for cap tint geometry.</summary>
  public const float FaceEndRatio = FaceLabelEndRatio;

  public const float FaceTopRatio = FaceLabelTopRatio;

  public const float FaceLeftRatio = SideInsetRatio;

  public const float FaceRightRatio = 1f - SideInsetRatio;

  /// <summary>Key2.svg viewBox origin and size (includes well margin for vent mask).</summary>
  public const float Key2ViewX = -6f;

  public const float Key2ViewY = -4f;

  public const float Key2ViewWidth = 60f;

  public const float Key2ViewHeight = 46f;

  /// <summary>UV crop mapping the 48x38 cap slot inside the Key2 viewBox.</summary>
  public static readonly Vector2 Key2CapUv0 = new(
    (0f - Key2ViewX) / Key2ViewWidth,
    (0f - Key2ViewY) / Key2ViewHeight);

  public static readonly Vector2 Key2CapUv1 = new(
    (ViewWidth - Key2ViewX) / Key2ViewWidth,
    (ViewHeight - Key2ViewY) / Key2ViewHeight);

  public static (Vector2 Min, Vector2 Max) FaceLabelRect(Vector2 capMin, Vector2 capMax) =>
    BandRect(capMin, capMax, FaceLabelTopRatio, FaceLabelEndRatio);

  public static (Vector2 Min, Vector2 Max) SkirtLabelBand(Vector2 capMin, Vector2 capMax)
  {
    Vector2 size = capMax - capMin;
    float lip = size.Y * (2f / ViewHeight);
    return (
      capMin + new Vector2(size.X * SideInsetRatio, size.Y * BottomTopRatio),
      new Vector2(capMin.X + size.X * (1f - SideInsetRatio), capMax.Y - lip));
  }

  public static (Vector2 Min, Vector2 Max) SkirtInkRect(Vector2 capMin, Vector2 capMax) =>
    SkirtLabelBand(capMin, capMax);

  public static (Vector2 Min, Vector2 Max) BottomLabelRect(Vector2 capMin, Vector2 capMax) =>
    SkirtLabelBand(capMin, capMax);

  public static (Vector2 Min, Vector2 Max) FaceRect(Vector2 capMin, Vector2 capMax) =>
    FaceLabelRect(capMin, capMax);

  public static (Vector2 Min, Vector2 Max) SkirtRect(Vector2 capMin, Vector2 capMax) =>
    BottomLabelRect(capMin, capMax);

  public static Vector2 BandCenter(Vector2 min, Vector2 max) => (min + max) * 0.5f;

  private static (Vector2 Min, Vector2 Max) BandRect(
    Vector2 capMin,
    Vector2 capMax,
    float topRatio,
    float bottomRatio)
  {
    Vector2 size = capMax - capMin;
    return (
      capMin + new Vector2(size.X * SideInsetRatio, size.Y * topRatio),
      capMin + new Vector2(size.X * (1f - SideInsetRatio), size.Y * bottomRatio));
  }
}
