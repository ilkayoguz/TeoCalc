using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicCpuProgramRamBaseTests
{
  private static string EngineRoot => TeoCalcPaths.ResourcePath("Engine");

  [TestMethod]
  public void Reset_Preserves_ModelProgramRamBase_Hp35()
  {
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      Path.Combine(EngineRoot, "HP-35", "Model.json"));
    Assert.AreEqual(0, model.Hardware.ProgramRamBase);

    ClassicCpu cpu = ClassicCpuFactory.Create(model, EngineRoot);
    Assert.AreEqual(0, cpu.State.ProgramRamBase);

    cpu.State.ProgramRamBase = 0;
    cpu.Reset();
    Assert.AreEqual(0, cpu.State.ProgramRamBase);
  }

  [TestMethod]
  public void Reset_Preserves_ModelProgramRamBase_Hp65()
  {
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      Path.Combine(EngineRoot, "HP-65", "Model.json"));
    Assert.AreEqual(112, model.Hardware.ProgramRamBase);

    ClassicCpu cpu = ClassicCpuFactory.Create(model, EngineRoot);
    Assert.AreEqual(112, cpu.State.ProgramRamBase);

    cpu.Reset();
    Assert.AreEqual(112, cpu.State.ProgramRamBase);
  }

  [TestMethod]
  public void PowerOff_Preserves_ProgramRamBase_ViaGateway()
  {
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(
      Path.Combine(EngineRoot, "HP-35", "Model.json"));
    ClassicCpu cpu = ClassicCpuFactory.Create(model, EngineRoot);
    ClassicFirmwareGateway gateway = new();
    gateway.AttachCpu(cpu);
    gateway.PowerOnResume();
    gateway.PowerOff();
    Assert.AreEqual(0, cpu.State.ProgramRamBase);
  }
}
