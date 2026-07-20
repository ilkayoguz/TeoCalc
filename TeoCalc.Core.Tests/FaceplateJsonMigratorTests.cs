using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class FaceplateJsonMigratorTests
{
  /// <summary>
  /// Manual one-shot: set TEOCALC_EXPORT_FACEPLATE=1 to rewrite CapFace/Style into JSON.
  /// Not run in normal CI — migration already applied to Resource/Engine.
  /// </summary>
  [TestMethod]
  public void Export_All_Models_CapFace_Style_And_Faceplate_Policy()
  {
    if (!string.Equals(
          Environment.GetEnvironmentVariable("TEOCALC_EXPORT_FACEPLATE"),
          "1",
          StringComparison.Ordinal))
    {
      Assert.Inconclusive("Set TEOCALC_EXPORT_FACEPLATE=1 to re-export faceplate JSON.");
    }

    FaceplateJsonMigrator.ExportAll();
  }
}
