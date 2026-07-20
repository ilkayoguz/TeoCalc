using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>
/// Wires firmware backends into <see cref="CalcFirmwareGatewayLocator"/>.
/// Classic pilot (HP-65) uses native <see cref="ClassicFirmwareGateway"/>;
/// other models keep the temporary emulator adapter.
/// </summary>
public static class CalcFirmwareBootstrap
{
  /// <summary>Engine ids that run on native ClassicCpu (no Panamatik at runtime).</summary>
  public static bool IsNativeClassicPilot(string catalogOrEngineId)
  {
    CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
    return string.Equals(identity.EngineId, "HP-65", StringComparison.OrdinalIgnoreCase)
      || string.Equals(identity.ShortId, "65", StringComparison.OrdinalIgnoreCase);
  }

  public static void UseEmulatorAdapter()
  {
    CalcFirmwareGatewayLocator.Create = CreateGateway;
    CalcFirmwareGatewayLocator.IsSupported = IsSupported;
    CalcFirmwareGatewayLocator.GetAssetWarnings = GetAssetWarnings;
  }

  private static ICalcFirmwareGateway CreateGateway(string catalogOrEngineId)
  {
    CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
    if (IsNativeClassicPilot(catalogOrEngineId))
    {
      return CreateNativeClassicGateway(NormalizeNativeClassicEngineId(identity));
    }

    IPanamatikEngine engine = PanamatikEngineFactory.Create(identity.EngineId);
    return new EmulatorFirmwareGateway(engine);
  }

  private static string NormalizeNativeClassicEngineId(CalcModelIdentity identity) =>
    string.Equals(identity.ShortId, "65", StringComparison.OrdinalIgnoreCase)
      ? "HP-65"
      : identity.EngineId;

  private static ClassicFirmwareGateway CreateNativeClassicGateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    ClassicCpu cpu = ClassicCpuFactory.Create(model, engineRoot);
    ClassicFirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static bool IsSupported(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId))
    {
      CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
      return NativeClassicAssetsExist(NormalizeNativeClassicEngineId(identity));
    }

    return PanamatikEngineFactory.IsSupported(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId))
    {
      CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
      string engineId = NormalizeNativeClassicEngineId(identity);
      return NativeClassicAssetsExist(engineId)
        ? []
        : [$"Native Classic ROM/handlers missing for {engineId}."];
    }

    return PanamatikEngineFactory.GetAssetWarnings(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static bool NativeClassicAssetsExist(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    if (!File.Exists(modelPath))
    {
      return false;
    }

    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));
    return File.Exists(romPath) && File.Exists(handlerPath);
  }
}
