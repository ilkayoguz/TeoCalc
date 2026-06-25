using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuStackDisplayTests
{
  private static ClassicCpu CreateCpu()
  {
    ClassicMicrocodeRom rom = ClassicMicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    return new ClassicCpu(rom, catalog, vocabulary);
  }

  [TestMethod]
  public void CToStack_RollsStackAndCopiesCIntoY()
  {
    ClassicCpu cpu = CreateCpu();
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      cpu.State.Registers.C[index] = index;
      cpu.State.Registers.Y[index] = 20;
      cpu.State.Registers.Z[index] = 30;
      cpu.State.Registers.T[index] = 40;
    }

    cpu.State.ProgramCounter = 0x43;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.CToStack", entry.HandlerId);
    Assert.AreEqual(0, cpu.State.Registers.Y[0]);
    Assert.AreEqual(20, cpu.State.Registers.Z[0]);
    Assert.AreEqual(30, cpu.State.Registers.T[0]);
    Assert.AreEqual(0, cpu.State.Registers.C[0]);
  }

  [TestMethod]
  public void DownRotate_RotatesCThroughT()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Registers.C[0] = 1;
    cpu.State.Registers.Y[0] = 2;
    cpu.State.Registers.Z[0] = 3;
    cpu.State.Registers.T[0] = 4;
    cpu.State.Rom = 2;
    cpu.State.ProgramCounter = 0x2E4;
    cpu.Step();
    Assert.AreEqual(2, cpu.State.Registers.C[0]);
    Assert.AreEqual(3, cpu.State.Registers.Y[0]);
    Assert.AreEqual(4, cpu.State.Registers.Z[0]);
    Assert.AreEqual(1, cpu.State.Registers.T[0]);
  }

  [TestMethod]
  public void DisplayToggle_FlipsDisplayOnFlag()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Rom = 1;
    cpu.State.ProgramCounter = 0x1DA;
    cpu.Step();
    Assert.IsTrue((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0);
    cpu.State.Rom = 1;
    cpu.State.ProgramCounter = 0x1DA;
    cpu.Step();
    Assert.IsFalse((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0);
  }

  [TestMethod]
  public void DisplayFormatter_ShowsDigitsWhenDisplayOn()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    cpu.State.Registers.A[12] = 5;
    cpu.State.Registers.B[12] = 0;
    string text = ClassicDisplayFormatter.FormatXRegister(cpu);
    Assert.IsTrue(text.Contains('5'));
  }

  [TestMethod]
  public void ProgramListing_FormatsInsertedSteps()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    cpu.State.Buffer = ClassicProgramCodes.Label;
    cpu.Program.InsertFromBuffer();
    string listing = ClassicProgramListing.Format(cpu.Program);
    Assert.IsTrue(listing.Contains("START"));
    Assert.IsTrue(listing.Contains("LBL"));
    Assert.IsTrue(listing.Contains("PTR"));
  }
}
