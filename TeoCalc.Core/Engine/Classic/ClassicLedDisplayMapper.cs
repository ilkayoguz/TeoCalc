namespace TeoCalc.Core.Engine.Classic;

/// <summary>
/// Maps Classic CPU A/B register digits to 15 logical HP-65 LED positions.
/// Positions 1-15 (1-based): mantissa sign, mantissa (10 digits + dedicated decimal slot),
/// exponent sign, exponent digits.
/// </summary>
public static class ClassicLedDisplayMapper
{
  public const int LogicalSlotCount = 15;

  private const int MantissaRegionCount = 11;

  public static ClassicLedDisplaySlot[] Map(
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode = false,
    byte programEndState = 0)
  {
    ClassicLedDisplaySlot[] slots = new ClassicLedDisplaySlot[LogicalSlotCount];
    if (!displayOn)
    {
      return slots;
    }

    slots[0] = MapSign(
      registers.A[13],
      registers.B[13],
      programEndState == 2 && programMode);

    ClassicLedDisplaySlot[] mantissa = BuildMantissaRegion(registers);
    Array.Copy(mantissa, 0, slots, 1, MantissaRegionCount);

    slots[12] = MapSign(registers.A[2], registers.B[2]);
    slots[13] = MapExponentDigit(registers.A[1], registers.B[1]);
    slots[14] = MapExponentDigit(registers.A[0], registers.B[0]);
    return slots;
  }

  public static ClassicLedDisplaySlot[] Map(ClassicCpu cpu, bool programMode = false) =>
    Map(
      cpu.State.Registers,
      (cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0,
      programMode,
      cpu.Program.EndState);

  private static ClassicLedDisplaySlot[] BuildMantissaRegion(ClassicRegisterFile registers)
  {
    List<ClassicLedDisplaySlot> items = new(MantissaRegionCount + 1);
    for (int cpuIndex = 1; cpuIndex <= 10; cpuIndex++)
    {
      byte mantissa = registers.A[13 - cpuIndex];
      byte meta = registers.B[13 - cpuIndex];
      if (meta > 7)
      {
        items.Add(ClassicLedDisplaySlot.Blank);
        continue;
      }

      items.Add(ClassicLedDisplaySlot.FromDigit(mantissa));
      if ((meta & 2) != 0)
      {
        items.Add(ClassicLedDisplaySlot.DecimalPoint);
      }
    }

    while (items.Count < MantissaRegionCount)
    {
      items.Insert(0, ClassicLedDisplaySlot.Blank);
    }

    if (items.Count > MantissaRegionCount)
    {
      items.RemoveRange(0, items.Count - MantissaRegionCount);
    }

    return items.ToArray();
  }

  private static ClassicLedDisplaySlot MapSign(byte mantissa, byte meta, bool forceMinus = false)
  {
    if (meta > 7)
    {
      return ClassicLedDisplaySlot.Blank;
    }

    return forceMinus || mantissa >= 8
      ? ClassicLedDisplaySlot.Minus
      : ClassicLedDisplaySlot.Blank;
  }

  private static ClassicLedDisplaySlot MapExponentDigit(byte mantissa, byte meta)
  {
    if (meta > 7)
    {
      return ClassicLedDisplaySlot.Blank;
    }

    return ClassicLedDisplaySlot.FromDigit(mantissa);
  }
}

public enum ClassicLedSlotKind
{
  Blank,
  Minus,
  DecimalPoint,
  Digit,
}

public readonly struct ClassicLedDisplaySlot
{
  public static ClassicLedDisplaySlot Blank => new(ClassicLedSlotKind.Blank, 0);

  public static ClassicLedDisplaySlot Minus => new(ClassicLedSlotKind.Minus, 0);

  public static ClassicLedDisplaySlot DecimalPoint => new(ClassicLedSlotKind.DecimalPoint, 0);

  public ClassicLedSlotKind Kind { get; }

  public byte Digit { get; }

  private ClassicLedDisplaySlot(ClassicLedSlotKind kind, byte digit)
  {
    Kind = kind;
    Digit = digit;
  }

  public static ClassicLedDisplaySlot FromDigit(byte mantissa) =>
    new(ClassicLedSlotKind.Digit, mantissa);
}
