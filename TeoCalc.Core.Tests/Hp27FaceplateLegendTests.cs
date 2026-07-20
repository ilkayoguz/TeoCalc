using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

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

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-27", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);
    foreach (int index in new[] { 10, 12, 13, 14 })
    {
      Assert.IsTrue(CalcBracketLegendComponent.CoversKey(spec, index), $"Covered {index}");
    }

    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 9));
    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 15));
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [10] = "PREFIX",
      [12] = "\u03a3",
      [13] = "REG",
      [14] = "STK",
    };

    foreach ((int index, string gold) in expectedGold)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-27",
        "Woodstock",
        vocabulary.KeyChart[index],
        vocabulary,
        FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_DoesNotShrinkCapFace_AndClearsCapAbove()
  {
    const float scale = 1f;
    float preferredCap = CalcKeyPanelComponent.PreferredCapHeightRef * scale;
    float slotTop = 100f;
    float capMinY = slotTop + CalcKeyPanelComponent.LabelAboveRef * scale;
    Assert.AreEqual(preferredCap, preferredCap, 0.01f);
    Assert.IsTrue(CalcBracketLegendComponent.HasClearance(slotTop, capMinY, scale));
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-27");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Woodstock", "HP-27", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-27");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-27");
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
