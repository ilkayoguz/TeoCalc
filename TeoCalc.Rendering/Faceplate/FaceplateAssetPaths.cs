using TeoCalc.Core;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Resolves faceplate asset directories: model-specific → Shared → HP-65 legacy fallback.
/// </summary>
public static class FaceplateAssetPaths
{
  public static string ResolveAssetsRoot(string? catalogOrEngineId)
  {
    string engineId = string.IsNullOrWhiteSpace(catalogOrEngineId)
      ? "HP-65"
      : CalcModelIds.ToEngineId(catalogOrEngineId);

    string modelRoot = TeoCalcPaths.ResourcePath(Path.Combine("Engine", engineId, "Assets"));
    if (Directory.Exists(modelRoot))
    {
      return modelRoot;
    }

    string sharedRoot = TeoCalcPaths.ResourcePath(Path.Combine("Engine", "Shared", "Assets"));
    if (Directory.Exists(sharedRoot))
    {
      return sharedRoot;
    }

    return TeoCalcPaths.ResourcePath(Path.Combine("Engine", "HP-65", "Assets"));
  }

  public static string ResolveFile(string? catalogOrEngineId, params string[] relativeParts)
  {
    string fileName = Path.Combine(relativeParts);
    string engineId = string.IsNullOrWhiteSpace(catalogOrEngineId)
      ? "HP-65"
      : CalcModelIds.ToEngineId(catalogOrEngineId);

    foreach (string rootCandidate in CandidateRoots(engineId))
    {
      string path = Path.Combine(rootCandidate, fileName);
      if (File.Exists(path))
      {
        return path;
      }
    }

    return Path.Combine(ResolveAssetsRoot(engineId), fileName);
  }

  private static IEnumerable<string> CandidateRoots(string engineId)
  {
    yield return TeoCalcPaths.ResourcePath(Path.Combine("Engine", engineId, "Assets"));
    yield return TeoCalcPaths.ResourcePath(Path.Combine("Engine", "Shared", "Assets"));
    yield return TeoCalcPaths.ResourcePath(Path.Combine("Engine", "HP-65", "Assets"));
  }
}
