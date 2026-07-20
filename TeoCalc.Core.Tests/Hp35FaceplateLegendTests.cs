using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp35FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-35/Program/program.vocabulary.json"));

  private static KeyLegendVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-35", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  [TestMethod]
  public void Family_IsClassic_NotSpice()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-35"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-35/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_IncludeClr_Index4()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-35");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 4), "CLR CapAbove requires physical cell index 4");
    FaceplateCell clr = cells.Single(c => c.KeyChartIndex == 4);
    Assert.AreEqual(0, clr.Row);
    Assert.AreEqual(4, clr.Column);

    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 15);
    Assert.AreEqual(2, enter.ColSpan);
    Assert.IsFalse(cells.Any(c => c.KeyChartIndex == 16));
  }

  [TestMethod]
  public void CapFace_BlankOnFunctionRows_EnterAndDigitPadRemain()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedCapFace = new()
    {
      // Rows 1–3 + CH S/E EX/CL X: CapFace blank (legends on CapAbove).
      [0] = "",
      [1] = "",
      [2] = "",
      [3] = "",
      [4] = "", // CLR — KeyCode 0; CapFace blank
      [5] = "",
      [6] = "",
      [7] = "",
      [8] = "",
      [9] = "",
      [10] = "",
      [11] = "",
      [12] = "",
      [13] = "",
      [14] = "",
      [15] = "ENTER",
      [17] = "",
      [18] = "",
      [19] = "",
      [20] = "-",
      [21] = "7",
      [25] = "+",
      [30] = "\u00d7",
      [35] = "\u00f7",
      [36] = "0",
      [37] = "\u00b7",
      [38] = "\u03c0",
    };

    foreach ((int index, string label) in expectedCapFace)
    {
      Assert.AreEqual(
        label,
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-35"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapAbove_WhiteLegends_MatchUserGrid()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedCapAbove = new()
    {
      [0] = "x^y",
      [1] = "log",
      [2] = "ln",
      [3] = "e^x",
      [4] = "CLR",
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
      [17] = "CH S",
      [18] = "E EX",
      [19] = "CL X",
    };

    foreach ((int index, string label) in expectedCapAbove)
    {
      KeyLegendVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(label, visual.GoldShift, $"CapAbove index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.Primary), $"CapFace blank at {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"No blue at {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlackShift), $"No black at {index}");
    }

    // Finseth note: HP-35 had x^y, not y^x.
    Assert.AreNotEqual("y^x", VisualAt(vocabulary, 0).GoldShift);
  }

  [TestMethod]
  public void CapAbove_ClxUsesCapitalMathXPath()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.AreEqual("CL X", VisualAt(vocabulary, 19).GoldShift);
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CL X"));
  }

  [TestMethod]
  public void CapAbove_ChsEex_AreSpacedPlainLabels()
  {
    Assert.IsTrue(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel("CH S"));
    Assert.IsTrue(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel("E EX"));
    Assert.IsFalse(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("CH S"));
    Assert.IsFalse(ClassicFaceplateGlyphs.UsesPrefixCapitalMathX("E EX"));
  }

  [TestMethod]
  public void CapAbove_XyPower_UsesGlyphPath()
  {
    Assert.IsFalse(ClassicFaceplateGlyphs.IsPlainArialSkirtLabel("x^y"));
  }

  [TestMethod]
  public void CapAbove_XExchange_UsesStackedChevrons()
  {
    Assert.IsTrue(ClassicFaceplateGlyphs.UsesCardSlotExchangeLabel("x\u2194y"));
  }

  [TestMethod]
  public void CapAboveInk_IsWhiteNotGold()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-35");
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F));
    Assert.IsTrue(
      CalcModifierPlacement.TryGetInkToken(model, CalcModifierKey.F, CalcLabelAnchor.CapAbove, out CalcColorToken ink));
    Assert.AreEqual(CalcFaceplateTokens.LabelOnDarkCapColor, ink.Name);
    Assert.AreNotEqual(CalcFaceplateTokens.ModifierFCapAboveColor, ink.Name);

    uint resolved = CalcFaceplateTheme.ResolveAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapAbove, model);
    uint gold = CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ModifierFCapAboveColor, model);
    Assert.AreNotEqual(gold, resolved);
  }

  [TestMethod]
  public void DigitPad_NoCapAbove_CapFaceOnly()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    foreach (int index in new[] { 20, 21, 25, 30, 35, 36, 37, 38 })
    {
      KeyLegendVisual visual = VisualAt(vocabulary, index);
      Assert.IsFalse(string.IsNullOrEmpty(visual.Primary), $"CapFace at {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"No CapAbove at {index}");
    }

    Assert.AreEqual("ENTER", VisualAt(vocabulary, 15).Primary);
    Assert.IsTrue(string.IsNullOrEmpty(VisualAt(vocabulary, 15).GoldShift));
  }

  [TestMethod]
  public void KeyColors_BlueExEnterOps_WhiteDigitsPi_BlackFunctions()
  {
    // Row1: black except CLR (index 4) blue; e^x (3) black.
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 0));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 1));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 2));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 3)); // [1,4] e^x
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 4)); // CLR

    // Row2: √x (5) black; arc sin cos tan (6–9) dark grey.
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 5));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 6));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 7));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 8));
    Assert.AreEqual(CalcButtonStyle.DarkGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 9)); // tan

    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 10));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 10));
    Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 14));

    // ENTER row blue
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 15));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 17));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 18));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 19));

    // Arithmetic blue
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 20));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 25));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 30));
    Assert.AreEqual(CalcButtonStyle.Blue, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 35));

    // Digits + · + π white
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 21));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 36));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 37));
    Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 38));

    // Must not inherit HP-65 orange f / blue g chart indices.
    Assert.AreNotEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", 10));
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

  [TestMethod]
  public void IndexTable_CapFaceCapAboveStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    // Spot-check the full index → CapFace / CapAbove / style contract.
    (int Index, string CapFace, string? CapAbove, CalcButtonStyle Style)[] rows =
    [
      (0, "", "x^y", CalcButtonStyle.Black),
      (3, "", "e^x", CalcButtonStyle.Black),
      (4, "", "CLR", CalcButtonStyle.Blue),
      (5, "", "\u221ax", CalcButtonStyle.Black),
      (6, "", "arc", CalcButtonStyle.DarkGrey),
      (7, "", "sin", CalcButtonStyle.DarkGrey),
      (8, "", "cos", CalcButtonStyle.DarkGrey),
      (9, "", "tan", CalcButtonStyle.DarkGrey),
      (11, "", "x\u2194y", CalcButtonStyle.Black),
      (15, "ENTER", null, CalcButtonStyle.Blue),
      (17, "", "CH S", CalcButtonStyle.Blue),
      (18, "", "E EX", CalcButtonStyle.Blue),
      (19, "", "CL X", CalcButtonStyle.Blue),
      (20, "-", null, CalcButtonStyle.Blue),
      (21, "7", null, CalcButtonStyle.White),
      (38, "\u03c0", null, CalcButtonStyle.White),
    ];

    foreach ((int index, string capFace, string? capAbove, CalcButtonStyle style) in rows)
    {
      KeyLegendVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(capFace, visual.Primary, $"CapFace {index}");
      if (capAbove is null)
      {
        Assert.IsTrue(string.IsNullOrEmpty(visual.GoldShift), $"CapAbove empty {index}");
      }
      else
      {
        Assert.AreEqual(capAbove, visual.GoldShift, $"CapAbove {index}");
      }

      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-35", index), $"Style {index}");
    }
  }
}
