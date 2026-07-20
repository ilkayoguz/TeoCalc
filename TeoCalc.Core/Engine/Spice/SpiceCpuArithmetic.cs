namespace TeoCalc.Core.Engine.Spice;

/// <summary>Panamatik HP25 field select + arithmetic / register ops.</summary>
internal static class SpiceCpuArithmetic
{
  public static (byte First, byte Last) ResolveField(ushort opcode, byte p) =>
    (((byte)opcode >> 2) & 7) switch
    {
      0 => (p, p),
      1 => ((byte)0, p),
      2 => ((byte)2, (byte)2),
      3 => ((byte)0, (byte)2),
      4 => ((byte)13, (byte)13),
      5 => ((byte)3, (byte)12),
      6 => ((byte)0, (byte)13),
      _ => ((byte)3, (byte)13),
    };

  public static void Execute(ushort opcode, SpiceCpuState state)
  {
    (byte first, byte last) = ResolveField(opcode, state.P);
    SpiceRegisterFile r = state.Registers;
    switch (opcode >> 5)
    {
      case 0:
        Zero(r.A, first, last);
        break;
      case 1:
        Zero(r.B, first, last);
        break;
      case 2:
        Exchange(r.B, r.A, first, last);
        break;
      case 3:
        Copy(r.A, r.B, first, last);
        break;
      case 4:
        Exchange(r.C, r.A, first, last);
        break;
      case 5:
        Copy(r.C, r.A, first, last);
        break;
      case 6:
        Copy(r.B, r.C, first, last);
        break;
      case 7:
        Exchange(r.C, r.B, first, last);
        break;
      case 8:
        Zero(r.C, first, last);
        break;
      case 9:
        Add(r.A, r.B, first, last, state);
        break;
      case 10:
        Add(r.A, r.C, first, last, state);
        break;
      case 11:
        Add(r.C, r.C, first, last, state);
        break;
      case 12:
        Add(r.C, r.A, first, last, state);
        break;
      case 13:
        Increment(r.A, first, last, state);
        break;
      case 14:
        ShiftLeft(r.A, first, last);
        break;
      case 15:
        Increment(r.C, first, last, state);
        break;
      case 16:
        Subtract(r.A, r.A, r.B, first, last, state);
        break;
      case 17:
        Subtract(r.C, r.A, r.C, first, last, state);
        break;
      case 18:
        state.Flags |= SpiceCpuFlags.Carry;
        Subtract(r.A, r.A, null, first, last, state);
        break;
      case 19:
        state.Flags |= SpiceCpuFlags.Carry;
        Subtract(r.C, r.C, null, first, last, state);
        break;
      case 20:
        Subtract(r.C, null, r.C, first, last, state);
        break;
      case 21:
        state.Flags |= SpiceCpuFlags.Carry;
        Subtract(r.C, null, r.C, first, last, state);
        break;
      case 22:
        state.InstructionState = SpiceInstructionState.Branch;
        TestNotEqual(r.B, null, first, last, state);
        break;
      case 23:
        state.InstructionState = SpiceInstructionState.Branch;
        TestNotEqual(r.C, null, first, last, state);
        break;
      case 24:
        state.InstructionState = SpiceInstructionState.Branch;
        Subtract(null, r.A, r.C, first, last, state);
        break;
      case 25:
        state.InstructionState = SpiceInstructionState.Branch;
        Subtract(null, r.A, r.B, first, last, state);
        break;
      case 26:
        state.InstructionState = SpiceInstructionState.Branch;
        TestEqual(r.A, null, first, last, state);
        break;
      case 27:
        state.InstructionState = SpiceInstructionState.Branch;
        TestEqual(r.C, null, first, last, state);
        break;
      case 28:
        Subtract(r.A, r.A, r.C, first, last, state);
        break;
      case 29:
        ShiftRight(r.A, first, last);
        break;
      case 30:
        ShiftRight(r.B, first, last);
        break;
      case 31:
        ShiftRight(r.C, first, last);
        break;
    }
  }

  private static void Zero(byte[] dest, byte first, byte last)
  {
    for (byte i = first; i <= last; i++)
    {
      dest[i] = 0;
    }
  }

  private static void Copy(byte[] src, byte[] dest, byte first, byte last)
  {
    for (byte i = first; i <= last; i++)
    {
      dest[i] = src[i];
    }
  }

  private static void Exchange(byte[] src, byte[] dest, byte first, byte last)
  {
    for (byte i = first; i <= last; i++)
    {
      byte tmp = dest[i];
      dest[i] = src[i];
      src[i] = tmp;
    }
  }

  private static void ShiftRight(byte[] dest, byte first, byte last)
  {
    for (byte i = first; i <= last; i++)
    {
      dest[i] = (byte)(i != last ? dest[i + 1] : 0);
    }
  }

  private static void ShiftLeft(byte[] dest, byte first, byte last)
  {
    for (sbyte i = (sbyte)last; i >= first; i--)
    {
      dest[i] = (byte)(i != first ? dest[i - 1] : 0);
    }
  }

  private static void Add(byte[] dest, byte[]? src, byte first, byte last, SpiceCpuState state)
  {
    for (byte i = first; i <= last; i++)
    {
      byte addend = src is null ? (byte)0 : src[i];
      byte sum = (byte)(dest[i] + addend + ((state.Flags & SpiceCpuFlags.Carry) != 0 ? 1 : 0));
      if (sum >= state.Base)
      {
        sum = (byte)(sum - state.Base);
        state.Flags |= SpiceCpuFlags.Carry;
      }
      else
      {
        state.Flags &= ~SpiceCpuFlags.Carry;
      }

      dest[i] = sum;
    }
  }

  private static void Increment(byte[] dest, byte first, byte last, SpiceCpuState state)
  {
    state.Flags |= SpiceCpuFlags.Carry;
    Add(dest, null, first, last, state);
  }

  private static void Subtract(
    byte[]? dest,
    byte[]? src,
    byte[]? src2,
    byte first,
    byte last,
    SpiceCpuState state)
  {
    for (byte i = first; i <= last; i++)
    {
      byte left = src is null ? (byte)0 : src[i];
      byte right = src2 is null ? (byte)0 : src2[i];
      sbyte diff = (sbyte)(left - right - ((state.Flags & SpiceCpuFlags.Carry) != 0 ? 1 : 0));
      if (diff < 0)
      {
        diff = (sbyte)(diff + state.Base);
        state.Flags |= SpiceCpuFlags.Carry;
      }
      else
      {
        state.Flags &= ~SpiceCpuFlags.Carry;
      }

      if (dest is not null)
      {
        dest[i] = (byte)diff;
      }
    }
  }

  private static void TestEqual(byte[] src, byte[]? dest, byte first, byte last, SpiceCpuState state)
  {
    state.Flags |= SpiceCpuFlags.Carry;
    for (byte i = first; i <= last; i++)
    {
      byte other = dest is null ? (byte)0 : dest[i];
      if (src[i] != other)
      {
        state.Flags &= ~SpiceCpuFlags.Carry;
        break;
      }
    }
  }

  private static void TestNotEqual(byte[] src, byte[]? dest, byte first, byte last, SpiceCpuState state)
  {
    state.Flags &= ~SpiceCpuFlags.Carry;
    for (byte i = first; i <= last; i++)
    {
      byte other = dest is null ? (byte)0 : dest[i];
      if (src[i] != other)
      {
        state.Flags |= SpiceCpuFlags.Carry;
        break;
      }
    }
  }
}
