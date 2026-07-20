namespace TeoCalc.Core.Engine.Act;

internal static class ActCpuStack
{
  public static void CToStack(ActRegisterFile r)
  {
    for (byte i = 0; i < 14; i++)
    {
      r.T[i] = r.Z[i];
      r.Z[i] = r.Y[i];
      r.Y[i] = r.C[i];
    }
  }

  public static void YToA(ActRegisterFile r)
  {
    for (byte i = 0; i < 14; i++)
    {
      r.A[i] = r.Y[i];
    }
  }

  public static void StackToA(ActRegisterFile r)
  {
    YToA(r);
    for (byte i = 0; i < 14; i++)
    {
      r.Y[i] = r.Z[i];
      r.Z[i] = r.T[i];
    }
  }

  public static void DownRotate(ActRegisterFile r)
  {
    for (byte i = 0; i < 14; i++)
    {
      byte tmp = r.C[i];
      r.C[i] = r.Y[i];
      r.Y[i] = r.Z[i];
      r.Z[i] = r.T[i];
      r.T[i] = tmp;
    }
  }

  public static void ClearRegisters(ActRegisterFile r)
  {
    for (byte i = 0; i < 14; i++)
    {
      r.A[i] = r.B[i] = r.C[i] = r.Y[i] = r.Z[i] = r.T[i] = 0;
    }
  }

  public static void Mx(ActRegisterFile r, ushort opcode)
  {
    byte[] other = (opcode & 0x80) != 0 ? r.N : r.M;
    for (byte i = 0; i < 14; i++)
    {
      byte tmp = r.C[i];
      r.C[i] = other[i];
      if ((opcode & 0x40) == 0)
      {
        other[i] = tmp;
      }
    }
  }
}
