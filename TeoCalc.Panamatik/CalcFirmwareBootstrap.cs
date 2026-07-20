using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>
/// Wires firmware backends into <see cref="CalcFirmwareGatewayLocator"/>.
/// ROM-ready Classic, Woodstock, and Spice family models use native gateways;
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
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "Classic");
  }

  /// <summary>
  /// True when the model is Woodstock-family and ROM/handler assets are present
  /// (HP-21/22/25/27/29 share one CPU core).
  /// </summary>
  public static bool IsNativeWoodstockPilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "Woodstock");
  }

  /// <summary>
  /// True when the model is Spice-family and ROM/handler assets are present
  /// (HP-31/32/33/34/37/38 share one CPU core).
  /// </summary>
  public static bool IsNativeSpicePilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "Spice");
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
      return CreateNativeClassicGateway(NormalizeEngineId(identity));
    }

    if (IsNativeWoodstockPilot(catalogOrEngineId))
    {
      return CreateNativeWoodstockGateway(NormalizeEngineId(identity));
    }

    if (IsNativeSpicePilot(catalogOrEngineId))
    {
      return CreateNativeSpiceGateway(NormalizeEngineId(identity));
    }

    IPanamatikEngine engine = PanamatikEngineFactory.Create(identity.EngineId);
    return new EmulatorFirmwareGateway(engine);
  }

  private static string NormalizeEngineId(CalcModelIdentity identity)
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

  private static WoodstockFirmwareGateway CreateNativeWoodstockGateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    WoodstockCpu cpu = WoodstockCpuFactory.Create(model, engineRoot);
    WoodstockFirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static SpiceFirmwareGateway CreateNativeSpiceGateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    SpiceCpu cpu = SpiceCpuFactory.Create(model, engineRoot);
    SpiceFirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static bool IsSupported(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId)
        || IsNativeWoodstockPilot(catalogOrEngineId)
        || IsNativeSpicePilot(catalogOrEngineId))
    {
      return true;
    }

    return PanamatikEngineFactory.IsSupported(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId)
        || IsNativeWoodstockPilot(catalogOrEngineId)
        || IsNativeSpicePilot(catalogOrEngineId))
    {
      return [];
    }

    return PanamatikEngineFactory.GetAssetWarnings(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static bool NativeFamilyAssetsExist(string engineId, string expectedFamily)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    if (!File.Exists(modelPath))
    {
      return false;
    }

    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    if (!string.Equals(model.Family, expectedFamily, StringComparison.OrdinalIgnoreCase))
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
