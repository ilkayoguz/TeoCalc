namespace TeoCalc.Panamatik;

public static class PanamatikEngineFactory
{
  public static bool TryCreate(string teoCalcModelId, out IPanamatikEngine? engine)
  {
    if (!PanamatikEngineRegistry.TryGet(teoCalcModelId, out PanamatikEngineBinding binding))
    {
      engine = null;
      return false;
    }

    string modelDirectory = ResolveModelDirectory(binding.SourceFolderId);
    engine = new PanamatikFormEngine(teoCalcModelId, modelDirectory, binding.FormType);
    return true;
  }

  public static IPanamatikEngine Create(string teoCalcModelId)
  {
    if (!TryCreate(teoCalcModelId, out IPanamatikEngine? engine) || engine is null)
    {
      throw new NotSupportedException($"No Panamatik engine is registered for '{teoCalcModelId}'.");
    }

    return engine;
  }

  public static bool IsSupported(string teoCalcModelId) =>
    PanamatikEngineRegistry.TryGet(teoCalcModelId, out _);

  internal static string ResolveModelDirectory(string sourceFolderId)
  {
    string assemblyDirectory = Path.GetDirectoryName(typeof(PanamatikEngineFactory).Assembly.Location)!;
    string fromAssembly = Path.Combine(assemblyDirectory, "Sources", sourceFolderId);
    if (Directory.Exists(fromAssembly))
    {
      return fromAssembly;
    }

    return Path.Combine(AppContext.BaseDirectory, "Sources", sourceFolderId);
  }

  public static IReadOnlyList<string> GetAssetWarnings(string teoCalcModelId)
  {
    if (!PanamatikEngineRegistry.TryGet(teoCalcModelId, out PanamatikEngineBinding binding))
    {
      return [];
    }

    List<string> warnings = [];
    string directory = ResolveModelDirectory(binding.SourceFolderId);
    if (!File.Exists(Path.Combine(directory, binding.KmlFileName)))
    {
      warnings.Add($"Panamatik keyboard layout '{binding.KmlFileName}' is missing from {directory}.");
    }

    return warnings;
  }
}
