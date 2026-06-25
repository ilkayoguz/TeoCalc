using System.Text;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>Text listing of user program steps in act_ram[ProgramRamBase+].</summary>
public static class ClassicProgramListing
{
  public static IEnumerable<ClassicProgramLine> Enumerate(ClassicProgramMemory program)
  {
    for (int index = 0; index < program.MemLength; index++)
    {
      byte code = program.ReadCode(index);
      if (code == 0 && index > 1)
      {
        yield break;
      }

      yield return new ClassicProgramLine(index, code, program.FormatCode(code));
    }
  }

  public static string Format(ClassicProgramMemory program)
  {
    StringBuilder builder = new();
    foreach (ClassicProgramLine line in Enumerate(program))
    {
      builder.AppendLine(line.ToString());
    }

    return builder.ToString();
  }
}

public readonly record struct ClassicProgramLine(int Index, byte Code, string Mnemonic)
{
  public override string ToString() => $"{Index,3}  {Mnemonic}";
}
