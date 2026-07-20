using TeoCalc.Core;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class TeoCalcModelCatalogTests
{
  [TestMethod]
  public void SupportedModels_IncludesPriorityModel()
  {
    CollectionAssert.Contains(TeoCalcModelCatalog.SupportedModels.ToList(), TeoCalcModelCatalog.PriorityModel);
  }

  [TestMethod]
  public void SupportedModels_HasTwentyEntries()
  {
    Assert.AreEqual(20, TeoCalcModelCatalog.SupportedModels.Count);
  }
}
