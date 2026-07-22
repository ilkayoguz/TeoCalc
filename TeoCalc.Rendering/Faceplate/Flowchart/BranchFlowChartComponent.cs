using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

/// <summary>GTO/GSB branch box (rect process chrome, distinct fill).</summary>
public sealed class BranchFlowChartComponent : FlowChartComponentBase
{
  private static readonly uint Fill = 0xFF3A424Au;

  public BranchFlowChartComponent(StudioFlowchartGraph.Node node)
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
    draw.AddRectFilled(min, max, fill, 0f);
    draw.AddRect(min, max, border, 0f, ImDrawFlags.None, thickness);
  }
}
