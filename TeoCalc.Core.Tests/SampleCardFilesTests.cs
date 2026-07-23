using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class SampleCardFilesTests
{
  [TestMethod]
  public void BundledT65_LoadsIntoSession()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(File.Exists(path), path);

    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.CardInserted);
  }

  [TestMethod]
  public void BundledT67_LoadsIntoSession()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp67T67FileName);
    Assert.IsTrue(File.Exists(path), path);

    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-67"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.CardInserted);
  }

  [TestMethod]
  public void SampleT65_ParseToClassicSnapshot()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    T6xDocument document = T6xCardFormat.ReadFile(path);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(
        ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json")),
        mnemonic));

    Assert.AreEqual(22, snapshot.ProgramCodes[6]);
    Assert.AreEqual(0, snapshot.Registers[0], 1e-6);
    Assert.AreEqual(3.14, snapshot.Registers[1], 1e-6);
    Assert.AreEqual(42, snapshot.Registers[9], 1e-6);
  }

  [TestMethod]
  public void SampleT65_MeanSd_ResolvesAllMnemonics()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), "teo-65-meansd.t65");
    Assert.IsTrue(File.Exists(path), path);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));
    T6xDocument document = T6xCardFormat.ReadFile(path);
    Assert.AreEqual(CardCodeEncoding.Mnemonic, document.CodeEncoding);
    Assert.AreEqual(81, document.Code.Count);
    Assert.AreEqual(5, document.Labels.Count);

    foreach (string step in document.Code)
    {
      Assert.IsNotNull(
        ClassicCardProgramIo.ResolveMnemonic(vocabulary, step),
        $"Unresolved mnemonic: '{step}'");
    }

    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(vocabulary, mnemonic));

    // Classic snapshot prepends internal START at [0].
    Assert.AreEqual(36, snapshot.ProgramCodes[1]); // 0
    Assert.AreEqual(49, snapshot.ProgramCodes[2]); // STO 1
    Assert.AreEqual(43, snapshot.ProgramCodes[6]); // LBL
    Assert.AreEqual(30, snapshot.ProgramCodes[7]); // A
    int xSwap = document.Code.FindIndex(s => string.Equals(s, "X<>Y", StringComparison.OrdinalIgnoreCase));
    int lstx = document.Code.FindIndex(s => string.Equals(s, "LSTX", StringComparison.OrdinalIgnoreCase));
    Assert.IsTrue(xSwap >= 0);
    Assert.IsTrue(lstx >= 0);
    Assert.AreEqual(17, snapshot.ProgramCodes[xSwap + 1]);
    Assert.AreEqual(60, snapshot.ProgramCodes[lstx + 1]);
  }

  [TestMethod]
  public void SampleT65_MeanSd_LblB_IsRcl2Rcl1Divide()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), "teo-65-meansd.t65");
    T6xDocument document = T6xCardFormat.ReadFile(path);

    int bIndex = -1;
    for (int i = 0; i < document.Code.Count - 1; i++)
    {
      if (string.Equals(document.Code[i], "LBL", StringComparison.OrdinalIgnoreCase)
          && string.Equals(document.Code[i + 1], "B", StringComparison.OrdinalIgnoreCase))
      {
        bIndex = i;
        break;
      }
    }

    Assert.IsTrue(bIndex >= 0, "LBL B missing");
    Assert.AreEqual("RCL 2", document.Code[bIndex + 2]);
    Assert.AreEqual("RCL 1", document.Code[bIndex + 3]);
    Assert.AreEqual("/", document.Code[bIndex + 4]);
    Assert.AreEqual("RTN", document.Code[bIndex + 5]);

    List<ClassicProgramLine> lines = [];
    for (int i = 0; i < document.Code.Count; i++)
    {
      lines.Add(new ClassicProgramLine(i, 0, document.Code[i]));
    }

    string[] strip = ["", "", "", "", ""];
    foreach (T6xLabelEntry label in document.Labels)
    {
      if (ClassicCardStripLabels.TryGetStripColumn(label.Key, out int col))
      {
        strip[col] = label.Caption ?? "";
      }
    }

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65", strip);

    StudioFlowchartGraph.Routine routineB = graph.Routines.First(r => r.LabelKey == "B");
    StudioFlowchartGraph.Node processB = graph.Nodes
      .First(n => n.RoutineId == routineB.Id && n.Kind == StudioFlowchartGraph.NodeKind.Process);
    // Strip caption x̄ when sole body; else compact R2/R1.
    Assert.IsTrue(
      processB.Caption is "x̄" or "R2/R1",
      $"Unexpected LBL B PROCESS caption: {processB.Caption}");
  }

  [TestMethod]
  public void SampleT65_MeanSd_HasNoComparisonDecisions()
  {
    // Authentic HP-65 Mean/SD (Museum / Standard Pac STD-02A) uses X<>Y, not x≠y / x=0 tests.
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), "teo-65-meansd.t65");
    T6xDocument document = T6xCardFormat.ReadFile(path);
    Assert.IsFalse(
      document.Code.Any(s =>
        s.Contains('?', StringComparison.Ordinal)
        || s.Contains("!=", StringComparison.Ordinal)
        || s.Contains("x=y", StringComparison.OrdinalIgnoreCase)
        || s.Contains("x<=y", StringComparison.OrdinalIgnoreCase)
        || s.Contains("x>y", StringComparison.OrdinalIgnoreCase)),
      "meansd.t65 unexpectedly contains comparison mnemonics");

    List<ClassicProgramLine> lines = [];
    for (int i = 0; i < document.Code.Count; i++)
    {
      lines.Add(new ClassicProgramLine(i, 0, document.Code[i]));
    }

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    Assert.AreEqual(
      0,
      graph.Nodes.Count(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision),
      "Mean/SD should not invent DECISION diamonds; X<>Y is PROCESS");
  }

  [TestMethod]
  public void SampleT65_Chebyshev_ResolvesMnemonicsAndHasDecisions()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), "teo-65-chebyshev.t65");
    Assert.IsTrue(File.Exists(path), path);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));
    T6xDocument document = T6xCardFormat.ReadFile(path);
    Assert.AreEqual(CardCodeEncoding.Mnemonic, document.CodeEncoding);
    Assert.IsFalse(
      document.Code.Any(s => string.Equals(s, "NOP", StringComparison.OrdinalIgnoreCase)),
      "Chebyshev sample should omit NOP fillers");

    foreach (string step in document.Code)
    {
      Assert.IsNotNull(
        ClassicCardProgramIo.ResolveMnemonic(vocabulary, step),
        $"Unresolved mnemonic: '{step}'");
    }

    Assert.IsTrue(document.Code.Contains("x<=y?"), "expected x<=y? decisions");
    Assert.IsTrue(document.Code.Contains("x=y?"), "expected x=y? decision");

    List<ClassicProgramLine> lines = [];
    for (int i = 0; i < document.Code.Count; i++)
    {
      lines.Add(new ClassicProgramLine(i, 0, document.Code[i]));
    }

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(rows, "HP-65");
    int decisions = graph.Nodes.Count(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision);
    Assert.IsTrue(decisions >= 1, $"Expected ≥1 DECISION node, got {decisions}");
    Assert.IsTrue(
      graph.Nodes.Any(n =>
        n.Kind == StudioFlowchartGraph.NodeKind.Decision
        && (n.Caption.Contains("≤", StringComparison.Ordinal)
            || n.Caption.Contains("<=", StringComparison.Ordinal)
            || n.Caption.Contains('=', StringComparison.Ordinal))),
      "DECISION captions should reflect x≤y / x=y");
    Assert.IsTrue(
      graph.Nodes
        .Where(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision)
        .All(n => !n.Caption.EndsWith('?')),
      "DECISION diamonds must not append trailing '?'");
    Assert.IsTrue(
      graph.Nodes.Any(n =>
        n.Kind == StudioFlowchartGraph.NodeKind.Process
        && n.Caption.Contains("+1", StringComparison.Ordinal)),
      "Chebyshev RCL 2 / 1 / + chunk should rewrite to +1");
  }

  [TestMethod]
  public void SampleT67_ParseToActSnapshot()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp67T67FileName);
    T6xDocument document = T6xCardFormat.ReadFile(path);
    Teo67CardSnapshot snapshot = T6xCardFormat.ToTeo67Snapshot(document, Teo67CardProgramIo.ResolveMnemonic);

    Assert.AreEqual(55, snapshot.ProgramCodes[2]);
    Assert.AreEqual(12.5, snapshot.Registers[0], 1e-6);
    Assert.AreEqual(-3, snapshot.Registers[25], 1e-6);
  }
}
