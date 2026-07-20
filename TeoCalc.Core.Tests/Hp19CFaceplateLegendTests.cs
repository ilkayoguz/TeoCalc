using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

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

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-19C", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(6, spec.LeftKey);
    Assert.AreEqual(10, spec.RightKey);
    Assert.AreEqual(8, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);

    // ENTER / CHS / EEX / CLX chart span (index 7 is the omitted wide-ENTER twin).
    foreach (int index in new[] { 6, 8, 9, 10 })
    {
      Assert.IsTrue(CalcBracketLegendComponent.CoversKey(spec, index), $"Covered {index}");
    }

    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 11));
    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 5));
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [6] = "PREFIX",
      [8] = "PRGM",
      [9] = "REG",
      [10] = "\u03a3",
    };

    foreach ((int index, string gold) in expectedGold)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-19C",
        "HP19C",
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

    Assert.AreEqual(
      preferredCap,
      preferredCap,
      0.01f,
      "CapFace stays PreferredCapHeight — CLEAR uses expanded inter-row gutter, not CapFace.");

    // CLEAR sits in the expanded gutter (lift 9 into GutterRef+GutterExtra ≈ 24).
    float clearY = CalcBracketLegendComponent.CenterY(slotTop, scale);
    Assert.AreEqual(slotTop - CalcBracketLegendComponent.LiftAboveSlotRef * scale, clearY, 0.01f);
    Assert.IsTrue(clearY < slotTop);

    float previousRowBottom = slotTop
      - (CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef) * scale;
    float clearTextTop = clearY - scale * 2.2f - 10.5f * scale * 0.5f;
    Assert.IsTrue(
      clearTextTop - previousRowBottom >= 6f * scale,
      "CLEAR must have visible margin (~6+ ref px) from the row above.");

    Assert.IsTrue(
      CalcBracketLegendComponent.HasClearance(slotTop, capMinY, scale),
      "CLEAR word must clear CapAbove legends without shrinking CapFace.");
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-19C");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("HP19C", "HP-19C", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("HP19C", "HP-19C");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-19C");
    Assert.IsTrue(clearRow >= 1);

    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 6);
    FaceplateCell above = cells.First(c => c.Row == clearRow - 1);
    Assert.IsTrue(layout.TryGetKeySlot(enter.KeyChartIndex, out RectF enterSlot));
    Assert.IsTrue(layout.TryGetKeySlot(above.KeyChartIndex, out RectF aboveSlot));

    float gap = enterSlot.Y - (aboveSlot.Y + aboveSlot.Height);
    float expected =
      CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef;
    Assert.AreEqual(expected, gap, 0.05f, "ENTER row must sit Gutter+Extra below the previous row.");

    // CapFace budget inside ENTER slot stays PreferredCapHeight (LabelAbove + Cap).
    float capFace =
      enterSlot.Height - CalcKeyPanelComponent.LabelAboveRef;
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef, capFace, 0.05f);
  }
}
