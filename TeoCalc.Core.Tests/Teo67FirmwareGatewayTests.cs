using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Teo67FirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp67Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-67");

  [TestMethod]
  [DataRow("HP-67")]
  [DataRow("HP-67BE")]
  [DataRow("67")]
  [DataRow("T-67")]
  public void Bootstrap_Routes_To_Teo67FirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<Teo67FirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Factory_Create_LoadsRomAndHandlers()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-67", "Model.json"));
    Teo67Cpu cpu = Teo67CpuFactory.Create(model, engineRoot);
    Assert.AreEqual(5120, model.Hardware.RomWordCount);
    Assert.AreEqual(0, cpu.State.ProgramCounter);
    Assert.AreEqual(0, cpu.StepCount);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
    StringAssert.StartsWith(handler.HandlerId, "ActCpu.");
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
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
  public void IsNativeTeo67Pilot_RomReady()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeTeo67Pilot("HP-67"));
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeTeo67Pilot("HP-67BE"));
  }

  [TestMethod]
  public void IsNativeTeo67Pilot_ExcludesClassicWrongIsa()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo67Pilot("HP-65"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-67"));
  }

  [TestMethod]
  public void Teo67Cpu_Is_ActCpuBase()
  {
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Teo67Cpu)));
  }
}
