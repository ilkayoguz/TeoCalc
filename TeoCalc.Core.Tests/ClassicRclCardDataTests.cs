using System.Reflection;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicRclCardDataTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void CardImport_LoadsDataSlots()
  {
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      T6xCardFormat.ReadFile(TeoCalcPaths.ResourcePath("Samples/Cards/teo-65-add123.t65")),
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    Assert.IsTrue(gateway.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers));

    Assert.AreEqual(0, ClassicDataRegisterCodec.GetRegisterValue(gateway.Cpu!.State.Ram, 0), 1e-6);
    Assert.AreEqual(3.14, ClassicDataRegisterCodec.GetRegisterValue(gateway.Cpu.State.Ram, 1), 1e-6);
    Assert.AreEqual(2.718281828, ClassicDataRegisterCodec.GetRegisterValue(gateway.Cpu.State.Ram, 2), 1e-6);
  }

  [TestMethod]
  public void RunMode_RclThen1_RecallsDataSlotIndex1()
  {
    ClassicFirmwareGateway gateway = LoadSampleInRunMode();
    Press(gateway, 13, KeyCode('r'));
    Press(gateway, 31, KeyCode('1'));
    Tick(gateway, 80);

    Assert.IsTrue(gateway.IsDisplayVisible());
    Assert.IsTrue(
      gateway.DisplayText.Contains('3', StringComparison.Ordinal),
      $"RCL 1 should recall π (DATA slot 1), got '{gateway.DisplayText}'");
  }

  [TestMethod]
  public void ProgramMode_RclThen1_ShowsProgramStep_NotRecalledValue()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    Tick(gateway, 40);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      T6xCardFormat.ReadFile(TeoCalcPaths.ResourcePath("Samples/Cards/teo-65-add123.t65")),
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
    Assert.IsTrue(gateway.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers));
    gateway.SetProgramMode(true);
    Tick(gateway, 40);

    Press(gateway, 13, KeyCode('r'));
    Press(gateway, 31, KeyCode('1'));
    Tick(gateway, 80);

    Assert.IsFalse(gateway.DisplayText.Contains('2', StringComparison.Ordinal), $"got '{gateway.DisplayText}'");
  }

  private static ClassicFirmwareGateway LoadSampleInRunMode()
  {
    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    Tick(gateway, 40);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      T6xCardFormat.ReadFile(TeoCalcPaths.ResourcePath("Samples/Cards/teo-65-add123.t65")),
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
    Assert.IsTrue(gateway.TryImportCardProgram(snapshot.ProgramCodes, snapshot.Registers));
    gateway.SetProgramMode(false);
    Tick(gateway, 40);
    return gateway;
  }

  private static void Tick(ICalcFirmwareGateway gateway, int batches)
  {
    for (int i = 0; i < batches; i++)
    {
      gateway.Tick(0.05f);
    }
  }

  private static void Press(ICalcFirmwareGateway gateway, int keyChartIndex, byte keyCode)
  {
    gateway.KeyDown(new FirmwareKeyCommand(keyChartIndex, keyCode));
    Tick(gateway, 40);
    gateway.KeyUp();
    Tick(gateway, 40);
  }

  private static byte KeyCode(char c)
  {
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(Vocabulary, c, out byte keyCode), c.ToString());
    return keyCode;
  }

  [TestMethod]
  public void Session_MouseTapRclThen1_RecallsPi()
  {
    string path = TeoCalcPaths.ResourcePath("Samples/Cards/teo-65-add123.t65");
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Tick(session, 40);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    GetFirmware(session).SetProgramMode(false);
    Tick(session, 40);

    TapKey(session, 13, KeyCode('r'));
    TapKey(session, 31, KeyCode('1'));

    Assert.IsTrue(
      session.DisplayText.Contains('3', StringComparison.Ordinal),
      $"Expected π after RCL 1, got '{session.DisplayText}' programMode={session.ProgramMode}");
  }

  [TestMethod]
  public void Session_GatewayPressAfterLoad_RecallsPi()
  {
    string path = TeoCalcPaths.ResourcePath("Samples/Cards/teo-65-add123.t65");
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Tick(session, 40);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    GetFirmware(session).SetProgramMode(false);
    Tick(session, 40);

    ClassicFirmwareGateway gateway = GetFirmware(session);
    Press(gateway, 13, KeyCode('r'));
    Press(gateway, 31, KeyCode('1'));

    Assert.IsTrue(
      gateway.DisplayText.Contains('3', StringComparison.Ordinal),
      $"Expected π, got '{gateway.DisplayText}'");
  }

  private static ClassicFirmwareGateway GetFirmware(CalcExplorerSession session)
  {
    FieldInfo? field = typeof(CalcExplorerSession).GetField("_firmware", BindingFlags.Instance | BindingFlags.NonPublic);
    Assert.IsNotNull(field);
    return (ClassicFirmwareGateway)field.GetValue(session)!;
  }

  private static void Tick(CalcExplorerSession session, int batches)
  {
    for (int i = 0; i < batches; i++)
    {
      session.Tick(0.05f);
    }
  }

  private static void TapKey(CalcExplorerSession session, int keyChartIndex, byte keyCode)
  {
    session.PressKey(keyChartIndex, keyCode);
    session.ReleaseMouseKey();
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }
  }
}
