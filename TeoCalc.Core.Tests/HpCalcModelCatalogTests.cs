using TeoCalc.Core;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class HpCalcModelCatalogTests
{
  [TestMethod]
  public void SupportedModels_IncludesPriorityModel()
  {
    CollectionAssert.Contains(HpCalcModelCatalog.SupportedModels.ToList(), HpCalcModelCatalog.PriorityModel);
  }

  [TestMethod]
  public void SupportedModels_HasTwentyEntries()
  {
    Assert.AreEqual(20, HpCalcModelCatalog.SupportedModels.Count);
  }
}
