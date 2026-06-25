using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuRegisterTests
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
  public void ClassicCpu_SetP_AtRom1B9_SetsDigitPointer()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Rom = 1;
    cpu.State.DelRom = 1;
    cpu.State.ProgramCounter = 0x1B9;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.SetP", entry.HandlerId);
    Assert.AreEqual(3, cpu.State.P);
  }

  [TestMethod]
  public void ClassicCpu_IncrementP_AtRom5A_WrapsNibble()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.P = 15;
    cpu.State.ProgramCounter = 0x5A;
    cpu.Step();
    Assert.AreEqual(0, cpu.State.P);
  }

  [TestMethod]
  public void ClassicCpu_ArithmeticAt44_ClearsRegisterC()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Registers.C[0] = 7;
    cpu.State.Registers.C[13] = 4;
    cpu.State.ProgramCounter = 0x44;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.Arithmetic", entry.HandlerId);
    Assert.IsTrue(cpu.State.Registers.C.All(digit => digit == 0));
  }

  [TestMethod]
  public void ClassicCpu_TestStatusZero_SetsCarryWhenBitSet()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Status = 1 << 3;
    cpu.State.ProgramCounter = 0x22;
    cpu.Step();
    Assert.IsTrue((cpu.State.Flags & ClassicCpuFlags.Carry) != 0);
  }

  [TestMethod]
  public void ClassicCpu_TestStatusZero_ClearsCarryWhenBitClear()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.ProgramCounter = 0x22;
    cpu.Step();
    Assert.IsFalse((cpu.State.Flags & ClassicCpuFlags.Carry) != 0);
  }

  [TestMethod]
  public void ClassicCpu_LoadConstant_WritesDigitAndDecrementsP()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.P = 5;
    cpu.State.ProgramCounter = 0xA9;
    cpu.Step();
    Assert.AreEqual(2, cpu.State.Registers.C[5]);
    Assert.AreEqual(4, cpu.State.P);
  }
}
