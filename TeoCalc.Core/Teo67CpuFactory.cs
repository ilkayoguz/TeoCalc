using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Teo67;

namespace TeoCalc.Core;

public static class Teo67CpuFactory
{
  public static Teo67Cpu Create(TeoCalcModelDefinition model, string engineRoot)
  {
    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));

    MicrocodeRom rom = MicrocodeRom.LoadBinary(romPath);
    MicrocodeHandlerCatalog handlers = MicrocodeHandlerCatalog.Load(handlerPath);

    Teo67Cpu cpu = new(rom, handlers);
    cpu.Reset();
    return cpu;
  }
}
