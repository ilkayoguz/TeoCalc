using TeoCalc.Core;
using TeoCalc.Rendering;

namespace TeoCalc;

internal class Program
{
  private static int Main(string[] args)
  {
    if (args.Length > 0 && args[0].Equals("models", StringComparison.OrdinalIgnoreCase))
    {
      foreach (string model in HpCalcModelCatalog.SupportedModels)
        Console.WriteLine(model);
      return 0;
    }

    return CalcAppHost.Run();
  }
}
