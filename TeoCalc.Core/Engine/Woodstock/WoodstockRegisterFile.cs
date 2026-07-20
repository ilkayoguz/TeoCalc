namespace TeoCalc.Core.Engine.Woodstock;

public sealed class WoodstockRegisterFile
{
  public const int DigitCount = 14;

  public byte[] A { get; } = new byte[DigitCount];

  public byte[] B { get; } = new byte[DigitCount];

  public byte[] C { get; } = new byte[DigitCount];

  public byte[] Y { get; } = new byte[DigitCount];

  public byte[] Z { get; } = new byte[DigitCount];

  public byte[] T { get; } = new byte[DigitCount];

  public byte[] M { get; } = new byte[DigitCount];

  public byte[] N { get; } = new byte[DigitCount];

  public void ClearAll()
  {
    Clear(A);
    Clear(B);
    Clear(C);
    Clear(Y);
    Clear(Z);
    Clear(T);
    Clear(M);
    Clear(N);
  }

  private static void Clear(byte[] register) =>
    Array.Clear(register);
}
