using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CpuMorphologyTests
{
  [TestMethod]
  public void Classic_Woodstock_Spice_Are_ICpu()
  {
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(IActCpu).IsAssignableFrom(typeof(WoodstockCpu)));
  }

  [TestMethod]
  public void Gateways_Share_CalcFirmwareGatewayBase()
  {
    Assert.IsTrue(typeof(CalcFirmwareGatewayBase).IsAssignableFrom(typeof(ClassicFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<WoodstockCpu>).IsAssignableFrom(typeof(WoodstockFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<SpiceCpu>).IsAssignableFrom(typeof(SpiceFirmwareGateway)));
  }

  [TestMethod]
  public void Factories_Produce_ICpu()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    ICpu classic = ClassicCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-65", "Model.json")),
      engineRoot);
    ICpu woodstock = WoodstockCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-25", "Model.json")),
      engineRoot);
    ICpu spice = SpiceCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-31", "Model.json")),
      engineRoot);

    Assert.AreEqual(0, classic.StepCount);
    Assert.AreEqual(0, woodstock.StepCount);
    Assert.AreEqual(0, spice.StepCount);
    Assert.IsFalse(string.IsNullOrWhiteSpace(classic.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(woodstock.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(spice.Step().HandlerId));
  }
}
