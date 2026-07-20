using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>Apply / capture Classic card snapshots against <see cref="ClassicCpu"/> program + data RAM.</summary>
public static class ClassicCardProgramIo
{
  public const int ProgramCapacity = 100;

  public const int RegisterCount = 10;

  public static void Export(
    ClassicCpu cpu,
    out byte[] programCodes,
    out double[] registers)
  {
    ArgumentNullException.ThrowIfNull(cpu);
    programCodes = new byte[ProgramCapacity];
    for (int i = 0; i < ProgramCapacity; i++)
    {
      programCodes[i] = cpu.Program.ReadCode(i);
    }

    registers = new double[RegisterCount];
    for (int i = 0; i < RegisterCount; i++)
    {
      registers[i] = ClassicDataRegisterCodec.GetRegisterValue(cpu.State.Ram, i);
    }
  }

  public static void Import(ClassicCpu cpu, IReadOnlyList<byte> programCodes, IReadOnlyList<double> registers)
  {
    ArgumentNullException.ThrowIfNull(cpu);
    ArgumentNullException.ThrowIfNull(programCodes);
    ArgumentNullException.ThrowIfNull(registers);

    int count = Math.Min(ProgramCapacity, programCodes.Count);
    for (int i = 0; i < count; i++)
    {
      cpu.Program.WriteCode(i, programCodes[i]);
    }

    for (int i = count; i < ProgramCapacity; i++)
    {
      cpu.Program.WriteCode(i, 0);
    }

    int regCount = Math.Min(RegisterCount, registers.Count);
    for (int i = 0; i < regCount; i++)
    {
      ClassicDataRegisterCodec.SetRegisterValue(cpu.State.Ram, i, registers[i]);
    }

    for (int i = regCount; i < RegisterCount; i++)
    {
      ClassicDataRegisterCodec.SetRegisterValue(cpu.State.Ram, i, 0d);
    }

    cpu.Program.Cleanup(7);
  }

  public static string FormatMnemonic(ProgramVocabulary? vocabulary, byte code)
  {
    if (code == 0)
    {
      return "NOP";
    }

    if (vocabulary is null)
    {
      return $"#{code}";
    }

    try
    {
      return vocabulary.ResolveCode(code).Mnemonic;
    }
    catch (KeyNotFoundException)
    {
      return $"#{code}";
    }
  }

  public static byte? ResolveMnemonic(ProgramVocabulary? vocabulary, string mnemonic)
  {
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return null;
    }

    if (mnemonic.StartsWith('#')
        && byte.TryParse(mnemonic.AsSpan(1), out byte numeric))
    {
      return numeric;
    }

    if (vocabulary is null)
    {
      return null;
    }

    ProgramStepEntry? step = vocabulary.TryResolveMnemonic(mnemonic);
    return step is null ? null : (byte)step.Code;
  }
}
