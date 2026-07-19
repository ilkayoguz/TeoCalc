using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp35FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-35/Program/program.vocabulary.json"));

  [TestMethod]
  public void Family_IsClassic_NotSpice()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-35"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-35/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_OmitBlankTopRight_ClassicMap()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-35");
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 4));
    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 15);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 16));
  }

  [TestMethod]
  public void PrimaryLabels_MatchFinsethClassicKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expected = new()
    {
      [0] = "x^y",
      [1] = "log",
      [2] = "ln",
      [3] = "e^x",
      // Index 4 = Finseth CLR, but Panamatik KeyCode 0 (omitted physical key).
      [5] = "\u221ax",
      [6] = "arc",
      [7] = "sin",
      [8] = "cos",
      [9] = "tan",
      [10] = "1/x",
      [11] = "x\u2194y",
      [12] = "R\u2193",
      [13] = "STO",
      [14] = "RCL",
      [15] = "ENTER",
      [17] = "CHS",
      [18] = "EEX",
      [19] = "CLX",
      [20] = "-",
      [21] = "7",
      [25] = "+",
      [30] = "\u00d7",
      [35] = "\u00f7",
      [36] = "0",
      [37] = "\u00b7",
      [38] = "\u03c0",
    };

    foreach ((int index, string label) in expected)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-35"),
        $"Index {index}");
    }

    // Finseth note: HP-35 had x^y, not y^x.
    Assert.AreNotEqual(
      "y^x",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[0], vocabulary, "Classic", "HP-35"));
  }

  [TestMethod]
  public void CapFace_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual(
      "CLX",
      CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[19], vocabulary, "Classic", "HP-35"));
    Assert.IsTrue(HpClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CLX"));
  }

  [TestMethod]
  public void CapFace_XyPower_UsesGlyphPath()
  {
    Assert.IsFalse(HpClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x^y"));
  }

  [TestMethod]
  public void NoShiftSkirts_EmptyFaceplateJson()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    for (int i = 0; i < vocabulary.KeyChart.Count; i++)
    {
      if (vocabulary.KeyChart[i].KeyCode == 0)
      {
        continue;
      }

      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-35", "Classic", vocabulary.KeyChart[i], vocabulary, FaceplateLabelStyle.Normal);
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"Gold at {i}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"Blue at {i}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"Black at {i}");
    }
  }

  [TestMethod]
  public void KeyColors_BlackFunctions_WhiteDigitPad_NoOrangeBluePrefixes()
  {
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 6));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 10));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 15));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 21));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 36));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 38));
    // Must not inherit HP-65 orange f / blue g chart indices.
    Assert.AreNotEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 10));
    Assert.AreNotEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 14));
  }

  [TestMethod]
  public void ShiftPreview_NoPrefixKeys()
  {
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-35"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Classic", "HP-35"));
    Assert.IsFalse(ShiftPreviewController.IsShiftPrefixKey(10, "Classic", "HP-35"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(10, "Classic", "HP-35");
    Assert.AreEqual(ShiftPreviewMode.None, preview.Mode);
  }
}
