using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioProgramAdvisoriesTests
{
  [TestMethod]
  public void Analyze_SelfGtoWithoutExit_FlagsInfiniteLoop()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 22, "GTO", 11, "A", StudioListingView.MergeKind.BranchPair),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    Assert.AreEqual(1, advisories.Count);
    Assert.AreEqual(StudioProgramAdvisories.Kind.InfiniteLoopSuspect, advisories[0].Kind);
    Assert.AreEqual("A", advisories[0].LabelKey);
    StringAssert.Contains(advisories[0].Message, "GTO A");
  }

  [TestMethod]
  public void Analyze_SelfGtoWithRtn_DoesNotFlag()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 22, "GTO", 11, "A", StudioListingView.MergeKind.BranchPair),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.AreEqual(0, StudioProgramAdvisories.Analyze(rows).Count);
  }

  [TestMethod]
  public void Analyze_SelfGtoWithOutwardGto_DoesNotFlag()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 22, "GTO", 12, "B", StudioListingView.MergeKind.BranchPair),
      new(4, 22, "GTO", 11, "A", StudioListingView.MergeKind.BranchPair),
      new(6, ClassicProgramCodes.Label, "LBL", 12, "B", StudioListingView.MergeKind.LabelPair),
      new(8, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.AreEqual(0, StudioProgramAdvisories.Analyze(rows).Count);
  }

  [TestMethod]
  public void Analyze_FusedSelfGto_Flags()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 0, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 0, "GTO A", null, null, StudioListingView.MergeKind.Single),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    Assert.AreEqual(1, advisories.Count);
    Assert.AreEqual("A", advisories[0].LabelKey);
  }

  [TestMethod]
  public void Analyze_Empty_ReturnsNone()
  {
    Assert.AreEqual(0, StudioProgramAdvisories.Analyze([]).Count);
  }

  [TestMethod]
  public void Analyze_GsbToMissingLabel_Flags()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 23, "GSB", 12, "B", StudioListingView.MergeKind.BranchPair),
      new(4, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    Assert.AreEqual(1, advisories.Count);
    Assert.AreEqual(StudioProgramAdvisories.Kind.MissingLabelTarget, advisories[0].Kind);
    Assert.AreEqual("B", advisories[0].LabelKey);
    StringAssert.Contains(advisories[0].Message, "GSB B");
    StringAssert.Contains(advisories[0].Message, "LBL B");
  }

  [TestMethod]
  public void Analyze_GtoToExistingLabel_DoesNotFlagMissing()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 22, "GTO", 12, "B", StudioListingView.MergeKind.BranchPair),
      new(4, ClassicProgramCodes.Label, "LBL", 12, "B", StudioListingView.MergeKind.LabelPair),
      new(6, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.IsFalse(
      StudioProgramAdvisories.Analyze(rows)
        .Any(a => a.Kind == StudioProgramAdvisories.Kind.MissingLabelTarget));
  }

  [TestMethod]
  public void Analyze_BareGtoWithoutTarget_FlagsIncomplete()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 22, "GTO", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    Assert.IsTrue(advisories.Any(a => a.Kind == StudioProgramAdvisories.Kind.IncompleteBranch));
    StudioProgramAdvisories.Advisory open = advisories.First(a => a.Kind == StudioProgramAdvisories.Kind.IncompleteBranch);
    StringAssert.Contains(open.Message, "GTO");
    StringAssert.Contains(open.Message, "no target");
  }

  [TestMethod]
  public void Analyze_FusedGsbMissingLabel_Flags()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, 0, "LBL A", null, null, StudioListingView.MergeKind.Single),
      new(1, 0, "GSB 9", null, null, StudioListingView.MergeKind.Single),
      new(2, 0, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    Assert.IsTrue(advisories.Any(a => a.Kind == StudioProgramAdvisories.Kind.MissingLabelTarget && a.LabelKey == "9"));
  }

  [TestMethod]
  public void Analyze_ConsecutiveNops_FlagsOptimize()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 0, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(3, 0, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(4, 0, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    StudioProgramAdvisories.Advisory? nop = advisories.FirstOrDefault(a => a.Kind == StudioProgramAdvisories.Kind.ConsecutiveNops);
    Assert.IsNotNull(nop);
    StringAssert.Contains(nop.Value.Message, "3 consecutive NOPs");
  }

  [TestMethod]
  public void Analyze_DuplicateRtn_FlagsOptimize()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
      new(4, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.IsTrue(
      StudioProgramAdvisories.Analyze(rows)
        .Any(a => a.Kind == StudioProgramAdvisories.Kind.DuplicateExit));
  }

  [TestMethod]
  public void Analyze_SingleNop_DoesNotFlag()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 0, "NOP", null, null, StudioListingView.MergeKind.Single),
      new(3, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.IsFalse(
      StudioProgramAdvisories.Analyze(rows)
        .Any(a => a.Kind == StudioProgramAdvisories.Kind.ConsecutiveNops));
  }
}
