using TeoCalc.Core;

namespace TeoCalc.Tools;

internal static class Program
{
  private static int Main(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine("TeoCalc.Tools — usage: models | cards [outputDir]");
      return 1;
    }

    if (args[0].Equals("models", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Priority: {TeoCalcModelCatalog.PriorityModel}");
      foreach (string model in TeoCalcModelCatalog.SupportedModels)
      {
        Console.WriteLine(model);
      }

      return 0;
    }

    if (args[0].Equals("cards", StringComparison.OrdinalIgnoreCase))
    {
      string? outDir = args.Length > 1 ? args[1] : null;
      return SampleCardExporter.ExportAll(outDir);
    }

    Console.Error.WriteLine($"Unknown command: {args[0]}");
    return 1;
  }
}
