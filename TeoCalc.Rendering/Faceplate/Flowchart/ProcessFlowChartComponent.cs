using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

public sealed class ProcessFlowChartComponent : FlowChartComponentBase
{
  private static readonly uint Fill = 0xFF3A3E42u;

  public ProcessFlowChartComponent(StudioFlowchartGraph.Node node)
    : base(node)
  {
  }

  protected override uint DarkFillColor => Fill;

  protected override void DrawShape(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    uint fill,
    uint border,
    float thickness)
  {
    draw.AddRectFilled(min, max, fill, 0f);
    draw.AddRect(min, max, border, 0f, ImDrawFlags.None, thickness);
  }
}
