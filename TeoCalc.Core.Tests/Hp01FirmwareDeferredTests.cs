using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Hp01FirmwareDeferredTests
{
  [TestMethod]
  public void Hp01_FirmwareAssetsPresent_ButNativeDeferred()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-01", "Model.json"));
    Assert.AreEqual(2304, model.Hardware.RomWordCount);
    Assert.IsFalse(string.IsNullOrWhiteSpace(model.Firmware.RomBinary));
    Assert.IsTrue(File.Exists(Path.Combine(engineRoot, "HP-01", model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar))));

    // ACThp01 uses a different op_fcn00 table — stay on EmulatorFirmwareGateway until a dedicated Cpu exists.
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeHp19Pilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeWoodstockPilot("HP-01"));
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-01");
    Assert.IsInstanceOfType(gateway, typeof(EmulatorFirmwareGateway));
  }
}
