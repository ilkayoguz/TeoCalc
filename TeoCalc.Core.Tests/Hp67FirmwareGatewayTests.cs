using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Hp67;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp67FirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp67Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-67");

  [TestMethod]
  [DataRow("HP-67")]
  [DataRow("HP-67BE")]
  [DataRow("67")]
  [DataRow("T-67")]
  public void Bootstrap_Routes_To_Hp67FirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<Hp67FirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Factory_Create_LoadsRomAndHandlers()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-67", "Model.json"));
    Hp67Cpu cpu = Hp67CpuFactory.Create(model, engineRoot);
    Assert.AreEqual(5120, model.Hardware.RomWordCount);
    Assert.AreEqual(0, cpu.State.ProgramCounter);
    Assert.AreEqual(0, cpu.StepCount);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
    StringAssert.StartsWith(handler.HandlerId, "ActCpu.");
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_WithoutPanamatikTypes()
  {
    ICalcFirmwareGateway gateway = CreateHp67Gateway();
    gateway.PowerOnResume();
    for (int i = 0; i < 80; i++)
    {
      gateway.Tick(0.05f);
    }

    Assert.IsTrue(gateway.PowerOn);
    Assert.IsTrue(gateway.IsDisplayVisible());
    StringAssert.Contains(gateway.DisplayText, "0");
  }

  [TestMethod]
  public void IsNativeHp67Pilot_RomReady()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-67"));
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-67BE"));
  }

  [TestMethod]
  public void IsNativeHp67Pilot_ExcludesClassicWrongIsa()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-65"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-67"));
  }

  [TestMethod]
  public void Hp67Cpu_Is_ActCpuBase()
  {
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Hp67Cpu)));
  }
}
