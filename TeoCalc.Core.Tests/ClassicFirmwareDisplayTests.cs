using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicFirmwareDisplayTests
{
  private static void SetRegisterMsbFirst(byte[] register, string digits)
  {
    Assert.AreEqual(ClassicRegisterFile.DigitCount, digits.Length);
    for (int i = 0; i < digits.Length; i++)
    {
      register[ClassicRegisterFile.DigitCount - 1 - i] = (byte)(digits[i] - '0');
    }
  }

  [TestMethod]
  public void IdleZero_FormatsRawPanamatikDisplayRegisters()
  {
    ClassicCpuState state = new();
    state.Flags = ClassicCpuFlags.DisplayOn;
    SetRegisterMsbFirst(state.Registers.A, "00000000000999");
    SetRegisterMsbFirst(state.Registers.B, "02009999999999");

    string text = ClassicFirmwareDisplay.BuildLedText(state);

    Assert.AreEqual("0.00", string.Join(' ', text.Replace(';', '.').Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)));
  }

  [TestMethod]
  public void DisplayOff_BlanksText()
  {
    ClassicCpuState state = new();
    SetRegisterMsbFirst(state.Registers.A, "00000000000999");
    SetRegisterMsbFirst(state.Registers.B, "02009999999999");

    Assert.AreEqual(string.Empty, ClassicFirmwareDisplay.BuildLedText(state));
  }

  [TestMethod]
  public void LongIntegerEntry_UsesRawFirmwareMask()
  {
    ClassicCpuState state = new();
    state.Flags = ClassicCpuFlags.DisplayOn;
    SetRegisterMsbFirst(state.Registers.A, "01234567890000");
    SetRegisterMsbFirst(state.Registers.B, "00000000002999");

    string text = ClassicFirmwareDisplay.BuildLedText(state);

    Assert.AreEqual("1234567890.", string.Join(' ', text.Replace(';', '.').Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)));
  }
}
