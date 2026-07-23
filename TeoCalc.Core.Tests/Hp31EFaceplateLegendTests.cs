using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp31EFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-31/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-31");
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
      [3] = "e^x",
      [4] = "LN",
      [5] = "x\u2194y",
      [6] = "R\u2193",
      [7] = "STO",
      [8] = "RCL",
      [9] = "f",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-31"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-31E"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_GoldFShowsLetter_NoBlueG()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    KeyLegendVisual f = ClassicKeyFaceplateLegend.Resolve(
      "HP-31E", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("f", f.Primary);
    Assert.IsTrue(string.IsNullOrEmpty(f.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(f.BlueShift));

    // Chart char 'g' at index 4 is CapFace LN — not a blue g prefix.
    Assert.AreEqual("g", vocabulary.KeyChart[4].Char);
    Assert.AreEqual(
      "LN",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[4], vocabulary, "Spice", "HP-31"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("\b", vocabulary.KeyChart[14].Char);
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[14], vocabulary, "Spice", "HP-31"));
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void CapFace_BottomRightIsPercent_NotRunStop()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual(" ", vocabulary.KeyChart[33].Char);
    Assert.AreEqual(
      "%",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-31"));
    Assert.AreNotEqual(
      "R/S",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[33], vocabulary, "Spice", "HP-31"));
  }

  [TestMethod]
  public void GoldSkirts_MatchFinsethChart_NoBlueSkirts()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string?> expectedGold = new()
    {
      [0] = "FIX",
      [1] = "SCI",
      [2] = "\u03c0",
      [3] = "10^x",
      [4] = "LOG",
      [5] = "DEG",
      [6] = "RAD",
      [7] = "GRD",
      [8] = "LST X",
      [10] = "MANT",
      [12] = "ALL",
      [13] = "REG",
      [14] = "STK",
      [15] = "\u2192R",
      [16] = "SIN",
      [17] = "COS",
      [18] = "TAN",
      [20] = "\u2192P",
      [21] = "sin^-1",
      [22] = "cos^-1",
      [23] = "tan^-1",
      [25] = "\u2192DEG",
      [26] = "\u2192in",
      [27] = "\u2192\u00b0F",
      [28] = "\u2192lbm",
      [30] = "\u2192RAD",
      [31] = "\u2192mm",
      [32] = "\u2192\u00b0C",
      [33] = "\u2192kg",
    };

    foreach ((int index, string? gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-31", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue at index {index} must be empty");
      if (index != 10)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShiftRight), $"GoldRight at index {index}");
      }
    }

    KeyLegendVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-31", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("MANT", enter.GoldShift);
    Assert.AreEqual("PREFIX", enter.GoldShiftRight);

    CalcKeyVisual enterVisual = CalcKeyVisual.FromLegacy(
      enter, CalcButtonStyle.Black, CalcButtonKind.EnterWide);
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "MANT", Align: CalcLabelAlign.Left }));
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "PREFIX", Align: CalcLabelAlign.Right }));

    KeyLegendVisual fKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-31", "Spice", vocabulary.KeyChart[9], vocabulary, FaceplateLabelStyle.Normal);
    Assert.IsTrue(string.IsNullOrEmpty(fKey.GoldShift));
    Assert.IsTrue(string.IsNullOrEmpty(fKey.BlueShift));
  }

  [TestMethod]
  public void KeyColors_GoldF_BlackRows_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 9));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 4));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 16));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31", 33));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-31E", 9));
  }

  [TestMethod]
  public void GoldSkirt_ArrowLegends_UseVectorGlyphPath()
  {
    string[] arrows =
    [
      "\u2192R",
      "\u2192P",
      "\u2192DEG",
      "\u2192in",
      "\u2192\u00b0F",
      "\u2192lbm",
      "\u2192RAD",
      "\u2192mm",
      "\u2192\u00b0C",
      "\u2192kg",
    ];

    foreach (string arrow in arrows)
    {
      Assert.IsFalse(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel(arrow), arrow);
    }
  }

  [TestMethod]
  public void GoldSkirt_InverseTrig_UseCompositeGlyphPath()
  {
    foreach (string inverse in new[] { "sin^-1", "cos^-1", "tan^-1", "SIN^-1", "COS^-1", "TAN^-1" })
    {
      Assert.IsFalse(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel(inverse), inverse);
    }
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldFOnly()
  {
    Assert.AreEqual(9, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-31"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-31"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(9, "Spice", "HP-31");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(4, "Spice", "HP-31");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
    preview.HandleKeyPress(9, "Spice", "HP-31E");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-31", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);
    Assert.AreEqual(CalcBracketLegendComponent.LeftEdgeAlign.MidKey, spec.LeftEdge);
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-31E", out _));
  }

  [TestMethod]
  public void ClearBracket_LeftRuleStartsAtEnterMid()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-31", out CalcBracketLegendComponent.Spec spec));
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-31");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-31", model);
    Assert.IsTrue(layout.TryGetKeySlot(10, out RectF enter));
    Assert.IsTrue(enter.Width > CalcKeyPanelComponent.PreferredCapHeightRef, "ENTER is wide");

    const float scale = 1f;
    float midX = enter.X + enter.Width * 0.5f;
    Assert.AreEqual(
      midX,
      CalcBracketLegendComponent.LeftRuleOuterX(in spec, enter, scale),
      0.01f);
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    KeyLegendVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-31", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("MANT", enter.GoldShift);
    Assert.AreEqual("PREFIX", enter.GoldShiftRight);

    Dictionary<int, string> expectedGold = new()
    {
      [12] = "ALL",
      [13] = "REG",
      [14] = "STK",
    };
    foreach ((int index, string gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-31", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-31");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-31", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-31");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-31");
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
