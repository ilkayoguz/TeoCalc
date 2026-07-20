using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp38EFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-38/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-38");
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
      [8] = "f",
      [9] = "g",
      [10] = "ENTER",
      [12] = "CHS",
      [13] = "x\u2194y",
      [14] = "CL X",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-38"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-38E"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FAndGShowLetters_OnRow2Keys4And5()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("r", vocabulary.KeyChart[8].Char);
    Assert.AreEqual("f", vocabulary.KeyChart[9].Char);

    HpCalcKeyVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-38E", "Spice", vocabulary.KeyChart[8], vocabulary, FaceplateLabelStyle.Normal);
    HpCalcKeyVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-38E", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
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
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CL X",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Spice", "HP-38"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CL X"));
  }

  [TestMethod]
  public void CapSkirt_DivideWeightedMean_UsesCapitalMathXBarWithPlainW()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
      "HP-38E", "Spice", vocabulary.KeyChart[30], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("1/x", visual.GoldShift);
    Assert.AreEqual("x\u0305w", visual.BlueShift);
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCapitalMathXBar(visual.BlueShift!));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel(visual.BlueShift!));
  }

  [TestMethod]
  public void CapSkirt_ZeroMean_UsesCapitalMathXBarOnly()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
      "HP-38E", "Spice", vocabulary.KeyChart[31], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("X\u0305", visual.BlueShift);
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCapitalMathXBarOnly(visual.BlueShift!));
    Assert.IsFalse(HpClassicFaceplateGlyphs.UsesCapitalMathXBar(visual.BlueShift!));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel(visual.BlueShift!));
  }

  [TestMethod]
  public void SwitchBank_RightSwitch_IsDualRowDateBeginEnd()
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId("HP-38E");
    Assert.AreEqual(2, bank.Count);
    Assert.AreEqual(CalcSwitchLabels.DateBeginEnd.Left, bank[1].LeftLabel);
    Assert.AreEqual(CalcSwitchLabels.DateBeginEnd.Right, bank[1].RightLabel);
    Assert.AreEqual("D.MY\nBEGIN", bank[1].LeftLabel);
    Assert.AreEqual("M.DY\nEND", bank[1].RightLabel);
  }

  [TestMethod]
  public void CapFace_BottomRightIsRunStop_NotSigmaPlus()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-38"));
    Assert.AreNotEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-38"));
  }

  [TestMethod]
  public void GoldAndBlueSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("AMORT", "12x"),
      [1] = ("INT", "12\u00f7"),
      [2] = ("NPV", "CFo"),
      [3] = ("RND", "CFj"),
      [4] = ("IRR", "Nj"),
      [5] = ("\u221ax", "y\u0302"),
      [6] = ("%T", "e^x"),
      [7] = ("\u0394%", "LN"),
      [10] = ("PREFIX", "LST X"),
      [12] = ("FIN", "EEX"),
      [13] = ("\u03a3", "R\u2193"),
      [14] = ("ALL", "CL P"),
      [15] = ("\u0394DAYS", "P/R"),
      [16] = (null, "GTO"),
      [17] = (null, "BST"),
      [18] = (null, "SST"),
      [20] = ("DATE", "MEM"),
      [21] = (null, "PSE"),
      [22] = (null, "x\u2264y"),
      [23] = (null, "x=0"),
      [25] = ("INTGR", "FRAC"),
      [26] = (null, "x\u0302,r"),
      [27] = (null, "y\u0302,r"),
      [28] = (null, "n!"),
      [30] = ("1/x", "x\u0305w"),
      [31] = (null, "X\u0305"),
      [32] = (null, "s"),
      [33] = ("\u03a3+", "\u03a3-"),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-38", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShiftRight), $"GoldRight at index {index}");
    }

    HpCalcKeyVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-38E", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("PREFIX", enter.GoldShift);
    Assert.AreEqual("LST X", enter.BlueShift);
    Assert.IsTrue(string.IsNullOrEmpty(enter.GoldShiftRight));

    CalcKeyVisual enterVisual = CalcKeyVisual.FromLegacy(
      enter, CalcButtonStyle.Black, CalcButtonKind.EnterWide);
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "PREFIX" }));
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapSkirt, Text: "LST X" }));

    HpCalcKeyVisual fKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-38", "Spice", vocabulary.KeyChart[8], vocabulary, FaceplateLabelStyle.Normal);
    HpCalcKeyVisual gKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-38", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(fKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(fKey.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(gKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(gKey.BlueShift));
  }

  [TestMethod]
  public void KeyColors_GoldF_BlueG_BlackRows_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 8));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 9));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 7));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 16));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38", 33));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38E", 8));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-38E", 9));
  }

  [TestMethod]
  public void GoldAndBlueSkirt_StatsAndMath_UseVectorGlyphPath()
  {
    string[] composite =
    [
      "12\u00f7",
      "\u221ax",
      "e^x",
      "\u0394%",
      "y\u0302",
      "LST X",
      "R\u2193",
      "\u03a3",
      "\u0394DAYS",
      "x\u2264y",
      "x=0",
      "x\u0302,r",
      "y\u0302,r",
      "1/x",
      "x\u0305w",
      "X\u0305",
      "\u03a3+",
      "\u03a3-",
    ];

    foreach (string label in composite)
    {
      Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel(label), label);
    }
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAndBlueG()
  {
    Assert.AreEqual(8, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-38"));
    Assert.AreEqual(9, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-38"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(8, "Spice", "HP-38");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(9, "Spice", "HP-38");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(0, "Spice", "HP-38");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
    preview.HandleKeyPress(8, "Spice", "HP-38E");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-38", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-38E", out _));
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [10] = "PREFIX",
      [12] = "FIN",
      [13] = "\u03a3",
      [14] = "ALL",
    };
    foreach ((int index, string gold) in expectedGold)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-38", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-38");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-38", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-38");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-38");
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
