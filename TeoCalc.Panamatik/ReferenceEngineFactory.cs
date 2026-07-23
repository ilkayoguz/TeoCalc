namespace TeoCalc.ReferenceEmulator;

public static class ReferenceEngineFactory
{
  public static bool TryCreate(string teoCalcModelId, out IReferenceEngine? engine)
  {
    if (!ReferenceEngineRegistry.TryGet(teoCalcModelId, out ReferenceEngineBinding binding))
    {
      engine = null;
      return false;
    }

    string modelDirectory = ResolveModelDirectory(binding.SourceFolderId);
    engine = new ReferenceFormEngine(teoCalcModelId, modelDirectory, binding.FormType);
    return true;
  }

  public static IReferenceEngine Create(string teoCalcModelId)
  {
    if (!TryCreate(teoCalcModelId, out IReferenceEngine? engine) || engine is null)
    {
      throw new NotSupportedException($"No reference engine is registered for '{teoCalcModelId}'.");
    }

    return engine;
  }

  public static bool IsSupported(string teoCalcModelId) =>
    ReferenceEngineRegistry.TryGet(teoCalcModelId, out _);

  internal static string ResolveModelDirectory(string sourceFolderId)
  {
    string assemblyDirectory = Path.GetDirectoryName(typeof(ReferenceEngineFactory).Assembly.Location)!;
    string fromAssembly = Path.Combine(assemblyDirectory, "Sources", sourceFolderId);
    if (Directory.Exists(fromAssembly))
    {
      return fromAssembly;
    }

    return Path.Combine(AppContext.BaseDirectory, "Sources", sourceFolderId);
  }

  public static IReadOnlyList<string> GetAssetWarnings(string teoCalcModelId)
  {
    if (!ReferenceEngineRegistry.TryGet(teoCalcModelId, out ReferenceEngineBinding binding))
    {
      return [];
    }

    List<string> warnings = [];
    string directory = ResolveModelDirectory(binding.SourceFolderId);
    if (!File.Exists(Path.Combine(directory, binding.KmlFileName)))
    {
      warnings.Add($"Reference keyboard layout '{binding.KmlFileName}' is missing from {directory}.");
    }

    return warnings;
  }
}
