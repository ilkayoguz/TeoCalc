using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class GtoLabelCardTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  private static string SampleT6xPath =>
    Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);

  [TestMethod]
  public void T6x_HasLbl1_AtExpectedIndex()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SampleT6xPath);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    int lbl1Index = -1;
    for (int i = 1; i < snapshot.ProgramCodes.Count - 1; i++)
    {
      if (snapshot.ProgramCodes[i] == ClassicProgramCodes.Label
          && snapshot.ProgramCodes[i + 1] == 4)
      {
        lbl1Index = i + 1;
        break;
      }
    }

    Assert.IsTrue(lbl1Index > 0, "LBL 1 not found in t65 program bytes");
  }

  [TestMethod]
  public void RunMode_GtoThen1_ThenRunStop_ExecutesLbl1Routine()
  {
    ClassicFirmwareGateway gateway = LoadT6xInRunMode();
    Press(gateway, 6, KeyCode('o'));
    Press(gateway, 31, KeyCode('1'));
    Press(gateway, 38, KeyCode(' '));
    Tick(gateway, 160);

    Assert.IsTrue(
      gateway.DisplayText.Contains('5', StringComparison.Ordinal)
        || gateway.DisplayText.Contains("15", StringComparison.Ordinal),
      $"Expected 4+5+6 result after GTO 1 R/S, got '{gateway.DisplayText}'");
  }

  [TestMethod]
  public void Session_GtoThen1_ThenRunStop_ExecutesLbl1Routine()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Tick(session, 40);
    Assert.IsTrue(session.TryLoadCardProgram(SampleT6xPath, out string? error), error);
    session.ToggleProgramModeTo(false);
    Tick(session, 40);

    TapKey(session, 6, KeyCode('o'));
    TapKey(session, 31, KeyCode('1'));
    TapKey(session, 38, KeyCode(' '));
    Tick(session, 160);

    Assert.IsTrue(
      session.DisplayText.Contains('5', StringComparison.Ordinal)
        || session.DisplayText.Contains("15", StringComparison.Ordinal),
      $"Expected 4+5+6 after GTO 1 R/S, got '{session.DisplayText}' programMode={session.ProgramMode}");
  }

  private static ClassicFirmwareGateway LoadT6xInRunMode()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SampleT6xPath);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    ClassicFirmwareGateway gateway = (ClassicFirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    gateway.PowerOnResume();
    Tick(gateway, 40);
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
    Tick(session, 40);
  }

  private static byte KeyCode(char c)
  {
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(Vocabulary, c, out byte keyCode), c.ToString());
    return keyCode;
  }
}
