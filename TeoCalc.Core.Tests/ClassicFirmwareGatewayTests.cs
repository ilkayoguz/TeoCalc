using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicFirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreatePilotGateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-65");

  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void Bootstrap_Routes_Hp65_To_ClassicFirmwareGateway()
  {
    ICalcFirmwareGateway gateway = CreatePilotGateway();
    Assert.IsInstanceOfType<ClassicFirmwareGateway>(gateway);
  }

  [TestMethod]
  [DataRow("HP-35")]
  [DataRow("HP-45")]
  [DataRow("HP-55")]
  [DataRow("HP-65")]
  [DataRow("HP-70")]
  [DataRow("HP-80")]
  [DataRow("35")]
  [DataRow("65")]
  public void Bootstrap_Routes_RomReadyClassic_To_ClassicFirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<ClassicFirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Bootstrap_Routes_Hp67_To_Teo67FirmwareGateway()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-67");
    Assert.IsInstanceOfType(gateway, typeof(Teo67FirmwareGateway));
  }

  [TestMethod]
  public void Bootstrap_Routes_Hp67BE_To_Teo67FirmwareGateway()
  {
    Assert.AreEqual("T-67", CalcModelIds.ToEngineId("HP-67BE"));
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-67BE");
    Assert.IsInstanceOfType(gateway, typeof(Teo67FirmwareGateway));
  }

  [TestMethod]
  public void Bootstrap_Routes_Hp31_To_SpiceFirmwareGateway()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-31");
    Assert.IsInstanceOfType(gateway, typeof(SpiceFirmwareGateway));
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
  {
    ICalcFirmwareGateway gateway = CreatePilotGateway();
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
    ICalcFirmwareGateway gateway = CreatePilotGateway();
    gateway.PowerOnResume();
    for (int i = 0; i < 40; i++)
    {
      gateway.Tick(0.05f);
    }

    ProgramVocabulary vocabulary = LoadVocabulary();
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode));

    gateway.KeyDown(new FirmwareKeyCommand(21, keyCode));
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
  public void ClassicGateway_Batch_PopulatesClassicDiagnostics()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CreatePilotGateway();
    gateway.PowerOnResume();
    Assert.IsNotNull(gateway.LastBatch.Classic);
  }

  [TestMethod]
  public void Hp01Gateway_Batch_ClassicDiagnosticsNull()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-01");
    Assert.IsInstanceOfType(gateway, typeof(Teo01FirmwareGateway));
    gateway.PowerOnResume();
    Assert.IsNull(gateway.LastBatch.Classic);
  }

  [TestMethod]
  [DataRow("HP-35")]
  [DataRow("HP-45")]
  [DataRow("HP-55")]
  [DataRow("HP-65")]
  [DataRow("HP-70")]
  [DataRow("HP-80")]
  public void IsNativeClassicPilot_RomReadyClassicModels(string modelId)
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeClassicPilot(modelId), modelId);
  }

  [TestMethod]
  public void IsNativeClassicPilot_ExcludesWoodstockSpiceAndHp67()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-25"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-67"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-31"));
  }
}
