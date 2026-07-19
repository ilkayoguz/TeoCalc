using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>Wires the emulator adapter into <see cref="CalcFirmwareGatewayLocator"/>.</summary>
public static class CalcFirmwareBootstrap
{
  public static void UseEmulatorAdapter()
  {
    CalcFirmwareGatewayLocator.Create = CreateGateway;
    CalcFirmwareGatewayLocator.IsSupported = IsSupported;
    CalcFirmwareGatewayLocator.GetAssetWarnings = GetAssetWarnings;
  }

  private static ICalcFirmwareGateway CreateGateway(string catalogOrEngineId)
  {
    string engineId = CalcModelIds.ToEngineId(catalogOrEngineId);
    IPanamatikEngine engine = PanamatikEngineFactory.Create(engineId);
    return new EmulatorFirmwareGateway(engine);
  }

  private static bool IsSupported(string catalogOrEngineId) =>
    PanamatikEngineFactory.IsSupported(CalcModelIds.ToEngineId(catalogOrEngineId));

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId) =>
    PanamatikEngineFactory.GetAssetWarnings(CalcModelIds.ToEngineId(catalogOrEngineId));
}
