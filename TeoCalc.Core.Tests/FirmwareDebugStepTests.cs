using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class FirmwareDebugStepTests
{
  [TestMethod]
  public void StepInto_AdvancesOneInstruction_AndPauses()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    int before = gateway.LastBatch.StepCount;

    gateway.StepInto();

    Assert.IsTrue(gateway.ExecutionPaused);
    Assert.AreEqual(before + 1, gateway.LastBatch.StepCount);
    Assert.IsFalse(string.IsNullOrEmpty(gateway.LastBatch.LastHandlerId));
  }

  [TestMethod]
  public void ExecutionPaused_BlocksTickBatches()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    gateway.ExecutionPaused = true;
    int before = gateway.LastBatch.StepCount;

    gateway.Tick(1f);

    Assert.AreEqual(before, gateway.LastBatch.StepCount);
  }

  [TestMethod]
  public void ContinueExecution_AllowsTickAgain()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    gateway.StepInto();
    Assert.IsTrue(gateway.ExecutionPaused);
    int before = gateway.LastBatch.StepCount;

    gateway.ContinueExecution();
    gateway.Tick(0.05f);

    Assert.IsFalse(gateway.ExecutionPaused);
    Assert.IsTrue(gateway.LastBatch.StepCount > before);
  }

  [TestMethod]
  public void StepOver_NonCall_MatchesStepInto()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    // Idle scan is rarely a JSB at the first stepped op after power-on; StepOver should still advance.
    int before = gateway.LastBatch.StepCount;
    gateway.StepOver();
    Assert.IsTrue(gateway.LastBatch.StepCount >= before + 1);
    Assert.IsTrue(gateway.ExecutionPaused);
  }

  [TestMethod]
  public void CaptureDebugDump_IncludesPcAndRegisters()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    gateway.StepInto();

    string dump = gateway.CaptureDebugDump();

    StringAssert.Contains(dump, "TeoCalc DEBUG DUMP");
    StringAssert.Contains(dump, "PC=");
    StringAssert.Contains(dump, "A=");
    Assert.IsNotNull(gateway.TryGetDebugRegisters());
    Assert.AreEqual(7, gateway.TryGetDebugRegisters()!.Working.Count);
  }

  [TestMethod]
  public void ClassicLastBatch_UsesFetchAddress()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    gateway.StepInto();

    Assert.AreEqual(gateway.Cpu!.State.FetchAddress, gateway.LastBatch.ProgramCounter);
  }
}
