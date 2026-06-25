namespace TeoCalc.Core.Engine.Classic;

internal static class ClassicCpuDataRam
{
  public static void DataToC(ClassicCpuState state)
  {
    int baseOffset = state.RamAddress * ClassicRegisterFile.DigitCount / 2;
    for (byte index = 0; index < 7; index++)
    {
      byte packed = state.Ram[baseOffset + index];
      state.Registers.C[index * 2] = (byte)(packed & 0xF);
      state.Registers.C[index * 2 + 1] = (byte)(packed >> 4);
    }
  }

  public static void CToAddress(ClassicCpuState state)
  {
    if (state.RamSlotSize <= 10)
    {
      state.RamAddress = state.Registers.C[12];
    }
    else
    {
      state.RamAddress = (byte)(state.Registers.C[12] * 10 + state.Registers.C[11]);
    }
  }

  public static void CToData(ClassicCpuState state)
  {
    WriteCToRamAddress(state, state.RamAddress);
  }

  private static void WriteCToRamAddress(ClassicCpuState state, byte address)
  {
    if (address >= 64)
    {
      return;
    }

    int baseOffset = address * ClassicRegisterFile.DigitCount / 2;
    for (byte index = 0; index < 7; index++)
    {
      byte low = state.Registers.C[index * 2];
      byte high = state.Registers.C[index * 2 + 1];
      state.Ram[baseOffset + index] = (byte)(low | (high << 4));
    }
  }
}
