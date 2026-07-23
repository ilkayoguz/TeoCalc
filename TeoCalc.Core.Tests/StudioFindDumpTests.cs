using TeoCalc.Core;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioFindDumpTests
{
  private static CalcExplorerSession CreateLoadedHp65()
  {
    CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int idx = Array.FindIndex(session.Models, id => id.Contains("65", StringComparison.Ordinal));
    Assert.IsTrue(idx >= 0);
    session.LoadModel(idx);
    session.PowerOnResume();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    return session;
  }

  [TestMethod]
  public void RowMatchesFind_HitsMnemonicAndMachine()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.LoadedTeoCard?.Program.Steps);
    Assert.IsTrue(rows.Count > 0);
    StudioListingView.Row rtn = rows.First(
      r => r.DisplayMnemonic.Contains("RTN", StringComparison.OrdinalIgnoreCase));
    Assert.IsTrue(
      CalcStudioPanelComponent.RowMatchesFind(
        rtn,
        "RTN",
        session.EngineModelId,
        session.CardStripLabels));
    Assert.IsTrue(
      CalcStudioPanelComponent.RowMatchesFind(
        rtn,
        "42",
        session.EngineModelId,
        session.CardStripLabels));
  }

  [TestMethod]
  public void CaptureDebugDump_IncludesRomAndProgramSections()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    string dump = session.CaptureDebugDump();
    StringAssert.Contains(dump, "## ROM around PC");
    StringAssert.Contains(dump, "## User program");
    StringAssert.Contains(dump, "LBL");
  }
}
