using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo01;
using TeoCalc.Core.Engine.Teo19;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>
/// Wires firmware backends into <see cref="CalcFirmwareGatewayLocator"/>.
/// ROM-ready Classic, Woodstock, Spice, HP-67, HP-19C, and HP-01 models use native gateways;
/// other models keep the emulator adapter.
/// </summary>
public static class CalcFirmwareBootstrap
{
  /// <summary>
  /// True when the model is Classic-family and ROM/handler assets are present
  /// (HP-35/45/55/65/70/80). HP-67 is ACT ISA — see <see cref="IsNativeTeo67Pilot"/>.
  /// </summary>
  public static bool IsNativeClassicPilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    if (string.Equals(engineId, "HP-67", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

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

  /// <summary>True when HP-67 ACT ROM/handler assets are present.</summary>
  public static bool IsNativeTeo67Pilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "Hp67");
  }

  /// <summary>True when HP-19C ACT ROM/handler assets are present.</summary>
  public static bool IsNativeTeo19Pilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "Hp19");
  }

  /// <summary>True when HP-01 ACThp01 ROM/handler assets are present.</summary>
  public static bool IsNativeTeo01Pilot(string catalogOrEngineId)
  {
    string engineId = NormalizeEngineId(CalcModelIds.Resolve(catalogOrEngineId));
    return NativeFamilyAssetsExist(engineId, "HP01");
  }

  /// <summary>
  /// Optional T-01 tone sink for alarm / stopwatch beeps. Hosts set this before
  /// <see cref="UseEmulatorAdapter"/>; tests keep the default no-op sink.
  /// </summary>
  public static ITeo01ToneSink Teo01ToneSink { get; set; } = NullTeo01ToneSink.Instance;

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

    if (IsNativeTeo67Pilot(catalogOrEngineId))
    {
      return CreateNativeTeo67Gateway(NormalizeEngineId(identity));
    }

    if (IsNativeTeo19Pilot(catalogOrEngineId))
    {
      return CreateNativeTeo19Gateway(NormalizeEngineId(identity));
    }

    if (IsNativeTeo01Pilot(catalogOrEngineId))
    {
      return CreateNativeTeo01Gateway(NormalizeEngineId(identity));
    }

    IPanamatikEngine engine = PanamatikEngineFactory.Create(identity.EngineId);
    // Native T-01 / T-19C prefer dedicated gateways; if assets are missing and the
    // emulator adapter is still used, keep reference timer cadences (10ms / 50ms).
    string engineId = NormalizeEngineId(identity);
    if (string.Equals(engineId, "HP-01", StringComparison.OrdinalIgnoreCase))
    {
      return new EmulatorFirmwareGateway(engine, runTickSeconds: 0.01f, stepsPerBatch: 100);
    }

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

  private static Teo67FirmwareGateway CreateNativeTeo67Gateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    Teo67Cpu cpu = Teo67CpuFactory.Create(model, engineRoot);
    Teo67FirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static Teo19FirmwareGateway CreateNativeTeo19Gateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    Teo19Cpu cpu = Teo19CpuFactory.Create(model, engineRoot);
    Teo19FirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static Teo01FirmwareGateway CreateNativeTeo01Gateway(string engineId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    Teo01Cpu cpu = Teo01CpuFactory.Create(model, engineRoot, Teo01ToneSink);
    Teo01FirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    return gateway;
  }

  private static bool IsSupported(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId)
        || IsNativeWoodstockPilot(catalogOrEngineId)
        || IsNativeSpicePilot(catalogOrEngineId)
        || IsNativeTeo67Pilot(catalogOrEngineId)
        || IsNativeTeo19Pilot(catalogOrEngineId)
        || IsNativeTeo01Pilot(catalogOrEngineId))
    {
      return true;
    }

    return PanamatikEngineFactory.IsSupported(CalcModelIds.Resolve(catalogOrEngineId).EngineId);
  }

  private static IReadOnlyList<string> GetAssetWarnings(string catalogOrEngineId)
  {
    if (IsNativeClassicPilot(catalogOrEngineId)
        || IsNativeWoodstockPilot(catalogOrEngineId)
        || IsNativeSpicePilot(catalogOrEngineId)
        || IsNativeTeo67Pilot(catalogOrEngineId)
        || IsNativeTeo19Pilot(catalogOrEngineId)
        || IsNativeTeo01Pilot(catalogOrEngineId))
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
