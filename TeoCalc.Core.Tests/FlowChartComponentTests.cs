using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;
using TeoCalc.Rendering.Faceplate.Flowchart;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class FlowChartComponentTests
{
  [TestMethod]
  public void Factory_MapsNodeKindsToTypedComponents()
  {
    Assert.IsInstanceOfType(
      FlowChartComponentFactory.Create(Node(StudioFlowchartGraph.NodeKind.Start)),
      typeof(StartFlowChartComponent));
    Assert.IsInstanceOfType(
      FlowChartComponentFactory.Create(Node(StudioFlowchartGraph.NodeKind.Process)),
      typeof(ProcessFlowChartComponent));
    Assert.IsInstanceOfType(
      FlowChartComponentFactory.Create(Node(StudioFlowchartGraph.NodeKind.Decision)),
      typeof(DecisionFlowChartComponent));
    Assert.IsInstanceOfType(
      FlowChartComponentFactory.Create(Node(StudioFlowchartGraph.NodeKind.End)),
      typeof(EndFlowChartComponent));
    Assert.IsInstanceOfType(
      FlowChartComponentFactory.Create(Node(StudioFlowchartGraph.NodeKind.Branch)),
      typeof(BranchFlowChartComponent));
  }

  [TestMethod]
  public void TryGetLiveRegisters_AvailableAfterPowerOn()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryGetLiveRegisters(out IReadOnlyList<double> live));
    Assert.IsTrue(live.Count >= 9, "Expected Classic R0–R9 export for Data footer.");
  }

  private static StudioFlowchartGraph.Node Node(StudioFlowchartGraph.NodeKind kind) =>
    new(
      Id: 1,
      RoutineId: 0,
      Kind: kind,
      Caption: "X",
      FirstRow: 0,
      LastRow: 0,
      FirstStep: 0,
      LastStep: 0);
}
