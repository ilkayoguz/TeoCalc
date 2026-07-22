using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

/// <summary>Shared hit-test + chrome for START / PROCESS / DECISION / END / BRANCH boxes.</summary>
public abstract class FlowChartComponentBase : IFlowChartComponent
{
  protected FlowChartComponentBase(StudioFlowchartGraph.Node node)
  {
    Node = node;
  }

  public StudioFlowchartGraph.Node Node { get; }

  protected abstract uint FillColor { get; }

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
}
