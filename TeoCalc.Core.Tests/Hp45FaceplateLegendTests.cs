using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp45FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-45/Program/program.vocabulary.json"));

  private static HpCalcKeyVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-45", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  [TestMethod]
  public void Family_IsClassic_ProductLabel_T45()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-45"));
    Assert.AreEqual("T-45", CalcModelIds.ToProductLabel("HP-45"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-45/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_IncludeGold_Index4()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-45");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 4), "gold CapFace requires physical cell index 4");
    FaceplateCell gold = cells.Single(c => c.KeyChartIndex == 4);
    Assert.AreEqual(0, gold.Row);
    Assert.AreEqual(4, gold.Column);

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
      [0] = "1/x",
      [1] = "ln",
      [2] = "e^x",
      [3] = "FIX",
      [4] = "", // gold prefix — blank CapFace
      [5] = "x\u00b2",
      [6] = "\u2192P",
      [7] = "SIN",
      [8] = "COS",
      [9] = "TAN",
      [10] = "x\u2194y",
      [11] = "R\u2193",
      [12] = "STO",
      [13] = "RCL",
      [14] = "%",
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
      [38] = "\u03a3+",
    };

    foreach ((int index, string label) in expectedCapFace)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-45"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapAbove_GoldLegends_MatchFinseth()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedCapAbove = new()
    {
      [0] = "y^x",
      [1] = "log",
      [2] = "10^x",
      [3] = "SCI",
      [5] = "\u221ax",
      [6] = "\u2192R",
      [7] = "SIN^-1",
      [8] = "COS^-1",
      [9] = "TAN^-1",
      [10] = "n!",
      [11] = "x\u0305,s",
      [12] = "\u2192D.MS",
      [13] = "D.MS\u2192",
      [14] = "\u0394%",
      [15] = "DEG",
      [17] = "RAD",
      [18] = "GRD",
      [19] = "CLEAR",
      [21] = "cm/in",
      [22] = "kg/lb",
      [23] = "ltr/gal",
      [36] = "LST X",
      [37] = "\u03c0",
      [38] = "\u03a3-",
    };

    foreach ((int index, string label) in expectedCapAbove)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(label, visual.GoldShift, $"CapAbove index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"No CapSkirt/blue at {index}");
    }

    Assert.IsTrue(string.IsNullOrEmpty(VisualAt(vocabulary, 4).GoldShift), "gold key has no CapAbove");
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("CL X", VisualAt(vocabulary, 19).Primary);
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CL X"));
  }

  [TestMethod]
  public void Annotation_GoldCapAboveOnly_NoBlueG()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-45");
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F));
    Assert.IsTrue(
      CalcModifierPlacement.TryGetInkToken(model, CalcModifierKey.F, CalcLabelAnchor.CapAbove, out CalcColorToken ink));
    Assert.AreEqual(CalcFaceplateTokens.ModifierFCapAboveColor, ink.Name);
    Assert.IsFalse(model.ModifierKeys.Contains(CalcModifierKey.G));
  }

  [TestMethod]
  public void KeyColors_GoldTrigDarkGreyGreyEnterWhitePad()
  {
    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 4));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 0));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 5));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 7));
    Assert.AreEqual(CalcButtonStyle.Grey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 15));
    Assert.AreEqual(CalcButtonStyle.Grey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 20));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 21));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", 38));
  }

  [TestMethod]
  public void ShiftPreview_GoldPrefixAtIndex4()
  {
    Assert.AreEqual(4, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-45"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Classic", "HP-45"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(4, "Classic", "HP-45"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(4, "Classic", "HP-45");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }

  [TestMethod]
  public void IndexTable_CapFaceCapAboveStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    (int Index, string CapFace, string? CapAbove, CalcButtonStyle Style)[] rows =
    [
      (0, "1/x", "y^x", CalcButtonStyle.DarkGrey),
      (4, "", null, CalcButtonStyle.Orange),
      (5, "x\u00b2", "\u221ax", CalcButtonStyle.DarkGrey),
      (7, "SIN", "SIN^-1", CalcButtonStyle.Black),
      (14, "%", "\u0394%", CalcButtonStyle.DarkGrey),
      (15, "ENTER", "DEG", CalcButtonStyle.Grey),
      (19, "CL X", "CLEAR", CalcButtonStyle.Grey),
      (21, "7", "cm/in", CalcButtonStyle.White),
      (37, "\u00b7", "\u03c0", CalcButtonStyle.White),
      (38, "\u03a3+", "\u03a3-", CalcButtonStyle.White),
    ];

    foreach ((int index, string capFace, string? capAbove, CalcButtonStyle style) in rows)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(capFace, visual.Primary, $"CapFace {index}");
      if (capAbove is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"CapAbove empty {index}");
      }
      else
      {
        Assert.AreEqual(capAbove, visual.GoldShift, $"CapAbove {index}");
      }

      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-45", index), $"Style {index}");
    }
  }
}
