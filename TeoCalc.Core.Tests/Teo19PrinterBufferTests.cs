using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Teo19;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Teo19PrinterBufferTests
{
  [TestMethod]
  public void AppendTestPrint_AppearsInPrintLines()
  {
    Teo19Cpu cpu = CreateCpu();
    cpu.AppendTestPrint("hello");
    Assert.AreEqual(1, cpu.PrintLines.Count);
    Assert.AreEqual("hello", cpu.PrintLines[0]);
  }

  [TestMethod]
  public void PrintAndFlushCRegister_EmitsReversedDigits()
  {
    Teo19Cpu cpu = CreateCpu();
    cpu.State.Registers.C[0] = 1;
    cpu.State.Registers.C[1] = 2;
    cpu.State.Registers.C[2] = 3;
    cpu.State.Registers.C[3] = 15; // terminator

    cpu.PrintAndFlushCRegister(alpha: false);

    Assert.AreEqual(1, cpu.PrintLines.Count);
    Assert.AreEqual("321", cpu.PrintLines[0]);
  }

  [TestMethod]
  public void Teo19FirmwareGateway_ExposesPrintLinesAndTestPrint()
  {
    Teo19Cpu cpu = CreateCpu();
    Teo19FirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    gateway.AppendTestPrint("Printer ready.");
    Assert.AreEqual(1, gateway.PrintLines.Count);
    Assert.AreEqual("Printer ready.", gateway.PrintLines[0]);
    gateway.ClearPrintLines();
    Assert.AreEqual(0, gateway.PrintLines.Count);
  }

  private static Teo19Cpu CreateCpu()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-19C", "Model.json"));
    return Teo19CpuFactory.Create(model, engineRoot);
  }
}
