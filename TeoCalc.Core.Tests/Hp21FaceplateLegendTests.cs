using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp21FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-21/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-21");
    Assert.AreEqual(30, cells.Count);
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 11));
    Assert.AreEqual(CalcButtonKind.EnterWide, CalcFaceplateLayout.ButtonKindForKey(
      LoadVocabulary().KeyChart[10],
      enter,
      "Woodstock"));
  }

  [TestMethod]
  public void PrimaryLabels_MatchPrintedKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "1/x",
      [1] = "SIN",
      [2] = "COS",
      [3] = "TAN",
      [4] = string.Empty, // blank CapFace on blue g
      [5] = "x\u2194y",
      [6] = "R\u2193",
      [7] = "e^x",
      [8] = "STO",
      [9] = "RCL",
      [10] = "ENTER",
      [12] = "CHS",
      [13] = "EEX",
      [14] = "CLx",
      [15] = "-",
      [16] = "7",
      [20] = "+",
      [25] = "\u00d7",
      [30] = "\u00f7",
      [31] = "0",
      [32] = "\u00b7",
      [33] = "DSP",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Woodstock", "HP-21"),
        $"Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_GKeyIsBlank_NotLetterG()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-21", "Woodstock", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual(string.Empty, g.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(g.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.GoldShift));
  }

  [TestMethod]
  public void CapFace_SpaceKeyIsDsp_NotRunStop()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual(" ", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "DSP",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-21"));
    // Classic R/S mapping must not leak onto HP-21.
    Assert.AreNotEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-21"));
  }

  [TestMethod]
  public void BlueSkirts_MatchOwnersHandbookRows()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedBlue = new()
    {
      [0] = "y^x",
      [1] = "SIN^-1",
      [2] = "COS^-1",
      [3] = "TAN^-1",
      [5] = "\u2192R",
      [6] = "\u2192P",
      [7] = "LN",
      [8] = "LOG",
      [9] = "10^x",
      [12] = "\u221ax",
      [13] = "\u03c0",
      [14] = "CLR",
      [15] = "M-",
      [20] = "M+",
      [25] = "M\u00d7",
      [30] = "M\u00f7",
    };

    foreach ((int index, string blue) in expectedBlue)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-21", "Woodstock", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"No gold at index {index}");
    }

    // ENTER has no skirt.
    HpCalcKeyVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-21", "Woodstock", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(enter.BlueShift));
  }

  [TestMethod]
  public void KeyColors_BluePrefixAndWhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-21", 4));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-21", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-21", 16));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-21", 0));
  }

  [TestMethod]
  public void ShiftPreview_FramesBlueGAtIndex4()
  {
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Woodstock"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Woodstock"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(4, "Woodstock");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(0, "Woodstock");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }
}
