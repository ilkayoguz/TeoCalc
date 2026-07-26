using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class FirmwareCallStackTests
{
  [TestMethod]
  public void FromHardware_MarksTopFromStackPointer()
  {
    FirmwareCallStackSnapshot snap = FirmwareCallStackSnapshot.FromHardware(
      new ushort[] { 0x10, 0x20 },
      stackPointer: 1);
    Assert.AreEqual(2, snap.Slots.Count);
    Assert.IsTrue(snap.Slots[0].IsTop);
    Assert.IsFalse(snap.Slots[1].IsTop);
    Assert.AreEqual(1, snap.StackPointer);
  }

  [TestMethod]
  public void FromHardware_WithoutSp_MarksSlot0()
  {
    FirmwareCallStackSnapshot snap = FirmwareCallStackSnapshot.FromHardware(new ushort[] { 0xAB, 0 });
    Assert.IsTrue(snap.Slots[0].IsTop);
    Assert.IsFalse(snap.Slots[1].IsTop);
    Assert.IsNull(snap.StackPointer);
  }

  [TestMethod]
  public void ClassicGateway_ExposesReturnStack_AfterJsb()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();

    int guard = 0;
    while (guard++ < 100_000)
    {
      gateway.StepInto();
      if (FirmwareDebugOpcodes.IsSubroutineCall(gateway.LastBatch.LastHandlerId))
      {
        break;
      }
    }

    Assert.IsTrue(FirmwareDebugOpcodes.IsSubroutineCall(gateway.LastBatch.LastHandlerId));
    FirmwareCallStackSnapshot? stack = gateway.TryGetCallStack();
    Assert.IsNotNull(stack);
    Assert.AreEqual(2, stack.Slots.Count);
    Assert.AreEqual(gateway.Cpu!.State.ReturnStack[0], stack.Slots[0].Address);
    Assert.IsTrue(stack.Slots[0].IsTop);
    Assert.AreNotEqual(0, stack.Slots[0].Address);
  }
}
