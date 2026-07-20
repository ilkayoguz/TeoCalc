using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp55FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-55/Program/program.vocabulary.json"));

  private static HpCalcKeyVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-55", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  [TestMethod]
  public void Family_IsClassic_ProductLabel_T55()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-55"));
    Assert.AreEqual("T-55", CalcModelIds.ToProductLabel("HP-55"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-55/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_IncludeBst_Index4()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-55");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 4), "BST CapFace requires physical cell index 4");
    FaceplateCell bst = cells.Single(c => c.KeyChartIndex == 4);
    Assert.AreEqual(0, bst.Row);
    Assert.AreEqual(4, bst.Column);

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
      [0] = "\u03a3+",
      [1] = "y^x",
      [2] = "1/x",
      [3] = "%",
      [4] = "BST",
      [5] = "y\u0302",
      [6] = "x\u2194y",
      [7] = "R\u2193",
      [8] = "FIX",
      [9] = "SST",
      [10] = "f",
      [11] = "g",
      [12] = "STO",
      [13] = "RCL",
      [14] = "GTO",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-55"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapAbove_DualFg_MatchFinseth()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, (string? Gold, string? Blue)> expected = new()
    {
      [0] = ("\u03a3-", null),
      [1] = ("sin", "^-1"),
      [2] = ("cos", "^-1"),
      [3] = ("tan", "^-1"),
      [5] = ("L.R.", null),
      [6] = ("ln", "e^x"),
      [7] = ("log", "10^x"),
      [8] = ("SCI", null),
      [12] = ("x\u0305", "s"),
      [13] = ("LST X", null),
      [14] = ("x\u2264y", "x=y"),
      [15] = ("H.MS", "+-"),
      [17] = ("\u221ax", "x\u00b2"),
      [18] = ("n!", null),
      [19] = ("CLR", "CL.R"),
      [20] = ("DEG", null),
      [21] = ("ln\u2192", "\u2190mm"),
      [22] = ("ft\u2192", "\u2190m"),
      [23] = ("gal\u2192", "\u2190l"),
      [25] = ("RAD", null),
      [26] = ("lbm\u2192", "\u2190kg"),
      [27] = ("lbf\u2192", "\u2190N"),
      [28] = ("\u00b0F\u2192", "\u2190\u00b0C"),
      [30] = ("GRD", null),
      [31] = ("H\u2192", "\u2190H.MS"),
      [32] = ("D\u2192", "\u2190R"),
      [33] = ("Btu\u2192", "\u2190J"),
      [36] = ("R\u2192", "\u2190P"),
      [37] = ("\u03c0", null),
    };

    foreach ((int index, (string? gold, string? blue)) in expected)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      if (gold is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"Gold empty {index}");
      }
      else
      {
        Assert.AreEqual(gold, visual.GoldShift, $"Gold CapAbove {index}");
      }

      if (blue is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue empty {index}");
      }
      else
      {
        Assert.AreEqual(blue, visual.BlueShift, $"Blue CapAbove {index}");
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
  public void Annotation_DualCapAbove_Fg()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-55");
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F));
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G));
    Assert.IsTrue(model.ModifierKeys.Contains(CalcModifierKey.F));
    Assert.IsTrue(model.ModifierKeys.Contains(CalcModifierKey.G));
    Assert.IsFalse(model.ModifierKeys.Contains(CalcModifierKey.H));
  }

  [TestMethod]
  public void KeyColors_LightGreyOliveOrangeBlueBlackWhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.LightGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 0));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 4));
    Assert.AreEqual(CalcButtonStyle.Olive, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 9));
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 10));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 11));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 14));
    Assert.AreEqual(CalcButtonStyle.LightGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 15));
    Assert.AreEqual(CalcButtonStyle.LightGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 20));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 21));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", 38));
  }

  [TestMethod]
  public void ShiftPreview_FgAtIndices10And11()
  {
    Assert.AreEqual(10, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-55"));
    Assert.AreEqual(11, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Classic", "HP-55"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(10, "Classic", "HP-55"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(11, "Classic", "HP-55"));
    Assert.IsFalse(ShiftPreviewController.IsShiftPrefixKey(4, "Classic", "HP-55"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(10, "Classic", "HP-55");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
    preview.HandleKeyPress(11, "Classic", "HP-55");
    Assert.AreEqual(ShiftPreviewMode.Blue, preview.Mode);
  }

  [TestMethod]
  public void TrigCapAbove_SpaceSavingGoldBasePlusBlueInverseSuffix()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-55");

    foreach ((int index, string baseName) in new[] { (1, "sin"), (2, "cos"), (3, "tan") })
    {
      HpCalcKeyVisual legacy = VisualAt(vocabulary, index);
      Assert.IsTrue(
        CalcCapAboveComposite.IsSpaceSavingInverse(legacy.GoldShift, legacy.BlueShift),
        $"Index {index}");
      Assert.AreEqual(baseName, legacy.GoldShift, $"Gold base {index}");
      Assert.AreEqual("^-1", legacy.BlueShift, $"Blue suffix {index}");

      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        legacy, CalcButtonStyle.LightGrey, CalcButtonKind.Standard, model);
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Align: CalcLabelAlign.Center }));
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapAbove, Text: "^-1", Align: CalcLabelAlign.Center }));
    }

    Assert.AreEqual("cos^-1", CalcCapAboveComposite.ComposeInversePreviewFace("cos", "^-1"));
    Assert.AreEqual("SIN^-1", CalcCapAboveComposite.ComposeInversePreviewFace("SIN", "^-1"));
  }

  [TestMethod]
  public void EnterCapAbove_SpaceSavingHmsPlusMinus()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-55");
    HpCalcKeyVisual legacy = VisualAt(vocabulary, 15);
    Assert.AreEqual("H.MS", legacy.GoldShift);
    Assert.AreEqual("+-", legacy.BlueShift);
    Assert.IsTrue(CalcCapAboveComposite.IsSpaceSavingHmsPlusMinus(legacy.GoldShift, legacy.BlueShift));
    Assert.IsTrue(CalcCapAboveComposite.IsSpaceSavingDualInk(legacy.GoldShift, legacy.BlueShift));
    Assert.AreEqual(
      "H.MS+",
      CalcCapAboveComposite.ComposeHmsPlusMinusPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: false));
    Assert.AreEqual(
      "H.MS-",
      CalcCapAboveComposite.ComposeHmsPlusMinusPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: true));

    CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
      legacy, CalcButtonStyle.LightGrey, CalcButtonKind.EnterWide, model);
    Assert.IsTrue(visual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "H.MS", Align: CalcLabelAlign.Center }));
    Assert.IsTrue(visual.Annotations.Any(a =>
      a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapAbove, Text: "+-", Align: CalcLabelAlign.Center }));
  }

  [TestMethod]
  public void DigitCapAbove_SpaceSavingUnitConversionPairs()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-55");
    (int Index, string Left, string Right)[] pairs =
    [
      (21, "ln", "mm"),
      (22, "ft", "m"),
      (23, "gal", "l"),
      (26, "lbm", "kg"),
      (27, "lbf", "N"),
      (28, "\u00b0F", "\u00b0C"),
      (31, "H", "H.MS"),
      (32, "D", "R"),
      (33, "Btu", "J"),
      (36, "R", "P"),
    ];

    foreach ((int index, string left, string right) in pairs)
    {
      HpCalcKeyVisual legacy = VisualAt(vocabulary, index);
      Assert.AreEqual($"{left}\u2192", legacy.GoldShift, $"Gold {index}");
      Assert.AreEqual($"\u2190{right}", legacy.BlueShift, $"Blue {index}");
      Assert.IsTrue(
        CalcCapAboveComposite.TryParseUnitConversionPair(
          legacy.GoldShift, legacy.BlueShift, out string parsedLeft, out string parsedRight),
        $"Parse {index}");
      Assert.AreEqual(left, parsedLeft, $"Left {index}");
      Assert.AreEqual(right, parsedRight, $"Right {index}");
      Assert.IsTrue(CalcCapAboveComposite.IsSpaceSavingDualInk(legacy.GoldShift, legacy.BlueShift), $"Dual {index}");
      Assert.AreEqual(
        $"{left}\u2192",
        CalcCapAboveComposite.ComposeUnitConversionPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: false));
      Assert.AreEqual(
        $"\u2190{right}",
        CalcCapAboveComposite.ComposeUnitConversionPreviewFace(legacy.GoldShift, legacy.BlueShift, blueShift: true));

      CalcKeyVisual visual = CalcKeyVisual.FromLegacy(
        legacy, CalcButtonStyle.White, CalcButtonKind.Standard, model);
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Align: CalcLabelAlign.Center }),
        $"Gold centered {index}");
      Assert.IsTrue(visual.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapAbove, Align: CalcLabelAlign.Center }),
        $"Blue centered {index}");
    }
  }

  [TestMethod]
  public void IndexTable_CapFaceCapAboveStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    (int Index, string CapFace, string? CapAbove, string? CapAboveBlue, CalcButtonStyle Style)[] rows =
    [
      (0, "\u03a3+", "\u03a3-", null, CalcButtonStyle.LightGrey),
      (1, "y^x", "sin", "^-1", CalcButtonStyle.LightGrey),
      (4, "BST", null, null, CalcButtonStyle.Olive),
      (5, "y\u0302", "L.R.", null, CalcButtonStyle.LightGrey),
      (10, "f", null, null, CalcButtonStyle.Orange),
      (11, "g", null, null, CalcButtonStyle.Blue),
      (14, "GTO", "x\u2264y", "x=y", CalcButtonStyle.Black),
      (15, "ENTER", "H.MS", "+-", CalcButtonStyle.LightGrey),
      (19, "CL X", "CLR", "CL.R", CalcButtonStyle.LightGrey),
      (20, "-", "DEG", null, CalcButtonStyle.LightGrey),
      (21, "7", "ln\u2192", "\u2190mm", CalcButtonStyle.White),
      (38, "R/S", null, null, CalcButtonStyle.Black),
    ];

    foreach ((int index, string capFace, string? gold, string? blue, CalcButtonStyle style) in rows)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(capFace, visual.Primary, $"CapFace {index}");
      if (gold is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"Gold empty {index}");
      }
      else
      {
        Assert.AreEqual(gold, visual.GoldShift, $"Gold {index}");
      }

      if (blue is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue empty {index}");
      }
      else
      {
        Assert.AreEqual(blue, visual.BlueShift, $"Blue {index}");
      }

      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-55", index), $"Style {index}");
    }
  }
}
