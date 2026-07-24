using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

/// <summary>Shared hit-test + chrome for START / PROCESS / DECISION / END / BRANCH boxes.</summary>
public abstract class FlowChartComponentBase : IFlowChartComponent
{
  protected FlowChartComponentBase(StudioFlowchartGraph.Node node)
  {
    Node = node;
  }

  public StudioFlowchartGraph.Node Node { get; }

  /// <summary>Dark-theme fill; Light theme remaps via <see cref="ResolveFill"/>.</summary>
  protected abstract uint DarkFillColor { get; }

  protected uint FillColor => ResolveFill(DarkFillColor);

  public virtual bool HitTest(
    Vector2 pointer,
    Vector2 screenMin,
    Vector2 screenMax,
    Vector2 clipMin,
    Vector2 clipMax) =>
    StudioFlowchartView.HitVisibleNode(pointer, screenMin, screenMax, clipMin, clipMax);

  public void Draw(
    ImDrawListPtr draw,
    Vector2 screenMin,
    Vector2 screenMax,
    in FlowChartDrawContext context)
  {
    DrawShape(draw, screenMin, screenMax, FillColor, context.Border, context.BorderThickness);
    DrawCaption(draw, screenMin, screenMax, context);
  }

  protected abstract void DrawShape(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    uint fill,
    uint border,
    float thickness);

  protected virtual void DrawCaption(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    in FlowChartDrawContext context) =>
    StudioFlowchartView.DrawNodeCaption(draw, Node, min, max, context.ModelId, context.CardStripCaptions);

  /// <summary>
  /// Light theme: lift dark charcoal fills toward the shell panel so boxes are not black holes.
  /// Keeps a hint of the node-role tint from the dark palette.
  /// </summary>
  internal static uint ResolveFill(uint darkFill)
  {
    CalcAppTheme.EnsureInitialized();
    if (CalcAppTheme.Appearance != ThemeAppearance.Light)
    {
      return darkFill;
    }

    ThemePalette palette = CalcAppTheme.Current;
    ThemeColor panel = palette.Get(ThemeTokens.ShellContentPanelBackColor);
    float dr = ((darkFill >> 0) & 0xFF) / 255f;
    float dg = ((darkFill >> 8) & 0xFF) / 255f;
    float db = ((darkFill >> 16) & 0xFF) / 255f;
    // Mix ~78% panel (light) with ~22% role tint so Light stays readable.
    float r = panel.R * 0.78f + dr * 0.22f;
    float g = panel.G * 0.78f + dg * 0.22f;
    float b = panel.B * 0.78f + db * 0.22f;
    return ImGui.ColorConvertFloat4ToU32(new Vector4(r, g, b, 1f));
  }
}
