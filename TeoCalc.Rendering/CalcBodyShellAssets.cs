using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using TeoCalc.Core;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Prototype body shell bitmap from calc-body-layout-draft.png (cropped, keys/model text cleared).</summary>
public static class CalcBodyShellAssets
{
  private const int ShellRevision = 10;

  private static readonly BitmapRasterCache Cache = new();

  private static string ShellPath => TeoCalcPaths.ResourcePath("Engine/Shared/Assets/calc-body-shell.png");

  public static bool IsReady => Cache.IsInitialized && File.Exists(ShellPath);

  /// <summary>Bitmap shell disabled — Modern body is drawn procedurally.</summary>
  public static bool UseForCurrentTheme => false;

  public static void TryInitialize(GL gl) => Cache.Initialize(gl);

  public static bool TryDrawShell(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    if (!IsReady)
    {
      return false;
    }

    Vector2 max = origin + new Vector2(metrics.Width, metrics.Height);
    return Cache.TryDraw(draw, origin, max, ShellPath, ShellRevision);
  }

  public static void Dispose() => Cache.Dispose();
}
