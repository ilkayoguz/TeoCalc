using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class LauncherLabelTests
{
  [TestMethod]
  public void LauncherLabel_UsesFullProductLabel()
  {
    CalculatorLauncherEntry entry = new(
      ModelId: "HP-65",
      DisplayName: "HP-65",
      ProductLabel: CalcModelIds.ToProductLabel("HP-65"),
      TeoCalcModelId: "HP-65",
      TeoCalcStatus: "ok",
      CanOpenTeoCalc: true,
      Reference: null);

    Assert.AreEqual("T-65", entry.LauncherLabel);
    Assert.AreEqual("T-65", CalcModelIds.ToProductLabel("HP-65"));
  }
}
