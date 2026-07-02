using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>HP-65 faceplate SVG assets: Body, HpLogo, KeyCap variants.</summary>
public static class Hp65FaceplateSvgAssets
{
  private static readonly SvgRasterCache Cache = new();

  /// <summary>Bump when Body.svg panel colors change (invalidates raster cache).</summary>
  private const int BodySvgRevision = 3;

  private static string AssetsRoot => TeoCalcPaths.ResourcePath("Engine/HP-65/Assets");

  private static string BodyPath => Path.Combine(AssetsRoot, "Body.svg");

  private static string LogoPath => Path.Combine(AssetsRoot, "HpLogo.svg");

  public static bool IsReady => Cache.IsInitialized && File.Exists(BodyPath);

  /// <summary>Body.svg chrome from faceplate-d03-layout.json companion (409×861).</summary>
  public static bool UseBodyChrome => true;

  public static bool CanDrawKeyCaps => Cache.IsInitialized;

  public static bool CanDrawCardSlotLabels => Cache.IsInitialized;

  public static void TryInitialize(GL gl)
  {
    Cache.Initialize(gl);
  }

  public static void DrawBody(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    if (!IsReady)
    {
      return;
    }

    int w = Math.Max(1, (int)MathF.Ceiling(metrics.Width * 3f));
    int h = Math.Max(1, (int)MathF.Ceiling(metrics.Height * 3f));
    Vector2 max = origin + new Vector2(metrics.Width, metrics.Height);
    Cache.TryDraw(draw, origin, max, BodyPath, w, h, revision: BodySvgRevision);
  }

  public static void DrawLogo(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    BodyFaceplateLayout.EnsureLoaded();
    RectF plate = BodyFaceplateLayout.BrandPlate;
    float plateWidth = plate.Width * metrics.Scale;
    float plateHeight = plate.Height * metrics.Scale;
    Vector2 plateMin = origin + new Vector2(plate.X * metrics.Scale, plate.Y * metrics.Scale);
    Vector2 plateMax = plateMin + new Vector2(plateWidth, plateHeight);

    float logoAspect = 888f / 562f;
    float logoPadX = plateWidth * 0.02f;
    float maxLogoWidth = plateWidth * 0.42f;
    float logoHeight = plateHeight * 0.82f;
    float logoWidth = logoHeight * logoAspect;
    if (logoWidth > maxLogoWidth)
    {
      logoWidth = maxLogoWidth;
      logoHeight = logoWidth / logoAspect;
    }

    Vector2 logoMin = plateMin + new Vector2(logoPadX, (plateHeight - logoHeight) * 0.5f);
    Vector2 logoMax = logoMin + new Vector2(logoWidth, logoHeight);

    if (Cache.IsInitialized && File.Exists(LogoPath))
    {
      int w = Math.Max(1, (int)MathF.Ceiling(logoWidth * 5f));
      int h = Math.Max(1, (int)MathF.Ceiling(logoHeight * 5f));
      if (!Cache.TryDraw(draw, logoMin, logoMax, LogoPath, w, h))
      {
        Cache.TryDraw(draw, logoMin, logoMax, LogoPath, w, h);
      }
    }

    float textMargin = plateWidth * 0.025f;
    float textLeft = logoMax.X + textMargin;
    CalcChassisRenderer.DrawBrandPlateText(draw, textLeft, plateMin, plateMax, textRightMargin: textMargin);
  }

  public static bool TryDrawSvg(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int rasterWidth,
    int rasterHeight) =>
    Cache.TryDraw(draw, min, max, path, rasterWidth, rasterHeight);

  public static bool TryDrawSvgRotated(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int rasterWidth,
    int rasterHeight,
    float rotateDegrees,
    uint tintColor) =>
    Cache.TryDraw(draw, min, max, path, rasterWidth, rasterHeight, rotateDegrees, tintColor);

  public static void DrawKeyCap(
    ImDrawListPtr draw,
    Vector2 capMin,
    Vector2 capMax,
    CalcButtonStyle style)
  {
    if (!Cache.IsInitialized)
    {
      return;
    }

    string path = KeyCapPath(style);
    if (!File.Exists(path))
    {
      return;
    }

    float width = capMax.X - capMin.X;
    float height = capMax.Y - capMin.Y;
    int w = Math.Max(1, (int)MathF.Ceiling(width * 2f));
    int h = Math.Max(1, (int)MathF.Ceiling(height * 2f));
    Cache.TryDraw(draw, capMin, capMax, path, w, h);
  }

  public static string KeyCapPath(CalcButtonStyle style)
  {
    string file = style switch
    {
      CalcButtonStyle.Grey => "KeyCap-grey.svg",
      CalcButtonStyle.Blue => "KeyCap-blue.svg",
      CalcButtonStyle.Orange => "KeyCap.svg",
      _ => "KeyCap-black.svg",
    };

    return Path.Combine(AssetsRoot, "Keys", file);
  }

  public static void Dispose() => Cache.Dispose();
}
