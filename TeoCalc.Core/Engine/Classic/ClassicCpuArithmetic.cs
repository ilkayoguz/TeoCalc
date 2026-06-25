namespace TeoCalc.Core.Engine.Classic;

internal static class ClassicCpuArithmetic
{
  public static void Execute(ushort opcode, ClassicCpuState state)
  {
    (byte first, byte last) = ResolveField(opcode, state.P);
    int operation = opcode >> 5;
    ClassicRegisterFile registers = state.Registers;

    switch (operation)
    {
      case 0:
        TestNotEqual(registers.B, null, first, last, state);
        break;
      case 1:
        Zero(registers.B, first, last);
        break;
      case 2:
        Subtract(registers.A, registers.C, null, first, last, state);
        break;
      case 3:
        TestEqual(registers.C, null, first, last, state);
        break;
      case 4:
        Copy(registers.C, registers.B, first, last);
        break;
      case 5:
        Subtract(null, registers.C, registers.C, first, last, state);
        break;
      case 6:
        Zero(registers.C, first, last);
        break;
      case 7:
        state.Flags |= ClassicCpuFlags.Carry;
        Subtract(null, registers.C, registers.C, first, last, state);
        break;
      case 8:
        ShiftLeft(registers.A, first, last);
        break;
      case 9:
        Copy(registers.B, registers.A, first, last);
        break;
      case 10:
        Subtract(registers.A, registers.C, registers.C, first, last, state);
        break;
      case 11:
        state.Flags |= ClassicCpuFlags.Carry;
        Subtract(registers.C, registers.C, null, first, last, state);
        break;
      case 12:
        Copy(registers.A, registers.C, first, last);
        break;
      case 13:
        TestNotEqual(registers.C, null, first, last, state);
        break;
      case 14:
        Add(registers.C, registers.A, first, last, state);
        break;
      case 15:
        Increment(registers.C, first, last, state);
        break;
      case 16:
        Subtract(registers.A, registers.B, null, first, last, state);
        break;
      case 17:
        Exchange(registers.B, registers.C, first, last);
        break;
      case 18:
        ShiftRight(registers.C, first, last);
        break;
      case 19:
        TestEqual(registers.A, null, first, last, state);
        break;
      case 20:
        ShiftRight(registers.B, first, last);
        break;
      case 21:
        Add(registers.C, registers.C, first, last, state);
        break;
      case 22:
        ShiftRight(registers.A, first, last);
        break;
      case 23:
        Zero(registers.A, first, last);
        break;
      case 24:
        Subtract(registers.A, registers.B, registers.A, first, last, state);
        break;
      case 25:
        Exchange(registers.A, registers.B, first, last);
        break;
      case 26:
        Subtract(registers.A, registers.C, registers.A, first, last, state);
        break;
      case 27:
        state.Flags |= ClassicCpuFlags.Carry;
        Subtract(registers.A, registers.A, null, first, last, state);
        break;
      case 28:
        Add(registers.A, registers.B, first, last, state);
        break;
      case 29:
        Exchange(registers.A, registers.C, first, last);
        break;
      case 30:
        Add(registers.A, registers.C, first, last, state);
        break;
      case 31:
        Increment(registers.A, first, last, state);
        break;
    }
  }

  private static (byte First, byte Last) ResolveField(ushort opcode, byte p)
  {
    return ((opcode >> 2) & 7) switch
    {
      0 => (p, p),
      1 => (3, 12),
      2 => (0, 2),
      3 => (0, 13),
      4 => (0, p),
      5 => (3, 13),
      6 => (2, 2),
      _ => (13, 13),
    };
  }

  private static void Zero(byte[] dest, byte first, byte last)
  {
    for (byte index = first; index <= last; index++)
    {
      dest[index] = 0;
    }
  }

  private static void Copy(byte[] dest, byte[] src, byte first, byte last)
  {
    for (byte index = first; index <= last; index++)
    {
      dest[index] = src[index];
    }
  }

  private static void Exchange(byte[] dest, byte[] src, byte first, byte last)
  {
    for (byte index = first; index <= last; index++)
    {
      byte temp = dest[index];
      dest[index] = src[index];
      src[index] = temp;
    }
  }

  private static void ShiftRight(byte[] dest, byte first, byte last)
  {
    for (byte index = first; index <= last; index++)
    {
      dest[index] = index != last ? dest[(byte)(index + 1)] : (byte)0;
    }
  }

  private static void ShiftLeft(byte[] dest, byte first, byte last)
  {
    for (int index = last; index >= first; index--)
    {
      dest[index] = index != first ? dest[index - 1] : (byte)0;
    }
  }

  private static void TestEqual(byte[] src, byte[]? dest, byte first, byte last, ClassicCpuState state)
  {
    state.Flags |= ClassicCpuFlags.Carry;
    for (byte index = first; index <= last; index++)
    {
      byte destValue = dest is null ? (byte)0 : dest[index];
      if (src[index] != destValue)
      {
        state.Flags &= ~ClassicCpuFlags.Carry;
        break;
      }
    }
  }

  private static void TestNotEqual(byte[] src, byte[]? dest, byte first, byte last, ClassicCpuState state)
  {
    state.Flags &= ~ClassicCpuFlags.Carry;
    for (byte index = first; index <= last; index++)
    {
      byte destValue = dest is null ? (byte)0 : dest[index];
      if (src[index] != destValue)
      {
        state.Flags |= ClassicCpuFlags.Carry;
        break;
      }
    }
  }

  private static void Increment(byte[] dest, byte first, byte last, ClassicCpuState state)
  {
    state.Flags |= ClassicCpuFlags.Carry;
    Add(dest, null, first, last, state);
  }

  private static void Add(byte[] dest, byte[]? src, byte first, byte last, ClassicCpuState state)
  {
    for (byte index = first; index <= last; index++)
    {
      byte srcValue = src is null ? (byte)0 : src[index];
      int carry = (state.Flags & ClassicCpuFlags.Carry) != 0 ? 1 : 0;
      int sum = dest[index] + srcValue + carry;
      if (sum >= state.Base)
      {
        sum -= state.Base;
        state.Flags |= ClassicCpuFlags.Carry;
      }
      else
      {
        state.Flags &= ~ClassicCpuFlags.Carry;
      }

      dest[index] = (byte)sum;
    }
  }

  private static void Subtract(byte[]? src, byte[]? src2, byte[]? dest, byte first, byte last, ClassicCpuState state)
  {
    for (byte index = first; index <= last; index++)
    {
      byte left = src is null ? (byte)0 : src[index];
      byte right = src2 is null ? (byte)0 : src2[index];
      int borrow = (state.Flags & ClassicCpuFlags.Carry) != 0 ? 1 : 0;
      int difference = left - right - borrow;
      if (difference < 0)
      {
        difference += state.Base;
        state.Flags |= ClassicCpuFlags.Carry;
      }
      else
      {
        state.Flags &= ~ClassicCpuFlags.Carry;
      }

      if (dest is not null)
      {
        dest[index] = (byte)difference;
      }
    }
  }
}
