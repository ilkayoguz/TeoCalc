using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Woodstock;

namespace TeoCalc.Core;

public static class WoodstockCpuFactory
{
  public static WoodstockCpu Create(TeoCalcModelDefinition model, string engineRoot)
  {
    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));

    WoodstockMicrocodeRom rom = WoodstockMicrocodeRom.LoadBinary(romPath);
    MicrocodeHandlerCatalog handlers = MicrocodeHandlerCatalog.Load(handlerPath);

    WoodstockCpu cpu = new(rom, handlers);
    cpu.Reset();
    return cpu;
  }
}
