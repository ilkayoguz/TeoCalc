using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcFaceplateThemeTests
{
  [TestMethod]
  public void DefaultTheme_Is_Modern()
  {
    Assert.AreEqual("Modern", CalcThemeCatalog.DefaultThemeId);
    CalcThemePack modern = CalcThemeCatalog.LoadDefault();
    Assert.AreEqual("Modern", modern.Id);
  }

  [TestMethod]
  public void RetroTheme_Loads_And_Resolves_FrameToken()
  {
    CalcThemePack retro = CalcThemeCatalog.Load("Retro");
    Assert.AreEqual("Retro", retro.Id);
    Assert.IsFalse(string.IsNullOrWhiteSpace(retro.DisplayName));

    CalcFaceplateTheme.SetTheme(retro);
    uint frame = CalcFaceplateTheme.Resolve(CalcFaceplateTokens.FrameColor);
    Assert.AreNotEqual(0u, frame);
  }

  [TestMethod]
  public void Model_ThemeId_Resolves_Through_ModelDefinition()
  {
    CalcModelDefinition hp65 = CalcModelCatalog.Hp65;
    uint bezel = CalcFaceplateTheme.Resolve(CalcFaceplateTokens.DisplayBezelColor, hp65);
    Assert.AreNotEqual(0u, bezel);
  }

  [TestMethod]
  public void ModernTheme_Resolves_ChromeTokens()
  {
    CalcFaceplateTheme.SetTheme(CalcThemeCatalog.LoadDefault());
    uint fitil = CalcFaceplateTheme.Resolve(CalcFaceplateTokens.ChromeBlackFitilColor);
    uint body = Calc00dWireStyle.InnerBodyFill;
    Assert.AreNotEqual(0u, fitil);
    Assert.AreEqual(body, CalcChassisPalette.ChromeInnerBody);
  }

  [TestMethod]
  public void CalcKeyVisual_FromLegacy_Maps_ShiftLabels()
  {
    KeyLegendVisual legacy = new()
    {
      Primary = "1",
      GoldShift = "LN",
      BlueShift = "e^x",
    };

    CalcKeyVisual visual = CalcKeyVisual.FromLegacy(legacy, CalcButtonStyle.Black, CalcButtonKind.Standard);
    Assert.AreEqual("1", visual.CapFace);
    Assert.AreEqual(2, visual.Annotations.Count);
    Assert.IsTrue(visual.Annotations.Any(annotation =>
      annotation is { Modifier: CalcModifierKey.F, Anchor: CalcLabelAnchor.CapAbove, Text: "LN" }));
    Assert.IsTrue(visual.Annotations.Any(annotation =>
      annotation is { Modifier: CalcModifierKey.G, Anchor: CalcLabelAnchor.CapSkirt, Text: "e^x" }));
  }

  [TestMethod]
  public void ModifierPlacement_Uses_Model_Bindings_Not_Hardcoded_Slots()
  {
    CalcModelDefinition remapped = new()
    {
      Id = "test",
      DisplayName = "Test",
      ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
      AnnotationStyles =
      [
        new(CalcModifierKey.F, CalcLabelAnchor.CapBelow, CalcKeyColorPalette.ModifierFOnCapAbove),
        new(CalcModifierKey.G, CalcLabelAnchor.CapAbove, CalcKeyColorPalette.ModifierGOnCapSkirt),
      ],
    };

    Assert.AreEqual(CalcLabelAnchor.CapBelow, CalcModifierPlacement.PrimaryAnchor(remapped, CalcModifierKey.F));
    Assert.AreEqual(CalcLabelAnchor.CapAbove, CalcModifierPlacement.PrimaryAnchor(remapped, CalcModifierKey.G));

    CalcKeyAnnotation gold = CalcModifierPlacement.Annotate(remapped, CalcModifierKey.F, "LN");
    CalcKeyAnnotation blue = CalcModifierPlacement.Annotate(remapped, CalcModifierKey.G, "e^x");
    Assert.AreEqual(CalcLabelAnchor.CapBelow, gold.Anchor);
    Assert.AreEqual(CalcLabelAnchor.CapAbove, blue.Anchor);
  }
}
