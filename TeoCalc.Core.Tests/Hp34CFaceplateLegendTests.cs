using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp34CFaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-34/Program/program.vocabulary.json"));

  [TestMethod]
  public void PhysicalCells_MatchSpiceWoodstockFiveColumnMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-34");
    Assert.AreEqual(30, cells.Count);
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 10);
    Assert.AreEqual(2, enter.ColSpan);
  }

  [TestMethod]
  public void PrimaryLabels_MatchPrintedKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "A",
      [1] = "B",
      [2] = "GSB",
      [3] = "f",
      [4] = "g",
      [5] = "x\u2194y",
      [6] = "GTO",
      [7] = "STO",
      [8] = "RCL",
      [9] = "h",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-34"),
        $"Index {index}");
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Spice", "HP-34C"),
        $"Catalog id Index {index}");
    }
  }

  [TestMethod]
  public void CapFace_FGHShowLetters()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("f", CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[3], vocabulary, "Spice", "HP-34C"));
    Assert.AreEqual("g", CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[4], vocabulary, "Spice", "HP-34C"));
    Assert.AreEqual("h", CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[9], vocabulary, "Spice", "HP-34C"));
  }

  [TestMethod]
  public void GoldBlueBlack_MatchFinsethThreeShiftLayout()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue, string? Black)> expected = new()
    {
      [0] = ("FIX", "DEG", "DSP I"),
      [1] = ("SCI", "RAD", "RTN"),
      [2] = ("ENG", "GRD", "LBL"),
      [5] = ("x\u2194I", null, "x\u2194(i)"),
      [6] = ("R\u2191", "R\u2193", "DEL"),
      [7] = ("I", "DSE", "BST"),
      [8] = ("(i)", "ISG", "SST"),
      [10] = ("MEM", "PREFIX", "MANT"),
      [12] = ("PRGM", null, "INT"),
      [13] = ("REG", null, "FRAC"),
      [14] = ("\u03a3", null, "ABS"),
      [15] = ("x\u2264y", "x<0", "%"),
      [16] = ("SIN", "^-1", "\u0394%"),
      [17] = ("COS", "^-1", "x\u0305"),
      [18] = ("TAN", "^-1", "s"),
      [20] = ("x>y", "x>0", "SF"),
      [21] = ("\u2192R", "\u2192P", "y\u0302"),
      [22] = ("\u2192D", "\u2192R", "r"),
      [23] = ("\u2192H.MS", "\u2192H", "L.R."),
      [25] = ("x\u2260y", "x\u22600", "CF"),
      [26] = ("LN", "e^x", "x!"),
      [27] = ("LOG", "10^x", "1/x"),
      [28] = ("\u221ax", "x\u00b2", "y^x"),
      [30] = ("x=y", "x=0", "F?"),
      [31] = ("\u222Byx", null, "LST X"),
      [32] = ("SOLVE", null, "\u03c0"),
      [33] = ("\u03a3+", "\u03a3-", "PSE"),
    };

    foreach ((int index, (string? gold, string? blue, string? black)) in expected)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-34C", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"Gold at index {index}");
      Assert.AreEqual(blue, visual.BlueShift, $"Blue at index {index}");
      Assert.AreEqual(black, visual.BlackShift, $"Black at index {index}");
    }

    // →D / →R use vector arrow + suffix (not Arial → / not expanded →DEG/→RAD).
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("\u2192D"));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("\u2192R"));
    // ∫yx CapAbove on 0-key — ∫ must not fall through to Arial (?).
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("\u222Byx"));

    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-34C");
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G));
    Assert.AreEqual(CalcLabelAnchor.CapSkirt, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.H));

    HpCalcKeyVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-34C", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    CalcKeyVisual enterVisual = CalcKeyVisual.FromLegacy(enter, CalcButtonStyle.Black, CalcButtonKind.EnterWide, model);
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "MEM", Align: CalcLabelAlign.Left }));
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapAbove, Text: "PREFIX", Align: CalcLabelAlign.Right }));
    Assert.IsTrue(enterVisual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.H, Anchor: CalcLabelAnchor.CapSkirt, Text: "MANT" }));
  }

  [TestMethod]
  public void CapAbove_XExchangeI_UsesCardSlotChevrons()
  {
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("x\u2194I"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("x\u2194(i)"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("x\u2194y"));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x\u2194I"));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x\u2194(i)"));

    float fontSize = 14f;
    float cardSlotConnector = HpClassicFaceplateGlyphs.MeasureCardSlotExchangeConnector(fontSize);
    float stackedArrowConnector = fontSize * 0.98f;
    Assert.IsTrue(cardSlotConnector < stackedArrowConnector);
  }

  [TestMethod]
  public void DualCapAbove_Inset_PullsLegendsInward()
  {
    Assert.AreEqual(70f, CalcLabelAlignMetrics.DualCapAboveInset(14f));
  }

  [TestMethod]
  public void TrigCapAbove_SpaceSavingGoldBasePlusBlueInverseSuffix()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-34C");

    foreach (int index in new[] { 16, 17, 18 })
    {
      HpCalcKeyVisual legacy = ClassicKeyFaceplateLegend.Resolve(
        "HP-34C", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.IsTrue(CalcCapAboveComposite.IsSpaceSavingInverse(legacy.GoldShift, legacy.BlueShift), $"Index {index}");
      Assert.AreEqual("^-1", legacy.BlueShift, $"Blue suffix at {index}");

      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        legacy, CalcButtonStyle.White, CalcButtonKind.Standard, model);
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Align: CalcLabelAlign.Center }));
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapAbove, Text: "^-1", Align: CalcLabelAlign.Center }));
    }

    Assert.AreEqual("COS^-1", CalcCapAboveComposite.ComposeInversePreviewFace("COS", "^-1"));
  }

  [TestMethod]
  public void CapSkirtInk_HShift_LightOnBlackKeys_DarkOnWhiteKeys()
  {
    Assert.AreEqual(
      CalcChassisPalette.KeyText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.Black));
    Assert.AreEqual(
      CalcChassisPalette.KeyText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.Blue));
    Assert.AreEqual(
      CalcChassisPalette.KeyCapDarkText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.White));
    Assert.AreEqual(
      CalcChassisPalette.KeyCapDarkText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.Orange));

    // HP-34C must not force dark skirt ink via UsesBlackGShiftSkirtInk (black-on-black).
    Assert.IsFalse(CalcKeyLabelPalette.UsesBlackGShiftSkirtInk("HP-34C"));
    Assert.IsFalse(CalcKeyLabelPalette.UsesBlackGShiftSkirtInk("HP-34"));

    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-34C");

    HpCalcKeyVisual blackKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-34C", "Spice", vocabulary.KeyChart[0], vocabulary, FaceplateLabelStyle.Normal);
    CalcKeyVisual blackVisual = CalcKeyVisual.FromLegacy(
      blackKey, CalcButtonStyle.Black, CalcButtonKind.Standard, model);
    Assert.AreEqual("DSP I", blackVisual.Annotations.Single(a => a.Modifier == CalcModifierKey.H).Text);
    Assert.AreEqual(CalcChassisPalette.KeyText, blackVisual.CapSkirtInkOverride);

    HpCalcKeyVisual whiteKey = ClassicKeyFaceplateLegend.Resolve(
      "HP-34C", "Spice", vocabulary.KeyChart[16], vocabulary, FaceplateLabelStyle.Normal);
    CalcKeyVisual whiteVisual = CalcKeyVisual.FromLegacy(
      whiteKey, CalcButtonStyle.White, CalcButtonKind.Standard, model);
    Assert.AreEqual("\u0394%", whiteVisual.Annotations.Single(a => a.Modifier == CalcModifierKey.H).Text);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, whiteVisual.CapSkirtInkOverride);
  }

  [TestMethod]
  public void KeyColors_GoldF_BlueG_BlackH_WhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 3));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 4));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 9));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 0));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 10));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Spice", "HP-34C", 16));
  }

  [TestMethod]
  public void ShiftPreview_FramesGoldF_BlueG_BlackH()
  {
    Assert.AreEqual(3, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Spice", "HP-34C"));
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Spice", "HP-34C"));
    Assert.AreEqual(9, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Black, "Spice", "HP-34C"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(3, "Spice", "HP-34C");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(9, "Spice", "HP-34");
    Assert.AreEqual(ShiftPreviewMode.Black, preview.Mode);
  }

  [TestMethod]
  public void CatalogAlias_Hp34CMapsToEngineHp34()
  {
    Assert.AreEqual("HP-34", CalcModelIds.ToEngineId("HP-34C"));
    Assert.AreEqual("34C", CalcModelIds.ToShortId("HP-34C"));
  }

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-34", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(10, spec.LeftKey);
    Assert.AreEqual(14, spec.RightKey);
    Assert.AreEqual(12, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);
    Assert.AreEqual(CalcBracketLegendComponent.LeftEdgeAlign.MidKey, spec.LeftEdge);
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-34C", out _));
  }

  [TestMethod]
  public void ClearBracket_LeftRuleStartsAtEnterMid()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-34", out CalcBracketLegendComponent.Spec spec));
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-34C");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-34", model);
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
    // ENTER CapAbove: f=MEM (gold), g=PREFIX (blue dual CapAbove); CapSkirt h=MANT.
    HpCalcKeyVisual enter = ClassicKeyFaceplateLegend.Resolve(
      "HP-34", "Spice", vocabulary.KeyChart[10], vocabulary, FaceplateLabelStyle.Normal);
    Assert.AreEqual("MEM", enter.GoldShift);
    Assert.AreEqual("PREFIX", enter.BlueShift);

    Dictionary<int, string> expectedGold = new()
    {
      [12] = "PRGM",
      [13] = "REG",
      [14] = "\u03a3",
    };
    foreach ((int index, string gold) in expectedGold)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-34", "Spice", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-34C");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Spice", "HP-34", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Spice", "HP-34");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-34");
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
