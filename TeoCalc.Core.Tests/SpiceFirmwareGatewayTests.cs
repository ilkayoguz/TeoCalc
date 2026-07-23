using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class SpiceFirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp31Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-31");

  private static ProgramVocabulary LoadHp31Vocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-31/Program/program.vocabulary.json"));

  [TestMethod]
  [DataRow("HP-31")]
  [DataRow("HP-32")]
  [DataRow("HP-33")]
  [DataRow("HP-34")]
  [DataRow("HP-37")]
  [DataRow("HP-38")]
  [DataRow("HP-31E")]
  [DataRow("HP-34C")]
  [DataRow("31E")]
  [DataRow("T-31E")]
  public void Bootstrap_Routes_RomReadySpice_To_SpiceFirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<SpiceFirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Bootstrap_Keeps_NonSpiceModels_Off_SpiceGateway()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-19C"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-67"));
  }

  [TestMethod]
  [DataRow("HP-31")]
  [DataRow("HP-32")]
  [DataRow("HP-33")]
  [DataRow("HP-34")]
  [DataRow("HP-37")]
  [DataRow("HP-38")]
  public void Factory_Create_LoadsRomAndHandlers(string modelId)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    string modelPath = Path.Combine(engineRoot, CalcModelIds.ToEngineId(modelId), "Model.json");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    SpiceCpu cpu = SpiceCpuFactory.Create(model, engineRoot);
    Assert.IsTrue(cpu.State.ProgramCounter == 0);
    Assert.IsTrue(cpu.StepCount == 0);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
  {
    ICalcFirmwareGateway gateway = CreateHp31Gateway();
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
    ICalcFirmwareGateway gateway = CreateHp31Gateway();
    gateway.PowerOnResume();
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    ProgramVocabulary vocabulary = LoadHp31Vocabulary();
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
  public void SpiceGateway_Batch_ClassicDiagnosticsNull()
  {
    ICalcFirmwareGateway gateway = CreateHp31Gateway();
    gateway.PowerOnResume();
    Assert.IsNull(gateway.LastBatch.Classic);
  }

  [TestMethod]
  [DataRow("HP-31")]
  [DataRow("HP-32")]
  [DataRow("HP-33")]
  [DataRow("HP-34")]
  [DataRow("HP-37")]
  [DataRow("HP-38")]
  public void IsNativeSpicePilot_RomReadyModels(string modelId)
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeSpicePilot(modelId), modelId);
  }

  [TestMethod]
  public void IsNativeSpicePilot_ExcludesClassicAndWoodstock()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-65"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-25"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeSpicePilot("HP-35"));
  }
}
