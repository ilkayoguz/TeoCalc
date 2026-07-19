using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp22FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-22/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-22");
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
      [0] = "n",
      [1] = "i",
      [2] = "PMT",
      [3] = "PV",
      [4] = "FV",
      [5] = "x\u2194y",
      [6] = "R\u2193",
      [7] = "STO",
      [8] = "RCL",
      [9] = string.Empty, // blank CapFace on gold f
      [10] = "ENTER",
      [12] = "CHS",
      [13] = "%",
      [14] = "CLX",
      [15] = "-",
      [16] = "7",
      [20] = "+",
      [25] = "\u00d7",
      [30] = "\u00f7",
      [31] = "0",
      [32] = "\u00b7",
      [33] = "\u03a3+",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Woodstock", "HP-22"),
        $"Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_GoldFKeyIsBlank_NotLetterF()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-22", "Woodstock", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual(string.Empty, f.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
  }

  [TestMethod]
  public void CapFace_HashKeyIsSigmaPlus_NotDsp()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-22"));
    Assert.AreNotEqual(
      "DSP",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-22"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Woodstock", "HP-22"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void GoldSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [0] = "12x",
      [1] = "12\u00f7",
      [2] = "ACC",
      [3] = "INT",
      [4] = "BAL",
      [5] = "L.R.",
      [6] = "y\u0302",
      [7] = "x\u0305",
      [8] = "s",
      [10] = "RESET",
      [12] = "%\u03a3",
      [13] = "\u0394%",
      [14] = "CLEAR",
      [15] = "ln",
      [20] = "e^x",
      [25] = "y^x",
      [30] = "\u221ax",
      [33] = "\u03a3-",
    };

    foreach ((int index, string gold) in expectedGold)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-22", "Woodstock", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"No blue at index {index}");
    }

    // Blank gold f has no CapAbove either.
    HpCalcKeyVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-22", "Woodstock", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
  }

  [TestMethod]
  public void GoldLegend_DeltaPercent_UsesVectorGlyphPath()
  {
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("\u0394%"));
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
      "HP-22", "Woodstock", vocabulary.KeyChart[13], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("\u0394%", visual.GoldShift);
  }

  [TestMethod]
  public void GoldLegend_YHat_UsesCompositeGlyphPath()
  {
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("y\u0302"));
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
      "HP-22", "Woodstock", vocabulary.KeyChart[6], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("y\u0302", visual.GoldShift);
  }

  [TestMethod]
  public void GoldLegend_XBar_StillComposite()
  {
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x\u0305"));
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
      "HP-22", "Woodstock", vocabulary.KeyChart[7], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("x\u0305", visual.GoldShift);
  }

  [TestMethod]
  public void KeyColors_GoldPrefixAndWhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-22", 9));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-22", 4));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-22", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-22", 16));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-22", 0));
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAtIndex9()
  {
    Assert.AreEqual(9, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Woodstock", "HP-22"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Woodstock", "HP-22"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(9, "Woodstock", "HP-22");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(0, "Woodstock", "HP-22");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }
}
