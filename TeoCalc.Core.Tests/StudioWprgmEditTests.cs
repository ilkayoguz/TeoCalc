using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioWprgmEditTests
{
  private static CalcExplorerSession CreateHp65Session()
  {
    CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int idx = Array.FindIndex(session.Models, id => CalcModelIds.SameEngine(id, "T-65"));
    Assert.IsTrue(idx >= 0, "T-65 / HP-65 model missing");
    session.LoadModel(idx);
    session.PowerOnResume();
    Assert.IsTrue(session.SupportsCardProgram, "T-65 must support card program I/O.");
    return session;
  }

  [TestMethod]
  public void PowerOn_Baseline_IsNotDirty()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(session.IsProgramDirty);
  }

  [TestMethod]
  public void Wprgm_NoEdits_LeavesRunWithoutConfirm()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(session.IsProgramDirty);
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.ProgramMode);
    Assert.IsFalse(session.IsProgramDirty, "Entering W/PRGM alone must not mark dirty.");

    session.ToggleProgramModeTo(false);
    Assert.IsFalse(session.PendingLeaveProgramConfirm);
    Assert.IsFalse(session.ProgramMode);
  }

  [TestMethod]
  public void Wprgm_DigitEntry_MarksDirty_AndBlocksRunUntilConfirm()
  {
    using CalcExplorerSession session = CreateHp65Session();
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.ProgramMode);

    // Editor / paste path writes RAM directly (faceplate keys also mutate RAM in W/PRGM).
    Assert.IsTrue(session.TryApplyProgramCodes([7], out string? applyError), applyError);
    Assert.IsTrue(session.IsProgramDirty, "Program RAM edit should dirty vs power-on baseline.");

    session.ToggleProgramModeTo(false);
    Assert.IsTrue(session.PendingLeaveProgramConfirm);
    Assert.IsTrue(session.ProgramMode, "Must stay in W/PRGM until confirm.");

    session.CancelLeaveProgramConfirm();
    Assert.IsFalse(session.PendingLeaveProgramConfirm);
    Assert.IsTrue(session.ProgramMode);

    session.ToggleProgramModeTo(false);
    Assert.IsTrue(session.PendingLeaveProgramConfirm);
    session.ConfirmDiscardProgramEditsAndRun();
    Assert.IsFalse(session.ProgramMode);
    Assert.IsTrue(session.IsProgramDirty, "Don't Save keeps RAM dirty vs last card snapshot.");
  }

  [TestMethod]
  public void FollowPointer_DoesNotChangeStudioFocus()
  {
    StudioPaneSync.NoteFlowchartFocus();
    Assert.AreEqual(StudioPaneSync.StudioFocus.Flowchart, StudioPaneSync.Focus);
    StudioPaneSync.FollowPointer(12);
    Assert.AreEqual(StudioPaneSync.StudioFocus.Flowchart, StudioPaneSync.Focus);
    Assert.IsTrue(StudioPaneSync.TryConsumeCodeFollow(out int step));
    Assert.AreEqual(12, step);
    StudioPaneSync.EndFrame();
  }

  [TestMethod]
  public void TrySetProgramStartStep_SeeksPointer()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.TrySetProgramStartStep(3));
    Assert.AreEqual(3, session.SelectedProgramStep);
  }

  [TestMethod]
  public void TrySetProgramStartStep_UpdatesFaceplateLedToMuseumCodes()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    session.ToggleProgramModeTo(true);
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.LoadedTeoCard?.Program.Steps);
    Assert.IsTrue(rows.Count > 3, "sample card should have several Studio rows");

    // Digit-3 row museum "03" — distinct from power-on / RTN "24".
    StudioListingView.Row target = rows[3];
    string museum = StudioMuseumKeycodes.FormatMachineDisplay(target, session.EngineModelId);
    Assert.AreEqual("03", museum);

    Assert.IsTrue(session.TrySetProgramStartStep(target.Index));
    Assert.AreEqual(target.Index, session.SelectedProgramStep);

    string ledDigits = new string(session.DisplayText.Where(char.IsDigit).ToArray());
    Assert.AreEqual(
      "03",
      ledDigits,
      $"Faceplate LED should show museum for sought step, got '{session.DisplayText}'");
  }

  [TestMethod]
  public void TrySetProgramStartStep_MergedLabel_ShowsMuseumPairOnLed()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    session.ToggleProgramModeTo(true);
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.LoadedTeoCard?.Program.Steps);
    StudioListingView.Row lbl = rows[0];
    string museum = StudioMuseumKeycodes.FormatMachineDisplay(lbl, session.EngineModelId);
    Assert.AreEqual("23 11", museum);

    Assert.IsTrue(session.TrySetProgramStartStep(lbl.Index));
    string ledDigits = new string(session.DisplayText.Where(char.IsDigit).ToArray());
    Assert.AreEqual(
      "2311",
      ledDigits,
      $"Merged LBL should paint both museum pairs on LED, got '{session.DisplayText}'");
  }

  [TestMethod]
  public void ApplyMuseumText_RightAlignsPairsLikeSst()
  {
    ClassicRegisterFile regs = new();
    ClassicWprgmLedSync.ApplyMuseumText(regs, "35 01");
    Assert.AreEqual(3, regs.A[7]);
    Assert.AreEqual(5, regs.A[6]);
    Assert.AreEqual(0, regs.A[5]);
    Assert.AreEqual(0, regs.A[4]);
    Assert.AreEqual(1, regs.A[3]);
    Assert.AreEqual(0, regs.B[7]);
    Assert.AreEqual(0, regs.B[6]);
    Assert.AreEqual(9, regs.B[5]);
    Assert.AreEqual(0, regs.B[4]);
    Assert.AreEqual(0, regs.B[3]);
    Assert.AreEqual(9, regs.B[2]);

    ClassicWprgmLedSync.ApplyMuseumText(regs, "24");
    Assert.AreEqual(2, regs.A[4]);
    Assert.AreEqual(4, regs.A[3]);
    Assert.AreEqual(0, regs.B[4]);
    Assert.AreEqual(0, regs.B[3]);
    Assert.AreEqual(9, regs.B[5]);
  }

  [TestMethod]
  public void Save_ClearsDirty()
  {
    using CalcExplorerSession session = CreateHp65Session();
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.TryApplyProgramCodes([1], out string? applyError), applyError);
    Assert.IsTrue(session.IsProgramDirty);

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-wprgm-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? error), error);
      Assert.IsFalse(session.IsProgramDirty);
      Assert.IsFalse(session.CardInserted, "Save without prior Load is export-only.");
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }
}
