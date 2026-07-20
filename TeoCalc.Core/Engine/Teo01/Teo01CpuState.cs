namespace TeoCalc.Core.Engine.Teo01;

/// <summary>ACThp01 register file and peripheral buffers (WSIZE=14, WSIZE01=12).</summary>
public sealed class Teo01CpuState
{
  public const int WordSize = 14;
  public const int WordSize01 = 12;
  public const int StackSize = 2;

  public byte[] A { get; } = new byte[WordSize];
  public byte[] B { get; } = new byte[WordSize];
  public byte[] C { get; } = new byte[WordSize];
  public byte[] Y { get; } = new byte[WordSize];
  public byte[] Z { get; } = new byte[WordSize];
  public byte[] T { get; } = new byte[WordSize];
  public byte[] M { get; } = new byte[WordSize];
  public byte[] Dsp { get; } = new byte[WordSize01];
  public byte[] Cl { get; } = new byte[WordSize01];
  public byte[] Sw { get; } = new byte[WordSize01];
  public byte[] Al { get; } = new byte[WordSize01];

  public ushort[] Stack { get; } = new ushort[StackSize];

  public byte Sp { get; set; }
  public byte P { get; set; }
  public byte F { get; set; }
  public ushort ProgramCounter { get; set; }
  public ushort Opcode { get; set; }
  public ushort Status { get; set; }
  public Teo01CpuFlags Flags { get; set; }
  public Teo01ExtraFlags ExtraFlags { get; set; }
  public byte Rom { get; set; }
  public byte KeyBuffer { get; set; }
  public byte TickCnt { get; set; }

  public ushort LastOpcode { get; set; }
  public string? LastHandlerId { get; set; }

  public void Reset()
  {
    Array.Clear(A);
    Array.Clear(B);
    Array.Clear(C);
    Array.Clear(Y);
    Array.Clear(Z);
    Array.Clear(T);
    Array.Clear(M);
    Array.Clear(Dsp);
    Array.Clear(Cl);
    Array.Clear(Sw);
    Array.Clear(Al);
    Array.Clear(Stack);
    Sp = 0;
    P = 0;
    F = 0;
    ProgramCounter = 0;
    Opcode = 0;
    Status = 0;
    Flags = Teo01CpuFlags.None;
    ExtraFlags = Teo01ExtraFlags.None;
    Rom = 0;
    KeyBuffer = 0;
    TickCnt = 0;
    LastOpcode = 0;
    LastHandlerId = null;
  }
}
