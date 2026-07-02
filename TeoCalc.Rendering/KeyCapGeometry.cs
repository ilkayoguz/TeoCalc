using System.Numerics;

namespace TeoCalc.Rendering;

/// <summary>KeyCap.svg layout ratios (48×38 viewBox).</summary>
public static class KeyCapGeometry
{
  private const float ViewWidth = 48f;
  private const float ViewHeight = 38f;

  public const float FaceEndRatio = 26f / ViewHeight;

  public const float FaceTopRatio = 1.35f / ViewHeight;

  public const float FaceLeftRatio = 3.35f / ViewWidth;

  public const float FaceRightRatio = 44.65f / ViewWidth;

  public static (Vector2 Min, Vector2 Max) FaceRect(Vector2 capMin, Vector2 capMax)
  {
    Vector2 size = capMax - capMin;
    return (
      capMin + new Vector2(size.X * FaceLeftRatio, size.Y * FaceTopRatio),
      capMin + new Vector2(size.X * FaceRightRatio, size.Y * FaceEndRatio));
  }

  public static (Vector2 Min, Vector2 Max) SkirtRect(Vector2 capMin, Vector2 capMax)
  {
    Vector2 size = capMax - capMin;
    return (
      capMin + new Vector2(size.X * 0.04f, size.Y * FaceEndRatio),
      capMax - new Vector2(size.X * 0.04f, size.Y * 0.04f));
  }
}
