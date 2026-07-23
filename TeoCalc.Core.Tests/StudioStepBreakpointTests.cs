using TeoCalc.Core;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioStepBreakpointTests
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
    session.ToggleProgramModeTo(false);
    return session;
  }

  private static IReadOnlyList<StudioListingView.Row> BuildRows(CalcExplorerSession session)
  {
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    return StudioListingView.Build(lines, session.LoadedTeoCard?.Program.Steps);
  }

  [TestMethod]
  public void StepStudioLine_AdvancesOneCodeRow()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    IReadOnlyList<StudioListingView.Row> rows = BuildRows(session);
    Assert.IsTrue(rows.Count >= 3);

    StudioPaneSync.NoteCodeFocus();
    Assert.IsTrue(session.TrySetProgramStartStep(rows[0].Index));
    session.StepStudioLine();
    Assert.AreEqual(rows[1].Index, session.SelectedProgramStep);
  }

  [TestMethod]
  public void StepStudioKey_AdvancesOneRamSlot()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    IReadOnlyList<StudioListingView.Row> rows = BuildRows(session);
    Assert.IsTrue(session.TrySetProgramStartStep(rows[0].Index));
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> beforeLines));
    int ptrBefore = ClassicProgramListing.FindPointerIndex(beforeLines);
    Assert.IsTrue(ptrBefore >= 0);

    session.StepStudioKey();

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> afterLines));
    int ptrAfter = ClassicProgramListing.FindPointerIndex(afterLines);
    Assert.AreEqual(ptrBefore + 1, ptrAfter, "F11 should AdvancePointer by one RAM slot.");
  }

  [TestMethod]
  public void StepStudioLine_OnRtn_WrapsToRoutineLbl()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    IReadOnlyList<StudioListingView.Row> rows = BuildRows(session);
    int rtnIdx = -1;
    for (int i = 0; i < rows.Count; i++)
    {
      if (string.Equals(rows[i].DisplayMnemonic.Trim(), "RTN", StringComparison.OrdinalIgnoreCase)
          || string.Equals(rows[i].Mnemonic.Trim(), "RTN", StringComparison.OrdinalIgnoreCase))
      {
        rtnIdx = i;
        break;
      }
    }

    Assert.IsTrue(rtnIdx >= 0, "Sample card should contain RTN.");
    StudioPaneSync.NoteCodeFocus();
    int rtnStep = rows[rtnIdx].Index;
    Assert.IsTrue(session.TrySetProgramStartStep(rtnStep));
    session.StepStudioLine();

    int landed = session.SelectedProgramStep;
    bool onLbl = false;
    foreach (StudioListingView.Row row in rows)
    {
      if (!row.ContainsIndex(landed))
      {
        continue;
      }

      onLbl = row.Mnemonic.Contains("LBL", StringComparison.OrdinalIgnoreCase)
        || row.DisplayMnemonic.Contains("LBL", StringComparison.OrdinalIgnoreCase);
      break;
    }

    Assert.IsTrue(
      onLbl,
      $"F10 on RTN should wrap to a LBL row; rtnRow={rtnIdx} rtnStep={rtnStep} landed={landed}");
  }

  [TestMethod]
  public void ToggleStudioBreakpoint_AddAndRemove()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    Assert.IsTrue(session.ToggleStudioBreakpoint(3));
    Assert.IsTrue(session.HasStudioBreakpoint(3));
    Assert.IsFalse(session.ToggleStudioBreakpoint(3));
    Assert.IsFalse(session.HasStudioBreakpoint(3));
  }

  [TestMethod]
  public void Breakpoint_PausesWhenPtrLandsOnStep()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    IReadOnlyList<StudioListingView.Row> rows = BuildRows(session);
    int bp = rows[Math.Min(2, rows.Count - 1)].Index;
    Assert.IsTrue(session.ToggleStudioBreakpoint(bp));

    Assert.IsTrue(session.TrySetProgramStartStep(rows[0].Index));
    session.ContinueExecution();
    Assert.IsFalse(session.ExecutionPaused);

    // Move PTR onto the breakpoint without Continue’s ignore latch.
    Assert.IsTrue(session.TrySetProgramStartStep(bp));
    for (int i = 0; i < 30; i++)
    {
      session.Tick(0.05f);
      if (session.ExecutionPaused)
      {
        break;
      }
    }

    Assert.IsTrue(session.ExecutionPaused, "RUN batch should pause on Studio breakpoint.");
    Assert.AreEqual(bp, session.SelectedProgramStep);
  }

  [TestMethod]
  public void ExecutionSpeed_NudgesAcrossSteps()
  {
    using CalcExplorerSession session = CreateLoadedHp65();
    Assert.AreEqual(1f, session.ExecutionSpeed, 1e-6);
    session.NudgeExecutionSpeed(1);
    Assert.AreEqual(2f, session.ExecutionSpeed, 1e-6);
    session.NudgeExecutionSpeed(-2);
    Assert.AreEqual(0.5f, session.ExecutionSpeed, 1e-6);
  }
}
