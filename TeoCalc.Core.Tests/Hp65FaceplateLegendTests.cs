using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp65FaceplateLegendTests
{
  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void ClearBracket_SpansEnterThroughClx()
  {
    Assert.IsTrue(CalcBracketLegendComponent.TryResolve("HP-65", out CalcBracketLegendComponent.Spec spec));
    Assert.AreEqual(15, spec.LeftKey);
    Assert.AreEqual(19, spec.RightKey);
    Assert.AreEqual(17, spec.TextCenterKey);
    Assert.AreEqual("CLEAR", spec.Text);

    foreach (int index in new[] { 15, 17, 18, 19 })
    {
      Assert.IsTrue(CalcBracketLegendComponent.CoversKey(spec, index), $"Covered {index}");
    }

    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 14));
    Assert.IsFalse(CalcBracketLegendComponent.CoversKey(spec, 20));
  }

  [TestMethod]
  public void ClearBracket_CapAboveLegends_RemainUnderBracket()
  {
    ProgramVocabulary vocabulary = LoadVocabulary();
    Dictionary<int, string> expectedGold = new()
    {
      [15] = "PREFIX",
      [17] = "STK",
      [18] = "REG",
      [19] = "PRGM",
    };
    Dictionary<int, string> expectedBlue = new()
    {
      [15] = "DEG",
      [17] = "RAD",
      [18] = "GRD",
      [19] = "DEL",
    };

    foreach ((int index, string gold) in expectedGold)
    {
      KeyLegendVisual visual = ClassicKeyFaceplateLegend.Resolve(
        "HP-65",
        "Classic",
        vocabulary.KeyChart[index],
        vocabulary,
        FaceplateLabelStyle.Normal);
      Assert.AreEqual(gold, visual.GoldShift, $"CapAbove gold at {index}");
      Assert.AreEqual(expectedBlue[index], visual.BlueShift, $"CapSkirt blue at {index}");
    }

    // CapAbove annotations must reach the key visual path (Modern previously skipped them).
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-65");
    KeyLegendVisual enterLegacy = ClassicKeyFaceplateLegend.Resolve(
      "HP-65", "Classic", vocabulary.KeyChart[15], vocabulary, FaceplateLabelStyle.Normal);
    CalcKeyVisual enter = CalcKeyVisual.FromLegacy(enterLegacy, CalcButtonStyle.Black, CalcButtonKind.EnterWide, model);
    Assert.IsTrue(
      enter.Annotations.Any(a =>
        a is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "PREFIX" }),
      "PREFIX CapAbove annotation required on ENTER");
  }

  [TestMethod]
  public void ClearBracket_DoesNotShrinkCapFace_AndClearsCapAbove()
  {
    const float scale = 1f;
    float slotTop = 100f;
    float capMinY = slotTop + CalcKeyPanelComponent.LabelAboveRef * scale;
    Assert.IsTrue(CalcBracketLegendComponent.HasClearance(slotTop, capMinY, scale));
  }

  [TestMethod]
  public void ClearBracket_LayoutInsertsGutterExtraAboveEnterRow()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-65");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Classic", "HP-65", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-65");
    int clearRow = CalcBracketLegendComponent.FindBracketRow(cells, "HP-65");
    Assert.IsTrue(clearRow >= 1);

    FaceplateCell enter = cells.Single(c => c.KeyChartIndex == 15);
    FaceplateCell above = cells.First(c => c.Row == clearRow - 1);
    Assert.IsTrue(layout.TryGetKeySlot(enter.KeyChartIndex, out RectF enterSlot));
    Assert.IsTrue(layout.TryGetKeySlot(above.KeyChartIndex, out RectF aboveSlot));

    float gap = enterSlot.Y - (aboveSlot.Y + aboveSlot.Height);
    Assert.AreEqual(CalcKeyPanelComponent.GutterRef + CalcBracketLegendComponent.GutterExtraAboveRef, gap, 0.05f);
    float capFace = enterSlot.Height - CalcKeyPanelComponent.LabelAboveRef;
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCapHeightRef, capFace, 0.05f);
  }
}
