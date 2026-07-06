using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcBodyLayoutTests
{
  [TestMethod]
  public void Hp65Layout_MatchesBodyFaceplateLayout()
  {
    CalcBodyLayout layout = Hp65CalcBodyLayout.Instance;
    BodyFaceplateLayout.EnsureLoaded();

    Assert.AreEqual(Hp65CalcBodyLayout.LayoutId, layout.Id);
    Assert.AreEqual(BodyFaceplateLayout.ReferenceWidth, layout.ReferenceWidth);
    Assert.AreEqual(BodyFaceplateLayout.DisplayWindow, layout.DisplaySlot);
    Assert.AreEqual(BodyFaceplateLayout.KeypadPanel, layout.KeypadSlot);
    Assert.IsTrue(layout.TryGetKeySlot(0, out RectF key0));
    Assert.IsTrue(BodyFaceplateLayout.TryGetKeyRect(0, out RectF legacyKey0));
    Assert.AreEqual(legacyKey0, key0);
  }

  [TestMethod]
  public void ModelCatalog_Resolves_Hp65BodyLayout()
  {
    CalcBodyLayout layout = CalcBodyLayoutCatalog.Resolve(CalcModelCatalog.Hp65);
    Assert.AreEqual(Hp65CalcBodyLayout.LayoutId, layout.Id);
  }

  [TestMethod]
  public void BodySlots_MeasureFromMetrics()
  {
    CalcChassisMetrics metrics = new(Hp65CalcBodyLayout.Instance, 1f);
    CalcBodySlots slots = CalcBodyComponent.MeasureSlots(System.Numerics.Vector2.Zero, metrics);

    Assert.AreEqual(metrics.DisplayRect(System.Numerics.Vector2.Zero), slots.Display);
    Assert.AreEqual(metrics.LogoRect(System.Numerics.Vector2.Zero), slots.Logo);
  }

  [TestMethod]
  public void Hp21Layout_HasWoodstockSlots()
  {
    CalcBodyLayout layout = Hp21CalcBodyLayout.Instance;
    Assert.AreEqual(Hp21CalcBodyLayout.LayoutId, layout.Id);
    Assert.AreEqual(360f, layout.ReferenceWidth);
    Assert.IsFalse(layout.HasCardSlots);
    Assert.AreEqual(CalcSwitchLabels.WoodstockAngle, layout.SwitchLabels);
    Assert.IsTrue(layout.TryGetKeySlot(0, out _));
    Assert.IsTrue(layout.TryGetKeySlot(33, out RectF dsp));
    Assert.IsTrue(dsp.Width > dsp.Height);
  }

  [TestMethod]
  public void ModelCatalog_Hp21_UsesWoodstockBody()
  {
    CalcModelDefinition model = CalcModelCatalog.Hp21;
    CalcBodyLayout layout = CalcBodyLayoutCatalog.Resolve(model);
    Assert.AreEqual(Hp21CalcBodyLayout.LayoutId, layout.Id);
  }

  [TestMethod]
  public void ThemeCatalog_Loads_RetroAndModern()
  {
    IReadOnlyList<CalcThemePack> themes = CalcThemeCatalog.LoadAll();
    Assert.IsTrue(themes.Count >= 2);
    Assert.IsTrue(themes.Any(theme => theme.Id == "Retro"));
    Assert.IsTrue(themes.Any(theme => theme.Id == "Modern"));
  }
}
