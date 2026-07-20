using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp67FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-67/Program/program.vocabulary.json"));

  private static HpCalcKeyVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-67", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  private static CalcKeyVisual KeyVisualAt(ProgramVocabulary vocabulary, int index)
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-67");
    CalcButtonStyle style = CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", index);
    return CalcKeyVisual.FromLegacy(VisualAt(vocabulary, index), style, CalcButtonKind.Standard, model);
  }

  [TestMethod]
  public void Family_IsClassic_ProductLabel_T67()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-67"));
    Assert.AreEqual("T-67", CalcModelIds.ToProductLabel("HP-67"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-67/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_IncludeFgH_AndWideEnter()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-67");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 10), "f CapFace");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 11), "g CapFace");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 14), "h CapFace");
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 15);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 16));
  }

  [TestMethod]
  public void CapFace_PrimaryLegends_MatchFinsethBase()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedCapFace = new()
    {
      [0] = "A",
      [1] = "B",
      [2] = "C",
      [3] = "D",
      [4] = "E",
      [5] = "\u03a3+",
      [6] = "GTO",
      [7] = "DSP",
      [8] = "(i)",
      [9] = "SST",
      [10] = "f",
      [11] = "g",
      [12] = "STO",
      [13] = "RCL",
      [14] = "h",
      [15] = "ENTER",
      [17] = "CHS",
      [18] = "EEX",
      [19] = "CL X",
      [20] = "-",
      [21] = "7",
      [25] = "+",
      [30] = "\u00d7",
      [35] = "\u00f7",
      [36] = "0",
      [37] = "\u00b7",
      [38] = "R/S",
    };

    foreach ((int index, string label) in expectedCapFace)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-67"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapBelowCapSkirt_MatchFinsethFgh()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue, string? Black)> expected = new()
    {
      [0] = ("a", null, null),
      [5] = ("x\u0305", "s", "\u03a3-"),
      [6] = ("GSB", "f", "RTN"),
      [9] = ("LBL", "f", "BST"),
      [10] = (null, null, null),
      [14] = (null, null, null),
      [15] = ("W/DATA", "MERGE", "DEG"),
      [17] = ("P\u2194S", null, "RAD"),
      [19] = ("CL PRGM", null, "DEL"),
      [26] = ("SIN", "^-1", "1/x"),
      [27] = ("COS", "^-1", "y^x"),
      [28] = ("TAN", "^-1", "ABS"),
      [38] = ("-x-", "STK", "SPACE"),
    };

    foreach ((int index, (string? gold, string? blue, string? black)) in expected)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      if (gold is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"Gold empty {index}");
      }
      else
      {
        Assert.AreEqual(gold, visual.GoldShift, $"Gold CapBelow {index}");
      }

      if (blue is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue empty {index}");
      }
      else
      {
        Assert.AreEqual(blue, visual.BlueShift, $"Blue CapBelow {index}");
      }

      if (black is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"Black empty {index}");
      }
      else
      {
        Assert.AreEqual(black, visual.BlackShift, $"Black CapSkirt {index}");
      }
    }
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("CL X", VisualAt(vocabulary, 19).Primary);
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CL X"));
  }

  [TestMethod]
  public void Annotation_Fgh_CapBelowPlusCapSkirt_NoCapAbove()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-67");
    Assert.AreEqual(CalcLabelAnchor.CapBelow, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F));
    Assert.AreEqual(CalcLabelAnchor.CapBelow, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G));
    Assert.AreEqual(CalcLabelAnchor.CapSkirt, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.H));
    Assert.IsFalse(CalcModifierPlacement.ReservesCapAboveBand(model));
    Assert.IsTrue(CalcModifierPlacement.ReservesCapBelowBand(model));
    Assert.IsTrue(model.ModifierKeys.Contains(CalcModifierKey.F));
    Assert.IsTrue(model.ModifierKeys.Contains(CalcModifierKey.G));
    Assert.IsTrue(model.ModifierKeys.Contains(CalcModifierKey.H));
  }

  [TestMethod]
  public void KeyPanel_NoCapAbove_ReclaimsAboveIntoCapBelow()
  {
    CalcModelDefinition hp67 = CalcModelCatalog.Resolve("HP-67");
    IReadOnlyList<FaceplateCell> cells67 = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-67");
    CalcKeyPanelComponent.PanelMetrics metrics67 = CalcKeyPanelComponent.Measure(cells67, model: hp67);
    Assert.AreEqual(0f, metrics67.LabelAbove, "HP-67 must not reserve empty CapAbove");
    Assert.AreEqual(CalcKeyPanelComponent.LabelBelowRef, metrics67.LabelBelow, "CapBelow band budget");
    Assert.IsTrue(metrics67.LabelBelow > 0f);
    Assert.AreEqual(
      metrics67.LabelAbove + CalcKeyPanelComponent.PreferredCapHeightRef + metrics67.LabelBelow,
      metrics67.CellHeight,
      "Row pitch = CapFace + CapBelow (no CapAbove)");

    CalcModelDefinition hp65 = CalcModelCatalog.Hp65;
    IReadOnlyList<FaceplateCell> cells65 = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-65");
    CalcKeyPanelComponent.PanelMetrics metrics65 = CalcKeyPanelComponent.Measure(cells65, model: hp65);
    Assert.AreEqual(CalcKeyPanelComponent.LabelAboveRef, metrics65.LabelAbove);
    Assert.AreEqual(0f, metrics65.LabelBelow);
    // Same total band budget as CapAbove models — relocated below the cap.
    Assert.AreEqual(metrics65.CellHeight, metrics67.CellHeight);
  }

  [TestMethod]
  public void CapBelow_DualLeftGold_RightBlue_Align()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-67");
    foreach ((int index, string gold, string blue) in new[]
    {
      (5, "x\u0305", "s"),
      (6, "GSB", "f"),
      (9, "LBL", "f"),
      (15, "W/DATA", "MERGE"),
      (38, "-x-", "STK"),
    })
    {
      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        VisualAt(vocabulary, index),
        CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", index),
        CalcButtonKind.Standard,
        model);
      Assert.AreEqual(
        0,
        visual.Annotations.Count(a => a.Anchor == CalcLabelAnchor.CapAbove && !string.IsNullOrEmpty(a.Text)),
        $"CapAbove empty {index}");
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapBelow, Align: CalcLabelAlign.Left, Text: var t }
          && t == gold),
        $"Gold CapBelow left {index}");
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapBelow, Align: CalcLabelAlign.Right, Text: var t }
          && t == blue),
        $"Blue CapBelow right {index}");
    }
  }

  [TestMethod]
  public void CapAbove_CountIsZero_AllShiftLegendsOnCapBelowOrSkirt()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    for (int index = 0; index < vocabulary.KeyChart.Count; index++)
    {
      CalcKeyVisual visual = KeyVisualAt(vocabulary, index);
      Assert.AreEqual(
        0,
        visual.Annotations.Count(a => a.Anchor == CalcLabelAnchor.CapAbove && !string.IsNullOrEmpty(a.Text)),
        $"CapAbove must be empty at {index}");

      HpCalcKeyVisual legacy = VisualAt(vocabulary, index);
      if (!string.IsNullOrEmpty(legacy.GoldShift))
      {
        Assert.IsTrue(
          visual.Annotations.Any(a =>
            a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapBelow, Text: var t }
            && t == legacy.GoldShift),
          $"Gold CapBelow {index}");
      }

      if (!string.IsNullOrEmpty(legacy.BlueShift))
      {
        Assert.IsTrue(
          visual.Annotations.Any(a =>
            a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapBelow, Text: var t }
            && t == legacy.BlueShift),
          $"Blue CapBelow {index}");
      }

      if (!string.IsNullOrEmpty(legacy.BlackShift))
      {
        Assert.IsTrue(
          visual.Annotations.Any(a =>
            a is { Modifier: CalcModifierKey.H, Anchor: CalcLabelAnchor.CapSkirt, Text: var t }
            && t == legacy.BlackShift),
          $"Black CapSkirt {index}");
      }
    }
  }

  [TestMethod]
  public void KeyColors_OrangeF_BlueG_BlackH_OliveBody_WhitePadAndRs()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 10));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 11));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 14));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 0));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 15));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 20));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 21));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", 38));
    Assert.AreEqual(CalcChassisPalette.KeyText, CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Olive));
    Assert.AreEqual(CalcChassisPalette.KeyText, CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Black));
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.White));
  }

  [TestMethod]
  public void CapSkirtInk_AlwaysBlack_EvenOnOlive()
  {
    Assert.IsTrue(CalcKeyLabelPalette.UsesBlackHShiftSkirtInk("HP-67"));
    Assert.AreEqual(
      CalcChassisPalette.KeyCapDarkText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.Olive, "HP-67"));
    Assert.AreEqual(
      CalcChassisPalette.KeyCapDarkText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.White, "HP-67"));
    Assert.AreEqual(
      CalcChassisPalette.KeyCapDarkText,
      CalcKeyLabelPalette.HShiftSkirtInk(CalcButtonStyle.Black, "HP-67"));
    // CapFace olive stays light; skirt-only override.
    Assert.AreEqual(CalcChassisPalette.KeyText, CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Olive));
    Assert.AreEqual(CalcChassisPalette.KeyText, CalcKeyLabelPalette.SkirtOnCap(CalcButtonStyle.Olive));

    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcKeyVisual olive = KeyVisualAt(vocabulary, 15);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, olive.CapSkirtInkOverride);
    CalcKeyVisual white = KeyVisualAt(vocabulary, 26);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, white.CapSkirtInkOverride);
  }

  [TestMethod]
  public void ShiftPreview_FghAtIndices10_11_14()
  {
    Assert.AreEqual(10, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-67"));
    Assert.AreEqual(11, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Classic", "HP-67"));
    Assert.AreEqual(14, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Black, "Classic", "HP-67"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(10, "Classic", "HP-67"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(11, "Classic", "HP-67"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(14, "Classic", "HP-67"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(10, "Classic", "HP-67");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(11, "Classic", "HP-67");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
    preview.HandleKeyPress(14, "Classic", "HP-67");
    Assert.AreEqual(ShiftPreviewMode.Black, preview.Mode);
  }

  [TestMethod]
  public void TrigCapBelow_GoldBasePlusBlueInverseSuffix_SpaceSavingDualInk()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-67");
    foreach ((int index, string baseName) in new[] { (26, "SIN"), (27, "COS"), (28, "TAN") })
    {
      HpCalcKeyVisual legacy = VisualAt(vocabulary, index);
      Assert.AreEqual(baseName, legacy.GoldShift, $"Gold base {index}");
      Assert.AreEqual("^-1", legacy.BlueShift, $"Blue suffix {index}");
      Assert.IsTrue(
        CalcCapAboveComposite.IsSpaceSavingInverse(legacy.GoldShift, legacy.BlueShift),
        $"Space-saving inverse {index}");

      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        legacy, CalcButtonStyle.White, CalcButtonKind.Standard, model);
      Assert.AreEqual(
        0,
        visual.Annotations.Count(a => a.Anchor == CalcLabelAnchor.CapAbove),
        $"No CapAbove trig {index}");
      // Centered dual-ink unit (not left/right split) — Finseth CapBelow space-saving.
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapBelow, Text: var t }
          && t == baseName
          && a.Align == CalcLabelAlign.Center),
        $"Gold CapBelow center {index}");
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapBelow, Text: "^-1" }
          && a.Align == CalcLabelAlign.Center),
        $"Blue CapBelow center {index}");
      // CapSkirt remains h-shift (1/x, y^x, ABS) — not the trig dual-ink.
      Assert.IsFalse(string.IsNullOrEmpty(legacy.BlackShift), $"Black CapSkirt {index}");
    }
  }

  [TestMethod]
  public void Digits123_CapBelow_UnitConversion_DualInkArrowStack()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-67");
    (int Index, string Left, string Right)[] pairs =
    [
      (31, "R", "P"),
      (32, "D", "R"),
      (33, "H", "H.MS"),
    ];

    foreach ((int index, string left, string right) in pairs)
    {
      HpCalcKeyVisual legacy = VisualAt(vocabulary, index);
      Assert.AreEqual($"{left}\u2192", legacy.GoldShift, $"Gold {index}");
      Assert.AreEqual($"\u2190{right}", legacy.BlueShift, $"Blue {index}");
      Assert.IsTrue(
        CalcCapAboveComposite.TryParseUnitConversionPair(
          legacy.GoldShift, legacy.BlueShift, out string parsedLeft, out string parsedRight),
        $"Parse CapBelow unit {index}");
      Assert.AreEqual(left, parsedLeft, $"Left {index}");
      Assert.AreEqual(right, parsedRight, $"Right {index}");
      Assert.IsTrue(
        CalcCapAboveComposite.IsSpaceSavingDualInk(legacy.GoldShift, legacy.BlueShift),
        $"Dual-ink CapBelow {index}");
      Assert.AreEqual(
        $"{left}\u2192",
        CalcCapAboveComposite.ComposeUnitConversionPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: false));
      Assert.AreEqual(
        $"\u2190{right}",
        CalcCapAboveComposite.ComposeUnitConversionPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: true));

      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        legacy, CalcButtonStyle.White, CalcButtonKind.Standard, model);
      Assert.AreEqual(
        0,
        visual.Annotations.Count(a => a.Anchor == CalcLabelAnchor.CapAbove),
        $"No CapAbove {index}");
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapBelow, Align: CalcLabelAlign.Center }),
        $"Gold CapBelow centered {index}");
      Assert.IsTrue(
        visual.Annotations.Any(a =>
          a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapBelow, Align: CalcLabelAlign.Center }),
        $"Blue CapBelow centered {index}");
    }
  }

  [TestMethod]
  public void EnterRow_NotHp65PrefixStkRegPrgm()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("W/DATA", VisualAt(vocabulary, 15).GoldShift);
    Assert.AreEqual("P\u2194S", VisualAt(vocabulary, 17).GoldShift);
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("P\u2194S"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("x\u2194y"));
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("P\u2194S"));
    // CapBelow P↔S chevrons share the same card-slot glyph height / connector as CapFace x↔y.
    const float fontSize = 14f;
    float connector = HpClassicFaceplateGlyphs.MeasureCardSlotExchangeConnector(fontSize);
    Assert.AreEqual(
      CardSlotExchangeArt.MeasureWidth(HpClassicFaceplateGlyphs.CardSlotExchangeGlyphHeight(fontSize)),
      connector);
    Assert.IsTrue(connector > 0f);
    Assert.AreEqual("CL REG", VisualAt(vocabulary, 18).GoldShift);
    Assert.AreEqual("CL PRGM", VisualAt(vocabulary, 19).GoldShift);
    Assert.AreNotEqual("PREFIX", VisualAt(vocabulary, 15).GoldShift);
  }

  [TestMethod]
  public void IndexTable_CapFaceCapBelowStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    (int Index, string CapFace, string? CapBelowGold, string? CapBelowBlue, string? CapSkirt, CalcButtonStyle Style)[] rows =
    [
      (0, "A", "a", null, null, CalcButtonStyle.Olive),
      (5, "\u03a3+", "x\u0305", "s", "\u03a3-", CalcButtonStyle.Olive),
      (6, "GTO", "GSB", "f", "RTN", CalcButtonStyle.Olive),
      (9, "SST", "LBL", "f", "BST", CalcButtonStyle.Olive),
      (10, "f", null, null, null, CalcButtonStyle.Orange),
      (11, "g", null, null, null, CalcButtonStyle.Blue),
      (14, "h", null, null, null, CalcButtonStyle.Black),
      (15, "ENTER", "W/DATA", "MERGE", "DEG", CalcButtonStyle.Olive),
      (17, "CHS", "P\u2194S", null, "RAD", CalcButtonStyle.Olive),
      (26, "4", "SIN", "^-1", "1/x", CalcButtonStyle.White),
      (31, "1", "R\u2192", "\u2190P", "PAUSE", CalcButtonStyle.White),
      (32, "2", "D\u2192", "\u2190R", "\u03c0", CalcButtonStyle.White),
      (33, "3", "H\u2192", "\u2190H.MS", "REG", CalcButtonStyle.White),
      (38, "R/S", "-x-", "STK", "SPACE", CalcButtonStyle.White),
    ];

    foreach ((int index, string capFace, string? gold, string? blue, string? black, CalcButtonStyle style) in rows)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(capFace, visual.Primary, $"CapFace {index}");
      if (gold is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"Gold empty {index}");
      }
      else
      {
        Assert.AreEqual(gold, visual.GoldShift, $"Gold CapBelow {index}");
      }

      if (blue is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue empty {index}");
      }
      else
      {
        Assert.AreEqual(blue, visual.BlueShift, $"Blue CapBelow {index}");
      }

      if (black is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"Black empty {index}");
      }
      else
      {
        Assert.AreEqual(black, visual.BlackShift, $"Black CapSkirt {index}");
      }

      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-67", index), $"Style {index}");

      CalcKeyVisual drawn = KeyVisualAt(vocabulary, index);
      Assert.AreEqual(
        0,
        drawn.Annotations.Count(a => a.Anchor == CalcLabelAnchor.CapAbove && !string.IsNullOrEmpty(a.Text)),
        $"CapAbove empty {index}");
    }
  }
}
