namespace TeoCalc.Core.Engine.Classic;

public sealed class ClassicCpuState
{
  public const int RamBytes = 448;

  public const int DefaultProgramRamBase = 112;

  public ushort ProgramCounter { get; set; }

  public byte Grp { get; set; }

  public byte Rom { get; set; }

  public byte DelRom { get; set; }

  public byte DelGrp { get; set; }

  public ClassicCpuFlags Flags { get; set; }

  public ushort[] ReturnStack { get; } = new ushort[2];

  public int BranchOffset { get; set; }

  public int Buffer { get; set; }

  public byte F { get; set; }

  public byte P { get; set; }

  public ushort Status { get; set; }

  public byte Base { get; set; } = 10;

  public byte KeyBuffer { get; set; }

  public bool KeyAvailable { get; set; }

  public ClassicKeyInputState KeyInputState { get; set; }

  public byte RamAddress { get; set; }

  public byte RamSlotSize { get; set; } = 10;

  public int ProgramRamBase { get; set; } = DefaultProgramRamBase;

  public ClassicRegisterFile Registers { get; } = new();

  public byte[] Ram { get; } = new byte[RamBytes];

  public ushort LastOpcode { get; set; }

  public string? LastHandlerId { get; set; }

  public int FetchAddress => (Grp << 11) | (Rom << 8) | (byte)ProgramCounter;

  public void Reset()
  {
    ProgramCounter = 0;
    Grp = 0;
    Rom = 0;
    DelRom = 0;
    DelGrp = 0;
    Flags = ClassicCpuFlags.None;
    ReturnStack[0] = 0;
    ReturnStack[1] = 0;
    BranchOffset = 0;
    Buffer = 0;
    F = 0;
    P = 0;
    Status = 0;
    Base = 10;
    KeyBuffer = 0;
    KeyAvailable = false;
    KeyInputState = ClassicKeyInputState.Idle;
    RamAddress = 0;
    RamSlotSize = 10;
    ProgramRamBase = DefaultProgramRamBase;
    Registers.ClearAll();
    Array.Clear(Ram);
    ClassicPowerOnDefaults.Apply(this);
    LastOpcode = 0;
    LastHandlerId = null;
  }

  public void PrepareOpcodeFlags()
  {
    if ((Flags & ClassicCpuFlags.Carry) != 0)
    {
      Flags &= ~ClassicCpuFlags.Carry;
      Flags |= ClassicCpuFlags.PrevCarry;
    }
    else
    {
      Flags &= ~ClassicCpuFlags.PrevCarry;
    }
  }

  public void ApplyKeyInput()
  {
    if (KeyInputState != ClassicKeyInputState.Pressed)
    {
      return;
    }

    Status |= 1;
    KeyInputState = ClassicKeyInputState.Wait;
  }

  public void HandleIo()
  {
    switch (KeyInputState)
    {
      case ClassicKeyInputState.Idle:
        if (KeyAvailable)
        {
          KeyAvailable = false;
          KeyInputState = ClassicKeyInputState.Pressed;
        }

        break;
      case ClassicKeyInputState.Wait:
        if ((Status & 1) == 0)
        {
          KeyInputState = ClassicKeyInputState.Depressed;
        }

        break;
      case ClassicKeyInputState.Depressed:
        KeyInputState = ClassicKeyInputState.Idle;
        break;
    }
  }
}
