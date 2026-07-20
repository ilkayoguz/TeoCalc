using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp25FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-25/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-25");
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
      [0] = "SST",
      [1] = "BST",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Woodstock", "HP-25"),
        $"Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FAndGShowLetters()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    KeyLegendVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-25", "Woodstock", vocabulary.KeyChart[3], vocabulary, FaceplateLabelStyle.Normal);
    KeyLegendVisual g = ClassicKeyFaceplateLegend.Resolve(
      "HP-25", "Woodstock", vocabulary.KeyChart[4], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.AreEqual("g", g.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(g.BlueShift));
  }

  [TestMethod]
  public void CapFace_SigmaPlusOnRow2_NotBottomRight()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("#", vocabulary.KeyChart[9].Char);
    Assert.AreEqual(
      "\u03a3+",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[9], vocabulary, "Woodstock", "HP-25"));
    Assert.AreEqual(" ", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-25"));
    Assert.AreNotEqual(
      "DSP",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Woodstock", "HP-25"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Woodstock", "HP-25"));
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void GoldAndBlueSkirts_MatchFinsethChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("FIX", null),
      [1] = ("SCI", null),
      [2] = ("ENG", null),
      [5] = ("x\u0305", "%"),
      [6] = ("s", "1/x"),
      [9] = ("\u03a3-", null),
      [10] = ("PREFIX", null),
      [12] = ("PRGM", "DEG"),
      [13] = ("REG", "RAD"),
      [14] = ("STK", "GRD"),
      [15] = ("x<y", "x<0"),
      [16] = ("ln", "e^x"),
      [17] = ("log", "10^x"),
      [18] = ("\u2192R", "\u2192P"),
      [20] = ("x\u2265y", "x\u22650"),
      [21] = ("sin", "sin^-1"),
      [22] = ("cos", "cos^-1"),
      [23] = ("tan", "tan^-1"),
      [25] = ("x\u2260y", "x\u22600"),
      [26] = ("INT", "FRAC"),
      [27] = ("\u221ax", "x\u00b2"),
      [28] = ("y^x", "ABS"),
      [30] = ("x=y", "x=0"),
      [31] = ("\u2192H.MS", "\u2192H"),
      [32] = ("LST X", "\u03c0"),
      [33] = ("PAUSE", "NOP"),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-25", "Woodstock", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
    }
  }

  [TestMethod]
  public void KeyColors_GoldAndBluePrefix()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-25", 3));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-25", 4));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-25", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-25", 16));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Woodstock", "HP-25", 0));
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFAndBlueG()
  {
    Assert.AreEqual(3, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Woodstock", "HP-25"));
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Woodstock", "HP-25"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(3, "Woodstock", "HP-25");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(4, "Woodstock", "HP-25");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(0, "Woodstock", "HP-25");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-25", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);

    // ENTER / CHS / EEX / CLX (index 11 is the omitted wide-ENTER twin).
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
      [12] = "PRGM",
      [13] = "REG",
      [14] = "STK",
    };

    foreach ((int index, string gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-25",
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
    float labelAbove = CalcKeyPanelComponent.LabelAboveRef * scale;
    float preferredCap = CalcKeyPanelComponent.PreferredCapHeightRef * scale;
    float slotTop = 100f;
    float capMinY = slotTop + labelAbove;

    Assert.AreEqual(preferredCap, preferredCap, 0.01f);

    float clearY = CalcBracketLegendComponent.CenterY(slotTop, scale);
    Assert.AreEqual(slotTop - CalcBracketLegendComponent.LiftAboveSlotRef * scale, clearY, 0.01f);

    float previousRowBottom = slotTop
      - (CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef) * scale;
    float clearTextTop = clearY - scale * 2.2f - 10.5f * scale * 0.5f;
    Assert.IsTrue(
      clearTextTop - previousRowBottom >= 6f * scale,
      "CLEAR must have visible margin (~6+ ref px) from the row above.");

    Assert.IsTrue(CalcBracketLegendComponent.HasClearance(slotTop, capMinY, scale));
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-25");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Woodstock", "HP-25", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-25");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-25");
    Assert.IsTrue(clearRow >= 1);

    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    FaceplateCell above = cells.First(c => c.Row == clearRow - 1);
    Assert.IsTrue(layout.TryGetKeySlot(enter.KeyChartIndex, out RectF enterSlot));
    Assert.IsTrue(layout.TryGetKeySlot(above.KeyChartIndex, out RectF aboveSlot));

    float gap = enterSlot.Y - (aboveSlot.Y + aboveSlot.Height);
    float expected =
      CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef;
    Assert.AreEqual(expected, gap, 0.05f);

    float capFace = enterSlot.Height - CalcKeyPanelComponent.LabelAboveRef;
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef, capFace, 0.05f);
  }
}
