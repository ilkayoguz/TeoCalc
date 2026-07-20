namespace TeoCalc.Core.Engine.Hp01;

/// <summary>Panamatik HP-01 <c>ShowDisplay</c> (9 digits from <c>act_dsp</c>).</summary>
public static class Hp01FirmwareDisplay
{
  private static readonly char[] DigitTab =
  [
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    '.', '-', ':', 'o', '-', ' ',
  ];

  public static string BuildLedText(Hp01CpuState state)
  {
    if ((state.Flags & Hp01CpuFlags.DisplayOn) == 0)
    {
      return string.Empty;
    }

    // Panamatik prefixes three spaces then digits dsp[11]..dsp[3].
    Span<char> chars = stackalloc char[12];
    chars[0] = ' ';
    chars[1] = ' ';
    chars[2] = ' ';
    for (int i = 0; i < 9; i++)
    {
      byte nibble = state.Dsp[11 - i];
      chars[3 + i] = nibble < DigitTab.Length ? DigitTab[nibble] : ' ';
    }

    return new string(chars);
  }
}
