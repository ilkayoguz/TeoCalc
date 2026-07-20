using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class WoodstockFirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp25Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-25");

  private static ProgramVocabulary LoadHp25Vocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-25/Program/program.vocabulary.json"));

  [TestMethod]
  [DataRow("HP-21")]
  [DataRow("HP-22")]
  [DataRow("HP-25")]
  [DataRow("HP-27")]
  [DataRow("HP-29")]
  [DataRow("HP-29C")]
  [DataRow("25")]
  [DataRow("T-25")]
  public void Bootstrap_Routes_RomReadyWoodstock_To_WoodstockFirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<WoodstockFirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Bootstrap_Routes_Hp31_To_SpiceFirmwareGateway()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-31");
    Assert.IsInstanceOfType(gateway, typeof(SpiceFirmwareGateway));
  }

  [TestMethod]
  [DataRow("HP-21")]
  [DataRow("HP-22")]
  [DataRow("HP-25")]
  [DataRow("HP-27")]
  [DataRow("HP-29")]
  public void Factory_Create_LoadsRomAndHandlers(string modelId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, modelId, "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    WoodstockCpu cpu = WoodstockCpuFactory.Create(model, engineRoot);
    Assert.IsTrue(cpu.State.ProgramCounter == 0);
    Assert.IsTrue(cpu.StepCount == 0);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
  {
    ICalcFirmwareGateway gateway = CreateHp25Gateway();
    gateway.PowerOnResume();
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    Assert.IsTrue(gateway.PowerOn);
    Assert.IsTrue(gateway.IsDisplayVisible());
    StringAssert.Contains(gateway.DisplayText, "0");
  }

  [TestMethod]
  public void KeyDown_Digit_UpdatesDisplayText()
  {
    ICalcFirmwareGateway gateway = CreateHp25Gateway();
    gateway.PowerOnResume();
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    ProgramVocabulary vocabulary = LoadHp25Vocabulary();
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode));

    gateway.KeyDown(new FirmwareKeyCommand(16, keyCode));
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    gateway.KeyUp();
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    Assert.IsTrue(gateway.IsDisplayVisible());
    StringAssert.Contains(gateway.DisplayText, "7");
  }

  [TestMethod]
  public void WoodstockGateway_Batch_ClassicDiagnosticsNull()
  {
    ICalcFirmwareGateway gateway = CreateHp25Gateway();
    gateway.PowerOnResume();
    Assert.IsNull(gateway.LastBatch.Classic);
  }

  [TestMethod]
  [DataRow("HP-21")]
  [DataRow("HP-22")]
  [DataRow("HP-25")]
  [DataRow("HP-27")]
  [DataRow("HP-29")]
  public void IsNativeWoodstockPilot_RomReadyModels(string modelId)
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeWoodstockPilot(modelId), modelId);
  }

  [TestMethod]
  public void IsNativeWoodstockPilot_ExcludesClassicAndSpice()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeWoodstockPilot("HP-65"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeWoodstockPilot("HP-31"));
  }
}
