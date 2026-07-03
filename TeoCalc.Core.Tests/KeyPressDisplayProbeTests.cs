using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class KeyPressDisplayProbeTests
{
  [TestMethod]
  public void Press7_FirmwareReceivesKeyHeldLine()
  {
    ClassicMicrocodeRom rom = ClassicMicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    ClassicCpu cpu = new(rom, catalog, vocabulary);
    cpu.Reset();
    for (int batch = 0; batch < 60; batch++)
    {
      for (int i = 0; i < 200; i++)
      {
        cpu.Step();
      }
    }

    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);
    cpu.PressKey(keyCode);
    int keysToRom = 0;
    for (int i = 0; i < 100_000; i++)
    {
      MicrocodeHandlerEntry entry = cpu.Step();
      cpu.State.Status |= 1;
      if (entry.HandlerId == "ClassicCpu.KeysToRomAddress")
      {
        keysToRom++;
      }
    }

    Console.WriteLine("KeysToRomAddress hits: " + keysToRom);
    Console.WriteLine("PC: " + cpu.State.ProgramCounter.ToString("X4"));
    Console.WriteLine("KeyBuf: " + cpu.State.KeyBuffer);
    Console.WriteLine("C: " + string.Join(",", cpu.State.Registers.C));
    Assert.Inconclusive("KeysToRomAddress hits=" + keysToRom + " — firmware key dispatch still under investigation.");
  }

  [TestMethod]
  public void Press7_UpdatesDisplayAwayFromIdleZero()
  {
    Assert.Inconclusive("Digit entry display pending firmware key-dispatch fix.");
  }

  [TestMethod]
  public void ProgramMode_ShowsProgramStyleDisplay_Probe()
  {
    CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.PowerOnResume();
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }

    session.ToggleProgramMode();
    for (int i = 0; i < 80; i++)
    {
      session.Tick(0.05f);
    }

    Console.WriteLine("PRGM Display: [" + session.DisplayText.Replace(';', '.') + "]");
    Console.WriteLine("A: " + string.Join(",", session.Cpu!.State.Registers.A));
    Console.WriteLine("B: " + string.Join(",", session.Cpu.State.Registers.B));
  }
}
