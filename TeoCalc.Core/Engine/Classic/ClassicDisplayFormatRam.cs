namespace TeoCalc.Core.Engine.Classic;

/// <summary>Reads FIX decimal places from RAM display-format register ($3D).</summary>
public static class ClassicDisplayFormatRam
{
  public static bool TryGetFixDecimalPlaces(ClassicCpuState state, out int decimalPlaces)
  {
    decimalPlaces = 2;
    int offset = ClassicPowerOnDefaults.DisplayFormatRamAddress * ClassicRegisterFile.DigitCount / 2;
    if (offset + 7 > state.Ram.Length)
    {
      return false;
    }

    Span<byte> digits = stackalloc byte[ClassicRegisterFile.DigitCount];
    for (int index = 0; index < 7; index++)
    {
      byte packed = state.Ram[offset + index];
      digits[index * 2] = (byte)(packed & 0xF);
      digits[index * 2 + 1] = (byte)(packed >> 4);
    }

    // ...1 2 n ... at c[7]=1, c[6]=2, c[5]=n for FIX n (default n=2).
    if (digits[7] == 1 && digits[6] == 2 && digits[5] is >= 0 and <= 9)
    {
      decimalPlaces = digits[5];
      return true;
    }

    if (digits[4] is >= 0 and <= 9)
    {
      decimalPlaces = digits[4];
      return true;
    }

    return false;
  }
}
