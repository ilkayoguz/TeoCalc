using System.Globalization;
using System.Text;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>
/// Canonical user-program step listing (addr / machine / mnemonic).
/// Shared source of truth for Studio editor, explorer panel, and future flowchart sync.
/// </summary>
public static class ClassicProgramListing
{
  public static IEnumerable<ClassicProgramLine> Enumerate(ClassicProgramMemory program)
  {
    ArgumentNullException.ThrowIfNull(program);
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

  /// <summary>
  /// Build a listing from exported card / RAM bytes using an ISA-specific mnemonic formatter.
  /// Stops at the first trailing zero after step 1 (same rule as live program RAM).
  /// </summary>
  public static IEnumerable<ClassicProgramLine> Enumerate(
    IReadOnlyList<byte> codes,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(codes);
    ArgumentNullException.ThrowIfNull(formatMnemonic);

    for (int index = 0; index < codes.Count; index++)
    {
      byte code = codes[index];
      if (code == 0 && index > 1)
      {
        yield break;
      }

      yield return new ClassicProgramLine(index, code, formatMnemonic(code));
    }
  }

  public static IReadOnlyList<ClassicProgramLine> ToList(
    IReadOnlyList<byte> codes,
    Func<byte, string> formatMnemonic) =>
    Enumerate(codes, formatMnemonic).ToArray();

  public static string Format(ClassicProgramMemory program) =>
    Format(Enumerate(program), ClassicProgramListingStyle.Mnemonic);

  public static string Format(
    IEnumerable<ClassicProgramLine> lines,
    ClassicProgramListingStyle style)
  {
    ArgumentNullException.ThrowIfNull(lines);
    StringBuilder builder = new();
    foreach (ClassicProgramLine line in lines)
    {
      builder.AppendLine(line.Format(style));
    }

    return builder.ToString();
  }

  /// <summary>
  /// Index of the Classic program pointer / mark marker, or -1 when not present
  /// (e.g. ACT / T-67 listings).
  /// </summary>
  public static int FindPointerIndex(IEnumerable<ClassicProgramLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);
    foreach (ClassicProgramLine line in lines)
    {
      if (line.Code is ClassicProgramCodes.Pointer or ClassicProgramCodes.Mark)
      {
        return line.Index;
      }
    }

    return -1;
  }
}

public enum ClassicProgramListingStyle
{
  Mnemonic,
  Machine,
}

/// <summary>One user-program step: address, internal machine byte, and resolved mnemonic.</summary>
public readonly record struct ClassicProgramLine(int Index, byte Code, string Mnemonic)
{
  public string Body(ClassicProgramListingStyle style) =>
    style == ClassicProgramListingStyle.Machine
      ? Code.ToString(CultureInfo.InvariantCulture)
      : (string.IsNullOrEmpty(Mnemonic) ? $"#{Code}" : Mnemonic);

  public string Format(ClassicProgramListingStyle style) =>
    $"{Index,3}  {Body(style)}";

  public override string ToString() => Format(ClassicProgramListingStyle.Mnemonic);
}
