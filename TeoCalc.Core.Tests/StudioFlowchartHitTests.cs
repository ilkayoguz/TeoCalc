using System.Numerics;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioFlowchartHitTests
{
  [TestMethod]
  public void HitVisibleNode_AcceptsPartialTopStrip()
  {
    Vector2 clipMin = new(10f, 100f);
    Vector2 clipMax = new(400f, 500f);
    // Node straddles the top clip edge — only bottom 12px visible.
    Vector2 nodeMin = new(40f, 80f);
    Vector2 nodeMax = new(200f, 112f);
    Vector2 mouse = new(100f, 105f);

    Assert.IsTrue(
      StudioFlowchartView.HitVisibleNode(mouse, nodeMin, nodeMax, clipMin, clipMax));
  }

  [TestMethod]
  public void HitVisibleNode_AcceptsPartialBottomStrip()
  {
    Vector2 clipMin = new(10f, 100f);
    Vector2 clipMax = new(400f, 500f);
    Vector2 nodeMin = new(40f, 488f);
    Vector2 nodeMax = new(200f, 540f);
    Vector2 mouse = new(100f, 495f);

    Assert.IsTrue(
      StudioFlowchartView.HitVisibleNode(mouse, nodeMin, nodeMax, clipMin, clipMax));
  }

  [TestMethod]
  public void HitVisibleNode_RejectsOutsideClip()
  {
    Vector2 clipMin = new(10f, 100f);
    Vector2 clipMax = new(400f, 500f);
    Vector2 nodeMin = new(40f, 80f);
    Vector2 nodeMax = new(200f, 112f);
    // Inside node but above the visible clip.
    Vector2 mouse = new(100f, 90f);

    Assert.IsFalse(
      StudioFlowchartView.HitVisibleNode(mouse, nodeMin, nodeMax, clipMin, clipMax));
  }

  [TestMethod]
  public void HitVisibleNode_RejectsOutsideNode()
  {
    Vector2 clipMin = new(10f, 100f);
    Vector2 clipMax = new(400f, 500f);
    Vector2 nodeMin = new(40f, 120f);
    Vector2 nodeMax = new(200f, 180f);
    Vector2 mouse = new(300f, 150f);

    Assert.IsFalse(
      StudioFlowchartView.HitVisibleNode(mouse, nodeMin, nodeMax, clipMin, clipMax));
  }

  [TestMethod]
  public void SplitCaptionParts_GtoLabelTarget_TaggedAsLabelKeycap()
  {
    List<string> parts = StudioFlowchartView.SplitCaptionParts("GTO 1");
    Assert.AreEqual(3, parts.Count);
    Assert.AreEqual("GTO", parts[0]);
    Assert.AreEqual(" ", parts[1]);
    Assert.AreEqual("\u00031", parts[2]);
  }

  [TestMethod]
  public void SplitCaptionParts_GsbStripLetter_TaggedAsLabelKeycap()
  {
    List<string> parts = StudioFlowchartView.SplitCaptionParts("GSB A");
    Assert.AreEqual("\u0003A", parts[^1]);
  }

  [TestMethod]
  public void SplitCaptionParts_FusedGtoDigit_TaggedAsLabelKeycap()
  {
    List<string> parts = StudioFlowchartView.SplitCaptionParts("GTO1");
    Assert.AreEqual("\u00031", parts[^1]);
  }
}
