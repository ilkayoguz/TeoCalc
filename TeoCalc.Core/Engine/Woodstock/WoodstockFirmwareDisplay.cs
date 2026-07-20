using System.Text;

namespace TeoCalc.Core.Engine.Woodstock;

/// <summary>Panamatik HP25 <c>ShowDisplay</c> (12-digit LED with digittab).</summary>
public static class WoodstockFirmwareDisplay
{
  private static readonly char[] DigitTab =
  [
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    'r', 'F', 'o', 'P', 'E', ' ',
  ];

  public static string BuildLedText(WoodstockCpuState state)
  {
    if ((state.Flags & WoodstockCpuFlags.DisplayOn) == 0)
    {
      return string.Empty;
    }

    StringBuilder text = new();
    for (int i = 0; i < 12; i++)
    {
      byte mantissa = state.Registers.A[13 - i];
      byte meta = state.Registers.B[13 - i];
      char c = DigitTab[mantissa & 0xF];
      if ((meta & 2) != 0)
      {
        c = mantissa == 9 ? '-' : ' ';
      }

      text.Append(c);
      if ((meta & 1) != 0)
      {
        text.Append('.');
      }
    }

    return text.ToString();
  }

  /// <summary>LED font uses ';' for decimal point (Classic explorer convention).</summary>
  public static string ToLedFontText(WoodstockCpuState state) =>
    BuildLedText(state).Replace('.', ';');
}
