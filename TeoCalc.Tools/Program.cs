using TeoCalc.Core;

namespace TeoCalc.Tools;

internal static class Program
{
  private static int Main(string[] args)
  {
    if (args.Length == 0)
    {
      Console.WriteLine("TeoCalc.Tools — usage: models");
      return 1;
    }

    if (args[0].Equals("models", StringComparison.OrdinalIgnoreCase))
    {
      Console.WriteLine($"Priority: {HpCalcModelCatalog.PriorityModel}");
      foreach (string model in HpCalcModelCatalog.SupportedModels)
        Console.WriteLine(model);
      return 0;
    }

    Console.Error.WriteLine($"Unknown command: {args[0]}");
    return 1;
  }
}
