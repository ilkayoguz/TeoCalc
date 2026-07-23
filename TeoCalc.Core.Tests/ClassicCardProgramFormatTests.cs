using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCardProgramFormatTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void T65_RoundTrip_ProgramAndData_PreservesCodesAndRegisters()
  {
    ProgramVocabulary vocabulary = Vocabulary;
    byte[] codes = new byte[ClassicCardSnapshot.DefaultProgramCapacity];
    codes[0] = ClassicProgramCodes.Start;
    codes[1] = 43; // LBL
    codes[2] = 4;  // 1
    codes[3] = ClassicProgramCodes.Pointer;
    double[] registers = [1.25, -3.5, 0, 0, 0, 0, 0, 0, 0, 42];

    TeoCardDocument teo = TeoCardProgramFormat.FromClassicSnapshot(
      new ClassicCardSnapshot(codes, registers),
      code => ClassicCardProgramIo.FormatMnemonic(vocabulary, code),
      "HP-65");
    T6xDocument t65 = T6xCardFormat.FromTeoCardDocument(teo);
    string text = T6xCardFormat.Format(t65);

    Assert.Contains("T-65", text, StringComparison.Ordinal);
    Assert.Contains("LBL", text, StringComparison.Ordinal);

    T6xDocument parsedDoc = T6xCardFormat.Parse(text);
    ClassicCardSnapshot parsed = T6xCardFormat.ToClassicSnapshot(
      parsedDoc,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(vocabulary, mnemonic));

    Assert.AreEqual(ClassicProgramCodes.Start, parsed.ProgramCodes[0]);
    Assert.AreEqual(43, parsed.ProgramCodes[1]);
    Assert.AreEqual(4, parsed.ProgramCodes[2]);
    Assert.IsTrue(parsed.ProgramCodes.Contains(ClassicProgramCodes.Pointer));
    Assert.AreEqual(1.25, parsed.Registers[0], 1e-9);
    Assert.AreEqual(-3.5, parsed.Registers[1], 1e-9);
    Assert.AreEqual(42, parsed.Registers[9], 1e-9);
  }

  [TestMethod]
  public void ClassicCardProgramIo_ImportExport_RoundTripsThroughCpu()
  {
    ClassicCpu cpu = CreateCpu();
    cpu.Reset();
    cpu.Program.WriteCode(1, 43);
    cpu.Program.WriteCode(2, 4);
    ClassicDataRegisterCodec.SetRegisterValue(cpu.State.Ram, 0, 12.5);

    ClassicCardProgramIo.Export(cpu, out byte[] codes, out double[] registers);
    Assert.AreEqual(43, codes[1]);
    Assert.AreEqual(12.5, registers[0], 1e-6);

    ClassicCpu other = CreateCpu();
    other.Reset();
    ClassicCardProgramIo.Import(other, codes, registers);
    Assert.AreEqual(43, other.Program.ReadCode(1));
    Assert.AreEqual(12.5, ClassicDataRegisterCodec.GetRegisterValue(other.State.Ram, 0), 1e-6);
  }

  private static ClassicCpu CreateCpu()
  {
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      TeoCalcPaths.ResourcePath("Engine/T-65/Firmware/hp65.microcode.bin"));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(
      TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
    return new ClassicCpu(rom, catalog, Vocabulary);
  }
}
