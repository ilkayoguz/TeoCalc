using TeoCalc.Core;
using TeoCalc.Rendering;

namespace TeoCalc;

internal class Program
{
  [STAThread]
  private static int Main(string[] args)
  {
    try
    {
      if (args.Length > 0 && args[0].Equals("models", StringComparison.OrdinalIgnoreCase))
      {
        foreach (string model in HpCalcModelCatalog.SupportedModels)
          Console.WriteLine(model);
        return 0;
      }

      return CalcAppHost.Run();
    }
    catch (Exception exception)
    {
      FatalErrorDialog.Show(exception);
      return 1;
    }
  }
}
