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
  public void ChassisGeometry_MatchesBodyLayout()
  {
    BodyFaceplateLayout.EnsureLoaded();
    CalcChassisMetrics metrics = new(1f);

    Assert.AreEqual(409f, metrics.Width);
    Assert.AreEqual(861f, metrics.Height);
    Assert.AreEqual(28f, metrics.FooterHeight);

    RectF key0 = metrics.KeyRect(Vector2.Zero, 0);
    Assert.AreEqual(38f, key0.X);
    Assert.AreEqual(237f, key0.Y);
    Assert.AreEqual(48f, key0.Width);
    Assert.AreEqual(38f, key0.Height);

    RectF enter = metrics.KeyRect(Vector2.Zero, 15);
    Assert.AreEqual(38f, enter.X);
    Assert.AreEqual(459f, enter.Y);
    Assert.AreEqual(118f, enter.Width);
    Assert.AreEqual(38f, enter.Height);

    RectF band = metrics.CardSlotBandRect(Vector2.Zero);
    Assert.AreEqual(195f, band.Y);
    Assert.AreEqual(23f, band.Height);

    Assert.IsTrue(metrics.TryGetCardSlotColumn(Vector2.Zero, 0, out RectF slot0));
    Assert.AreEqual(38f, slot0.X);
    Assert.AreEqual(48f, slot0.Width);
  }

  [TestMethod]
  public void BodyLayout_KeyCount_MatchesClassicCells()
  {
    BodyFaceplateLayout.EnsureLoaded();
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic");

    Assert.AreEqual(35, BodyFaceplateLayout.KeyCount);
    Assert.AreEqual(35, cells.Count);
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
      new[] { "1/x", "\u221ax", "R\u2191", "R\u2193", "x\u2194y" },
      CalcFaceplateLayout.CardSlotLabels);
  }

  [TestMethod]
  public void FaceplateFonts_Exist()
  {
    string root = TeoCalcPaths.ResourcePath("Font");
    Assert.IsTrue(File.Exists(Path.Combine(root, "LiberationSans-Bold.ttf")));
    Assert.IsTrue(
      File.Exists(Path.Combine(root, "STIXTwoText-BoldItalic.ttf"))
      || File.Exists(Path.Combine(root, "LiberationSerif-BoldItalic.ttf")));
  }

  [TestMethod]
  public void CardSlotLabelSvgAssets_Exist()
  {
    string root = Path.Combine(TeoCalcPaths.ResourcePath("Engine/HP-65/Assets"), "CardSlot");
    string[] expected =
    [
      "label-1-over-x.svg",
      "label-sqrt-x.svg",
      "label-y-pow-x.svg",
      "label-r-down.svg",
      "label-x-exchange-y.svg",
      "exchange-arrow-up.svg",
      "sqrt-radical.svg",
    ];

    foreach (string file in expected)
    {
      Assert.IsTrue(File.Exists(Path.Combine(root, file)), $"Missing card slot label asset: {file}");
    }
  }
}
