using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Hp19;
using TeoCalc.Core.Engine.Hp67;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CpuMorphologyTests
{
  [TestMethod]
  public void Classic_Woodstock_Spice_Hp67_Hp19_Are_ICpu()
  {
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Hp67Cpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Hp19Cpu)));
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Hp67Cpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Hp19Cpu)));
    Assert.IsTrue(typeof(IActCpu).IsAssignableFrom(typeof(WoodstockCpu)));
  }

  [TestMethod]
  public void Gateways_Share_CalcFirmwareGatewayBase()
  {
    Assert.IsTrue(typeof(CalcFirmwareGatewayBase).IsAssignableFrom(typeof(ClassicFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<WoodstockCpu>).IsAssignableFrom(typeof(WoodstockFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<SpiceCpu>).IsAssignableFrom(typeof(SpiceFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<Hp67Cpu>).IsAssignableFrom(typeof(Hp67FirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<Hp19Cpu>).IsAssignableFrom(typeof(Hp19FirmwareGateway)));
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
    ICpu hp67 = Hp67CpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-67", "Model.json")),
      engineRoot);
    ICpu hp19 = Hp19CpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-19C", "Model.json")),
      engineRoot);

    Assert.AreEqual(0, classic.StepCount);
    Assert.AreEqual(0, woodstock.StepCount);
    Assert.AreEqual(0, spice.StepCount);
    Assert.AreEqual(0, hp67.StepCount);
    Assert.AreEqual(0, hp19.StepCount);
    Assert.IsFalse(string.IsNullOrWhiteSpace(classic.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(woodstock.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(spice.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(hp67.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(hp19.Step().HandlerId));
  }

  [TestMethod]
  [DataRow("HP-25")]
  [DataRow("HP-31")]
  public void ActHandlerCatalog_UsesActCpuPrefix(string modelId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, modelId, "Model.json"));
    string handlerPath = Path.Combine(
      engineRoot,
      model.Model,
      model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(handlerPath);
    Assert.IsTrue(catalog.Handlers.TrueForAll(h => h.HandlerId.StartsWith("ActCpu.", StringComparison.Ordinal)));
    Assert.AreEqual("Act", catalog.CpuFamily);
  }
}
