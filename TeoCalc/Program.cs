using TeoCalc.Core;
using TeoCalc.Panamatik;
using TeoCalc.Rendering;

namespace TeoCalc;

internal class Program
{
  [STAThread]
  private static int Main(string[] args)
  {
    try
    {
      CalcFirmwareBootstrap.Teo01ToneSink = HostTeo01ToneSink.Instance;
      CalcFirmwareBootstrap.UseEmulatorAdapter();

      if (args.Length > 0 && args[0].Equals("models", StringComparison.OrdinalIgnoreCase))
      {
        foreach (string model in TeoCalcModelCatalog.SupportedModels)
        {
          Console.WriteLine(model);
        }

        return 0;
      }

      return CalcAppHost.Run(args);
    }
    catch (Exception exception)
    {
      FatalErrorDialog.Show(exception);
      return 1;
    }
  }
}
