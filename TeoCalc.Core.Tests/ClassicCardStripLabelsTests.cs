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

  [TestMethod]
  public void CaptionForLetter_ReturnsStripCaptionForA()
  {
    Assert.AreEqual("+123", ClassicCardStripLabels.CaptionForLetter(["+123", "", "", "", ""], "A"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(["+123", "", "", "", ""], "B"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(["+123", "", "", "", ""], "1"));
    Assert.IsTrue(ClassicCardStripLabels.TryGetStripColumn("E", out int col));
    Assert.AreEqual(4, col);
  }

  [TestMethod]
  public void CaptionForLetter_NullCaptions_FallsBackToNoCardDefaults()
  {
    Assert.AreEqual("1/x", ClassicCardStripLabels.CaptionForLetter(null, "A"));
    Assert.AreEqual("\u221ax", ClassicCardStripLabels.CaptionForLetter(null, "B"));
    Assert.AreEqual("y^x", ClassicCardStripLabels.CaptionForLetter(null, "C"));
    Assert.AreEqual("R\u2193", ClassicCardStripLabels.CaptionForLetter(null, "D"));
    Assert.AreEqual("x\u2194y", ClassicCardStripLabels.CaptionForLetter(null, "E"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(null, "1"));
    Assert.IsTrue(ClassicCardStripLabels.UsesNoCardStripChrome(null));
    Assert.IsFalse(ClassicCardStripLabels.UsesNoCardStripChrome(["+123", "", "", "", ""]));
  }

  [TestMethod]
  public void CaptionForLetter_CardLoaded_EmptyColumnsStayEmpty()
  {
    // Partial [Label] / missing LBL B–E must not fall back to DefaultNoCardCaptions.
    string[] card = ["+123", "", "", "", ""];
    Assert.AreEqual("+123", ClassicCardStripLabels.CaptionForLetter(card, "A"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(card, "B"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(card, "C"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(card, "D"));
    Assert.AreEqual(string.Empty, ClassicCardStripLabels.CaptionForLetter(card, "E"));
  }

  [TestMethod]
  public void RemoveEmptyStripLabelStubs_DropsInjectedLblRtnOnly()
  {
    List<string> steps =
    [
      "LBL", "A", "1", "2", "3", "+", "RTN",
      "LBL", "1", "4", "RTN",
      "LBL", "B", "RTN",
      "LBL", "C", "NOP", "RTN",
      "LBL", "D", "RTN",
      "LBL", "E", "RTN",
      "PTR",
    ];

    ClassicCardStripLabels.RemoveEmptyStripLabelStubs(steps);

    CollectionAssert.AreEqual(
      new[] { "LBL", "A", "1", "2", "3", "+", "RTN", "LBL", "1", "4", "RTN", "PTR" },
      steps);
  }

  [TestMethod]
  public void DefaultNoCardCaptions_MatchFaceplateStripUnicode()
  {
    CollectionAssert.AreEqual(
      new[] { "1/x", "\u221ax", "y^x", "R\u2193", "x\u2194y" },
      ClassicCardStripLabels.DefaultNoCardCaptions);
  }
}
