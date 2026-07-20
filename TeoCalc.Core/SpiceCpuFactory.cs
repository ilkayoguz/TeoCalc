using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Spice;

namespace TeoCalc.Core;

public static class SpiceCpuFactory
{
  public static SpiceCpu Create(TeoCalcModelDefinition model, string engineRoot)
  {
    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));

    SpiceMicrocodeRom rom = SpiceMicrocodeRom.LoadBinary(romPath);
    MicrocodeHandlerCatalog handlers = MicrocodeHandlerCatalog.Load(handlerPath);

    SpiceCpu cpu = new(rom, handlers);
    cpu.Reset();
    return cpu;
  }
}