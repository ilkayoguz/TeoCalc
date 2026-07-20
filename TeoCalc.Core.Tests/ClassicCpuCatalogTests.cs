using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuCatalogTests
{
  private static string Handlers => TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json");

  private static string Vocabulary => TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json");

  private static string RomBinary => TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin");

  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(RomBinary);
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(Handlers);
    return new ClassicCpu(rom, catalog);
  }

  [TestMethod]
  public void MicrocodeHandlerCatalog_HasClassicHandlers()
  {
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(Handlers);
    Assert.AreEqual("Classic", catalog.CpuFamily);
    Assert.AreEqual(40, catalog.Handlers.Count);
    Assert.IsTrue(catalog.Handlers.Exists(h => h.HandlerId == "ClassicCpu.SubroutineJump"));
  }

  [TestMethod]
  public void ProgramVocabulary_HasSixtyFourHp65Steps()
  {
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(Vocabulary);
    Assert.AreEqual("HP-65", vocabulary.Model);
    Assert.AreEqual(64, vocabulary.Steps.Count);
    Assert.AreEqual(40, vocabulary.KeyChart.Count);
    Assert.AreEqual("LBL", vocabulary.ResolveCode(43).Mnemonic);
    Assert.AreEqual("ProgramStep.LBL", vocabulary.ResolveCode(43).StepId);
  }

  [TestMethod]
  public void MicrocodeRom_LoadsHp65Binary()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(RomBinary);
    Assert.AreEqual(3072, rom.WordCount);
    Assert.AreEqual(773, rom.ReadWord(0));
  }

  [TestMethod]
  public void ClassicCpu_FirstStep_IsSubroutineJumpTo0xC1()
  {
    ClassicCpu cpu = CreateCpu();
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.SubroutineJump", entry.HandlerId);
    Assert.AreEqual("JSB", entry.Mnemonic.Trim());
    Assert.AreEqual(1, cpu.State.ReturnStack[0]);
    Assert.AreEqual(0xC1, cpu.State.ProgramCounter);
  }

  [TestMethod]
  public void ClassicCpu_SecondStep_AtJsbTarget_IsDelayedSelectGroup()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Step();
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.DelayedSelectGroup", entry.HandlerId);
    Assert.AreEqual(0xC2, cpu.State.ProgramCounter);
  }

  [TestMethod]
  public void ClassicCpu_Return_RestoresSavedProgramCounter()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.ReturnStack[0] = 1;
    cpu.State.ProgramCounter = 0x60;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.Return", entry.HandlerId);
    Assert.AreEqual(1, cpu.State.ProgramCounter);
  }

  [TestMethod]
  public void ClassicCpu_Branch_SkipsWhenPrevCarrySet()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.ProgramCounter = 1;
    cpu.State.Flags = ClassicCpuFlags.Carry;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.Branch", entry.HandlerId);
    Assert.AreEqual(2, cpu.State.ProgramCounter);
  }

  [TestMethod]
  public void ClassicCpu_Branch_TakesTargetWhenPrevCarryClear()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.ProgramCounter = 1;
    MicrocodeHandlerEntry entry = cpu.Step();
    Assert.AreEqual("ClassicCpu.Branch", entry.HandlerId);
    Assert.AreEqual(0x2E, cpu.State.ProgramCounter);
  }
}
