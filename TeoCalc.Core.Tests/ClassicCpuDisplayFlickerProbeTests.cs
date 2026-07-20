using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuDisplayFlickerProbeTests
{
  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    return new ClassicCpu(rom, catalog);
  }

  [TestMethod]
  public void IdleBatches_DisplayOnSetAtAnyPointDuringBatch_MostlyTrue()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    int litBatches = 0;
    for (int batch = 0; batch < 40; batch++)
    {
      bool anyOn = false;
      for (int i = 0; i < 200; i++)
      {
        cpu.Step();
        if ((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0)
        {
          anyOn = true;
        }
      }

      if (anyOn)
      {
        litBatches++;
      }
    }

    Assert.IsTrue(litBatches >= 35, $"any-on batches={litBatches}/40");
  }

  [TestMethod]
  [Ignore("Manual display flicker probe; end-state sampling is no longer a regression expectation.")]
  public void IdleBatches_EndStateDisplayOn_IsUnstableSample()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    int endOn = 0;
    for (int batch = 0; batch < 40; batch++)
    {
      for (int i = 0; i < 200; i++)
      {
        cpu.Step();
      }

      if ((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0)
      {
        endOn++;
      }
    }

    Assert.IsTrue(endOn < 20, $"end-on batches={endOn}/40 — sampling end flag alone flickers");
  }
}
