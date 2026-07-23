using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp37EFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-37/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-37");
    Assert.AreEqual(30, cells.Count);
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 11));
    Assert.AreEqual(CalcButtonKind.EnterWide, CalcFaceplateLayout.ButtonKindForKey(
      LoadVocabulary().KeyChart[10],
      enter,
      "Spice"));
  }

  [TestMethod]
  public void PrimaryLabels_MatchPrintedKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "n",
      [1] = "i",
      [2] = "PV",
      [3] = "PMT",
      [4] = "FV",
      [5] = "STO",
      [6] = "RCL",
      [7] = "%",
      [8] = "%T",
      [9] = "f",
      [10] = "ENTER",
      [12] = "CHS",
      [13] = "x\u2194y",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-37"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-37E"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_GoldFShowsLetter_NoBlueG()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("f", vocabulary.KeyChart[9].Char);

    KeyLegendVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-37E", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));

    // Chart char '%' at index 13 is CapFace x↔y — not EEX.
    Assert.AreEqual("%", vocabulary.KeyChart[13].Char);
    Assert.AreEqual(
      "x\u2194y",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[13], vocabulary, "Spice", "HP-37"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Spice", "HP-37"));
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void CapFace_BottomRightIsSigmaPlus_NotRunStop()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-37"));
    Assert.AreNotEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-37"));
  }

  [TestMethod]
  public void GoldSkirts_MatchFinsethChart_NoBlueSkirts()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string?> expectedGold = new()
    {
      [0] = "12x",
      [1] = "12\u00f7",
      [2] = "1/x",
      [3] = "\u221ax",
      [4] = "y^x",
      [5] = "e^x",
      [6] = "LN",
      [7] = "\u0394%",
      [8] = "PRICE",
      [10] = "AMORT",
      [12] = "CL FIN",
      [13] = "R\u2193",
      [14] = "CL ALL",
      [15] = "x\u0305",
      [20] = "s",
      [25] = "y\u0302,r",
      [30] = "n!",
      [33] = "\u03a3-",
    };

    foreach ((int index, string? gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-37", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue at index {index} must be empty");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShiftRight), $"GoldRight at index {index}");
    }

    KeyLegendVisual fKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-37", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(fKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(fKey.BlueShift));

    CalcKeyVisual enterVisual = CalcKeyVisual.FromLegacy(
      ClassicKeyFaceplateLegend.Resolve(
        "HP-37E", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal),
      CalcButtonStyle.Black,
      CalcButtonKind.EnterWide);
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "AMORT" }));
  }

  [TestMethod]
  public void KeyColors_GoldF_BlackRows_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 9));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 8));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 16));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37", 33));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-37E", 9));
  }

  [TestMethod]
  public void GoldSkirt_StatsAndMath_UseVectorGlyphPath()
  {
    string[] composite =
    [
      "12\u00f7",
      "1/x",
      "\u221ax",
      "y^x",
      "e^x",
      "\u0394%",
      "R\u2193",
      "x\u0305",
      "y\u0302,r",
      "\u03a3+",
      "\u03a3-",
    ];

    foreach (string label in composite)
    {
      Assert.IsFalse(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel(label), label);
    }
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFOnly()
  {
    Assert.AreEqual(9, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-37"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-37"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(9, "Spice", "HP-37");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(8, "Spice", "HP-37");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
    preview.HandleKeyPress(9, "Spice", "HP-37E");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }
}
