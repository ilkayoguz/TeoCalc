using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class HelpAboutPolishTests
{
  [TestMethod]
  public void ClassicCrossRef_ResolvesKnownHandler()
  {
    string path = TeoCalcPaths.ResourcePath("Engine/Classic/microcode.crossref.json");
    Assert.IsTrue(File.Exists(path), path);
    MicrocodeCrossRefCatalog catalog = MicrocodeCrossRefCatalog.Load(path);
    Assert.IsTrue(catalog.Handlers.Count > 0);
    MicrocodeCrossRefEntry first = catalog.Handlers[0];
    MicrocodeCrossRefEntry? found = catalog.TryGetHandler(first.HandlerId);
    Assert.IsNotNull(found);
    Assert.AreEqual(first.HandlerId, found.HandlerId);
  }

  [TestMethod]
  public void Hp65Session_LoadsCrossRef()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int idx = Array.FindIndex(session.Models, id => id.Contains("65", StringComparison.Ordinal));
    Assert.IsTrue(idx >= 0);
    session.LoadModel(idx);
    Assert.IsNotNull(session.CrossRef);
    Assert.IsTrue(session.CrossRef!.Handlers.Count > 0);
  }
}
