using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Teo19;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Teo19FirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp19Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-19C");

  [TestMethod]
  [DataRow("HP-19C")]
  [DataRow("19C")]
  [DataRow("T-19C")]
  public void Bootstrap_Routes_To_Teo19FirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<Teo19FirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Factory_Create_LoadsRomAndHandlers()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-19C", "Model.json"));
    Teo19Cpu cpu = Teo19CpuFactory.Create(model, engineRoot);
    Assert.AreEqual(5120, model.Hardware.RomWordCount);
    Assert.AreEqual(0, cpu.State.ProgramCounter);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
    StringAssert.StartsWith(handler.HandlerId, "ActCpu.");
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
  {
    ICalcFirmwareGateway gateway = CreateHp19Gateway();
    Assert.IsInstanceOfType(gateway, typeof(Teo19FirmwareGateway));
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
  public void IsNativeTeo19Pilot_RomReady()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeTeo19Pilot("HP-19C"));
  }

  [TestMethod]
  public void IsNativeTeo19Pilot_ExcludesOthers()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo19Pilot("HP-67"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo19Pilot("HP-31"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-19C"));
  }

  [TestMethod]
  public void Teo19Cpu_Is_ActCpuBase()
  {
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Teo19Cpu)));
  }
}
