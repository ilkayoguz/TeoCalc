using System.Numerics;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp80FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-80/Program/program.vocabulary.json"));

  private static HpCalcKeyVisual VisualAt(ProgramVocabulary vocabulary, int index) =>
    ClassicKeyFaceplateLegend.Resolve(
      "HP-80", "Classic", vocabulary.KeyChart[index], vocabulary, FaceplateLabelStyle.Normal);

  [TestMethod]
  public void Family_IsClassic_ProductLabel_T80()
  {
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("HP-80"));
    Assert.AreEqual("T-80", CalcModelIds.ToProductLabel("HP-80"));
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-80/Model.json"));
    Assert.AreEqual("Classic", model.Family);
  }

  [TestMethod]
  public void PhysicalCells_IncludeGold_Index5()
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-80");
    Assert.IsTrue(cells.Any(c => c.KeyChartIndex == 5), "gold CapFace requires physical cell index 5");
    FaceplateCell gold = cells.Single(c => c.KeyChartIndex == 5);
    Assert.AreEqual(1, gold.Row);
    Assert.AreEqual(0, gold.Column);

    FaceplateCell save = cells.Single(c => c.KeyChartIndex == 15);
    Assert.AreEqual(2, save.ColSpan);
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
      [5] = "", // gold prefix — blank CapFace
      [6] = "%",
      [7] = "TL",
      [8] = "SOD",
      [9] = "DAY",
      [10] = "x\u2194y",
      [11] = "R\u2193",
      [12] = "STO",
      [13] = "y^x",
      [14] = "x\u0305",
      [15] = "SAVE",
      [17] = "RCL",
      [18] = "CHS",
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
        CalcFaceplateLayout.LabelForKey(vocabulary.KeyChart[index], vocabulary, "Classic", "HP-80"),
        $"CapFace index {index}");
    }
  }

  [TestMethod]
  public void CapAbove_GoldLegends_MatchFinseth()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedCapAbove = new()
    {
      [1] = "YTM",
      [2] = "INTR",
      [3] = "BOND",
      [6] = "\u0394%",
      [9] = "DATE",
      [13] = "\u221ax",
      [14] = "\u2192\u03a3",
      [19] = "CLEAR",
      [38] = "\u03a3-",
    };

    foreach ((int index, string label) in expectedCapAbove)
    {
      HpCalcKeyVisual visual = VisualAt(vocabulary, index);
      Assert.AreEqual(label, visual.GoldShift, $"CapAbove index {index}");
      Assert.IsTrue(string.IsNullOrEmpty(visual.BlueShift), $"No CapSkirt/blue at {index}");
    }

    Assert.IsTrue(string.IsNullOrEmpty(VisualAt(vocabulary, 5).GoldShift), "gold key has no CapAbove");
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
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-80");
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F));
    Assert.IsTrue(
      CalcModifierPlacement.TryGetInkToken(model, CalcModifierKey.F, CalcLabelAnchor.CapAbove, out CalcColorToken ink));
    Assert.AreEqual(CalcFaceplateTokens.ModifierFCapAboveColor, ink.Name);
    Assert.IsFalse(model.ModifierKeys.Contains(CalcModifierKey.G));
  }

  [TestMethod]
  public void KeyColors_CementOnlyFinancePowers_LightGreyStackSaveOps_WhitePad()
  {
    foreach (int index in new[] { 0, 1, 2, 3, 4 })
      Assert.AreEqual(CalcButtonStyle.Black, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", index), $"Black {index}");

    Assert.AreEqual(CalcButtonStyle.Orange, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", 5));

    // [2,2]–[2,5] and [3,4]–[3,5]
    foreach (int index in new[] { 6, 7, 8, 9, 13, 14 })
      Assert.AreEqual(CalcButtonStyle.Cement, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", index), $"Cement {index}");

    foreach (int index in new[] { 10, 11, 12, 15, 17, 18, 19, 20, 25, 30, 35 })
      Assert.AreEqual(CalcButtonStyle.LightGrey, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", index), $"LightGrey {index}");

    foreach (int index in new[] { 21, 22, 23, 26, 27, 28, 31, 32, 33, 36, 37, 38 })
      Assert.AreEqual(CalcButtonStyle.White, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", index), $"White pad {index}");
  }

  [TestMethod]
  public void ApplyRowBands_ReservesCapAboveOnDigitRows_EqualCapFaceHeight()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-80");
    Assert.IsTrue(CalcModifierPlacement.ReservesCapAboveBand(model));
    Assert.IsFalse(CalcModifierPlacement.ReservesCapBelowBand(model));

    const float scale = 1f;
    float slotH = CalcKeyPanelComponent.LabelAboveRef + CalcKeyPanelComponent.PreferredCapHeightRef;
    // Digit pad keys have no CapAbove text — without the model reserve they would keep full slot height.
    CalcKeyVisual digit = new() { CapFace = "7" };
    Vector2[] slotMins = [new(0f, 0f), new(50f, 0f)];
    Vector2[] slotMaxs = [new(40f, slotH), new(90f, slotH)];
    Vector2[] capMins = new Vector2[2];
    Vector2[] capMaxs = new Vector2[2];
    CalcKeyRowLayout.ApplyRowBands([digit, digit], slotMins, slotMaxs, capMins, capMaxs, scale, model);

    Assert.AreEqual(CalcKeyPanelComponent.LabelAboveRef * scale, capMins[0].Y, 0.01f);
    Assert.AreEqual(capMins[0].Y, capMins[1].Y, 0.01f);
    float face0 = capMaxs[0].Y - capMins[0].Y;
    float face1 = capMaxs[1].Y - capMins[1].Y;
    Assert.AreEqual(face0, face1, 0.01f);
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef * scale, face0, 0.01f);
  }

  [TestMethod]
  public void ShiftPreview_GoldPrefixAtIndex5()
  {
    Assert.AreEqual(5, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Gold, "Classic", "HP-80"));
    Assert.AreEqual(-1, ShiftPreviewController.IndicatorKeyIndex(ShiftPreviewMode.Blue, "Classic", "HP-80"));
    Assert.IsTrue(ShiftPreviewController.IsShiftPrefixKey(5, "Classic", "HP-80"));
    Assert.IsFalse(ShiftPreviewController.IsShiftPrefixKey(4, "Classic", "HP-80"));

    ShiftPreviewController preview = new();
    preview.HandleKeyPress(5, "Classic", "HP-80");
    Assert.AreEqual(ShiftPreviewMode.Gold, preview.Mode);
  }

  [TestMethod]
  public void IndexTable_CapFaceCapAboveStyle_MatchesDeliverable()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    (int Index, string CapFace, string? CapAbove, CalcButtonStyle Style)[] rows =
    [
      (0, "n", null, CalcButtonStyle.Black),
      (1, "i", "YTM", CalcButtonStyle.Black),
      (5, "", null, CalcButtonStyle.Orange),
      (6, "%", "\u0394%", CalcButtonStyle.Cement),
      (9, "DAY", "DATE", CalcButtonStyle.Cement),
      (10, "x\u2194y", null, CalcButtonStyle.LightGrey),
      (12, "STO", null, CalcButtonStyle.LightGrey),
      (13, "y^x", "\u221ax", CalcButtonStyle.Cement),
      (14, "x\u0305", "\u2192\u03a3", CalcButtonStyle.Cement),
      (15, "SAVE", null, CalcButtonStyle.LightGrey),
      (19, "CL X", "CLEAR", CalcButtonStyle.LightGrey),
      (20, "-", null, CalcButtonStyle.LightGrey),
      (21, "7", null, CalcButtonStyle.White),
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

      Assert.AreEqual(style, CalcFaceplateKeyStyles.StyleForKey("Classic", "HP-80", index), $"Style {index}");
    }
  }

  [TestMethod]
  public void ClearBracket_SpansTlThroughSod_AsCompute()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-80", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(7, spec.LeftKey);
    Assert.AreEqual(8, spec.RightKey);
    Assert.AreEqual(7, spec.TextCenterKey);
    Assert.AreEqual("COMPUTE", spec.Text);
    Assert.IsTrue(CalcBracketLegendComponent.CoversKey(spec, 7));
    Assert.IsTrue(CalcBracketLegendComponent.CoversKey(spec, 8));
    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 6));
    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 9));
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveCementRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-80");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Classic", "HP-80", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-80");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-80");
    Assert.IsTrue(clearRow >= 1);

    FaceplateCell tl = cells.Single(c => c.KeyChartIndex == 7);
    FaceplateCell above = cells.First(c => c.Row == clearRow - 1);
    Assert.IsTrue(layout.TryGetKeySlot(tl.KeyChartIndex, out RectF tlSlot));
    Assert.IsTrue(layout.TryGetKeySlot(above.KeyChartIndex, out RectF aboveSlot));

    float gap = tlSlot.Y - (aboveSlot.Y + aboveSlot.Height);
    Assert.AreEqual(CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef, gap, 0.05f);
    float capFace = tlSlot.Height - CalcKeyPanelComponent.LabelAboveRef;
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef, capFace, 0.05f);
  }
}
