using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>
/// Wires firmware backends into <see cref="CalcFirmwareGatewayLocator"/>.
/// ROM-ready Classic family models use native <see cref="ClassicFirmwareGateway"/>;
/// other models keep the temporary emulator adapter.
/// </summary>
public static class CalcFirmwareBootstrap
{
  /// <summary>
  /// True when the model is Classic-family and ROM/handler assets are present
  /// (HP-35/45/55/65/70/80 today; HP-67 deferred until ROM-ready).
  /// </summary>
  public static bool IsNativeClassicPilot(string catalogOrEngineId)
  {
    CalcModelIdentity identity = CalcModelIds.Resolve(catalogOrEngineId);
    if (!string.Equals(identity.Family, "Classic", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    string engineId = NormalizeNativeClassicEngineId(identity);
    return NativeClassicAssetsExist(engineId);
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

  private static string NormalizeNativeClassicEngineId(CalcModelIdentity identity)
  {
    string engineId = identity.EngineId;
    if (engineId.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      return engineId;
    }

    return $"HP-{identity.ShortId}";
  }

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
      return true;
    }

    return PanamatikEngineFactory.IsSupported(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId))
    {
      return [];
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
    if (!string.Equals(model.Family, "Classic", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (string.IsNullOrWhiteSpace(model.Firmware.RomBinary)
        || string.IsNullOrWhiteSpace(model.Firmware.HandlerCatalog))
    {
      return false;
    }

    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));
    return File.Exists(romPath) && File.Exists(handlerPath);
  }
}
