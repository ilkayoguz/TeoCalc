using System.Text;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>Formats Classic X register digits (Panamatik ShowDisplay study reference).</summary>
public static class ClassicDisplayFormatter
{
  public static string FormatXRegister(
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode = false,
    byte programEndState = 0)
  {
    if (!displayOn)
    {
      return string.Empty;
    }

    StringBuilder text = new();
    for (int index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      byte mantissa = registers.A[13 - index];
      byte meta = registers.B[13 - index];
      if (programEndState == 2 && programMode && index == 0)
      {
        text.Append('-');
      }

      if (meta > 7)
      {
        text.Append(' ');
        continue;
      }

      char digit = index is not 0 and not 11
        ? (char)(mantissa + '0')
        : mantissa >= 8 ? '-' : ' ';
      text.Append(digit);
      if ((meta & 2) != 0)
      {
        text.Append('.');
      }
    }

    return text.ToString();
  }

  /// <summary>Panamatik <c>ShowDisplay</c> with LED font decimal (<c>;</c>).</summary>
  public static string ToLedFontText(
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode = false,
    byte programEndState = 0)
  {
    if (!displayOn)
    {
      return string.Empty;
    }

    StringBuilder text = new();
    for (int index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      byte mantissa = registers.A[13 - index];
      byte meta = registers.B[13 - index];
      if (programEndState == 2 && programMode && index == 0)
      {
        text.Append('-');
      }

      if (meta > 7)
      {
        text.Append(' ');
        continue;
      }

      char digit = index is not 0 and not 11
        ? (char)(mantissa + '0')
        : mantissa >= 8 ? '-' : ' ';
      text.Append(digit);
      if ((meta & 2) != 0)
      {
        text.Append(';');
      }
    }

    return text.ToString();
  }

  public static string FormatXRegister(ClassicCpu cpu, bool programMode = false)
  {
    return FormatXRegister(
      cpu.State.Registers,
      (cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0,
      programMode,
      cpu.Program.EndState);
  }
}
