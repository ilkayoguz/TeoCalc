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

  [TestMethod]
  public void Resolve_BuildsIdentityInOnePath()
  {
    CalcModelIdentity hp29c = CalcModelIds.Resolve("HP-29C");
    Assert.AreEqual("HP-29C", hp29c.CatalogId);
    Assert.AreEqual("HP-29", hp29c.EngineId);
    Assert.AreEqual("29C", hp29c.ShortId);
    Assert.AreEqual("T-29C", hp29c.ProductLabel);
    Assert.AreEqual("Woodstock", hp29c.Family);

    CalcModelIdentity hp34c = CalcModelIds.Resolve("HP-34C", "Spice");
    Assert.AreEqual("HP-34", hp34c.EngineId);
    Assert.AreEqual("34C", hp34c.ShortId);
    Assert.AreEqual("T-34C", hp34c.ProductLabel);
    Assert.AreEqual("Spice", hp34c.Family);
  }
}
