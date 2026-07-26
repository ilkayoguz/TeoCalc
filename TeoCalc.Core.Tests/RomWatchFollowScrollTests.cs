using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class RomWatchFollowScrollTests
{
  [TestMethod]
  public void Adjust_KeepsScroll_WhenPcInsideMarginBand()
  {
    // Window 64 @ scroll 10 → rows 10..73; margin 4 → comfort 14..69
    Assert.AreEqual(10, RomWatchFollowScroll.Adjust(10, 15, wordCount: 1000));
    Assert.AreEqual(10, RomWatchFollowScroll.Adjust(10, 69, wordCount: 1000));
  }

  [TestMethod]
  public void Adjust_Recenters_WhenPcLeavesBand()
  {
    // PC below comfort band → soft center, clamped to 0
    int down = RomWatchFollowScroll.Adjust(10, 12, wordCount: 1000);
    Assert.AreEqual(0, down);

    // PC above band (last comfort = 69)
    int up = RomWatchFollowScroll.Adjust(10, 70, wordCount: 1000);
    Assert.AreEqual(70 - RomWatchFollowScroll.DefaultWindowRows / 3, up);
  }

  [TestMethod]
  public void Adjust_ClampsToRomBounds()
  {
    Assert.AreEqual(0, RomWatchFollowScroll.Adjust(0, 0, wordCount: 100));
    int nearEnd = RomWatchFollowScroll.Adjust(0, 99, wordCount: 100);
    Assert.AreEqual(99 - RomWatchFollowScroll.DefaultWindowRows / 3, nearEnd);
    Assert.AreEqual(0, RomWatchFollowScroll.Adjust(5, 0, wordCount: 0));
  }

  [TestMethod]
  public void CenterOn_PlacesPcNearUpperThird()
  {
    Assert.AreEqual(
      100 - RomWatchFollowScroll.DefaultWindowRows / 3,
      RomWatchFollowScroll.CenterOn(100, wordCount: 1000));
  }

  [TestMethod]
  public void SyncRomWatch_DoesNotSnapEveryStep()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.FollowRomWatch = true;
    session.MicrocodeScroll = 50;
    session.SelectedAddress = 60;

    // Simulate in-window PC advance via public step path after power-on.
    session.PowerOnResume();
    int before = session.MicrocodeScroll;
    session.PreferMicrocodeHotkeys = true;
    session.StepMicrocodeInto();
    int pc = session.LastBatch.ProgramCounter;
    int after = session.MicrocodeScroll;

    if (pc >= before + RomWatchFollowScroll.DefaultMargin
        && pc <= before + RomWatchFollowScroll.DefaultWindowRows - 1 - RomWatchFollowScroll.DefaultMargin)
    {
      Assert.AreEqual(before, after);
    }
    else
    {
      Assert.AreEqual(RomWatchFollowScroll.Adjust(before, pc, session.Map?.WordCount ?? 0), after);
    }

    Assert.AreEqual(pc, session.SelectedAddress);
  }
}
