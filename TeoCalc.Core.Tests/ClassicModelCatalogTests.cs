using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicModelCatalogTests
{
  private static string EngineRoot => TeoCalcPaths.ResourcePath("Engine");

  [TestMethod]
  public void ClassicModels_LoadFirmwareAndMatchRomWordCount()
  {
    foreach (string modelId in new[] { "HP-35", "HP-45", "HP-55", "HP-65", "HP-70", "HP-80" })
    {
      TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(EngineRoot, CalcModelIds.ToEngineId(modelId), "Model.json"));
      string romPath = Path.Combine(EngineRoot, CalcModelIds.ToEngineId(modelId), model.Firmware.RomBinary);
      MicrocodeRom rom = MicrocodeRom.LoadBinary(romPath);
      Assert.AreEqual(model.Hardware.RomWordCount, rom.WordCount, modelId);
      ClassicCpu cpu = ClassicCpuFactory.Create(model, EngineRoot);
      Assert.IsNotNull(cpu);
    }
  }

  [TestMethod]
  public void StudyModels_LoadFirmwareAndMatchRomWordCount()
  {
    foreach (string modelId in new[]
             {
               "HP-21", "HP-22", "HP-25", "HP-27", "HP-29",
               "HP-31", "HP-32", "HP-33", "HP-34", "HP-37", "HP-38",
             })
    {
      TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(EngineRoot, CalcModelIds.ToEngineId(modelId), "Model.json"));
      string romPath = Path.Combine(EngineRoot, CalcModelIds.ToEngineId(modelId), model.Firmware.RomBinary);
      MicrocodeRom rom = MicrocodeRom.LoadBinary(romPath);
      Assert.AreEqual(model.Hardware.RomWordCount, rom.WordCount, modelId);

      string mapPath = Path.Combine(EngineRoot, CalcModelIds.ToEngineId(modelId), model.Firmware.RomMap);
      MicrocodeMapCatalog map = MicrocodeMapCatalog.Load(mapPath);
      Assert.AreEqual(model.Hardware.RomWordCount, map.WordCount, modelId);
    }
  }

  [TestMethod]
  public void MicrocodeCrossRef_HasJsbAndGoEntries()
  {
    MicrocodeCrossRefCatalog crossRef = MicrocodeCrossRefCatalog.Load(
      Path.Combine(EngineRoot, "Classic", "microcode.crossref.json"));
    Assert.IsNotNull(crossRef.TryGetHandler("ClassicCpu.SubroutineJump"));
    Assert.AreEqual("go", crossRef.TryGetHandler("ClassicCpu.Branch")?.NonpareilMnemonic);
  }
}
