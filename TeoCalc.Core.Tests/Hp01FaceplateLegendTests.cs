using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp01FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-01/Program/program.vocabulary.json"));

  [TestMethod]
  public void PrimaryLabels_MatchOwnersGuideKeyboard()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    string[] expected =
    [
      "R", "0", "1", "2", "3", "4", "S",
      ".", "5", "6", "7", "8", "9", "C",
      ":", "+", "-", "\u00d7", "\u00f7", "=", "p",
      "D", "/", "A", "\u2206", "M", "%", "T",
    ];

    Assert.AreEqual(28, vocabulary.KeyChart.Count);
    for (int i = 0; i < expected.Length; i++)
    {
      ProgramKeyEntry key = vocabulary.KeyChart[i];
      string label = CalcFaceplateLayout.LabelForKey(key, vocabulary, "HP01", "HP-01");
      Assert.AreEqual(expected[i], label, $"Index {i} (char '{key.Char}')");
    }
  }

  [TestMethod]
  public void KeyCodes_UnchangedFromPanamatikChart()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    byte[] expected =
    [
      62, 15, 14, 12, 11, 9, 60,
      54, 7, 6, 4, 3, 1, 57,
      55, 31, 30, 28, 27, 25, 59,
      52, 39, 38, 36, 35, 33, 51,
    ];

    for (int i = 0; i < expected.Length; i++)
    {
      Assert.AreEqual(expected[i], vocabulary.KeyChart[i].KeyCode, $"Index {i}");
    }
  }

  [TestMethod]
  public void GoldShiftLegends_MatchOwnersGuideAboveOperatorRow()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    string?[] expectedGold = new string?[28];
    expectedGold[14] = "DW";
    expectedGold[15] = "21";
    expectedGold[16] = "-/+";
    expectedGold[17] = "\u2194";
    expectedGold[18] = "T\u2192";
    expectedGold[19] = "\u2192T";
    expectedGold[20] = "\u0251"; // single-story ɑ (AM)

    for (int i = 0; i < 28; i++)
    {
      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-01",
        "HP01",
        vocabulary.KeyChart[i],
        vocabulary,
        FaceplateLabelStyle.Normal);
      Assert.AreEqual(expectedGold[i], visual.GoldShift, $"Gold at index {i}");
    }
  }

  [TestMethod]
  public void ClassicEnterRowGold_DoesNotOverrideHp01()
  {
    Assert.AreEqual("PREFIX", CalcEnterRowLabels.GoldLabelForKey(15));
    ProgramVocabulary vocabulary = LoadVocabulary();
    HpCalcKeyVisual plus = ClassicKeyFaceplateLegend.Resolve(
      "HP-01",
      "HP01",
      vocabulary.KeyChart[15],
      vocabulary,
      FaceplateLabelStyle.Normal);
    Assert.AreEqual("21", plus.GoldShift);
    Assert.AreEqual("+", plus.Primary);
  }

  [TestMethod]
  public void KeyCaps_AreBlack_ExceptGoldModifierDelta()
  {
    // Classic palette would paint index 14 blue and 10/11 orange — wrong for the watch.
    // Δ (24) is intentionally orange so the gold modifier reads as a shift key.
    for (int i = 0; i < 28; i++)
    {
      CalcButtonStyle expected = i == 24 ? CalcButtonStyle.Orange : CalcButtonStyle.Black;
      Assert.AreEqual(
        expected,
        CalcFaceplateKeyStyles.StyleForKey("HP01", "HP-01", i),
        $"Index {i}");
    }
  }

  [TestMethod]
  public void GoldShiftPreview_FramesDeltaNotDigitSeven()
  {
    Assert.AreEqual(24, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "HP01"));
    Assert.AreEqual(10, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(24, "HP01");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }

}
