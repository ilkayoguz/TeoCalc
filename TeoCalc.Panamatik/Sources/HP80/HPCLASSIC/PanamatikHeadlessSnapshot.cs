namespace Panamatik.Calc.HP80;

public readonly struct PanamatikHeadlessSnapshot
{
  public PanamatikHeadlessSnapshot(
    ushort programCounter,
    ushort status,
    byte keyBuffer,
    byte flags,
    byte p,
    byte rom,
    byte grp)
  {
    ProgramCounter = programCounter;
    Status = status;
    KeyBuffer = keyBuffer;
    Flags = flags;
    P = p;
    Rom = rom;
    Grp = grp;
  }

  public ushort ProgramCounter { get; }

  public ushort Status { get; }

  public byte KeyBuffer { get; }

  public byte Flags { get; }

  public byte P { get; }

  public byte Rom { get; }

  public byte Grp { get; }
}
