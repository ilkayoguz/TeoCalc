namespace TeoCalc.Rendering.Faceplate.Flowchart;

public static class FlowChartComponentFactory
{
  public static IFlowChartComponent Create(StudioFlowchartGraph.Node node) =>
    node.Kind switch
    {
      StudioFlowchartGraph.NodeKind.Start => new StartFlowChartComponent(node),
      StudioFlowchartGraph.NodeKind.End => new EndFlowChartComponent(node),
      StudioFlowchartGraph.NodeKind.Decision => new DecisionFlowChartComponent(node),
      StudioFlowchartGraph.NodeKind.Branch => new BranchFlowChartComponent(node),
      _ => new ProcessFlowChartComponent(node),
    };
}
