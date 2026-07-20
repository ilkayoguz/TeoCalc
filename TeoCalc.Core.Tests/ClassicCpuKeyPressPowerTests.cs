using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuKeyPressPowerTests
{
  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    return new ClassicCpu(rom, catalog, vocabulary);
  }

  [TestMethod]
  public void TogglePowerViaXor_WhenDisplayAlreadyOn_TurnsOff()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    cpu.State.Flags ^= ClassicCpuFlags.DisplayOn;
    Assert.IsFalse((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0);
  }

  [TestMethod]
  public void PressKeyDigit7_After200Steps_LeavesDisplayOnWhenStartedOn()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
    ClassicProgramInput.TryResolveKeyCode(
      ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json")),
      '7',
      out byte keyCode);

    cpu.PressKey(keyCode);
    int displayOff = 0;
    int displayToggle = 0;
    for (int step = 0; step < 200; step++)
    {
      MicrocodeHandlerEntry entry = cpu.Step();
      if (entry.HandlerId == "ClassicCpu.DisplayOff")
      {
        displayOff++;
      }
      else if (entry.HandlerId == "ClassicCpu.DisplayToggle")
      {
        displayToggle++;
      }
    }

    Assert.IsTrue((cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0);
    Assert.AreEqual(0, displayOff, "DisplayOff handlers during digit entry");
    Assert.AreEqual(0, displayToggle, "DisplayToggle handlers during digit entry");
  }
}
