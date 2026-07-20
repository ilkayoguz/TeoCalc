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
    CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
    IPanamatikEngine engine = PanamatikEngineFactory.Create(identity.EngineId);
    return new EmulatorFirmwareGateway(engine);
  }

  private static bool IsSupported(string catalogOrEngineId) =>
    PanamatikEngineFactory.IsSupported(CalcModelIds.Resolve(catalogOrEngineId).EngineId);

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId) =>
    PanamatikEngineFactory.GetAssetWarnings(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
}
