using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate.Flowchart;

/// <summary>One flowchart symbol: measure/hit/draw owned by a typed component.</summary>
public interface IFlowChartComponent
{
  StudioFlowchartGraph.Node Node { get; }

  void Draw(
    ImDrawListPtr draw,
    Vector2 screenMin,
    Vector2 screenMax,
    in FlowChartDrawContext context);

  bool HitTest(
    Vector2 pointer,
    Vector2 screenMin,
    Vector2 screenMax,
    Vector2 clipMin,
    Vector2 clipMax);
}
