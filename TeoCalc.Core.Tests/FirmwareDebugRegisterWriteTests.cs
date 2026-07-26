using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class FirmwareDebugRegisterWriteTests
{
  [TestMethod]
  public void ParseDigitRegister_RoundTripsFormat()
  {
    byte[] digits = new byte[ClassicRegisterFile.DigitCount];
    digits[0] = 0xA;
    digits[13] = 0xF;
    string hex = FirmwareDebugOpcodes.FormatDigitRegister(digits);
    byte[] copy = new byte[ClassicRegisterFile.DigitCount];
    Assert.IsTrue(FirmwareDebugOpcodes.TryParseDigitRegister(hex, copy, out string? error), error);
    CollectionAssert.AreEqual(digits, copy);
  }

  [TestMethod]
  public void ParseDigitRegister_RejectsTooLongAndBadHex()
  {
    byte[] dest = new byte[4];
    Assert.IsFalse(FirmwareDebugOpcodes.TryParseDigitRegister("12345", dest, out _));
    Assert.IsFalse(FirmwareDebugOpcodes.TryParseDigitRegister("12G4", dest, out _));
  }

  [TestMethod]
  public void ClassicGateway_TrySetDebugRegister_WritesA()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    gateway.ExecutionPaused = true;

    const string value = "0000000000001A";
    Assert.IsTrue(gateway.TrySetDebugRegister("A", value, out string? error), error);
    FirmwareDebugRegisters? regs = gateway.TryGetDebugRegisters();
    Assert.IsNotNull(regs);
    Assert.AreEqual(value, regs.Working.First(r => r.Name == "A").DigitsHex);
    Assert.AreEqual(0xA, gateway.Cpu!.State.Registers.A[0]);
    Assert.AreEqual(0x1, gateway.Cpu.State.Registers.A[1]);
  }

  [TestMethod]
  public void ClassicGateway_TrySetDebugRegister_UnknownNameFails()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    Assert.IsFalse(gateway.TrySetDebugRegister("X", "0", out string? error));
    StringAssert.Contains(error!, "Unknown");
  }
}
