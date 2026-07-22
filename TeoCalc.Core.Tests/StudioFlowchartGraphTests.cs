using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioFlowchartGraphTests
{
  [TestMethod]
  public void Build_LblRoutine_HasStartProcessEnd()
  {
    // LBL A · 1 2 3 + · RTN  →  START / PROCESS / END
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Pointer, "PTR"),
      new(2, ClassicProgramCodes.Label, "LBL"),
      new(3, 11, "A"),
      new(4, 1, "1"),
      new(5, 2, "2"),
      new(6, 3, "3"),
      new(7, 51, "+"),
      new(8, 24, "RTN"),
      new(9, ClassicProgramCodes.Label, "LBL"),
      new(10, 1, "1"),
      new(11, 4, "4"),
      new(12, 5, "5"),
      new(13, 6, "6"),
      new(14, 51, "+"),
      new(15, 24, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      modelId: null,
      cardStripCaptions: ["+123", "", "", "", ""]);

    Assert.AreEqual(2, graph.Routines.Count);
    Assert.AreEqual("A", graph.Routines[0].Title);
    Assert.AreEqual("A", graph.Routines[0].LabelKey);
    Assert.AreEqual("1", graph.Routines[1].Title);

    StudioFlowchartGraph.Node[] aNodes = graph.Nodes
      .Where(n => n.RoutineId == 0)
      .ToArray();
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Start, aNodes[0].Kind);
    Assert.AreEqual("A", aNodes[0].Caption);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Process, aNodes[1].Kind);
    Assert.AreEqual("+123", aNodes[1].Caption);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.End, aNodes[2].Kind);
    Assert.AreEqual("RTN", aNodes[2].Caption);

    // Independent routines: no fall-through between LBL A and LBL 1.
    Assert.IsFalse(graph.Edges.Any(e =>
      e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough
      && graph.Nodes[e.FromId].RoutineId == 0
      && e.ToId is int to
      && graph.Nodes[to].RoutineId == 1));
  }

  [TestMethod]
  public void Build_ProcessCaption_UsesStudioLegendNotKeystrokes()
  {
    // g 4 → Legend 1/x; f 9 → √x; keep one chunked PROCESS.
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 8, "g", 4, "4", StudioListingView.MergeKind.ShiftPair),
      new(4, 31, "f", 9, "9", StudioListingView.MergeKind.ShiftPair),
      new(6, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("1/x · √x", process.Caption);
    Assert.AreEqual(1, graph.Nodes.Count(n => n.Kind == StudioFlowchartGraph.NodeKind.Process));
  }

  [TestMethod]
  public void Build_NopFillers_AreOmittedFromProcessAndDoNotSplitChunk()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 10, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(2, 35, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(3, 2, "2", null, null, StudioListingView.MergeKind.Single),
      new(4, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("1 · 2", process.Caption);
    Assert.IsFalse(process.Caption.Contains("NOP", StringComparison.OrdinalIgnoreCase));
  }

  [TestMethod]
  public void Build_ProcessCaption_RclDivide_IsR2OverR1()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 12, "B", StudioListingView.MergeKind.LabelPair),
      new(2, 35, "RCL 2", null, null, StudioListingView.MergeKind.Single),
      new(3, 34, "RCL 1", null, null, StudioListingView.MergeKind.Single),
      new(4, 81, "/", null, null, StudioListingView.MergeKind.Single),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    // No strip caption → compact algebra.
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65", cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("R2 / R1", process.Caption);
  }

  [TestMethod]
  public void Build_ProcessCaption_RclThenSqrt_IsSqrtR1()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 34, "RCL 1", null, null, StudioListingView.MergeKind.Single),
      new(3, 31, "f", 9, "9", StudioListingView.MergeKind.ShiftPair),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("√R1", process.Caption);
  }

  [TestMethod]
  public void Build_ProcessCaption_StoPlusDigit_IsStoPlusR()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 11, "A"),
      new(2, 11, "STO"),
      new(3, 51, "+"), // not Pointer (61)
      new(4, 4, "1"),
      new(5, 24, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(StudioListingView.MergeKind.RegisterArith, rows[1].Kind);
    Assert.AreEqual("STO+1", rows[1].DisplayMnemonic);

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("STO + 1", process.Caption);
  }

  [TestMethod]
  public void Build_ProcessCaption_DigitThenPlus_IsPrefixForm()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 51, "+", null, null, StudioListingView.MergeKind.Single),
      new(4, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("+1", process.Caption);
  }

  [TestMethod]
  public void Build_ProcessCaption_MixedChunk_RewritesEmbeddedDigitPlus()
  {
    // Chebyshev-style: RCL 2 · 1 · + · STO 2  →  RCL R2 · +1 · STO R2
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 1, "1", StudioListingView.MergeKind.LabelPair),
      new(2, 35, "RCL 2", null, null, StudioListingView.MergeKind.Single),
      new(3, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(4, 51, "+", null, null, StudioListingView.MergeKind.Single),
      new(5, 45, "STO 2", null, null, StudioListingView.MergeKind.Single),
      new(6, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("R2 · +1 · STO R2", process.Caption);
  }

  [TestMethod]
  public void Build_ProcessCaption_DigitsThenPlus_IsPrefixForm()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 1, "1", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 2, "2", null, null, StudioListingView.MergeKind.Single),
      new(4, 3, "3", null, null, StudioListingView.MergeKind.Single),
      new(5, 51, "+", null, null, StudioListingView.MergeKind.Single),
      new(6, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("+123", process.Caption);
  }

  [TestMethod]
  public void Build_DecisionCaption_OmitsTrailingQuestionMark()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 10, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 50, "x=y?", null, null, StudioListingView.MergeKind.Single),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    StudioFlowchartGraph.Node decision = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision);
    Assert.AreEqual("x=y", decision.Caption);
    Assert.IsFalse(decision.Caption.EndsWith('?'));
  }

  [TestMethod]
  public void Build_ProcessCaption_BareSto_IsStoRn()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 45, "STO 1", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["", "", "", "", ""]);
    StudioFlowchartGraph.Node process = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("STO R1", process.Caption);
  }

  [TestMethod]
  public void Build_EmptyLblRtnStubs_AreOmittedFromGraph()
  {
    // Real LBL A body + Classic fall-through stubs B–E (LBL + RTN only).
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(4, ClassicProgramCodes.Label, "LBL", 12, "B", StudioListingView.MergeKind.LabelPair),
      new(6, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(7, ClassicProgramCodes.Label, "LBL", 13, "C", StudioListingView.MergeKind.LabelPair),
      new(9, 35, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(10, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(11, ClassicProgramCodes.Label, "LBL", 14, "D", StudioListingView.MergeKind.LabelPair),
      new(13, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(14, ClassicProgramCodes.Label, "LBL", 15, "E", StudioListingView.MergeKind.LabelPair),
      new(16, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    Assert.AreEqual(1, graph.Routines.Count);
    Assert.AreEqual("A", graph.Routines[0].Title);
    Assert.IsFalse(graph.Routines.Any(r =>
      r.LabelKey is "B" or "C" or "D" or "E"));
  }

  [TestMethod]
  public void Build_ListingOmitsInjectedStripStubs_BeforeGraph()
  {
    // Same shape ToClassicSnapshot injects after a sparse .t65 (only LBL A + LBL 1).
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Label, "LBL"),
      new(2, 11, "A"),
      new(3, 1, "1"),
      new(4, 24, "RTN"),
      new(5, ClassicProgramCodes.Label, "LBL"),
      new(6, 1, "1"),
      new(7, 4, "4"),
      new(8, 24, "RTN"),
      new(9, ClassicProgramCodes.Label, "LBL"),
      new(10, 12, "B"),
      new(11, 24, "RTN"),
      new(12, ClassicProgramCodes.Label, "LBL"),
      new(13, 13, "C"),
      new(14, 24, "RTN"),
      new(15, ClassicProgramCodes.Label, "LBL"),
      new(16, 14, "D"),
      new(17, 24, "RTN"),
      new(18, ClassicProgramCodes.Label, "LBL"),
      new(19, 15, "E"),
      new(20, 24, "RTN"),
      new(21, ClassicProgramCodes.Pointer, "PTR"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.IsFalse(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key)
      && key is "B" or "C" or "D" or "E"));
    Assert.IsTrue(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key) && key == "A"));
    Assert.IsTrue(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key) && key == "1"));

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      cardStripCaptions: ["+123", "", "", "", ""]);
    Assert.AreEqual(2, graph.Routines.Count);
    Assert.IsFalse(graph.Routines.Any(r => r.LabelKey is "B" or "C" or "D" or "E"));
  }

  [TestMethod]
  public void Build_ListingOmitsDuplicateLblA_G4Builtin_AtEnd()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Label, "LBL"),
      new(2, 11, "A"),
      new(3, 45, "STO 1"),
      new(4, 24, "RTN"),
      new(5, ClassicProgramCodes.Label, "LBL"),
      new(6, 11, "A"),
      new(7, 8, "g"),
      new(8, 20, "4"),
      new(9, 24, "RTN"),
      new(10, ClassicProgramCodes.Pointer, "PTR"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual("RTN", rows[^1].DisplayMnemonic.Trim());
    Assert.AreEqual(1, rows.Count(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key)
      && string.Equals(key, "A", StringComparison.OrdinalIgnoreCase)));
    Assert.IsFalse(rows.Any(r => r.Mnemonic == "g" && r.SecondMnemonic == "4"));
  }

  [TestMethod]
  public void Build_Listing_SynthesizesMissingNoCardLblE()
  {
    // Classic ROM often boots A–D only; faceplate still shows E=x↔y.
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 30, "A"),
      new(2, 8, "g"),
      new(3, 20, "4"),
      new(4, 42, "RTN"),
      new(5, ClassicProgramCodes.Label, "LBL"),
      new(6, 28, "B"),
      new(7, 14, "f"),
      new(8, 50, "9"),
      new(9, 42, "RTN"),
      new(10, ClassicProgramCodes.Label, "LBL"),
      new(11, 27, "C"),
      new(12, 8, "g"),
      new(13, 19, "5"),
      new(14, 42, "RTN"),
      new(15, ClassicProgramCodes.Label, "LBL"),
      new(16, 26, "D"),
      new(17, 13, "RDOWN"),
      new(18, 42, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.IsTrue(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key)
      && string.Equals(key, "E", StringComparison.OrdinalIgnoreCase)));
    Assert.IsTrue(rows.Any(r => r.DisplayMnemonic.Equals("X<>Y", StringComparison.OrdinalIgnoreCase)));

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    Assert.AreEqual(5, graph.Routines.Count);
    CollectionAssert.AreEquivalent(
      new[] { "A", "B", "C", "D", "E" },
      graph.Routines.Select(r => r.LabelKey).ToArray());
  }

  [TestMethod]
  public void Build_Listing_KeepsNoCardLblA_FaceplateBuiltin()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 11, "A"),
      new(2, 8, "g"),
      new(3, 20, "4"),
      new(4, 24, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.IsTrue(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key)
      && string.Equals(key, "A", StringComparison.OrdinalIgnoreCase)));

    // Sole LBL A firmware body expands to the full no-card A–E faceplate catalog.
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    Assert.AreEqual(5, graph.Routines.Count);
    CollectionAssert.AreEquivalent(
      new[] { "A", "B", "C", "D", "E" },
      graph.Routines.Select(r => r.LabelKey).ToArray());
  }

  [TestMethod]
  public void Build_ListingOmitsFirmwareLblE_XyBuiltin_WithoutAuthoringFilter()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Label, "LBL"),
      new(2, 11, "A"),
      new(3, 45, "STO 1"),
      new(4, 24, "RTN"),
      new(5, ClassicProgramCodes.Label, "LBL"),
      new(6, 15, "E"),
      new(7, 24, "RTN"),
      new(8, ClassicProgramCodes.Label, "LBL"),
      new(9, 15, "E"),
      new(10, 17, "X<>Y"),
      new(11, 24, "RTN"),
      new(12, ClassicProgramCodes.Pointer, "PTR"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual("RTN", rows[^1].DisplayMnemonic.Trim());
    Assert.IsFalse(rows.Any(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key) && key == "E"));
    Assert.IsFalse(rows.Any(r => r.DisplayMnemonic.Equals("X<>Y", StringComparison.OrdinalIgnoreCase)));

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    Assert.AreEqual(1, graph.Routines.Count);
    Assert.AreEqual("A", graph.Routines[0].LabelKey);
  }

  [TestMethod]
  public void Build_ListingOmitsFirmwareLblA_G4Builtin_WithoutAuthoringFilter()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Label, "LBL"),
      new(2, 11, "A"),
      new(3, 45, "STO 1"),
      new(4, 24, "RTN"),
      new(5, ClassicProgramCodes.Label, "LBL"),
      new(6, 11, "A"),
      new(7, 8, "g"),
      new(8, 20, "4"),
      new(9, 24, "RTN"),
      new(10, ClassicProgramCodes.Pointer, "PTR"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual("RTN", rows[^1].DisplayMnemonic.Trim());
    Assert.AreEqual(1, rows.Count(r =>
      StudioFlowchartGraph.TryGetLabelKey(r, out string key)
      && string.Equals(key, "A", StringComparison.OrdinalIgnoreCase)));
    Assert.IsFalse(rows.Any(r => r.Mnemonic == "g" && r.SecondMnemonic == "4"));
  }

  [TestMethod]
  public void Build_ClassicTwoStepGto_MakesGotoEdgeToTargetStart()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 11, "A"),
      new(2, 22, "GTO"),
      new(3, 12, "B"),
      new(4, ClassicProgramCodes.Label, "LBL"),
      new(5, 12, "B"),
      new(6, 34, "R/S"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);

    Assert.AreEqual(2, graph.Routines.Count);
    StudioFlowchartGraph.Edge gto = graph.Edges
      .First(e => e.Kind == StudioFlowchartGraph.EdgeKind.Goto);
    Assert.AreEqual("B", gto.TargetKey);
    Assert.AreEqual(1, gto.TargetRoutineId);
    Assert.AreEqual(graph.Routines[1].StartNodeId, gto.ToId);

    StudioFlowchartGraph.Node branch = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Branch);
    Assert.AreEqual("GTO B", branch.Caption);
    Assert.AreEqual(gto.FromId, branch.Id);
  }

  [TestMethod]
  public void Build_FusedGsbMnemonic_MakesGosubEdge()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 10, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 20, "GSB B", null, null, StudioListingView.MergeKind.Single),
      new(2, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(3, 11, "LBL B", null, null, StudioListingView.MergeKind.Single),
      new(4, 34, "R/S", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    Assert.AreEqual(2, graph.Routines.Count);
    StudioFlowchartGraph.Edge gsb = graph.Edges
      .First(e => e.Kind == StudioFlowchartGraph.EdgeKind.Gosub);
    Assert.AreEqual("B", gsb.TargetKey);
    Assert.AreEqual(graph.Routines[1].StartNodeId, gsb.ToId);

    // GSB continues to RTN within LBL A.
    Assert.IsTrue(graph.Edges.Any(e =>
      e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough
      && e.FromId == gsb.FromId
      && e.ToId is int to
      && graph.Nodes[to].Kind == StudioFlowchartGraph.NodeKind.End));
  }

  [TestMethod]
  public void Build_EntryPrefix_BeforeFirstLbl()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 1, "1"),
      new(1, 2, "2"),
      new(2, ClassicProgramCodes.Label, "LBL"),
      new(3, 11, "A"),
      new(4, 3, "3"),
      new(5, 24, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);

    Assert.AreEqual(2, graph.Routines.Count);
    Assert.AreEqual("Entry", graph.Routines[0].Title);
    Assert.IsNull(graph.Routines[0].LabelKey);
    Assert.AreEqual("A", graph.Routines[1].Title);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Start, graph.Nodes[0].Kind);
    Assert.AreEqual(-1, graph.Nodes[0].FirstStep);

    StudioFlowchartGraph.Node entryProcess = graph.Nodes
      .First(n => n.RoutineId == 0 && n.Kind == StudioFlowchartGraph.NodeKind.Process);
    Assert.AreEqual("1 · 2", entryProcess.Caption);
  }

  [TestMethod]
  public void Build_ShiftGto_IsDecision_NotBranch()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 8, "g", 22, "GTO", StudioListingView.MergeKind.ShiftPair),
      new(4, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    Assert.AreEqual(1, graph.Routines.Count);
    Assert.IsFalse(graph.Edges.Any(e =>
      e.Kind is StudioFlowchartGraph.EdgeKind.Goto or StudioFlowchartGraph.EdgeKind.Gosub));
    Assert.IsTrue(graph.Nodes.Any(n =>
      n.Kind == StudioFlowchartGraph.NodeKind.Decision
      && n.Caption.Contains("GTO", StringComparison.OrdinalIgnoreCase)));
    Assert.IsTrue(graph.Edges.Any(e => e.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes));
    Assert.IsTrue(graph.Edges.Any(e => e.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo));
  }

  [TestMethod]
  public void Build_QuestionMnemonic_IsDecisionWithYesNo()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 10, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 50, "x=0?", null, null, StudioListingView.MergeKind.Single),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    StudioFlowchartGraph.Node decision = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision);
    Assert.AreEqual("x=0", decision.Caption);

    StudioFlowchartGraph.Edge yes = graph.Edges
      .First(e => e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes);
    StudioFlowchartGraph.Edge no = graph.Edges
      .First(e => e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo);
    // Skip-next: TRUE skips the next step (→ END); FALSE executes it (→ PROCESS).
    Assert.IsNotNull(no.ToId);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Process, graph.Nodes[no.ToId!.Value].Kind);
    Assert.IsNotNull(yes.ToId);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.End, graph.Nodes[yes.ToId!.Value].Kind);
    Assert.IsTrue(graph.Edges.Any(e =>
      e.FromId == no.ToId!.Value
      && e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough
      && e.ToId == yes.ToId));
    Assert.AreEqual(string.Empty, yes.Caption);
    Assert.AreEqual(string.Empty, no.Caption);
  }

  [TestMethod]
  public void Build_Chebyshev_LblA_XleY_SkipNextArms()
  {
    IReadOnlyList<StudioListingView.Row> rows = LoadChebyshevRows();
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    StudioFlowchartGraph.Routine routineA = graph.Routines.First(r => r.LabelKey == "A");
    StudioFlowchartGraph.Node decision = graph.Nodes.First(n =>
      n.RoutineId == routineA.Id
      && n.Kind == StudioFlowchartGraph.NodeKind.Decision
      && n.Caption.Contains('≤', StringComparison.Ordinal));

    StudioFlowchartGraph.Edge no = graph.Edges.First(e =>
      e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo);
    StudioFlowchartGraph.Edge yes = graph.Edges.First(e =>
      e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes);

    // Dual-block: TRUE → 1 then RTN; FALSE → 1 then STO 4.
    Assert.IsNotNull(yes.ToId);
    StudioFlowchartGraph.Node trueHead = graph.Nodes[yes.ToId!.Value];
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Process, trueHead.Kind);
    Assert.AreEqual("1", trueHead.Caption);

    StudioFlowchartGraph.Node trueRtn = graph.Nodes[
      graph.Edges.First(e =>
        e.FromId == trueHead.Id
        && e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough).ToId!.Value];
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.End, trueRtn.Kind);

    Assert.IsNotNull(no.ToId);
    StudioFlowchartGraph.Node falseHead = graph.Nodes[no.ToId!.Value];
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Process, falseHead.Kind);
    Assert.AreEqual("1", falseHead.Caption);
    Assert.IsTrue(
      graph.Edges.Any(e =>
        e.FromId == falseHead.Id
        && e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough
        && graph.Nodes[e.ToId!.Value].Caption.Contains("STO", StringComparison.OrdinalIgnoreCase)));

    StudioFlowchartGraph.Node sto4 = graph.Nodes[
      graph.Edges.First(e =>
        e.FromId == falseHead.Id
        && e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough).ToId!.Value];
    Assert.AreEqual("STO R4", sto4.Caption);
    StudioFlowchartGraph.Node xyDecision = graph.Nodes.First(n =>
      n.RoutineId == routineA.Id
      && n.Kind == StudioFlowchartGraph.NodeKind.Decision
      && n.Caption.Contains('=', StringComparison.Ordinal)
      && !n.Caption.Contains('≤', StringComparison.Ordinal));
    Assert.IsTrue(
      graph.Edges.Any(e =>
        e.FromId == sto4.Id
        && e.Kind == StudioFlowchartGraph.EdgeKind.FallThrough
        && e.ToId == xyDecision.Id),
      "STO 4 should fall through to x=y? decision.");
  }

  [TestMethod]
  public void Build_Chebyshev_DualBlockTrueArm_IsSideLayout()
  {
    IReadOnlyList<StudioListingView.Row> rows = LoadChebyshevRows();
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    StudioFlowchartGraph.Routine routineA = graph.Routines.First(r => r.LabelKey == "A");
    StudioFlowchartGraph.Node decision = graph.Nodes.First(n =>
      n.RoutineId == routineA.Id
      && n.Kind == StudioFlowchartGraph.NodeKind.Decision
      && n.Caption.Contains('≤', StringComparison.Ordinal));

    Assert.IsTrue(graph.SideChainsByDecision.ContainsKey(decision.Id));
    IReadOnlyList<int> chain = graph.SideChainsByDecision[decision.Id];
    Assert.AreEqual(2, chain.Count);
    Assert.IsTrue(graph.SideLayoutNodeIds.Contains(chain[0]));
    Assert.IsTrue(graph.SideLayoutNodeIds.Contains(chain[1]));
    Assert.AreEqual("1", graph.Nodes[chain[0]].Caption);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.End, graph.Nodes[chain[1]].Kind);
  }

  [TestMethod]
  public void DisplayStepNumber_Chebyshev_ComparisonsAdvanceByTwo()
  {
    IReadOnlyList<StudioListingView.Row> rows = LoadChebyshevRows();
    int xleIndex = -1;
    int xeqIndex = -1;
    for (int i = 0; i < rows.Count; i++)
    {
      string display = rows[i].DisplayMnemonic;
      if (xleIndex < 0 && display.Contains("x<=y", StringComparison.OrdinalIgnoreCase))
      {
        xleIndex = i;
      }

      if (display.Contains("x=y", StringComparison.OrdinalIgnoreCase)
          && !display.Contains("x<=y", StringComparison.OrdinalIgnoreCase))
      {
        xeqIndex = i;
      }
    }

    Assert.IsTrue(xleIndex >= 0 && xeqIndex >= 0);
    Assert.AreEqual(2, rows[xleIndex].StepSpan);
    Assert.AreEqual(2, rows[xeqIndex].StepSpan);
    Assert.IsTrue(xeqIndex > xleIndex, "x=y? should follow the first x<=y? in listing order.");
  }

  [TestMethod]
  public void Build_NumericLabel_RoutineTitleIsKeyOnly()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 1, "1", StudioListingView.MergeKind.LabelPair),
      new(2, 17, "CLX", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    Assert.AreEqual("1", graph.Routines[0].Title);
  }

  [TestMethod]
  public void Build_Chebyshev_MergesGto1TargetPair()
  {
    IReadOnlyList<StudioListingView.Row> rows = LoadChebyshevRows();
    StudioListingView.Row gto = rows.First(r =>
      r.DisplayMnemonic.StartsWith("GTO", StringComparison.OrdinalIgnoreCase));
    Assert.AreEqual(StudioListingView.MergeKind.BranchPair, gto.Kind);
    Assert.AreEqual("GTO 1", gto.DisplayMnemonic);
  }

  [TestMethod]
  public void Build_Chebyshev_Lbl1_LoopDecision_GtoFalseArm()
  {
    IReadOnlyList<StudioListingView.Row> rows = LoadChebyshevRows();
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    StudioFlowchartGraph.Routine routine1 = graph.Routines.First(r => r.LabelKey == "1");
    Assert.AreEqual("1", routine1.Title);
    StudioFlowchartGraph.Node decision = graph.Nodes.Last(n =>
      n.RoutineId == routine1.Id && n.Kind == StudioFlowchartGraph.NodeKind.Decision);

    StudioFlowchartGraph.Edge no = graph.Edges.First(e =>
      e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo);
    Assert.IsNotNull(no.ToId);
    StudioFlowchartGraph.Node falseArm = graph.Nodes[no.ToId!.Value];
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Branch, falseArm.Kind);
    Assert.AreEqual("GTO 1", falseArm.Caption);

    StudioFlowchartGraph.Edge yes = graph.Edges.First(e =>
      e.FromId == decision.Id && e.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes);
    Assert.IsNotNull(yes.ToId);
    StudioFlowchartGraph.Node trueArm = graph.Nodes[yes.ToId!.Value];
    Assert.AreEqual(
      StudioFlowchartGraph.NodeKind.Process,
      trueArm.Kind,
      $"TRUE arm caption was '{trueArm.Caption}'");
    Assert.IsTrue(
      trueArm.Caption.Contains("R1", StringComparison.OrdinalIgnoreCase),
      $"TRUE arm caption was '{trueArm.Caption}'");
  }

  private static IReadOnlyList<StudioListingView.Row> LoadChebyshevRows()
  {
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      "teo-65-chebyshev.t65");
    T6xDocument document = T6xCardFormat.ReadFile(path);
    List<ClassicProgramLine> lines = [];
    for (int i = 0; i < document.Code.Count; i++)
    {
      lines.Add(new ClassicProgramLine(i, 0, document.Code[i]));
    }

    return StudioListingView.Build(lines);
  }

  [TestMethod]
  public void Build_GShiftXyTest_IsDecisionDiamond()
  {
    // HP-65 g + DSP → CapSkirt x≠y (skip-next), not a PROCESS middot caption.
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 8, "g", 5, "DSP", StudioListingView.MergeKind.ShiftPair),
      new(4, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    StudioFlowchartGraph.Node decision = graph.Nodes
      .First(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision);
    Assert.AreEqual("x≠y", decision.Caption);
    Assert.IsTrue(graph.Edges.Any(e => e.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes));
    Assert.IsTrue(graph.Edges.Any(e => e.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo));
  }

  [TestMethod]
  public void Build_StackExchange_IsNotDecision()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 17, "X<>Y", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    Assert.IsFalse(graph.Nodes.Any(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision));
    Assert.IsTrue(graph.Nodes.Any(n =>
      n.Kind == StudioFlowchartGraph.NodeKind.Process
      && (n.Caption.Contains('↔', StringComparison.Ordinal)
          || n.Caption.Contains("X<>Y", StringComparison.OrdinalIgnoreCase)
          || n.Caption.Contains("x↔y", StringComparison.Ordinal))));
  }

  [TestMethod]
  public void FindNodeIdForStep_MatchesMergedLabelPairAndBody()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 11, "A"),
      new(2, 1, "1"),
      new(3, 24, "RTN"),
      new(4, ClassicProgramCodes.Label, "LBL"),
      new(5, 12, "B"),
      new(6, 34, "R/S"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);

    int startA = StudioFlowchartGraph.FindNodeIdForStep(graph, 0); // LBL
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Start, graph.Nodes[startA].Kind);
    Assert.AreEqual(startA, StudioFlowchartGraph.FindNodeIdForStep(graph, 1)); // A (pair)
    int proc = StudioFlowchartGraph.FindNodeIdForStep(graph, 2);
    Assert.AreEqual(StudioFlowchartGraph.NodeKind.Process, graph.Nodes[proc].Kind);
    Assert.AreEqual(1, StudioFlowchartGraph.FindRoutineIdForStep(graph, 4));
    Assert.AreEqual(-1, StudioFlowchartGraph.FindNodeIdForStep(graph, 99));
  }

  [TestMethod]
  public void TryParseBranch_UnresolvedTarget_StillEmitsEdge()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 22, "GTO", null, null, StudioListingView.MergeKind.Single),
      new(3, 12, "Z", null, null, StudioListingView.MergeKind.Single),
      new(4, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows);
    StudioFlowchartGraph.Edge gto = graph.Edges
      .First(e => e.Kind == StudioFlowchartGraph.EdgeKind.Goto);
    Assert.AreEqual("Z", gto.TargetKey);
    Assert.IsNull(gto.ToId);
  }
}
