using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp70FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-70/Program/program.vocabulary.json"));

  private static KeyLegendVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-70", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  [TestMethod]
  public void Family_IsClassic_ProductLabel_T70()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-70"));
    Assert.AreEqual("T-70", CalcModelIds.ToProductLabel("HP-70"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/T-70/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_RestoreFv_Index4()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-70");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 4), "FV CapFace requires physical cell index 4");
    FaceplateCell fv = cells.Single(c => c.KeyChartIndex == 4);
    Assert.AreEqual(0, fv.Row);
    Assert.AreEqual(4, fv.Column);
    Assert.AreEqual(35, cells.Count);

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
      [0] = "n",
      [1] = "i",
      [2] = "PMT",
      [3] = "PV",
      [4] = "FV",
      [5] = "INT",
      [6] = "%",
      [7] = "\u0394%",
      [8] = "y^x",
      [9] = "CLR",
      [10] = "x\u2194y",
      [11] = "R\u2193",
      [12] = "STO",
      [13] = "K",
      [14] = "DSP",
      [15] = "ENTER",
      [17] = "CHS",
      [18] = "M",
      [19] = "M+",
      [20] = "-",
      [21] = "7",
      [25] = "+",
      [30] = "\u00d7",
      [35] = "\u00f7",
      [36] = "0",
      [37] = "\u00b7",
      [38] = "CL X",
    };

    foreach ((int index, string label) in expectedCapFace)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-70"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapAbove_None_NoShiftSystem()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    foreach (int index in new[] { 0, 4, 5, 9, 15, 19, 38 })
    {
      KeyLegendVisual visual = VisualAt(vocabulary, index);
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"No CapAbove gold at {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"No CapAbove blue at {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"No CapSkirt at {index}");
    }
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("CL X", VisualAt(vocabulary, 38).Primary);
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CL X"));
  }

  [TestMethod]
  public void Annotation_NoModifiers()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-70");
    Assert.AreEqual(0, model.ModifierKeys.Count);
    Assert.AreEqual(0, CalcModifierPlacement.StylesOrDefault(model).Count);
  }

  [TestMethod]
  public void KeyColors_BlackTvm_DarkGreyFinance_OrangeEnterOps_WhitePad()
  {
    foreach (int index in new[] { 0, 1, 2, 3, 4, 9, 14 })
      Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-70", index), $"Black {index}");

    foreach (int index in new[] { 5, 6, 7, 8, 10, 11, 12, 13, 18, 19 })
      Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-70", index), $"DarkGrey {index}");

    foreach (int index in new[] { 15, 17, 20, 25, 30, 35 })
      Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-70", index), $"Orange {index}");

    foreach (int index in new[] { 21, 22, 23, 26, 27, 28, 31, 32, 33, 36, 37, 38 })
      Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-70", index), $"White {index}");
  }

  [TestMethod]
  public void ShiftPreview_None()
  {
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-70"));
    Assert.IsFalse(ShiftPreviewController.IsShiftPrefixKey(4, "Classic", "HP-70"));
    Assert.IsFalse(ShiftPreviewController.IsShiftPrefixKey(10, "Classic", "HP-70"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(4, "Classic", "HP-70");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }

  [TestMethod]
  public void IndexTable_CapFaceStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    (int Index, string CapFace, CalcButtonStyle Style)[] rows =
    [
      (0, "n", CalcButtonStyle.Black),
      (2, "PMT", CalcButtonStyle.Black),
      (4, "FV", CalcButtonStyle.Black),
      (5, "INT", CalcButtonStyle.DarkGrey),
      (9, "CLR", CalcButtonStyle.Black),
      (13, "K", CalcButtonStyle.DarkGrey),
      (15, "ENTER", CalcButtonStyle.Orange),
      (17, "CHS", CalcButtonStyle.Orange),
      (19, "M+", CalcButtonStyle.DarkGrey),
      (20, "-", CalcButtonStyle.Orange),
      (21, "7", CalcButtonStyle.White),
      (38, "CL X", CalcButtonStyle.White),
    ];

    foreach ((int index, string capFace, CalcButtonStyle style) in rows)
    {
      KeyLegendVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(capFace, visual.Primary, $"CapFace {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"CapAbove empty {index}");
      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-70", index), $"Style {index}");
    }
  }
}
