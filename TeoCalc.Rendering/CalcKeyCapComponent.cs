using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>
/// One HP key cap in screen space: Key2 geometry bands for runtime labels + procedural draw.
/// </summary>
public readonly struct CalcKeyCapComponent
{
  public Vector2 CapMin { get; init; }

  public Vector2 CapMax { get; init; }

  public (Vector2 Min, Vector2 Max) FaceBand => KeyCapGeometry.FaceLabelRect(CapMin, CapMax);

  public (Vector2 Min, Vector2 Max) BottomBand => KeyCapGeometry.BottomLabelRect(CapMin, CapMax);

  public Vector2 FaceCenter => KeyCapGeometry.BandCenter(FaceBand.Min, FaceBand.Max);

  public Vector2 BottomCenter => KeyCapGeometry.BandCenter(BottomBand.Min, BottomBand.Max);

  public void DrawCap(
    ImDrawListPtr draw,
    CalcButtonStyle style,
    bool hovered,
    bool pressed,
    float scale,
    CalcButtonKind kind = CalcButtonKind.Standard)
  {
    KeyCapRenderer.DrawBezel(draw, CapMin, CapMax, scale, fixedRadius: kind == CalcButtonKind.EnterWide);
    KeyCapRenderer.Draw(draw, CapMin, CapMax, KeyCapPalette.ForStyle(style, hovered, pressed), hovered, pressed, scale, fixedFaceRadius: kind == CalcButtonKind.EnterWide);
  }
}
