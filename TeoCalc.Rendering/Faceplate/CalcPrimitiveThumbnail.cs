using System.Numerics;
using ImGuiNET;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Minimal launcher thumbnail: outer black frame + body + LED block + key faces only.
/// No fitils, key frames, skirts, text, CLEAR brackets, or SVG caps.
/// </summary>
public static class CalcPrimitiveThumbnail
{
  /// <summary>Face must cover at least this fraction of the key cell (each axis).</summary>
  private const float MinFaceFill = 0.82f;

  /// <summary>
  /// Extra L/R/bottom gutter for the key cluster inside the visible body (ref units).
  /// Layout FacePad (~9) is only slightly larger than the black frame (~4), so at thumb
  /// scale keys otherwise sit flush against the chrome.
  /// </summary>
  private const float KeySetMarginRef = 22f;

  private const float KeySetMarginFloorPx = 4f;

  public static void Draw(
    ImDrawListPtr draw,
    Vector2 origin,
    Vector2 available,
    TeoCalcModelDefinition model,
    CalcModelDefinition faceplateModel,
    ProgramVocabulary? vocabulary,
    CalcBodyLayout bodyLayout)
  {
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available, bodyLayout);
    float scale = metrics.Scale;
    RectF body = new(origin.X, origin.Y, metrics.Layout.ReferenceWidth * scale, metrics.Layout.ReferenceHeight * scale);

    // Outer black frame so the calc does not melt into the launcher panel.
    // Keep the floor modest — a 2px chrome ring eats tiny icons and makes keys look sparse.
    float outerR = Calc00dWireStyle.OuterRadiusRef * scale;
    float frameW = Math.Clamp(Calc00dWireStyle.FitilWidthRef * scale, 1f, 4f * scale + 1.25f);
    draw.AddRectFilled(body.Min, body.Max, Calc00dWireStyle.BlackFitilFill, outerR, ImDrawFlags.RoundCornersAll);

    RectF inner = Inset(body, frameW);
    float innerR = MathF.Max(0f, outerR - frameW);
    draw.AddRectFilled(inner.Min, inner.Max, Calc00dWireStyle.InnerBodyFill, innerR, ImDrawFlags.RoundCornersAll);

    // LED: flat dark glass, no fitil stack.
    RectF bezel = ScaleRect(origin, scale, metrics.Layout.DisplaySlot);
    RectF glass = ScaleRect(origin, scale, Calc00dBodyLayout.GlassFromBezel(metrics.Layout.DisplaySlot));
    draw.AddRectFilled(bezel.Min, bezel.Max, Calc00dWireStyle.BlackFitilFill, Calc00dWireStyle.DisplayBezelRadiusRef * scale);
    draw.AddRectFilled(glass.Min, glass.Max, Calc00dWireStyle.DisplayGlassFill, Calc00dWireStyle.DisplayGlassRadiusRef * scale);

    // Switch strip as a flat dark band (no knobs/labels).
    if (metrics.Layout.SwitchSlot.Height > 0f)
    {
      RectF switches = ScaleRect(origin, scale, metrics.Layout.SwitchSlot);
      draw.AddRectFilled(switches.Min, switches.Max, Calc00dWireStyle.SwitchPanelFill, 2f * scale);
    }

    if (vocabulary is null)
    {
      return;
    }

    RectF keypadSrc = metrics.KeypadPanelRect(origin);
    ResolveKeySetMap(inner, keypadSrc, scale, out float mapOx, out float mapOy, out float mapScale);
    float gutterPx = CalcKeyPanelComponent.GutterRef * scale * mapScale;

    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(model.Family, model.Model);
    foreach (FaceplateCell cell in cells)
    {
      if (cell.KeyChartIndex < 0 || cell.KeyChartIndex >= vocabulary.KeyChart.Count)
      {
        continue;
      }

      ProgramKeyEntry key = vocabulary.KeyChart[cell.KeyChartIndex];
      if (key.KeyCode == 0 && !IsClassicKeyCodeZeroFaceplateSlot(model.Model, cell.KeyChartIndex))
      {
        continue;
      }

      RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
      if (keyRect.Width <= 0f || keyRect.Height <= 0f)
      {
        continue;
      }

      RectF mapped = MapKeyRect(keyRect, keypadSrc, mapOx, mapOy, mapScale);
      ResolveThumbKeyFace(mapped, scale * mapScale, gutterPx, out Vector2 faceMin, out Vector2 faceMax, out float keyR);

      CalcButtonStyle style = CalcFaceplateKeyStyles.StyleForKey(model.Family, model.Model, cell.KeyChartIndex);
      uint face = ThumbFaceColor(KeyCapPalette.ForStyle(style, hovered: false, pressed: false).Face);
      draw.AddRectFilled(faceMin, faceMax, face, keyR, ImDrawFlags.RoundCornersAll);
    }

