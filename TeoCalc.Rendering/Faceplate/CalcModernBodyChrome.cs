using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>00D body chrome — 1q fitils + dark-gray band + inner body. Keys/labels excluded.</summary>
public static class CalcModernBodyChrome
{
  public static void Draw(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcModelDefinition model)
  {
    float scale = metrics.Scale;

    RectF body = ScaleRect(origin, scale, new RectF(0f, 0f, metrics.Layout.ReferenceWidth, metrics.Layout.ReferenceHeight));
    RectF bezel = ScaleRect(origin, scale, metrics.Layout.DisplaySlot);
    RectF glass = ScaleRect(origin, scale, Calc00dBodyLayout.GlassFromBezel(metrics.Layout.DisplaySlot));

    DrawOuterFrameStack(draw, body, scale);
    DrawDisplayFrame(draw, bezel, glass, scale);

    // The logo is a fixed-height window-level bottom band now; only draw an in-faceplate
    // logo for layouts that still declare a non-zero logo slot.
    RectF logo = ScaleRect(origin, scale, metrics.Layout.LogoSlot);
    if (logo.Height > 0f)
    {
      CalcLogoPanelComponent.Draw(draw, logo, scale, model);
    }
  }

  /// <summary>
  /// Display surround outside→in: thin black fitil → thin dark-gray band → thin black fitil → LED glass.
  /// </summary>
  private static void DrawDisplayFrame(ImDrawListPtr draw, RectF bezel, RectF glass, float scale)
  {
    float fitilW = Calc00dWireStyle.DisplayFitilWidthRef * scale;
    float bandW = Calc00dWireStyle.DisplayBandWidthRef * scale;
    float r = Calc00dWireStyle.DisplayBezelRadiusRef * scale;
    float glassR = Calc00dWireStyle.DisplayGlassRadiusRef * scale;

    // 1. Outer thin black fitil
    RectF cursor = DrawFitil(draw, bezel, r, fitilW, Calc00dWireStyle.BlackFitilFill, Calc00dWireStyle.BlackFitilShine);
    r = MathF.Max(glassR + fitilW + bandW, 0f);

    // 2. Thin dark-gray band
    FillRoundedRect(draw, cursor, r, Calc00dWireStyle.DisplayBandFill);
    cursor = Inset(cursor, bandW);
    r = MathF.Max(glassR + fitilW, 0f);

    // 3. Inner thin black fitil
    cursor = DrawFitil(draw, cursor, r, fitilW, Calc00dWireStyle.BlackFitilFill, Calc00dWireStyle.BlackFitilShine);

    // 4. LED glass (layout slot; color already matches 00d)
    FillRoundedRect(draw, glass, glassR, Calc00dWireStyle.DisplayGlassFill);
  }

  /// <summary>
  /// Fills the body with the inner body color. The outer bead frame and dark-gray
  /// top band are drawn at the window level, so the faceplate only paints its body.
  /// </summary>
  private static void DrawOuterFrameStack(ImDrawListPtr draw, RectF body, float scale)
  {
    float faceR = Calc00dWireStyle.FaceplateRadiusRef * scale;
    FillRoundedRect(draw, body, faceR, Calc00dWireStyle.InnerBodyFill);
  }

  /// <summary>
  /// Paints one 1q fitil as outer shine crest + body, then returns the rect inside the fitil.
  /// </summary>
  private static RectF DrawFitil(
    ImDrawListPtr draw,
    RectF outer,
    float radius,
    float width,
    uint bodyColor,
    uint shineColor)
  {
    float shine = MathF.Max(1f, width * Calc00dWireStyle.FitilShineFraction);

    // Outer crest catches light.
    FillRoundedRect(draw, outer, radius, shineColor);

    // Remainder of the bead is the fitil body (center punched by the next layer).
    RectF body = Inset(outer, shine);
    FillRoundedRect(draw, body, MathF.Max(0f, radius - shine), bodyColor);

    return Inset(outer, width);
  }

  private static RectF Inset(RectF rect, float amount) => new(
    rect.X + amount,
    rect.Y + amount,
    MathF.Max(0f, rect.Width - amount * 2f),
    MathF.Max(0f, rect.Height - amount * 2f));

  private static RectF ScaleRect(Vector2 origin, float scale, RectF rect) => new(
    origin.X + rect.X * scale,
    origin.Y + rect.Y * scale,
    rect.Width * scale,
    rect.Height * scale);

  private static void FillRoundedRect(ImDrawListPtr draw, RectF rect, float radius, uint color)
  {
    draw.AddRectFilled(rect.Min, rect.Max, color, radius, ImDrawFlags.RoundCornersAll);
  }
}
