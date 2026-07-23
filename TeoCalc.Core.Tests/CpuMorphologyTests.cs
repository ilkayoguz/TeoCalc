using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo01;
using TeoCalc.Core.Engine.Teo19;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CpuMorphologyTests
{
  [TestMethod]
  public void Classic_Woodstock_Spice_Hp67_Hp19_Hp01_Are_ICpu()
  {
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Teo67Cpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Teo19Cpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Teo01Cpu)));
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(ClassicCpu)));
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(Teo01Cpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(WoodstockCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(SpiceCpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Teo67Cpu)));
    Assert.IsTrue(typeof(ActCpuBase).IsAssignableFrom(typeof(Teo19Cpu)));
    Assert.IsFalse(typeof(ActCpuBase).IsAssignableFrom(typeof(Teo01Cpu)));
    Assert.IsTrue(typeof(IActCpu).IsAssignableFrom(typeof(WoodstockCpu)));
  }

  [TestMethod]
  public void Gateways_Share_CalcFirmwareGatewayBase()
  {
    Assert.IsTrue(typeof(CalcFirmwareGatewayBase).IsAssignableFrom(typeof(ClassicFirmwareGateway)));
    Assert.IsTrue(typeof(CalcFirmwareGatewayBase).IsAssignableFrom(typeof(Teo01FirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<WoodstockCpu>).IsAssignableFrom(typeof(WoodstockFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<SpiceCpu>).IsAssignableFrom(typeof(SpiceFirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<Teo67Cpu>).IsAssignableFrom(typeof(Teo67FirmwareGateway)));
    Assert.IsTrue(typeof(ActFirmwareGatewayBase<Teo19Cpu>).IsAssignableFrom(typeof(Teo19FirmwareGateway)));
  }

  [TestMethod]
  public void Factories_Produce_ICpu()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    ICpu classic = ClassicCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-65", "Model.json")),
      engineRoot);
    ICpu woodstock = WoodstockCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-25", "Model.json")),
      engineRoot);
    ICpu spice = SpiceCpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-31", "Model.json")),
      engineRoot);
    ICpu hp67 = Teo67CpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-67", "Model.json")),
      engineRoot);
    ICpu hp19 = Teo19CpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-19C", "Model.json")),
      engineRoot);
    ICpu hp01 = Teo01CpuFactory.Create(
      TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-01", "Model.json")),
      engineRoot);

    Assert.AreEqual(0, classic.StepCount);
    Assert.AreEqual(0, woodstock.StepCount);
    Assert.AreEqual(0, spice.StepCount);
    Assert.AreEqual(0, hp67.StepCount);
    Assert.AreEqual(0, hp19.StepCount);
    Assert.AreEqual(0, hp01.StepCount);
    Assert.IsFalse(string.IsNullOrWhiteSpace(classic.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(woodstock.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(spice.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(hp67.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(hp19.Step().HandlerId));
    Assert.IsFalse(string.IsNullOrWhiteSpace(hp01.Step().HandlerId));
  }

  [TestMethod]
  [DataRow("HP-25")]
  [DataRow("HP-31")]
  public void ActHandlerCatalog_UsesActCpuPrefix(string modelId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, CalcModelIds.ToEngineId(modelId), "Model.json"));
    string handlerPath = Path.Combine(
      engineRoot,
      model.Model,
      model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));
    MicrocodeHandlerCatalog catalog = MicrocodeHandlerCatalog.Load(handlerPath);
    Assert.IsTrue(catalog.Handlers.TrueForAll(h => h.HandlerId.StartsWith("ActCpu.", StringComparison.Ordinal)));
    Assert.AreEqual("Act", catalog.CpuFamily);
  }
}
