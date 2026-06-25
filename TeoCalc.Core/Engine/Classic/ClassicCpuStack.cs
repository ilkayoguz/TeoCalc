namespace TeoCalc.Core.Engine.Classic;

internal static class ClassicCpuStack
{
  public static void CToStack(ClassicRegisterFile registers)
  {
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      registers.T[index] = registers.Z[index];
      registers.Z[index] = registers.Y[index];
      registers.Y[index] = registers.C[index];
    }
  }

  public static void StackToA(ClassicRegisterFile registers)
  {
    Copy(registers.A, registers.Y);
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      registers.Y[index] = registers.Z[index];
      registers.Z[index] = registers.T[index];
    }
  }

  public static void DownRotate(ClassicRegisterFile registers)
  {
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      byte temp = registers.C[index];
      registers.C[index] = registers.Y[index];
      registers.Y[index] = registers.Z[index];
      registers.Z[index] = registers.T[index];
      registers.T[index] = temp;
    }
  }

  public static void ExchangeCAndM(ClassicRegisterFile registers)
  {
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      byte temp = registers.C[index];
      registers.C[index] = registers.M[index];
      registers.M[index] = temp;
    }
  }

  public static void MToC(ClassicRegisterFile registers)
  {
    Copy(registers.C, registers.M);
  }

  private static void Copy(byte[] dest, byte[] src)
  {
    for (byte index = 0; index < ClassicRegisterFile.DigitCount; index++)
    {
      dest[index] = src[index];
    }
  }
}
