using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp27FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-27/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-27");
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
      [0] = "y\u0302",
      [1] = "x\u0305",
      [2] = "%",
      [3] = "f",
      [4] = "g",
      [5] = "x\u2194y",
      [6] = "R\u2193",
      [7] = "STO",
      [8] = "RCL",
      [9] = "y^x",
      [10] = "ENTER",
      [12] = "CHS",
      [13] = "EEX",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Woodstock", "HP-27"),
        $"Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FAndGShowLetters()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-27", "Woodstock", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    HpCalcKeyVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-27", "Woodstock", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.AreEqual("g", g.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.BlueShift));
  }

  [TestMethod]
  public void CapFace_YHatAndXBar_UseCompositeGlyphPaths()
  {
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("y\u0302"));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x\u0305"));
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual(
      "y\u0302",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[0], vocabulary, "Woodstock", "HP-27"));
    Assert.AreEqual(
      "x\u0305",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[1], vocabulary, "Woodstock", "HP-27"));
  }

  [TestMethod]
  public void CapFace_HashKeyIsSigmaPlus_NotDsp()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-27"));
    Assert.AreNotEqual(
      "DSP",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-27"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Woodstock", "HP-27"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void GoldAndBlueSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("FIX", "L.R."),
      [1] = ("SCI", "s"),
      [2] = ("ENG", "\u0394%"),
      [5] = ("n", "r"),
      [6] = ("i", "VAR"),
      [7] = ("PMT", "N.D."),
      [8] = ("PV", "NPV"),
      [9] = ("FV", "IRR"),
      [10] = ("PREFIX", "RESET"),
      [12] = ("\u03a3", "DEG"),
      [13] = ("REG", "RAD"),
      [14] = ("STK", "GRD"),
      [16] = ("ln", "e^x"),
      [17] = ("log", "10^x"),
      [18] = ("\u2192R", "\u2192P"),
      [21] = ("sin", "sin^-1"),
      [22] = ("cos", "cos^-1"),
      [23] = ("tan", "tan^-1"),
      [26] = ("H.MS+", "H.MS-"),
      [27] = ("\u221ax", "x\u00b2"),
      [28] = ("n!", "1/x"),
      [31] = ("\u2192H.MS", "\u2192H"),
      [32] = ("LST X", "\u03c0"),
      [33] = ("\u03a3-", "%\u03a3"),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-27", "Woodstock", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
    }
  }

  [TestMethod]
  public void KeyColors_OliveFunctionKeys_GoldF_BlackG_OliveEnter_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 0));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 9));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 12));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 3));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 4));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 16));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-27", 33));
  }

  [TestMethod]
  public void CapSkirtInk_BlackOnHp27_NotBlue()
  {
    uint oliveSkirt = CalcKeyLabelPalette.SkirtLabelInk("DEG", CalcButtonStyle.Olive, "HP-27");
    uint whiteSkirt = CalcKeyLabelPalette.SkirtLabelInk("e^x", CalcButtonStyle.White, "HP-27");
    uint hp25Skirt = CalcKeyLabelPalette.SkirtLabelInk("DEG", CalcButtonStyle.Olive, "HP-25");
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, oliveSkirt);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, whiteSkirt);
    Assert.AreNotEqual(CalcChassisPalette.KeyCapDarkText, hp25Skirt);
    Assert.AreEqual(CalcKeyLabelPalette.BlueOnSkirt, CalcKeyLabelPalette.BlueOnCap(CalcButtonStyle.Black));
    Assert.AreEqual(CalcChassisPalette.KeyText, CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Black));
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAndBlueG()
  {
    Assert.AreEqual(3, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Woodstock", "HP-27"));
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Woodstock", "HP-27"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(3, "Woodstock", "HP-27");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(4, "Woodstock", "HP-27");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(0, "Woodstock", "HP-27");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }
}
