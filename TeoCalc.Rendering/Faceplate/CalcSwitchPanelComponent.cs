using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Faceplate band that hosts slide switches (labels included in size).
/// Draws a recessed panel, then lays out <see cref="CalcSwitchComponent"/> units inside it.
/// Horizontally aligned with <see cref="CalcDisplayComponent.OuterSlot"/>.
/// </summary>
public static class CalcSwitchPanelComponent
{
  public const float GapBelowDisplayRef = 8f;

  public const float GapAboveKeypadRef = 8f;

  public const float MinHeightRef = 52f;

  public readonly record struct ContentMetrics(float Width, float Height, float MaxAboveTrack, float MaxBelowTrack);

  /// <summary>Reference-unit panel height from specs (no ImGui). Shrinks when Top/Bottom absent.</summary>
  public static float PreferredHeightRef(IReadOnlyList<CalcSwitchSpec> specs)
  {
    bool anyTop = false;
    bool anyBottom = false;
    for (int i = 0; i < specs.Count; i++)
    {
      anyTop |= !string.IsNullOrEmpty(specs[i].TopLabel);
      anyBottom |= !string.IsNullOrEmpty(specs[i].BottomLabel);
    }

    float knob = Calc00dWireStyle.SwitchKnobHeightRefPx;
    float pad = Calc00dWireStyle.SwitchPanelPadYRef * 2f;
    float label = CalcSwitchComponent.FontSizeRef * 0.85f;
    float vGap = CalcSwitchComponent.VerticalLabelGapRef;
    float h = pad + knob;
    if (anyTop)
    {
      h += label + vGap + knob * 0.05f;
    }

    if (anyBottom)
    {
      h += label + vGap + knob * 0.05f;
    }

    return MathF.Max(MinHeightRef, h);
  }

  /// <summary>Switch panel rect aligned to the face band, sized for the model’s labels.</summary>
  public static RectF ResolveSlotRef(IReadOnlyList<CalcSwitchSpec> specs, float bandLeft, float bandWidth, float displayBottom)
  {
    float height = PreferredHeightRef(specs);
    float y = displayBottom + GapBelowDisplayRef;
    return new RectF(bandLeft, y, bandWidth, height);
  }

  /// <summary>Legacy helper — uses <see cref="CalcDisplayComponent"/> band.</summary>
  public static RectF ResolveSlotRef(IReadOnlyList<CalcSwitchSpec> specs) =>
    ResolveSlotRef(
      specs,
      CalcDisplayComponent.BandLeft,
      CalcDisplayComponent.BandWidth,
      CalcDisplayComponent.BandBottom);

  /// <summary>Union size of all switch units (labels + knobs), plus panel padding.</summary>
  public static ContentMetrics MeasureContent(IReadOnlyList<CalcSwitchSpec> specs, float scale)
  {
    if (specs.Count == 0)
    {
      return new ContentMetrics(0f, 0f, 0f, 0f);
    }

    float maxAbove = 0f;
    float maxBelow = 0f;
    float maxUnitW = 0f;
    float sumW = 0f;
    for (int i = 0; i < specs.Count; i++)
    {
      CalcSwitchComponent.Metrics m = CalcSwitchComponent.Measure(specs[i], scale);
      maxAbove = MathF.Max(maxAbove, m.AboveTrack);
      maxBelow = MathF.Max(maxBelow, m.BelowTrack);
      maxUnitW = MathF.Max(maxUnitW, m.Width);
      sumW += m.Width;
    }

    float padX = Calc00dWireStyle.SwitchPanelPadXRef * scale;
    float padY = Calc00dWireStyle.SwitchPanelPadYRef * scale;
    float height = maxAbove + maxBelow + padY * 2f;
    float width = specs.Count <= 1
      ? maxUnitW + padX * 2f
      : sumW + padX * 2f;
    return new ContentMetrics(width, height, maxAbove, maxBelow);
  }

  public static RectF InnerSlot(RectF panelSlot, float scale)
  {
    float padX = Calc00dWireStyle.SwitchPanelPadXRef * scale;
    return new(
      panelSlot.X + padX,
      panelSlot.Y,
      MathF.Max(0f, panelSlot.Width - padX * 2f),
      panelSlot.Height);
  }

  public static float TrackCenterY(RectF panelSlot, IReadOnlyList<CalcSwitchSpec> specs, float scale)
  {
    ContentMetrics content = MeasureContent(specs, scale);
    float padY = Calc00dWireStyle.SwitchPanelPadYRef * scale;
    float bandH = content.MaxAboveTrack + content.MaxBelowTrack;
    float bandTop = panelSlot.Min.Y + MathF.Max(padY, (panelSlot.Height - bandH) * 0.5f);
    return bandTop + content.MaxAboveTrack;
  }

  public static float TrackCenterX(RectF panelSlot, float scale, IReadOnlyList<CalcSwitchSpec> specs, int index) =>
    CalcSwitchComponent.TrackCenterX(InnerSlot(panelSlot, scale), scale, specs, index);

  public static CalcSwitchComponent.HitLayout BuildHitLayout(
    RectF panelSlot,
    float scale,
    IReadOnlyList<CalcSwitchSpec> specs,
    int index,
    float positionNorm)
  {
    float trackCenterY = TrackCenterY(panelSlot, specs, scale);
    return CalcSwitchComponent.BuildHitLayout(
      InnerSlot(panelSlot, scale),
      trackCenterY,
      scale,
      specs,
      index,
      positionNorm);
  }

  public static void Draw(
    ImDrawListPtr draw,
    RectF panelSlot,
    float scale,
    IReadOnlyList<CalcSwitchSpec> specs,
    Func<int, CalcSwitchSpec, float> positionNormForIndex,
    bool modernChrome = true)
  {
    if (modernChrome)
    {
      float radius = Calc00dWireStyle.SwitchPanelRadiusRef * scale;
      draw.AddRectFilled(
        panelSlot.Min,
        panelSlot.Max,
        Calc00dWireStyle.SwitchPanelFill,
        radius,
        ImDrawFlags.RoundCornersAll);
    }
    else
    {
      draw.AddRectFilled(panelSlot.Min, panelSlot.Max, CalcChassisPalette.SliderTrack, 4f * scale);
    }

    if (specs.Count == 0)
    {
      return;
    }

    float trackCenterY = TrackCenterY(panelSlot, specs, scale);
    CalcSwitchComponent.DrawRow(
      draw,
      InnerSlot(panelSlot, scale),
      trackCenterY,
      scale,
      specs,
      positionNormForIndex,
      modernChrome);
  }
}
