using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicFirmwareDisplayTests
{
  [TestMethod]
  public void IdleZero_Fix2_ShowsTwoDecimalPlaces()
  {
    ClassicCpuState state = new();
    state.Reset();
    state.Flags = ClassicCpuFlags.DisplayOn;
    state.Registers.B[12] = 2;

    string? text = ClassicFirmwareDisplay.TryBuildLedText(state);
    Assert.IsNotNull(text);
    StringAssert.Contains(text, "0;00");
    Assert.IsFalse(text.Length >= 2 && text[^2] == '0' && text[^1] == '0', "Exponent digits should be blanked.");
  }

  [TestMethod]
  public void MultiplexStep_SkipsUpdate()
  {
    ClassicCpuState state = new();
    state.Flags = ClassicCpuFlags.DisplayOn;
    state.Registers.B[12] = 0;
    Assert.IsNull(ClassicFirmwareDisplay.TryBuildLedText(state));
  }

  [TestMethod]
  public void EnteredMantissaDecimalMarker_UpdatesDisplay()
  {
    ClassicCpuState state = new();
    state.Flags = ClassicCpuFlags.DisplayOn;
    state.Registers.A[12] = 7;
    state.Registers.B[12] = 0;
    state.Registers.A[11] = 6;
    state.Registers.B[11] = 2;

    string? text = ClassicFirmwareDisplay.TryBuildLedText(state);

    Assert.IsNotNull(text);
    StringAssert.Contains(text, "76;");
  }
}
