using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicProgramMemoryTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    return new ClassicCpu(rom, catalog, Vocabulary);
  }

  [TestMethod]
  public void Reset_InitializesProgramWithStartAndPointer()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    Assert.AreEqual(ClassicProgramCodes.Start, cpu.Program.ReadCode(0));
    Assert.AreEqual(ClassicProgramCodes.Pointer, cpu.Program.ReadCode(1));
    Assert.AreEqual(63, cpu.State.Buffer);
    Assert.AreEqual(1, cpu.Program.PointerPosition());
  }

  [TestMethod]
  public void InsertFromBuffer_InsertsAtPointerAndShiftsTail()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    cpu.State.Buffer = ClassicProgramCodes.Label;
    cpu.Program.InsertFromBuffer();
    Assert.AreEqual("START", cpu.Program.FormatStep(0));
    Assert.AreEqual("LBL", cpu.Program.FormatStep(1));
    Assert.AreEqual("PTR", cpu.Program.FormatStep(2));
  }

  [TestMethod]
  public void DeleteBeforePointer_RemovesPreviousStep()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    cpu.State.Buffer = ClassicProgramCodes.Label;
    cpu.Program.InsertFromBuffer();
    cpu.Program.DeleteBeforePointer();
    Assert.AreEqual("START", cpu.Program.FormatStep(0));
    Assert.AreEqual("PTR", cpu.Program.FormatStep(1));
    Assert.AreEqual(string.Empty, cpu.Program.FormatStep(2));
  }

  [TestMethod]
  public void ProgramInput_ResolvesKeyChartAndMnemonics()
  {
    ProgramVocabulary vocabulary = Vocabulary;
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte digitKey));
    Assert.IsTrue(ClassicProgramInput.TryResolveStepCode(vocabulary, "LBL", out byte lblCode));
    Assert.AreEqual(43, lblCode);
    Assert.AreNotEqual(0, digitKey);
  }

  [TestMethod]
  public void PressKey_SetsKeyBufferForKeysToRomAddress()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    ClassicProgramInput.TryResolveStepCode(Vocabulary, "LBL", out byte lblCode);
    cpu.PressKey(lblCode);
    cpu.State.Rom = 2;
    cpu.State.ProgramCounter = 0x2CE;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.KeysToRomAddress", entry.HandlerId);
    Assert.AreEqual(lblCode, (byte)cpu.State.ProgramCounter);
  }

  [TestMethod]
  public void MemoryInitializeHandler_MatchesResetLayout()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Rom = 9;
    cpu.State.ProgramCounter = 0x936;
    cpu.Step();
    Assert.AreEqual(ClassicProgramCodes.Start, cpu.Program.ReadCode(0));
    Assert.AreEqual(ClassicProgramCodes.Pointer, cpu.Program.ReadCode(1));
  }
}
