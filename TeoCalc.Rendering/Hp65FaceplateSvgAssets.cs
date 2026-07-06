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
  private const int BodySvgRevision = 5;

  private const int LogoSvgRevision = 2;

  private static string AssetsRoot => TeoCalcPaths.ResourcePath("Engine/HP-65/Assets");

  private static string BodyPath => Path.Combine(AssetsRoot, "Body.svg");

  private static string LogoPath => Path.Combine(AssetsRoot, "HpLogo.svg");

  public static bool IsReady => Cache.IsInitialized && File.Exists(BodyPath);

  /// <summary>Body.svg chrome from faceplate-d03-layout.json companion (409×861).</summary>
  public static bool UseBodyChrome => false;

  /// <summary>Body/logo SVG cache is ready (key caps are procedural).</summary>
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
    if (!TryGetLogoBounds(origin, metrics, plate, out Vector2 logoMin, out Vector2 logoMax, out Vector2 plateMin, out Vector2 plateMax))
    {
      return;
    }

    if (Cache.IsInitialized && File.Exists(LogoPath))
    {
      int w = Math.Max(1, (int)MathF.Ceiling((logoMax.X - logoMin.X) * 5f));
      int h = Math.Max(1, (int)MathF.Ceiling((logoMax.Y - logoMin.Y) * 5f));
      if (!Cache.TryDraw(draw, logoMin, logoMax, LogoPath, w, h, revision: LogoSvgRevision))
      {
        Cache.TryDraw(draw, logoMin, logoMax, LogoPath, w, h, revision: LogoSvgRevision);
      }
    }

    float textMargin = (plateMax.X - plateMin.X) * 0.025f;
    float textLeft = logoMax.X + textMargin;
    CalcChassisRenderer.DrawBrandPlateText(draw, textLeft, plateMin, plateMax, textRightMargin: textMargin);
  }

  public static bool TryDrawLogoMark(ImDrawListPtr draw, Vector2 stripMin, Vector2 stripMax, float scale)
  {
    if (!Cache.IsInitialized || !File.Exists(LogoPath))
    {
      return false;
    }

    float plateWidth = stripMax.X - stripMin.X;
    float plateHeight = stripMax.Y - stripMin.Y;
    float logoPadX = plateWidth * 0.03f;
    float logoPadY = plateHeight * 0.06f;
    float maxLogoWidth = plateWidth * 0.58f;
    float logoHeight = (plateHeight - logoPadY * 2f) * 0.82f;
    float logoWidth = logoHeight * (888f / 562f) * 1.4f;
    if (logoWidth > maxLogoWidth)
    {
      logoWidth = maxLogoWidth;
      logoHeight = logoWidth / (888f / 562f) / 1.4f;
    }

    Vector2 logoMin = stripMin + new Vector2(logoPadX, logoPadY + (plateHeight - logoPadY * 2f - logoHeight) * 0.5f);
    Vector2 logoMax = logoMin + new Vector2(logoWidth, logoHeight);
    int w = Math.Max(1, (int)MathF.Ceiling(logoWidth * 5f));
    int h = Math.Max(1, (int)MathF.Ceiling(logoHeight * 5f));
    return Cache.TryDraw(draw, logoMin, logoMax, LogoPath, w, h, revision: LogoSvgRevision);
  }

  private static bool TryGetLogoBounds(
    Vector2 origin,
    CalcChassisMetrics metrics,
    RectF plate,
    out Vector2 logoMin,
    out Vector2 logoMax,
    out Vector2 plateMin,
    out Vector2 plateMax)
  {
    float plateWidth = plate.Width * metrics.Scale;
    float plateHeight = plate.Height * metrics.Scale;
    plateMin = origin + new Vector2(plate.X * metrics.Scale, plate.Y * metrics.Scale);
    plateMax = plateMin + new Vector2(plateWidth, plateHeight);

    float logoAspect = 888f / 562f;
    float logoPadX = plateWidth * 0.03f;
    float logoPadY = plateHeight * 0.06f;
    float maxLogoWidth = plateWidth * 0.58f;
    float logoHeight = (plateHeight - logoPadY * 2f) * 0.82f;
    float logoWidth = logoHeight * logoAspect * 1.4f;
    if (logoWidth > maxLogoWidth)
    {
      logoWidth = maxLogoWidth;
      logoHeight = logoWidth / (logoAspect * 1.4f);
    }

    logoMin = plateMin + new Vector2(logoPadX, logoPadY + (plateHeight - logoPadY * 2f - logoHeight) * 0.5f);
    logoMax = logoMin + new Vector2(logoWidth, logoHeight);
    return true;
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
    KeyCapRenderer.Draw(
      draw,
      capMin,
      capMax,
      KeyCapPalette.ForStyle(style, hovered: false, pressed: false),
      hovered: false,
      pressed: false,
      scale: MathF.Max(1f, (capMax.X - capMin.X) / 48f));
  }

  public static string KeyCapPath(CalcButtonStyle style) => LegacyKeyCapPath(style);

  private static string LegacyKeyCapPath(CalcButtonStyle style)
  {
    string file = style switch
    {
      CalcButtonStyle.Grey => "KeyCap-grey.svg",
      CalcButtonStyle.White => "KeyCap-grey.svg",
      CalcButtonStyle.Blue => "KeyCap-blue.svg",
      CalcButtonStyle.Orange => "KeyCap.svg",
      _ => "KeyCap-black.svg",
    };

    return Path.Combine(AssetsRoot, "Keys", file);
  }

  public static void Dispose() => Cache.Dispose();
}
