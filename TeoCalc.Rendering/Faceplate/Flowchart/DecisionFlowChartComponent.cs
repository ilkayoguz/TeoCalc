using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

public sealed class DecisionFlowChartComponent : FlowChartComponentBase
{
  private static readonly uint Fill = 0xFF3E3A48u;

  public DecisionFlowChartComponent(StudioFlowchartGraph.Node node)
    : base(node)
  {
  }

  protected override uint FillColor => Fill;

  protected override void DrawShape(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    uint fill,
    uint border,
    float thickness)
  {
    Vector2 center = (min + max) * 0.5f;
    Vector2 top = new(center.X, min.Y);
    Vector2 right = new(max.X, center.Y);
    Vector2 bottom = new(center.X, max.Y);
    Vector2 left = new(min.X, center.Y);
    draw.AddQuadFilled(top, right, bottom, left, fill);
    draw.AddQuad(top, right, bottom, left, border, thickness);
  }
}
