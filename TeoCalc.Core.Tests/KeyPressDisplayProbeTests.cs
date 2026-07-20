using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class KeyPressDisplayProbeTests
{
  private static (MicrocodeRom Rom, MicrocodeHandlerCatalog Catalog, ProgramVocabulary Vocabulary) LoadModel()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    return (rom, catalog, vocabulary);
  }

  private static ClassicCpu WarmCpu(MicrocodeRom rom, MicrocodeHandlerCatalog catalog, ProgramVocabulary vocabulary)
  {
    ClassicCpu cpu = new(rom, catalog, vocabulary);
    cpu.Reset();
    for (int batch = 0; batch < 60; batch++)
    {
      for (int i = 0; i < 200; i++)
      {
        cpu.Step();
      }
    }

    return cpu;
  }

  private static int ProbeKeyDispatch(ClassicCpu cpu, Action<ClassicCpuState> applyKeyLine, int steps = 100_000)
  {
    int keysToRom = 0;
    int ioStepsUntilNext = 0;
    for (int i = 0; i < steps; i++)
    {
      applyKeyLine(cpu.State);
      MicrocodeHandlerEntry entry = cpu.Step();
      ioStepsUntilNext--;
      if (ioStepsUntilNext <= 0)
      {
        cpu.State.HandleIo();
        ioStepsUntilNext = 50;
      }

      cpu.State.ApplyKeyInput();

      if (entry.HandlerId == "ClassicCpu.KeysToRomAddress")
      {
        keysToRom++;
      }
    }

    return keysToRom;
  }

  [TestMethod]
  public void Press7_FirmwareReceivesKeyHeldLine()
  {
    (MicrocodeRom rom, MicrocodeHandlerCatalog catalog, ProgramVocabulary vocabulary) = LoadModel();
    ClassicCpu cpu = WarmCpu(rom, catalog, vocabulary);

    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);
    cpu.PressKey(keyCode);
    int keysToRom = 0;
    int ioStepsUntilNext = 0;
    List<string> trace = [];
    HashSet<int> addresses = [];
    for (int i = 0; i < 100_000; i++)
    {
      int address = cpu.State.FetchAddress;
      addresses.Add(address);
      ushort statusBefore = cpu.State.Status;
      ClassicCpuFlags flagsBefore = cpu.State.Flags;
      MicrocodeHandlerEntry entry = cpu.Step();
      ioStepsUntilNext--;
      if (ioStepsUntilNext <= 0)
      {
        cpu.State.HandleIo();
        ioStepsUntilNext = 50;
      }

      cpu.State.ApplyKeyInput();

      if (i < 120 && (address is >= 0x018E and <= 0x019B || entry.HandlerId != "ClassicCpu.Arithmetic"))
      {
        trace.Add(
          $"{i:D3} @{address:X4} {entry.HandlerId} S={statusBefore:X3}->{cpu.State.Status:X3} F={flagsBefore}->{cpu.State.Flags} P={cpu.State.P:X} PC={cpu.State.ProgramCounter:X4}");
      }

      if (entry.HandlerId == "ClassicCpu.KeysToRomAddress")
      {
        keysToRom++;
      }
    }

    Console.WriteLine("KeysToRomAddress hits: " + keysToRom);
    Console.WriteLine("PC: " + cpu.State.ProgramCounter.ToString("X4"));
    Console.WriteLine("KeyBuf: " + cpu.State.KeyBuffer);
    Console.WriteLine("C: " + string.Join(",", cpu.State.Registers.C));
    Console.WriteLine("Visited 02C8-02D0: " + string.Join(",", addresses.Where(a => a is >= 0x02C8 and <= 0x02D0).OrderBy(a => a).Select(a => a.ToString("X4"))));
    Console.WriteLine("Visited 04C8-04D0: " + string.Join(",", addresses.Where(a => a is >= 0x04C8 and <= 0x04D0).OrderBy(a => a).Select(a => a.ToString("X4"))));
    Console.WriteLine("Key path hits: " + string.Join(",", new[] { 0x01CA, 0x01CB, 0x0334, 0x0378, 0x02CD, 0x02CE, 0x04CC, 0x04CD }.Where(addresses.Contains).Select(a => a.ToString("X4"))));
    Console.WriteLine("Trace:");
    foreach (string line in trace)
    {
      Console.WriteLine(line);
    }

    Assert.IsTrue(keysToRom > 0, "Key press should reach KeysToRomAddress.");
  }

  [TestMethod]
  [Ignore("Manual trace probe.")]
  public void Press7_WritesTeoTrace_Probe()
  {
    (MicrocodeRom rom, MicrocodeHandlerCatalog catalog, ProgramVocabulary vocabulary) = LoadModel();
    ClassicCpu cpu = new(rom, catalog, vocabulary);
    cpu.Reset();
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);
    List<string> trace = ["step,phase,pc,opcode,handler,s_before,s_after,f_before,f_after,p_before,p_after,key,input,krom,btad"];
    int step = 0;
    int keysToRom = 0;
    int bufferToRom = 0;
    int ioStepsUntilNext = 0;

    TraceTicks("warmup", ticks: 60);
    cpu.PressKey(keyCode);
    TraceTicks("press7", ticks: 30);
    TraceTicks("release7", ticks: 30);
    trace.Add($"summary,pc,{cpu.State.ProgramCounter:X4},key,{cpu.State.KeyBuffer:X2},input,{cpu.State.KeyInputState},krom,{keysToRom},btad,{bufferToRom}");

    string outputPath = @"D:\$Board\Works\Side.Codes\TeoCalc\Catalog\Workspace\HpCalcExplorer\Reference\Decompiled\Panamatik\HP-65\teocalc-key7-trace.csv";
    File.WriteAllLines(outputPath, trace);
    Console.WriteLine(outputPath);
    Console.WriteLine(trace[^1]);
    Assert.Inconclusive("TeoCalc key7 trace written.");

    void TraceTicks(string phase, int ticks)
    {
      for (int tick = 0; tick < ticks; tick++)
      {
        for (int i = 0; i < 200; i++)
        {
          TraceStep(phase);
        }
      }
    }

    void TraceStep(string phase)
    {
      int address = cpu.State.FetchAddress;
      ushort opcode = rom.ReadWord(address);
      ushort statusBefore = cpu.State.Status;
      ClassicCpuFlags flagsBefore = cpu.State.Flags;
      byte pBefore = cpu.State.P;
      MicrocodeHandlerEntry entry = cpu.Step();
      ioStepsUntilNext--;
      if (ioStepsUntilNext <= 0)
      {
        cpu.State.HandleIo();
        ioStepsUntilNext = 50;
      }

      cpu.State.ApplyKeyInput();
      if (entry.HandlerId == "ClassicCpu.KeysToRomAddress")
      {
        keysToRom++;
      }
      else if (entry.HandlerId == "ClassicCpu.BufferToRomAddress")
      {
        bufferToRom++;
      }

      trace.Add(
        $"{step++},{phase},{address:X4},{opcode:X4},{entry.HandlerId},{statusBefore:X3},{cpu.State.Status:X3},{(byte)flagsBefore:X2},{(byte)cpu.State.Flags:X2},{pBefore:X1},{cpu.State.P:X1},{cpu.State.KeyBuffer:X2},{cpu.State.KeyInputState},{keysToRom},{bufferToRom}");
    }
  }

  [TestMethod]
  public void Press7_KeyLineStatusBitMatrix_Probe()
  {
    (MicrocodeRom rom, MicrocodeHandlerCatalog catalog, ProgramVocabulary vocabulary) = LoadModel();
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);

    for (int bit = 0; bit < 12; bit++)
    {
      ushort mask = (ushort)(1 << bit);

      ClassicCpu setCpu = WarmCpu(rom, catalog, vocabulary);
      setCpu.PressKey(keyCode);
      int setHits = ProbeKeyDispatch(setCpu, state => state.Status |= mask, steps: 20_000);

      ClassicCpu clearCpu = WarmCpu(rom, catalog, vocabulary);
      clearCpu.PressKey(keyCode);
      int clearHits = ProbeKeyDispatch(clearCpu, state => state.Status &= (ushort)~mask, steps: 20_000);

      Console.WriteLine(
        $"S{bit:D2}: set={setHits} pc={setCpu.State.ProgramCounter:X4} clear={clearHits} pc={clearCpu.State.ProgramCounter:X4}");
    }

    Assert.Inconclusive("Key line status-bit matrix probe.");
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
    Assert.IsTrue(session.DisplayText.Length > 0);
  }
}
