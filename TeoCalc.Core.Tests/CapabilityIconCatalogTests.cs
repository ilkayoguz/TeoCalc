using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CapabilityIconCatalogTests
{
  private static string EngineRoot => TeoCalcPaths.ResourcePath("Engine");

  private static CalcModelDefinition ResolveEngine(string engineId)
  {
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      Path.Combine(EngineRoot, engineId, "Model.json"));
    return CalcModelCatalog.Resolve(model, engineId);
  }

  [TestMethod]
  public void Hp65_HasCardSlot_NoPrinter()
  {
    CalcModelDefinition model = ResolveEngine("HP-65");
    Assert.IsTrue(CalcCardSlotComponent.ModelHasCardSlot(model));
    Assert.IsFalse(model.HasPrinter == true);
    Assert.AreEqual(
      CalcWindowTitlePanelComponent.ButtonWidth,
      CalcWindowTitlePanelComponent.CapabilityIconsWidth(
        hasCardSlot: true,
        hasPrinter: false,
        hasDebug: false,
        hasStudio: false));
  }

  [TestMethod]
  public void Hp19C_HasPrinter_NoCardSlot()
  {
    CalcModelDefinition model = ResolveEngine("HP-19C");
    Assert.IsFalse(CalcCardSlotComponent.ModelHasCardSlot(model));
    Assert.IsTrue(model.HasPrinter == true);
    Assert.AreEqual(
      CalcWindowTitlePanelComponent.ButtonWidth,
      CalcWindowTitlePanelComponent.CapabilityIconsWidth(
        hasCardSlot: false,
        hasPrinter: true,
        hasDebug: false,
        hasStudio: false));
  }

  [TestMethod]
  public void Hp25_HasNeitherCapabilityIcon()
  {
    CalcModelDefinition model = ResolveEngine("HP-25");
    Assert.IsFalse(CalcCardSlotComponent.ModelHasCardSlot(model));
    Assert.IsFalse(model.HasPrinter == true);
    Assert.AreEqual(
      0f,
      CalcWindowTitlePanelComponent.CapabilityIconsWidth(
        hasCardSlot: false,
        hasPrinter: false,
        hasDebug: false,
        hasStudio: false));
  }
}
