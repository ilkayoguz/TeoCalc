using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcModelIdsTests
{
  [TestMethod]
  public void ToEngineId_MapsCatalogSuffixesToTeoFolders()
  {
    Assert.AreEqual("T-29", CalcModelIds.ToEngineId("HP-29C"));
    Assert.AreEqual("T-31", CalcModelIds.ToEngineId("HP-31E"));
    Assert.AreEqual("T-65", CalcModelIds.ToEngineId("HP-65"));
    Assert.AreEqual("T-65", CalcModelIds.ToEngineId("T-65"));
    Assert.AreEqual("T-65", CalcModelIds.ToEngineId("65"));
    Assert.AreEqual("T-29", CalcModelIds.ToEngineId("T-29C"));
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
    Assert.AreEqual("T-29", hp29c.EngineId);
    Assert.AreEqual("29C", hp29c.ShortId);
    Assert.AreEqual("T-29C", hp29c.ProductLabel);
    Assert.AreEqual("Woodstock", hp29c.Family);

    CalcModelIdentity hp34c = CalcModelIds.Resolve("HP-34C", "Spice");
    Assert.AreEqual("T-34", hp34c.EngineId);
    Assert.AreEqual("34C", hp34c.ShortId);
    Assert.AreEqual("T-34C", hp34c.ProductLabel);
    Assert.AreEqual("Spice", hp34c.Family);
  }

  [TestMethod]
  public void InferFamily_UsesTeoEngineIds()
  {
    Assert.AreEqual("Teo01", CalcModelIds.InferFamily("HP-01"));
    Assert.AreEqual("Teo19", CalcModelIds.InferFamily("T-19C"));
    Assert.AreEqual("Teo67", CalcModelIds.InferFamily("HP-67BE"));
    Assert.AreEqual("Classic", CalcModelIds.InferFamily("T-65"));
  }
}
