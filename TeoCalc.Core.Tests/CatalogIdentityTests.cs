using TeoCalc.Core;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CatalogIdentityTests
{
  [TestMethod]
  public void ModelJson_DisplayNamesAreProductLabels()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    foreach (string path in Directory.EnumerateFiles(engineRoot, "Model.json", SearchOption.AllDirectories))
    {
      TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(path);
      string expected = CalcModelIds.ToProductLabel(
        string.IsNullOrWhiteSpace(model.DisplayName) ? model.Model : model.DisplayName);
      Assert.IsTrue(
        model.DisplayName.StartsWith("T-", StringComparison.Ordinal),
        $"{path}: DisplayName '{model.DisplayName}' should be T-*");
      Assert.AreEqual(expected, model.DisplayName, path);
      Assert.IsTrue(
        model.Model.StartsWith("T-", StringComparison.OrdinalIgnoreCase),
        $"{path}: Model key '{model.Model}' should be T-*");
      Assert.AreEqual(CalcModelIds.ToEngineId(model.Model), model.Model, path);
    }
  }

  [TestMethod]
  public void FamilyJson_DisplayNamesAreNeutral()
  {
    foreach (string family in new[] { "Classic", "Woodstock", "Spice" })
    {
      string path = TeoCalcPaths.ResourcePath(Path.Combine("Engine", family, "Family.json"));
      string json = File.ReadAllText(path);
      Assert.IsFalse(json.Contains("HP Classic", StringComparison.OrdinalIgnoreCase), path);
      Assert.IsFalse(json.Contains("HP Woodstock", StringComparison.OrdinalIgnoreCase), path);
      Assert.IsFalse(json.Contains("HP Spice", StringComparison.OrdinalIgnoreCase), path);
      Assert.IsFalse(json.Contains("Panamatik", StringComparison.OrdinalIgnoreCase), path);
    }
  }
}
