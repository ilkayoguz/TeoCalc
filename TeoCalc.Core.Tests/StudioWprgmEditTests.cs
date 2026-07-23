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
    int idx = Array.FindIndex(session.Models, id => id.Contains("65", StringComparison.Ordinal));
    Assert.IsTrue(idx >= 0, "HP-65 model missing");
    session.LoadModel(idx);
    session.PowerOnResume();
    return session;
  }

  [TestMethod]
  public void PowerOn_Baseline_IsNotDirty()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(session.IsProgramDirty);
  }

  [TestMethod]
  public void Wprgm_DigitEntry_MarksDirty_AndBlocksRunUntilConfirm()
  {
    using CalcExplorerSession session = CreateHp65Session();
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.ProgramMode);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode));
    session.PressKey(keyCode);

    Assert.IsTrue(session.IsProgramDirty, "W/PRGM entry should dirty program RAM vs power-on baseline.");
    Assert.IsTrue(session.SelectedProgramStep >= 0);

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
  public void Save_ClearsDirty()
  {
    using CalcExplorerSession session = CreateHp65Session();
    session.ToggleProgramModeTo(true);
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '1', out byte keyCode));
    session.PressKey(keyCode);
    Assert.IsTrue(session.IsProgramDirty);

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-wprgm-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? error), error);
      Assert.IsFalse(session.IsProgramDirty);
      Assert.IsTrue(session.CardInserted);
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
