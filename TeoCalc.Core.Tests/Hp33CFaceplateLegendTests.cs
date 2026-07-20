using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp33CFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-33/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-33");
    Assert.AreEqual(30, cells.Count);
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 11));
  }

  [TestMethod]
  public void PrimaryLabels_MatchPrintedKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "SST",
      [1] = "GSB",
      [2] = "GTO",
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
      [33] = "R/S",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-33"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-33C"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FAndGShowLetters_OnRow1Keys4And5()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    KeyLegendVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-33C", "Spice", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    KeyLegendVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-33C", "Spice", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.AreEqual("g", g.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.BlueShift));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Spice", "HP-33C"));
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void GoldAndBlueSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("FIX", "BST"),
      [1] = ("SCI", "RTN"),
      [2] = ("ENG", "NOP"),
      [5] = ("x\u0302", "DEG"),
      [6] = ("y\u0302", "RAD"),
      [7] = ("r", "GRD"),
      [8] = ("L.R.", "x\u0305"),
      [9] = ("\u03a3-", "s"),
      [10] = ("PREFIX", "MANT"),
      [12] = ("PRGM", "INT"),
      [13] = ("REG", "FRAC"),
      [14] = ("STK", "ABS"),
      [15] = ("x\u2264y", "x<0"),
      [16] = ("SIN", "sin^-1"),
      [17] = ("COS", "cos^-1"),
      [18] = ("TAN", "tan^-1"),
      [20] = ("x>y", "x>0"),
      [21] = ("\u2192R", "\u2192P"),
      [22] = ("\u2192RAD", "\u2192DEG"),
      [23] = ("\u2192H.MS", "\u2192H"),
      [25] = ("x\u2260y", "x\u22600"),
      [26] = ("LN", "e^x"),
      [27] = ("LOG", "10^x"),
      [28] = ("y^x", "1/x"),
      [30] = ("x=y", "x=0"),
      [31] = ("\u221ax", "x\u00b2"),
      [32] = ("LST X", "\u03c0"),
      [33] = ("PAUSE", "%"),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-33C", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShiftRight), $"GoldRight at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"Black at index {index}");
    }
  }

  [TestMethod]
  public void KeyColors_GoldF_BlueG_BlackRows_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33", 3));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33", 4));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33", 16));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-33C", 3));
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAndBlueG()
  {
    Assert.AreEqual(3, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-33C"));
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-33C"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(3, "Spice", "HP-33C");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(4, "Spice", "HP-33");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
  }

  [TestMethod]
  public void CatalogAlias_Hp33EMapsToEngineHp33()
  {
    Assert.AreEqual("HP-33", CalcModelIds.ToEngineId("HP-33E"));
    Assert.AreEqual("33E", CalcModelIds.ToShortId("HP-33E"));
    Assert.AreEqual("T-33E", CalcModelIds.ToProductLabel("HP-33E"));
    Assert.AreEqual("HP-33", CalcModelIds.ToEngineId("HP-33C"));
  }

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-33", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-33C", out _));
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [10] = "PREFIX",
      [12] = "PRGM",
      [13] = "REG",
      [14] = "STK",
    };
    foreach ((int index, string gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-33", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-33");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-33", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-33");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-33");
    Assert.IsTrue(clearRow >= 1);

    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    FaceplateCell above = cells.First(c => c.Row == clearRow - 1);
    Assert.IsTrue(layout.TryGetKeySlot(enter.KeyChartIndex, out RectF enterSlot));
    Assert.IsTrue(layout.TryGetKeySlot(above.KeyChartIndex, out RectF aboveSlot));

    float gap = enterSlot.Y - (aboveSlot.Y + aboveSlot.Height);
    Assert.AreEqual(CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef, gap, 0.05f);
    float capFace = enterSlot.Height - CalcKeyPanelComponent.LabelAboveRef;
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef, capFace, 0.05f);
  }
}
