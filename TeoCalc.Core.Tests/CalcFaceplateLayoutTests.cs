using System.Numerics;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcFaceplateLayoutTests
{
  private static ProgramVocabulary LoadHp65Vocabulary()
  {
    string path = Path.Combine(TeoCalcPaths.ResourcePath("Engine"), "HP-65", "Program", "program.vocabulary.json");
    return ProgramVocabulary.Load(path);
  }

  [TestMethod]
  public void LabelForKey_EnterChar_ResolvesToFaceplateEnter()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    ProgramKeyEntry enterKey = vocabulary.KeyChart[15];

    Assert.AreEqual("\r", enterKey.Char);
    Assert.AreEqual(62, enterKey.KeyCode);
    Assert.AreEqual("ENTER", CalcFaceplateLayout.LabelForKey(enterKey, vocabulary));
  }

  [TestMethod]
  public void LabelForKey_DspKey_UsesFaceplateNotMnemonic()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    ProgramKeyEntry dspKey = vocabulary.KeyChart[5];

    Assert.AreEqual("p", dspKey.Char);
    Assert.AreEqual("DSP", CalcFaceplateLayout.LabelForKey(dspKey, vocabulary));
  }

  [TestMethod]
  public void LabelForKey_SpaceChar_ResolvesToRunStop()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    ProgramKeyEntry runStopKey = vocabulary.KeyChart[38];

    Assert.AreEqual(" ", runStopKey.Char);
    Assert.AreEqual(34, runStopKey.KeyCode);
    Assert.AreEqual("R/S", CalcFaceplateLayout.LabelForKey(runStopKey, vocabulary));
  }

  [TestMethod]
  public void ButtonKindForKey_EnterAndDivide_UseSpecialCaps()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    FaceplateCell enter = CalcFaceplateLayout.GetPhysicalCells("Classic").Single(cell => cell.KeyChartIndex == 15);
    ProgramKeyEntry enterKey = vocabulary.KeyChart[15];
    ProgramKeyEntry divideKey = vocabulary.KeyChart[35];

    Assert.AreEqual(CalcButtonKind.EnterWide, CalcFaceplateLayout.ButtonKindForKey(enterKey, enter));
    Assert.AreEqual(CalcButtonKind.OperatorColon, CalcFaceplateLayout.ButtonKindForKey(divideKey, new FaceplateCell(35, 7, 0)));
  }

  [TestMethod]
  public void ChassisGeometry_MatchesHp65ReferenceRatios()
  {
    CalcChassisMetrics metrics = new(1f);
    Assert.AreEqual(47f, metrics.KeyWidth);
    Assert.AreEqual(57f, metrics.KeyHeight);
    Assert.AreEqual(10f, metrics.KeyGapH);
    Assert.AreEqual(8f, metrics.KeyGapV);
    Assert.AreEqual(12f, metrics.CardSlotBand);

    Vector2 enterSize = metrics.CellSize(new FaceplateCell(15, 3, 0, ColSpan: 2));
    Assert.AreEqual(47f * 2f + 10f, enterSize.X);
    Assert.AreEqual(57f, enterSize.Y);
  }

  [TestMethod]
  public void ClassicPhysicalLayout_HorizontalEnterRowAndNumpad()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic");
    FaceplateCell enter = cells.Single(cell => cell.KeyChartIndex == 15);

    Assert.AreEqual(3, enter.Row);
    Assert.AreEqual(0, enter.Column);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.AreEqual(1, enter.RowSpan);
    Assert.AreEqual(FaceplateLabelStyle.Normal, enter.LabelStyle);
    Assert.IsFalse(cells.Any(cell => cell.KeyChartIndex == 16));

    Assert.AreEqual(2, cells.Single(cell => cell.KeyChartIndex == 17).Column);
    Assert.AreEqual(3, cells.Single(cell => cell.KeyChartIndex == 18).Column);
    Assert.AreEqual(4, cells.Single(cell => cell.KeyChartIndex == 19).Column);

    Assert.AreEqual(0, cells.Single(cell => cell.KeyChartIndex == 20).Column);
    Assert.AreEqual(1, cells.Single(cell => cell.KeyChartIndex == 21).Column);
    Assert.AreEqual(3, cells.Single(cell => cell.KeyChartIndex == 23).Column);
    Assert.AreEqual(0, cells.Single(cell => cell.KeyChartIndex == 35).Column);
    Assert.AreEqual(3, cells.Single(cell => cell.KeyChartIndex == 38).Column);
  }

  [TestMethod]
  public void CardSlotLabels_MatchHp65Defaults()
  {
    Assert.AreEqual(5, CalcFaceplateLayout.CardSlotLabels.Length);
    CollectionAssert.AreEqual(
      new[] { "1/x", "\u221ax", "y^x", "R\u2193", "x\u2194y" },
      CalcFaceplateLayout.CardSlotLabels);
  }
}
