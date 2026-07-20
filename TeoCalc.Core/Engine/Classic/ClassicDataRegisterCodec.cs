using System.Globalization;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>BCD packed data-register encode/decode matching Panamatik HPClassic card I/O.</summary>
public static class ClassicDataRegisterCodec
{
  public static double GetRegisterValue(byte[] ram, int registerIndex)
  {
    ArgumentNullException.ThrowIfNull(ram);
    int baseOffset = registerIndex * 7;
    if (baseOffset + 7 > ram.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(registerIndex));
    }

    int exponent = 0;
    for (int digit = 2; digit >= 0; digit--)
    {
      byte packed = ram[baseOffset + digit / 2];
      byte nibble = (digit & 1) == 0 ? (byte)(packed & 0xF) : (byte)(packed >> 4);
      exponent = exponent * 10 + nibble;
    }

    if (exponent >= 100)
    {
      exponent -= 1000;
    }

    long mantissa = 0;
    for (int digit = 12; digit >= 3; digit--)
    {
      byte packed = ram[baseOffset + digit / 2];
      byte nibble = (digit & 1) == 0 ? (byte)(packed & 0xF) : (byte)(packed >> 4);
      mantissa = mantissa * 10 + nibble;
    }

    if (ram[baseOffset + 6] >> 4 == 9)
    {
      mantissa = -mantissa;
    }

    return mantissa / 1_000_000_000.0 * Math.Pow(10.0, exponent);
  }

  public static void SetRegisterValue(byte[] ram, int registerIndex, double value)
  {
    ArgumentNullException.ThrowIfNull(ram);
    int baseOffset = registerIndex * 7;
    if (baseOffset + 7 > ram.Length)
    {
      throw new ArgumentOutOfRangeException(nameof(registerIndex));
    }

    byte[] digits = new byte[14];
    SetRegisterDigits(digits, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0");
    for (int j = 0; j < 7; j++)
    {
      ram[baseOffset + j] = (byte)((digits[j * 2 + 1] << 4) | digits[j * 2]);
    }
  }

  /// <summary>Panamatik <c>SetRegisterValue</c> digit packing into a 14-nibble buffer.</summary>
  internal static void SetRegisterDigits(byte[] buf, string s)
  {
    for (int i = 0; i < 14; i++)
    {
      buf[i] = 0;
    }

    bool leadingZeros = true;
    bool sawPoint = false;
    int digitCount = 0;
    int pointIndex = 0;
    int exponent = 0;

    for (int i = 0; i < s.Length; i++)
    {
      char c = s[i];
      if (digitCount == 0 && c == '-')
      {
        buf[13] = 9;
        continue;
      }

      if (c is '.' or ',')
      {
        sawPoint = true;
        pointIndex = digitCount - 1;
        continue;
      }

      if (c is 'E' or 'e')
      {
        exponent = int.Parse(s.AsSpan(i + 1), CultureInfo.InvariantCulture);
        break;
      }

      if (c < '0' || c > '9')
      {
        continue;
      }

      if (c != '0')
      {
        leadingZeros = false;
      }

      if (leadingZeros)
      {
        if (sawPoint)
        {
          pointIndex--;
        }
      }
      else if (digitCount < 10)
      {
        buf[12 - digitCount] = (byte)(c - '0');
        digitCount++;
      }
    }

    if (leadingZeros)
    {
      buf[13] = 0;
      return;
    }

    if (!sawPoint)
    {
      pointIndex = digitCount - 1;
    }

    exponent += pointIndex;
    for (int i = 0; i < 10; i++)
    {
      if (buf[12] != 0)
      {
        break;
      }

      for (int n = 0; n < 10; n++)
      {
        buf[12 - n] = buf[12 - n - 1];
      }

      exponent--;
    }

    if (exponent < 0)
    {
      exponent = 1000 + exponent;
    }

    buf[2] = (byte)(exponent / 100);
    exponent -= buf[2] * 100;
    buf[1] = (byte)(exponent / 10);
    exponent -= buf[1] * 10;
    buf[0] = (byte)exponent;
  }
}
