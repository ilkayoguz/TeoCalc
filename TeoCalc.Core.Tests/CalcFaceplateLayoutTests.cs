using System.Numerics;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

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
    CalcChassisMetrics metrics = new(Hp65CalcBodyLayout.Instance, 1f);

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
    Assert.AreEqual(120f, enter.Width);
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
  public void Hp65KeyChart_MatchesPanamatikReference()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    string[] expectedChars =
    [
      "a", "b", "c", "d", "e", "p", "o", "l", "q", "t",
      "f", "h", "s", "r", "g", "\r", "\r", "n", "x", "\b",
      "-", "7", "8", "9", "\0", "+", "4", "5", "6", "\0",
      "*", "1", "2", "3", "\0", "/", "0", ".", " ", "\0",
    ];
    int[] expectedKeyCodes =
    [
      30, 28, 27, 26, 24, 46, 44, 43, 42, 40,
      14, 12, 11, 10, 8, 62, 62, 59, 58, 56,
      54, 52, 51, 50, 0, 22, 20, 19, 18, 0,
      6, 4, 3, 2, 0, 38, 36, 35, 34, 0,
    ];

    Assert.AreEqual(expectedChars.Length, vocabulary.KeyChart.Count);
    Assert.AreEqual(expectedKeyCodes.Length, vocabulary.KeyChart.Count);
    for (int index = 0; index < vocabulary.KeyChart.Count; index++)
    {
      ProgramKeyEntry key = vocabulary.KeyChart[index];
      Assert.AreEqual(expectedChars[index], key.Char, $"HP-65 key-chart char {index}");
      Assert.AreEqual(expectedKeyCodes[index], key.KeyCode, $"HP-65 key-chart code {index}");
    }
  }

  [TestMethod]
  public void ClassicPhysicalLayout_AllVisibleCellsHaveFirmwareKeyCodes()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-65");

    Assert.AreEqual(35, cells.Count);
    foreach (FaceplateCell cell in cells)
    {
      ProgramKeyEntry key = vocabulary.KeyChart[cell.KeyChartIndex];
      Assert.AreNotEqual(0, key.KeyCode, $"Visible key {cell.KeyChartIndex} should have a firmware key code.");
      Assert.IsFalse(string.IsNullOrEmpty(CalcFaceplateLayout.LabelForKey(key, vocabulary)));
    }

    Assert.AreEqual(36, vocabulary.KeyChart.Count(key => key.KeyCode != 0), "Panamatik maps 36 chart entries including the duplicate ENTER slot.");
    Assert.AreEqual(4, vocabulary.KeyChart.Count(key => key.KeyCode == 0), "The remaining HP-65 chart slots are spacers.");
  }

  [TestMethod]
  public void Hp65DigitKeys_ResolveToPanamatikFirmwareCodes()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    Dictionary<char, byte> expected = new()
    {
      ['0'] = 36,
      ['1'] = 4,
      ['2'] = 3,
      ['3'] = 2,
      ['4'] = 20,
      ['5'] = 19,
      ['6'] = 18,
      ['7'] = 52,
      ['8'] = 51,
      ['9'] = 50,
    };

    foreach ((char digit, byte keyCode) in expected)
    {
      Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, digit, out byte resolved));
      Assert.AreEqual(keyCode, resolved);
    }
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
    Assert.AreSame(
      TeoCalc.Formats.ClassicCardStripLabels.DefaultNoCardCaptions,
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
