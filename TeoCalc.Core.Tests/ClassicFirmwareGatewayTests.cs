using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicFirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreatePilotGateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-65");

  private static ProgramVocabulary LoadVocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void Bootstrap_Routes_Hp65_To_ClassicFirmwareGateway()
  {
    ICalcFirmwareGateway gateway = CreatePilotGateway();
    Assert.IsInstanceOfType<ClassicFirmwareGateway>(gateway);
  }

  [TestMethod]
  public void Bootstrap_Routes_OtherModel_To_EmulatorAdapter()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-25");
    Assert.IsInstanceOfType(gateway, typeof(EmulatorFirmwareGateway));
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_WithoutPanamatikTypes()
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
  public void IsNativeClassicPilot_Only_Hp65()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-65"));
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeClassicPilot("65"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-35"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-25"));
  }
}
