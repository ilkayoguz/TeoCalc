namespace TeoCalc.Core.Engine.Spice;

internal static class SpiceCpuDataRam
{
  public static void RegisterToC(SpiceCpuState state, byte addr)
  {
    if (addr < 64)
    {
      for (byte i = 0; i < 7; i++)
      {
        byte packed = state.Ram[addr * 14 / 2 + i];
        state.Registers.C[i * 2] = (byte)(packed & 0xF);
        state.Registers.C[i * 2 + 1] = (byte)(packed >> 4);
      }
    }
    else if (addr == byte.MaxValue)
    {
      state.Registers.C[0] = state.KeyBuffer;
    }
  }

  public static void CToRegister(SpiceCpuState state, byte addr)
  {
    if (addr >= 64)
    {
      return;
    }

    for (byte i = 0; i < 7; i++)
    {
      state.Ram[addr * 14 / 2 + i] = (byte)(
        (state.Registers.C[i * 2] & 0xF) | (state.Registers.C[i * 2 + 1] << 4));
    }
  }

  public static void CToAddress(SpiceCpuState state) =>
    state.RamAddress = (byte)((state.Registers.C[1] << 4) + state.Registers.C[0]);

  public static void CToData(SpiceCpuState state) =>
    CToRegister(state, state.RamAddress);

  public static void CToRegisterOpcode(SpiceCpuState state, ushort opcode)
  {
    state.RamAddress = (byte)((state.RamAddress & 0xF0) + (opcode >> 6));
    CToData(state);
  }

  public static void RegisterToCOpcode(SpiceCpuState state, ushort opcode)
  {
    if (opcode >> 6 != 0)
    {
      state.RamAddress = (byte)((state.RamAddress & 0xF0) + (opcode >> 6));
    }

    RegisterToC(state, state.RamAddress);
  }

  public static void ClearDataRegs(SpiceCpuState state)
  {
    byte baseAddr = (byte)(state.RamAddress & 0xF0);
    if (baseAddr >= 64)
    {
      return;
    }

    int start = baseAddr * 14 / 2;
    for (int i = start; i < start + 112; i++)
    {
      state.Ram[i] = 0;
    }
  }
}
