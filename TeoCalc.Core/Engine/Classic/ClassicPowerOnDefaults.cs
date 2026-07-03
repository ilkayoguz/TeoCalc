namespace TeoCalc.Core.Engine.Classic;

/// <summary>HP Classic power-on RAM defaults (ROM boot normally writes these).</summary>
internal static class ClassicPowerOnDefaults
{
  /// <summary>RAM data register $3D — default <c>00000012220000</c> (FIX 2, DEG).</summary>
  public const int DisplayFormatRamAddress = 0x3D;

  public static void Apply(ClassicCpuState state)
  {
    int offset = DisplayFormatRamAddress * ClassicRegisterFile.DigitCount / 2;
    if (offset + 7 > state.Ram.Length)
    {
      return;
    }

  // C = 0 0 0 0 0 0 1 2 2 2 0 0 0 0 (MSB index 13 .. 0).
    state.Ram[offset] = 0x00;
    state.Ram[offset + 1] = 0x00;
    state.Ram[offset + 2] = 0x22;
    state.Ram[offset + 3] = 0x12;
    state.Ram[offset + 4] = 0x00;
    state.Ram[offset + 5] = 0x00;
    state.Ram[offset + 6] = 0x00;
  }
}
