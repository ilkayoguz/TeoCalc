using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Hp01;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp01FirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp01Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-01");

  [TestMethod]
  [DataRow("HP-01")]
  [DataRow("01")]
  [DataRow("T-01")]
  public void Bootstrap_Routes_To_Hp01FirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<Hp01FirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Factory_Create_LoadsRomAndHandlers()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-01", "Model.json"));
    Assert.AreEqual(2304, model.Hardware.RomWordCount);
    Assert.AreEqual("HP01", model.Family);

    Hp01Cpu cpu = Hp01CpuFactory.Create(model, engineRoot);
    Assert.AreEqual(0, cpu.State.ProgramCounter);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
    StringAssert.StartsWith(handler.HandlerId, "Hp01Cpu.");
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_WithoutPanamatikTypes()
  {
    ICalcFirmwareGateway gateway = CreateHp01Gateway();
    Assert.IsInstanceOfType(gateway, typeof(Hp01FirmwareGateway));
    gateway.PowerOnResume();
    for (int i = 0; i < 80; i++)
    {
      gateway.Tick(0.05f);
    }

    Assert.IsTrue(gateway.PowerOn);
    Assert.IsTrue(((Hp01FirmwareGateway)gateway).Cpu!.StepCount > 0);
    Assert.IsTrue(gateway.IsDisplayVisible(), $"display='{gateway.DisplayText}' flags={((Hp01FirmwareGateway)gateway).Cpu!.State.Flags}");
    Assert.IsTrue(gateway.DisplayText.Length > 0);
    // Time idle typically includes digit and/or colon separators.
    Assert.IsTrue(
      gateway.DisplayText.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':']) >= 0,
      gateway.DisplayText);
  }

  [TestMethod]
  public void IsNativeHp01_RomReady()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeHp01("HP-01"));
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeHp01("01"));
  }

  [TestMethod]
  public void IsNativeHp01_ExcludesOthers()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp01("HP-67"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp01("HP-19C"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp01("HP-25"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeWoodstockPilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-01"));
  }

  [TestMethod]
  public void Hp01Cpu_Is_CpuBase_Not_ActCpuBase()
  {
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(Hp01Cpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Hp01Cpu)));
    Assert.IsFalse(typeof(TeoCalc.Core.Engine.Act.ActCpuBase).IsAssignableFrom(typeof(Hp01Cpu)));
  }

  [TestMethod]
  public void RomWordCount_Is_2304()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-01", "Model.json"));
    Assert.AreEqual(2304, model.Hardware.RomWordCount);
    string romPath = Path.Combine(engineRoot, "HP-01", model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    MicrocodeRom rom = MicrocodeRom.LoadBinary(romPath);
    Assert.AreEqual(2304, rom.WordCount);
  }
}
