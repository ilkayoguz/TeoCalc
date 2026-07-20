namespace TeoCalc.Core.Engine.Act;

public sealed class ActCpuState
{
  public const int RamBytes = 448;

  public ushort ProgramCounter { get; set; }

  public byte DelRom { get; set; }

  public ActCpuFlags Flags { get; set; }

  public ActInstructionState InstructionState { get; set; }

  public ushort[] ReturnStack { get; } = new ushort[2];

  public byte StackPointer { get; set; }

  public byte P { get; set; }

  public byte F { get; set; }

  public ushort Status { get; set; }

  public byte Base { get; set; } = 10;

  public byte KeyBuffer { get; set; }

  public byte CrcFlags { get; set; }

  public byte RamAddress { get; set; }

  public byte RomAddress { get; set; }

  public ActRegisterFile Registers { get; } = new();

  public byte[] Ram { get; } = new byte[RamBytes];

  public ushort LastOpcode { get; set; }

  public string? LastHandlerId { get; set; }

  public void Reset()
  {
    ProgramCounter = 0;
    DelRom = 0;
    Flags = ActCpuFlags.None;
    InstructionState = ActInstructionState.Norm;
    ReturnStack[0] = 0;
    ReturnStack[1] = 0;
    StackPointer = 0;
    P = 0;
    F = 0;
    Status = 0;
    Base = 10;
    KeyBuffer = 0;
    CrcFlags = 0;
    RamAddress = 0;
    RomAddress = 0;
    Registers.ClearAll();
    Array.Clear(Ram);
    LastOpcode = 0;
    LastHandlerId = null;
  }

  public void PressKey(byte keyCode)
  {
    KeyBuffer = keyCode;
    Status |= 0x8000;
  }

  public void ReleaseKey()
  {
    // Panamatik act_release_key preserves only the key bit mask literal.
    Status &= 0x8000;
  }

  /// <summary>
  /// Resolve ROM fetch address for the current PC, applying Panamatik bank side effects.
  /// </summary>
  public int ResolveFetchAddress()
  {
    int address = ProgramCounter;
    if ((Flags & ActCpuFlags.Bank) == 0)
    {
      return address;
    }

    if (address < 1024)
    {
      Flags &= ~ActCpuFlags.Bank;
      return address;
    }

    return address | 0x1000;
  }
}
