using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Loads museum W/PRGM keycode text (e.g. <c>34 01</c>) into Classic A/B so the faceplate
/// LED matches SST-style step display after Studio seek / pointer sync.
/// </summary>
public static class ClassicWprgmLedSync
{
  /// <summary>
  /// Right-align museum digit pairs into mantissa A/B (exponent slots blanked), matching
  /// firmware SST output shape. Does not touch <see cref="ClassicCpuFlags.DisplayOn"/>.
  /// </summary>
  public static void ApplyMuseumText(ClassicRegisterFile regs, string museum)
  {
    ArgumentNullException.ThrowIfNull(regs);

    Array.Clear(regs.A);
    Array.Clear(regs.B);
    for (int i = 0; i < ClassicRegisterFile.DigitCount; i++)
    {
      regs.B[i] = 9;
    }

    if (string.IsNullOrWhiteSpace(museum))
    {
      return;
    }

    string[] parts = museum.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    // Mantissa sits above the exponent region (indices 0..2); rightmost digit at index 3.
    int slot = 3;
    for (int partIndex = parts.Length - 1; partIndex >= 0; partIndex--)
    {
      string token = parts[partIndex];
      for (int charIndex = token.Length - 1; charIndex >= 0; charIndex--)
      {
        char ch = token[charIndex];
        if (ch is < '0' or > '9' || slot > 12)
        {
          continue;
        }

        regs.A[slot] = (byte)(ch - '0');
        regs.B[slot] = 0;
        slot++;
      }

      if (partIndex > 0 && slot <= 12)
      {
        slot++;
      }
    }
  }
}
