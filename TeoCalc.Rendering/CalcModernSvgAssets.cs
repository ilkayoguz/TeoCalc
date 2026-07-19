using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>Modern body brand marks via Svg.Skia (TeoMark.svg; HpMark.svg fallback).</summary>
public static class CalcModernSvgAssets
{
  private static readonly SvgRasterCache Cache = new();

  private const int HpMarkRevision = 3;

  private const int TeoMarkRevision = 6;

  /// <summary>Supersample so the small plate mark stays crisp when ImGui scales the texture down.</summary>
  private const float TeoMarkRasterScale = 8f;

  private const int TeoMarkMinRasterPx = 256;

  private static string HpMarkPath => TeoCalcPaths.ResourcePath("Engine/Shared/Assets/HpMark.svg");

  private static string TeoMarkPath => TeoCalcPaths.ResourcePath("Engine/Shared/Assets/TeoMark.svg");

  public static bool IsReady => Cache.IsInitialized && (File.Exists(TeoMarkPath) || File.Exists(HpMarkPath));

  public static void TryInitialize(GL gl) => Cache.Initialize(gl);

  public static void Dispose() => Cache.Dispose();

  /// <summary>Direct Svg.Skia raster of TeoMark.svg into an OpenGL texture.</summary>
  public static bool TryDrawTeoMark(ImDrawListPtr draw, Vector2 min, Vector2 max)
  {
    if (!Cache.IsInitialized || !File.Exists(TeoMarkPath))
    {
      return false;
    }

    float displayW = MathF.Max(1f, max.X - min.X);
    float displayH = MathF.Max(1f, max.Y - min.Y);
    int w = Math.Clamp((int)MathF.Ceiling(displayW * TeoMarkRasterScale), TeoMarkMinRasterPx, 1024);
    int h = Math.Clamp((int)MathF.Ceiling(displayH * TeoMarkRasterScale), TeoMarkMinRasterPx, 1024);
    return Cache.TryDraw(draw, min, max, TeoMarkPath, w, h, revision: TeoMarkRevision);
  }

  public static bool TryDrawHpMark(ImDrawListPtr draw, Vector2 min, Vector2 max)
  {
    if (!Cache.IsInitialized || !File.Exists(HpMarkPath))
    {
      return false;
    }

    int w = Math.Max(1, (int)MathF.Ceiling((max.X - min.X) * 4f));
    int h = Math.Max(1, (int)MathF.Ceiling((max.Y - min.Y) * 4f));
    return Cache.TryDraw(draw, min, max, HpMarkPath, w, h, revision: HpMarkRevision);
  }
}
