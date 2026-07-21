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
