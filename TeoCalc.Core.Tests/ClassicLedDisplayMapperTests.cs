using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicLedDisplayMapperTests
{
  [TestMethod]
  public void Map_ReturnsFifteenSlots()
  {
    ClassicRegisterFile registers = new();
    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(registers, displayOn: true);
    Assert.AreEqual(15, slots.Length);
  }

  [TestMethod]
  public void Map_MantissaSign_ShowsMinusOnlyForNegative()
  {
    ClassicRegisterFile registers = new();
    registers.A[13] = 8;
    registers.B[13] = 0;
    ClassicLedDisplaySlot[] negative = ClassicLedDisplayMapper.Map(registers, displayOn: true);
    Assert.AreEqual(ClassicLedSlotKind.Minus, negative[0].Kind);

    registers.A[13] = 0;
    ClassicLedDisplaySlot[] positive = ClassicLedDisplayMapper.Map(registers, displayOn: true);
    Assert.AreEqual(ClassicLedSlotKind.Blank, positive[0].Kind);
  }

  [TestMethod]
  public void Map_DecimalPoint_OccupiesDedicatedMantissaSlot()
  {
    ClassicRegisterFile registers = new();
    registers.A[12] = 1;
    registers.B[12] = 2;
    registers.A[11] = 2;
    registers.B[11] = 0;
    registers.A[10] = 3;
    registers.B[10] = 0;
    registers.A[9] = 4;
    registers.B[9] = 0;

    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(registers, displayOn: true);

    int decimalIndex = Array.FindIndex(slots, slot => slot.Kind == ClassicLedSlotKind.DecimalPoint);
    Assert.IsTrue(decimalIndex >= 1 && decimalIndex <= 11);
    Assert.AreEqual(ClassicLedSlotKind.Digit, slots[decimalIndex - 1].Kind);
    Assert.AreEqual((byte)1, slots[decimalIndex - 1].Digit);
  }

  [TestMethod]
  public void Map_AllEights_FillsTenMantissaDigitsPlusLeadingBlank()
  {
    ClassicRegisterFile registers = new();
    for (int cpuIndex = 1; cpuIndex <= 10; cpuIndex++)
    {
      registers.A[13 - cpuIndex] = 8;
      registers.B[13 - cpuIndex] = 0;
    }

    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(registers, displayOn: true);
    ClassicLedDisplaySlot[] mantissa = slots[1..12];

    Assert.AreEqual(ClassicLedSlotKind.Blank, mantissa[0].Kind);
    Assert.IsTrue(mantissa.Skip(1).All(slot => slot.Kind == ClassicLedSlotKind.Digit && slot.Digit == 8));
  }

  [TestMethod]
  public void Map_ExponentSignAndDigits_MapToLastThreeSlots()
  {
    ClassicRegisterFile registers = new();
    registers.A[2] = 8;
    registers.B[2] = 0;
    registers.A[1] = 8;
    registers.B[1] = 0;
    registers.A[0] = 8;
    registers.B[0] = 0;

    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(registers, displayOn: true);

    Assert.AreEqual(ClassicLedSlotKind.Minus, slots[12].Kind);
    Assert.AreEqual(ClassicLedSlotKind.Digit, slots[13].Kind);
    Assert.AreEqual((byte)8, slots[13].Digit);
    Assert.AreEqual(ClassicLedSlotKind.Digit, slots[14].Kind);
    Assert.AreEqual((byte)8, slots[14].Digit);
  }

  [TestMethod]
  public void Map_DisplayOff_ReturnsBlankSlots()
  {
    ClassicRegisterFile registers = new();
    registers.A[12] = 5;
    registers.B[12] = 0;

    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(registers, displayOn: false);
    Assert.IsTrue(slots.All(slot => slot.Kind == ClassicLedSlotKind.Blank));
  }
}
