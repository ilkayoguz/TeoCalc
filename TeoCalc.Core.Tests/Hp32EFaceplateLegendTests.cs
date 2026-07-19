using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp32EFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-32/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-32");
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
      [0] = "\u221ax",
      [1] = "1/x",
      [2] = "y^x",
      [3] = "f",
      [4] = "g",
      [5] = "x\u2194y",
      [6] = "R\u2193",
      [7] = "STO",
      [8] = "RCL",
      [9] = "\u03a3+",
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
      [33] = "%",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-32"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-32E"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FAndGShowLetters_OnRow1Keys4And5()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("p", vocabulary.KeyChart[3].Char);
    Assert.AreEqual("v", vocabulary.KeyChart[4].Char);

    HpCalcKeyVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-32E", "Spice", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    HpCalcKeyVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-32E", "Spice", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.AreEqual("g", g.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.BlueShift));

    // Chart char 'f' at index 9 is CapFace Σ+ — not the gold prefix letter.
    Assert.AreEqual("f", vocabulary.KeyChart[9].Char);
    Assert.AreEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[9], vocabulary, "Spice", "HP-32"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Spice", "HP-32"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void CapFace_BottomRightIsPercent_NotRunStop()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "%",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-32"));
    Assert.AreNotEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-32"));
  }

  [TestMethod]
  public void GoldAndBlueSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("FIX", "x\u00b2"),
      [1] = ("SCI", "\u03c0"),
      [2] = ("ENG", "LST X"),
      [5] = ("Q", "Q^-1"),
      [6] = ("y\u0302", "x\u0302"),
      [7] = ("L.R.", "r"),
      [8] = ("x\u0305", "s"),
      [9] = ("\u03a3-", "n!"),
      [10] = ("PREFIX", "MANT"),
      [12] = ("ALL", "RAD"),
      [13] = ("REG", "GRD"),
      [14] = ("\u03a3", "DEG"),
      [15] = ("\u2192in", "\u2192mm"),
      [16] = ("SIN", "sin^-1"),
      [17] = ("COS", "cos^-1"),
      [18] = ("TAN", "tan^-1"),
      [20] = ("\u2192\u00b0F", "\u2192\u00b0C"),
      [21] = ("\u2192R", "\u2192P"),
      [22] = ("\u2192RAD", "\u2192DEG"),
      [23] = ("\u2192H.MS", "\u2192H"),
      [25] = ("\u2192lbm", "\u2192kg"),
      [26] = ("SINH", "SINH^-1"),
      [27] = ("COSH", "COSH^-1"),
      [28] = ("TANH", "TANH^-1"),
      [30] = ("\u2192gal", "\u2192ltr"),
      [31] = ("LN", "e^x"),
      [32] = ("LOG", "10^x"),
      [33] = ("%\u03a3", "\u0394%"),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-32", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShiftRight), $"GoldRight at index {index}");
    }

    HpCalcKeyVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-32E", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("PREFIX", enter.GoldShift);
    Assert.AreEqual("MANT", enter.BlueShift);
    Assert.IsTrue(string.IsNullOrEmpty(enter.GoldShiftRight));

    CalcKeyVisual enterVisual = CalcKeyVisual.FromLegacy(
      enter, CalcButtonStyle.Black, CalcButtonKind.EnterWide);
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "PREFIX" }));
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapSkirt, Text: "MANT" }));

    HpCalcKeyVisual fKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-32", "Spice", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    HpCalcKeyVisual gKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-32", "Spice", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(fKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(fKey.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(gKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(gKey.BlueShift));
  }

  [TestMethod]
  public void KeyColors_GoldF_BlueG_BlackRows_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 3));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 4));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 9));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 16));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32", 33));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32E", 3));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-32E", 4));
  }

  [TestMethod]
  public void GoldSkirt_StatsAndArrowLegends_UseVectorGlyphPath()
  {
    string[] composite =
    [
      "y\u0302",
      "x\u0302",
      "x\u0305",
      "\u03a3+",
      "\u03a3-",
      "\u03a3",
      "%\u03a3",
      "\u0394%",
      "\u2192in",
      "\u2192mm",
      "\u2192R",
      "\u2192P",
      "\u2192RAD",
      "\u2192DEG",
      "\u2192H.MS",
      "\u2192H",
      "\u2192lbm",
      "\u2192kg",
      "\u2192gal",
      "\u2192ltr",
      "\u2192\u00b0F",
      "\u2192\u00b0C",
      "sin^-1",
      "cos^-1",
      "tan^-1",
      "SINH^-1",
      "COSH^-1",
      "TANH^-1",
      "Q^-1",
    ];

    foreach (string label in composite)
    {
      Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel(label), label);
    }
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAndBlueG()
  {
    Assert.AreEqual(3, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-32"));
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-32"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(3, "Spice", "HP-32");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(4, "Spice", "HP-32");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(0, "Spice", "HP-32");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
    preview.HandleKeyPress(3, "Spice", "HP-32E");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }
}
