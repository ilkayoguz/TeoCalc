using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuIdleDisplayStabilityTests
{
  private static ClassicCpu CreateCpu()
  {
    ClassicMicrocodeRom rom = ClassicMicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    return new ClassicCpu(rom, catalog);
  }

  [TestMethod]
  public void IdleScan_WhenDisplayOn_RegistersSnapshotIsStable()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    string? latched = null;
    for (int batch = 0; batch < 60; batch++)
    {
      for (int i = 0; i < 200; i++)
      {
        cpu.Step();
        latched = ClassicFirmwareDisplay.TryBuildLedText(cpu.State) ?? latched;
      }
    }

    Assert.IsNotNull(latched);
    StringAssert.Contains(latched, "0;00");
  }
}
