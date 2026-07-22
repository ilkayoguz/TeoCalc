using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

public sealed class EndFlowChartComponent : FlowChartComponentBase
{
  private static readonly uint Fill = 0xFF4A3A3Au;

  public EndFlowChartComponent(StudioFlowchartGraph.Node node)
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
    float radius = (max.Y - min.Y) * 0.5f;
    draw.AddRectFilled(min, max, fill, radius);
    draw.AddRect(min, max, border, radius, ImDrawFlags.None, thickness);
  }
}
