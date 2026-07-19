using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp19CFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-19C/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_IncludeWideEnter_AndOmitBlanks()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("HP19C", "HP-19C");
    Assert.AreEqual(31, cells.Count); // 36 - 5 omitted (7,17,23,29,35)
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 6);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 7));
    Assert.AreEqual(CalcButtonKind.EnterWide, CalcFaceplateLayout.ButtonKindForKey(
      LoadVocabulary().KeyChart[6],
      enter,
      "HP19C"));
  }

  [TestMethod]
  public void PrimaryLabels_MatchPrintedKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "x\u2194y",
      [1] = "R\u2193",
      [2] = "GSB",
      [3] = "GTO",
      [4] = "SST",
      [5] = "f",
      [6] = "ENTER",
      [8] = "CHS",
      [9] = "EEX",
      [10] = "CLX",
      [11] = "g",
      [12] = "-",
      [13] = "7",
      [16] = "\u03a3+",
      [24] = "\u00d7",
      [30] = "\u00f7",
      [33] = "R/S",
      [34] = "PRx",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "HP19C"),
        $"Index {index}");
    }
  }

  [TestMethod]
  public void GoldAndBlueLegends_MatchOwnersHandbook()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual xExchange = ClassicKeyFaceplateLegend.Resolve(
      "HP-19C", "HP19C", vocabulary.KeyChart[0], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("x\u0305", xExchange.GoldShift);
    Assert.AreEqual("%", xExchange.BlueShift);

    HpCalcKeyVisual roll = ClassicKeyFaceplateLegend.Resolve(
      "HP-19C", "HP19C", vocabulary.KeyChart[1], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("S", roll.GoldShift);
    Assert.AreEqual("i", roll.BlueShift);

    HpCalcKeyVisual nine = ClassicKeyFaceplateLegend.Resolve(
      "HP-19C", "HP19C", vocabulary.KeyChart[15], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("\u2192R", nine.GoldShift);
    Assert.AreEqual("\u2192P", nine.BlueShift);

    HpCalcKeyVisual gto = ClassicKeyFaceplateLegend.Resolve(
      "HP-19C", "HP19C", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("ENG", gto.GoldShift);
    Assert.AreEqual("LBL", gto.BlueShift);

    HpCalcKeyVisual sin = ClassicKeyFaceplateLegend.Resolve(
      "HP-19C", "HP19C", vocabulary.KeyChart[19], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("sin", sin.GoldShift);
    Assert.AreEqual("sin^-1", sin.BlueShift);
  }

  [TestMethod]
  public void KeyColors_MatchBlackWhiteFg()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("HP19C", "HP-19C", 5));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("HP19C", "HP-19C", 11));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("HP19C", "HP-19C", 13));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("HP19C", "HP-19C", 16));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("HP19C", "HP-19C", 0));
  }

  [TestMethod]
  public void ShiftPreview_FramesFAndG()
  {
    Assert.AreEqual(5, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "HP19C"));
    Assert.AreEqual(11, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "HP19C"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(5, "HP19C");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(11, "HP19C");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
  }
}
