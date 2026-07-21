using TeoCalc.Formats;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCardStripLabelsTests
{
  [TestMethod]
  public void InferFromSteps_LblA_AddProgram_SummarizesBody()
  {
    string[] labels = ClassicCardStripLabels.InferFromSteps(
    [
      "LBL",
      "A",
      "1",
      "2",
      "3",
      "+",
      "R/S",
      "PTR",
    ]);

    Assert.AreEqual("+123", labels[0]);
    Assert.AreEqual(string.Empty, labels[1]);
  }

  [TestMethod]
  public void InferFromSteps_LblNumeric_IgnoresStripColumns()
  {
    string[] labels = ClassicCardStripLabels.InferFromSteps(
    [
      "LBL",
      "1",
      "9",
      "+",
      "R/S",
    ]);

    Assert.AreEqual(string.Empty, labels[0]);
  }

  [TestMethod]
  public void InferFromSteps_CuveSoftStyle_UsesExplicitLabelsWhenProvided()
  {
    Assert.IsTrue(ClassicCardStripLabels.HasAnyLabel(["M", "D", "Y", "DAY", ""]));
    Assert.IsFalse(ClassicCardStripLabels.HasAnyLabel(["", "", "", "", ""]));
  }

  [TestMethod]
  public void Resolve_ExplicitCaptionWithoutLbl_MarksColumnDisabled()
  {
    CardStripPresentation strip = ClassicCardStripLabels.Resolve(
      ["+123", "GTO1", "", "", ""],
      ["LBL", "A", "1", "R/S", "PTR"]);

    Assert.AreEqual("GTO1", strip.Captions[1]);
    Assert.IsFalse(strip.Enabled[1]);
    Assert.IsTrue(strip.Enabled[0]);
  }
}
