using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcModelIdsTests
{
  [TestMethod]
  public void ToEngineId_MapsCatalogSuffixes()
  {
    Assert.AreEqual("HP-29", CalcModelIds.ToEngineId("HP-29C"));
    Assert.AreEqual("HP-31", CalcModelIds.ToEngineId("HP-31E"));
    Assert.AreEqual("HP-65", CalcModelIds.ToEngineId("HP-65"));
  }

  [TestMethod]
  public void ToProductLabel_UsesShortId()
  {
    Assert.AreEqual("T-65", CalcModelIds.ToProductLabel("HP-65"));
    Assert.AreEqual("T-29C", CalcModelIds.ToProductLabel("HP-29C"));
  }
}
