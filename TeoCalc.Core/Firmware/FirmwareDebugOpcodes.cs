using System.Text;

namespace TeoCalc.Core.Firmware;

/// <summary>Shared DEBUG/TRACE helpers for microcode call/return detection and register digests.</summary>
public static class FirmwareDebugOpcodes
{
  public static bool IsSubroutineCall(string? handlerId) =>
    handlerId is "ClassicCpu.SubroutineJump" or "op_jsb";

  public static bool IsReturn(string? handlerId) =>
    handlerId is "ClassicCpu.Return" or "op_return";

  public static string FormatDigitRegister(byte[] digits)
  {
    StringBuilder text = new(digits.Length);
    for (int i = digits.Length - 1; i >= 0; i--)
    {
      text.Append((digits[i] & 0xF).ToString("X"));
    }

    return text.ToString();
  }

  /// <summary>
  /// Parse hex digest (MSB left, same as <see cref="FormatDigitRegister"/>) into a digit array.
  /// Shorter input is left-padded with 0; longer input is rejected.
  /// </summary>
  public static bool TryParseDigitRegister(string? hex, byte[] dest, out string? error)
  {
    error = null;
    if (dest.Length == 0)
    {
      error = "Empty register.";
      return false;
    }

    string cleaned = (hex ?? string.Empty).Trim().Replace(" ", "", StringComparison.Ordinal);
    if (cleaned.Length > dest.Length)
    {
      error = $"At most {dest.Length} hex digits.";
      return false;
    }

    string padded = cleaned.PadLeft(dest.Length, '0');
    for (int i = 0; i < dest.Length; i++)
    {
      char c = padded[i];
      int nibble = HexNibble(c);
      if (nibble < 0)
      {
        error = $"Invalid hex digit '{c}'.";
        return false;
      }

      dest[dest.Length - 1 - i] = (byte)nibble;
    }

    return true;
  }

  public static bool TryWriteNamedClassicRegister(
    string name,
    string digitsHex,
    byte[] a,
    byte[] b,
    byte[] c,
    byte[] y,
    byte[] z,
    byte[] t,
    byte[] m,
    byte[]? n,
    out string? error)
  {
    error = null;
    byte[]? target = name.Trim().ToUpperInvariant() switch
    {
      "A" => a,
      "B" => b,
      "C" => c,
      "Y" => y,
      "Z" => z,
      "T" => t,
      "M" => m,
      "N" when n is not null => n,
      _ => null,
    };

    if (target is null)
    {
      error = $"Unknown register '{name}'.";
      return false;
    }

    return TryParseDigitRegister(digitsHex, target, out error);
  }

  private static int HexNibble(char c) =>
    c switch
    {
      >= '0' and <= '9' => c - '0',
      >= 'A' and <= 'F' => c - 'A' + 10,
      >= 'a' and <= 'f' => c - 'a' + 10,
      _ => -1,
    };

  public static FirmwareDebugRegisters FromClassicStyle(
    byte[] a,
    byte[] b,
    byte[] c,
    byte[] y,
    byte[] z,
    byte[] t,
    byte[] m,
    byte[]? n = null)
  {
    List<FirmwareRegisterDigest> working =
    [
      new("A", FormatDigitRegister(a)),
      new("B", FormatDigitRegister(b)),
      new("C", FormatDigitRegister(c)),
      new("Y", FormatDigitRegister(y)),
      new("Z", FormatDigitRegister(z)),
      new("T", FormatDigitRegister(t)),
      new("M", FormatDigitRegister(m)),
    ];
    if (n is not null)
    {
      working.Add(new("N", FormatDigitRegister(n)));
    }

    return new FirmwareDebugRegisters(working);
  }
}
