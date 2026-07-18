using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>Modern body SVG marks (HP logo from user-supplied HpMark.svg).</summary>
public static class CalcModernSvgAssets
{
  private static readonly SvgRasterCache Cache = new();

  private const int HpMarkRevision = 3;

  private static string HpMarkPath => TeoCalcPaths.ResourcePath("Engine/Shared/Assets/HpMark.svg");

  public static bool IsReady => Cache.IsInitialized && File.Exists(HpMarkPath);

  public static void TryInitialize(GL gl) => Cache.Initialize(gl);

  public static void Dispose() => Cache.Dispose();

  public static bool TryDrawHpMark(ImDrawListPtr draw, Vector2 min, Vector2 max)
  {
    if (!IsReady)
    {
      return false;
    }

    int w = Math.Max(1, (int)MathF.Ceiling((max.X - min.X) * 4f));
    int h = Math.Max(1, (int)MathF.Ceiling((max.Y - min.Y) * 4f));
    return Cache.TryDraw(draw, min, max, HpMarkPath, w, h, revision: HpMarkRevision);
  }
}
