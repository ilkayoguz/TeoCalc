using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicProgramMemoryTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/T-65/Firmware/hp65.microcode.bin"));
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
  public void InsertFromBuffer_OverwriteMode_ReplacesStepAfterPointer()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    cpu.State.Buffer = ClassicProgramCodes.Label;
    cpu.Program.InsertFromBuffer();
    cpu.State.Buffer = 11; // A
    cpu.Program.InsertFromBuffer();
    cpu.State.Buffer = 24; // RTN-ish
    cpu.Program.InsertFromBuffer();

    // PTR sits after inserted codes; seek so pointer is just before the last opcode.
    int last = cpu.Program.LastContentIndex();
    int rtnIndex = last - 1; // last is usually PTR
    while (rtnIndex > 1 && cpu.Program.ReadCode(rtnIndex) == ClassicProgramCodes.Pointer)
    {
      rtnIndex--;
    }

    cpu.Program.SeekPointer(Math.Max(1, rtnIndex - 1));
    byte before = cpu.Program.ReadCode(cpu.Program.PointerPosition() + 1);
    Assert.AreNotEqual(0, before);

    cpu.Program.OverwriteOnInsert = true;
    cpu.State.Buffer = 3; // digit 2 keycode path often lands in Buffer via firmware
    cpu.Program.InsertFromBuffer();

    Assert.AreEqual(3, cpu.Program.ReadCode(cpu.Program.PointerPosition() + 1));
    // Tail / earlier steps must remain (no insert-shift wipe).
    Assert.AreEqual(ClassicProgramCodes.Label, cpu.Program.ReadCode(1));
  }

  [TestMethod]
  public void Listing_Enumerate_KeepsCodesAfterMidProgramNop()
  {
    byte[] codes = [63, 1, 0, 3, 4, 0, 0, 0];
    List<ClassicProgramLine> lines = ClassicProgramListing.ToList(codes, c => c.ToString()).ToList();
    Assert.IsTrue(lines.Any(l => l.Index == 3 && l.Code == 3), "code after mid NOP must remain");
    Assert.IsTrue(lines.Any(l => l.Index == 4 && l.Code == 4));
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

  [TestMethod]
  public void SeekPointer_MovesMarkerToTargetIndex()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    Assert.AreEqual(1, cpu.Program.PointerPosition());
    // Seed body past the seek target so LastContentIndex allows the move.
    for (int i = 0; i < 6; i++)
    {
      cpu.State.Buffer = (byte)(i + 1);
      cpu.Program.InsertFromBuffer();
    }

    cpu.Program.SeekPointer(5);
    Assert.AreEqual(5, cpu.Program.PointerPosition());
    Assert.AreEqual(ClassicProgramCodes.Pointer, cpu.Program.ReadCode(5));
  }

  [TestMethod]
  public void SeekPointer_PastTrailingZeros_DoesNotDestroyProgram()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    // Insert a short body after PTR: LBL A, 1, RTN
    cpu.State.Buffer = ClassicProgramCodes.Label;
    cpu.Program.InsertFromBuffer();
    cpu.State.Buffer = 11; // A
    cpu.Program.InsertFromBuffer();
    cpu.State.Buffer = 1;
    cpu.Program.InsertFromBuffer();
    cpu.State.Buffer = 24; // RTN
    cpu.Program.InsertFromBuffer();

    byte[] before = new byte[cpu.Program.MemLength];
    for (int i = 0; i < before.Length; i++)
    {
      before[i] = cpu.Program.ReadCode(i);
    }

    int last = cpu.Program.LastContentIndex();
    cpu.Program.SeekPointer(last + 40); // would have pulled zeros into the body
    Assert.AreEqual(last, cpu.Program.PointerPosition());

    int nonZeroBefore = before.Count(b => b != 0);
    int nonZeroAfter = 0;
    for (int i = 0; i < cpu.Program.MemLength; i++)
    {
      if (cpu.Program.ReadCode(i) != 0)
      {
        nonZeroAfter++;
      }
    }

    Assert.AreEqual(nonZeroBefore, nonZeroAfter);
  }

  [TestMethod]
  public void AdvancePointer_MovesMarkerForwardOneSlot()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    Assert.AreEqual(1, cpu.Program.PointerPosition());
    cpu.Program.AdvancePointer();
    Assert.AreEqual(2, cpu.Program.PointerPosition());
  }
}