    _ = faceplateModel;
  }

  /// <summary>
  /// Shrink/center the layout keypad into the inner body with L/R/bottom margin.
  /// Top-aligned so LED/switch spacing stays; horizontally centered.
  /// </summary>
  private static void ResolveKeySetMap(
    RectF inner,
    RectF keypadSrc,
    float scale,
    out float mapOx,
    out float mapOy,
    out float mapScale)
  {
    float margin = MathF.Max(KeySetMarginFloorPx, KeySetMarginRef * scale);
    float dstLeft = inner.X + margin;
    float dstRight = inner.Max.X - margin;
    float dstBottom = inner.Max.Y - margin;
    float dstTop = keypadSrc.Y;
    float dstW = MathF.Max(1f, dstRight - dstLeft);
    float dstH = MathF.Max(1f, dstBottom - dstTop);
    float srcW = MathF.Max(1f, keypadSrc.Width);
    float srcH = MathF.Max(1f, keypadSrc.Height);
    mapScale = MathF.Min(1f, MathF.Min(dstW / srcW, dstH / srcH));
    float mappedW = srcW * mapScale;
    mapOx = dstLeft + (dstW - mappedW) * 0.5f;
    mapOy = dstTop;
  }

  private static RectF MapKeyRect(RectF keyRect, RectF keypadSrc, float mapOx, float mapOy, float mapScale) => new(
    mapOx + (keyRect.X - keypadSrc.X) * mapScale,
    mapOy + (keyRect.Y - keypadSrc.Y) * mapScale,
    keyRect.Width * mapScale,
    keyRect.Height * mapScale);

  /// <summary>
  /// Full key cell is the frame; face fills most of it. CapAbove/Below collapse into the face
  /// so legend bands do not leave empty “frames”. At tiny pixel sizes, inset shrinks and faces
  /// grow slightly into the inter-key gutter so AA does not turn keys into dots.
  /// </summary>
  private static void ResolveThumbKeyFace(
    RectF keyRect,
    float scale,
    float gutterPx,
    out Vector2 faceMin,
    out Vector2 faceMax,
    out float rounding)
  {
    float minDim = MathF.Min(keyRect.Width, keyRect.Height);

    // Side inset as a fraction of the cell — never more than (1 - MinFaceFill) / 2.
    float insetFrac = minDim switch
    {
      >= 12f => 0.07f,
      >= 8f => 0.05f,
      >= 5f => 0.03f,
      _ => 0.015f,
    };
    insetFrac = MathF.Min(insetFrac, (1f - MinFaceFill) * 0.5f);
    float insetX = keyRect.Width * insetFrac;
    float insetY = keyRect.Height * insetFrac;

    // Steal a bit of gutter when the cell is small so neighboring faces stay readable blocks.
    float grow = 0f;
    if (minDim < 10f && gutterPx > 0f)
    {
      float growFrac = minDim < 5f ? 0.42f : minDim < 7f ? 0.32f : 0.22f;
      grow = gutterPx * growFrac * 0.5f;
    }

    faceMin = keyRect.Min + new Vector2(insetX - grow, insetY - grow);
    faceMax = keyRect.Max - new Vector2(insetX - grow, insetY - grow);
    if (faceMax.X <= faceMin.X || faceMax.Y <= faceMin.Y)
    {
      faceMin = keyRect.Min;
      faceMax = keyRect.Max;
    }

    float faceW = faceMax.X - faceMin.X;
    float faceH = faceMax.Y - faceMin.Y;
    float faceMinDim = MathF.Min(faceW, faceH);
    // Rounding tracks the face — avoid a fixed px floor that turns 3px keys into circles.
    rounding = MathF.Min(faceMinDim * 0.18f, MathF.Max(0.35f, 2.4f * scale));
  }

  /// <summary>
  /// Nudge faces away from inner-body grey so dark/cement keys do not vanish at icon size.
  /// </summary>
  private static uint ThumbFaceColor(uint face)
  {
    byte r = (byte)(face & 0xFF);
    byte g = (byte)((face >> 8) & 0xFF);
    byte b = (byte)((face >> 16) & 0xFF);
    byte a = (byte)(face >> 24);
    int lum = (r + g + b) / 3;
    const int bodyLum = 0x58;
    int delta = lum - bodyLum;
    if (Math.Abs(delta) >= 48)
    {
      return face;
    }

    // Mid-grey / near-body faces: push darker keys down, lighter keys up.
    int push = lum >= bodyLum ? 42 : -48;
    r = (byte)Math.Clamp(r + push, 0, 255);
    g = (byte)Math.Clamp(g + push, 0, 255);
    b = (byte)Math.Clamp(b + push, 0, 255);
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }

  private static bool IsClassicKeyCodeZeroFaceplateSlot(string modelId, int keyChartIndex) =>
    (string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase) && keyChartIndex == 4)
    || (string.Equals(modelId, "HP-45", StringComparison.OrdinalIgnoreCase) && keyChartIndex == 4)
    || (string.Equals(modelId, "HP-55", StringComparison.OrdinalIgnoreCase) && keyChartIndex is 4 or 9);

  private static RectF ScaleRect(Vector2 origin, float scale, RectF rect) => new(
    origin.X + rect.X * scale,
    origin.Y + rect.Y * scale,
    rect.Width * scale,
    rect.Height * scale);

  private static RectF Inset(RectF rect, float amount) => new(
    rect.X + amount,
    rect.Y + amount,
    MathF.Max(0f, rect.Width - amount * 2f),
    MathF.Max(0f, rect.Height - amount * 2f));
}
