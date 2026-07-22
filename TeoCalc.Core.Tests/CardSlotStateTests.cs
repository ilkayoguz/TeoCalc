using TeoCalc.Core;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CardSlotStateTests
{
  [TestMethod]
  public void Session_StartsWithNoCardInserted()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int index = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(index);

    Assert.IsFalse(session.CardInserted);
    Assert.IsNull(session.LoadedCardPath);
  }

  [TestMethod]
  public void SaveCardProgram_MarksInserted_AndEjectClears()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int index = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(index);
    session.PowerOnResume();

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card-state-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? error), error);
      Assert.IsTrue(session.CardInserted);
      Assert.AreEqual(path, session.LoadedCardPath);

      session.EjectCard();
      Assert.IsFalse(session.CardInserted);
      Assert.IsNull(session.LoadedCardPath);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void LoadModel_ResetsCardInserted()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int hp65 = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(hp65);
    session.PowerOnResume();

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card-reset-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out _));
      Assert.IsTrue(session.CardInserted);

      int hp67 = Array.FindIndex(session.Models, id => id == "HP-67");
      session.LoadModel(hp67);
      Assert.IsFalse(session.CardInserted);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void LoadSampleHp65_SetsStripLabels()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);

    Assert.IsNotNull(session.CardStripLabels);
    Assert.AreEqual("+123", session.CardStripLabels![0]);
    Assert.AreEqual(string.Empty, session.CardStripLabels[1]);
    Assert.IsTrue(session.CardStripLabelsEnabled![0]);
  }

  [TestMethod]
  public void PowerOn_NoCard_StudioShowsFullAThroughECatalog()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsFalse(session.CardInserted);

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));

    // Classic ROM boot currently leaves LBL A–D in RAM; faceplate chrome is still A–E.
    // Studio synthesizes the missing strip letter(s) so the catalog matches the strip.
    HashSet<string> rawLabels = new(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < lines.Count - 1; i++)
    {
      if (string.Equals(lines[i].Mnemonic, "LBL", StringComparison.OrdinalIgnoreCase)
          && lines[i + 1].Mnemonic is "A" or "B" or "C" or "D" or "E")
      {
        rawLabels.Add(lines[i + 1].Mnemonic);
      }
    }

    CollectionAssert.IsSubsetOf(new[] { "A", "B", "C", "D" }, rawLabels.ToArray());

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    HashSet<string> studioLabels = new(StringComparer.OrdinalIgnoreCase);
    foreach (StudioListingView.Row row in rows)
    {
      if (StudioFlowchartGraph.TryGetLabelKey(row, out string key)
          && key is "A" or "B" or "C" or "D" or "E")
      {
        studioLabels.Add(key);
      }
    }

    CollectionAssert.AreEquivalent(
      new[] { "A", "B", "C", "D", "E" },
      studioLabels.ToArray(),
      "NoCard Studio catalog must match faceplate A–E (synthesize E if RAM omits it).");
    Assert.IsTrue(
      rows.Any(r => r.DisplayMnemonic.Equals("X<>Y", StringComparison.OrdinalIgnoreCase)),
      "LBL E body should be X<>Y.");
  }

  [TestMethod]
  public void EjectCard_RestoresNoCardStudioCatalog()
  {
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      "teo-65-chebyshev.t65");
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.CardInserted);

    session.EjectCard();

    Assert.IsFalse(session.CardInserted);
    Assert.IsNull(session.LoadedCardPath);
    Assert.IsNull(session.CardStripLabels);
    Assert.IsTrue(session.PowerOn);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    HashSet<string> studioLabels = new(StringComparer.OrdinalIgnoreCase);
    foreach (StudioListingView.Row row in rows)
    {
      if (StudioFlowchartGraph.TryGetLabelKey(row, out string key)
          && key is "A" or "B" or "C" or "D" or "E")
      {
        studioLabels.Add(key);
      }
    }

    CollectionAssert.AreEquivalent(
      new[] { "A", "B", "C", "D", "E" },
      studioLabels.ToArray(),
      "Eject must restore NoCard A–E catalog, not leave the ejected card program in Studio.");
    Assert.IsFalse(
      rows.Any(r => r.DisplayMnemonic.Contains("x<=y", StringComparison.OrdinalIgnoreCase)
        || r.DisplayMnemonic.Contains("x≤y", StringComparison.OrdinalIgnoreCase)),
      "Chebyshev decisions must not remain after eject.");
  }

  [TestMethod]
  public void PowerCycle_WithChebyshevCard_RestoresStudioListing()
  {
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      "teo-65-chebyshev.t65");
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> linesBefore));
    IReadOnlyList<StudioListingView.Row> rowsBefore = StudioListingView.Build(
      linesBefore,
      session.LoadedTeoCard?.Program.Steps);
    StudioFlowchartGraph.Graph graphBefore = StudioFlowchartGraph.Build(
      rowsBefore,
      "HP-65",
      session.CardStripLabels);
    int decisionNodesBefore = graphBefore.Nodes.Count(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision);

    session.PowerOff();
    session.PowerOnResume();

    Assert.IsTrue(session.CardInserted);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> linesAfter));
    IReadOnlyList<StudioListingView.Row> rowsAfter = StudioListingView.Build(
      linesAfter,
      session.LoadedTeoCard?.Program.Steps);
    StudioFlowchartGraph.Graph graphAfter = StudioFlowchartGraph.Build(
      rowsAfter,
      "HP-65",
      session.CardStripLabels);

    Assert.AreEqual(rowsBefore.Count, rowsAfter.Count, "Listing row count should match after power cycle.");
    Assert.AreEqual(
      decisionNodesBefore,
      graphAfter.Nodes.Count(n => n.Kind == StudioFlowchartGraph.NodeKind.Decision),
      "FC decision nodes should match after power cycle.");
    Assert.IsTrue(
      graphAfter.Nodes.Any(n =>
        n.Kind == StudioFlowchartGraph.NodeKind.Process
        && n.Caption.Contains("+1", StringComparison.Ordinal)),
      "Chebyshev register-arith chunk should survive power cycle.");
  }

  [TestMethod]
  public void LoadSampleHp65_HidesInjectedStripStubsFromStudioAndFc()
  {
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.LoadedTeoCard?.Program.Steps);

    // Injected stubs + firmware leftover built-ins (e.g. LBL E · X<>Y) must not appear.
    Assert.IsFalse(
      rows.Any(r =>
        StudioFlowchartGraph.TryGetLabelKey(r, out string key)
        && key is "B" or "C" or "D" or "E"),
      string.Join(" | ", rows.Select(r => r.DisplayMnemonic)));

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      "HP-65",
      session.CardStripLabels);
    Assert.IsFalse(graph.Routines.Any(r => r.LabelKey is "B" or "C" or "D" or "E"));

    // Missing strip columns stay empty — no DefaultNoCardCaptions (√x, …).
    StudioListingView.Row lblB = new(
      Index: 0,
      Code: ClassicProgramCodes.Label,
      Mnemonic: "LBL",
      SecondCode: 12,
      SecondMnemonic: "B",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paintB = StudioListingView.ResolvePaint(
      lblB,
      "HP-65",
      session.CardStripLabels);
    Assert.AreEqual(string.Empty, paintB.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.None, paintB.LegendKind);

    session.EjectCard();
    Assert.IsNull(session.CardStripLabels);
    StudioListingView.Paint noCard = StudioListingView.ResolvePaint(lblB, "HP-65", null);
    Assert.AreEqual("\u221ax", noCard.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.NoCardStrip, noCard.LegendKind);
  }
}
