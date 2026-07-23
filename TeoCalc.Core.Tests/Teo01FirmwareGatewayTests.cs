using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo01;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Teo01FirmwareGatewayTests
{
  private static ICalcFirmwareGateway CreateHp01Gateway() =>
    CalcFirmwareGatewayLocator.CreateGateway("HP-01");

  private static ProgramVocabulary LoadHp01Vocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-01/Program/program.vocabulary.json"));

  private static void Soak(ICalcFirmwareGateway gateway, int ticks, float deltaSeconds = 0.01f)
  {
    for (int i = 0; i < ticks; i++)
    {
      gateway.Tick(deltaSeconds);
    }
  }

  [TestMethod]
  [DataRow("HP-01")]
  [DataRow("01")]
  [DataRow("T-01")]
  public void Bootstrap_Routes_To_Teo01FirmwareGateway(string modelId)
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(modelId);
    Assert.IsInstanceOfType<Teo01FirmwareGateway>(gateway, modelId);
  }

  [TestMethod]
  public void Factory_Create_LoadsRomAndHandlers()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-01", "Model.json"));
    Assert.AreEqual(2304, model.Hardware.RomWordCount);
    Assert.AreEqual("Teo01", model.Family);

    Teo01Cpu cpu = Teo01CpuFactory.Create(model, engineRoot);
    Assert.AreEqual(0, cpu.State.ProgramCounter);
    MicrocodeHandlerEntry handler = cpu.Step();
    Assert.IsFalse(string.IsNullOrWhiteSpace(handler.HandlerId));
    StringAssert.StartsWith(handler.HandlerId, "Teo01Cpu.");
  }

  [TestMethod]
  public void PowerOnResume_ShowsIdleDisplay_NativeGateway()
  {
    ICalcFirmwareGateway gateway = CreateHp01Gateway();
    Assert.IsInstanceOfType(gateway, typeof(Teo01FirmwareGateway));
    gateway.PowerOnResume();
    // DisplayCnt≈200 at 10ms; keep soak under the ~2s blank timeout.
    Soak(gateway, ticks: 80);

    Assert.IsTrue(gateway.PowerOn);
    Assert.IsTrue(((Teo01FirmwareGateway)gateway).Cpu!.StepCount > 0);
    Assert.IsTrue(gateway.IsDisplayVisible(), $"display='{gateway.DisplayText}' flags={((Teo01FirmwareGateway)gateway).Cpu!.State.Flags}");
    Assert.IsTrue(gateway.DisplayText.Length > 0);
    // Time idle typically includes digit and/or colon separators.
    Assert.IsTrue(
      gateway.DisplayText.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':']) >= 0,
      gateway.DisplayText);
  }

  [TestMethod]
  public void KeyDown_Digit_UpdatesDisplayText()
  {
    ICalcFirmwareGateway gateway = CreateHp01Gateway();
    gateway.PowerOnResume();
    Soak(gateway, ticks: 80);

    ProgramVocabulary vocabulary = LoadHp01Vocabulary();
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode));
    Assert.AreEqual(4, keyCode);

    gateway.KeyDown(new FirmwareKeyCommand(10, keyCode));
    Soak(gateway, ticks: 20);

    gateway.KeyUp();
    Soak(gateway, ticks: 20);

    Assert.IsTrue(gateway.IsDisplayVisible(), gateway.DisplayText);
    StringAssert.Contains(gateway.DisplayText, "7");
  }

  [TestMethod]
  public void Tick_Uses_10ms_Timer_For_DisplayHold()
  {
    // Reference timer1 is 10ms; DisplayCnt after a digit key is 200 ticks (~2s).
    // Wrong 50ms cadence would still show the digit after only 2s of Tick budget.
    ICalcFirmwareGateway gateway = CreateHp01Gateway();
    gateway.PowerOnResume();
    Soak(gateway, ticks: 80);

    ProgramVocabulary vocabulary = LoadHp01Vocabulary();
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode));

    gateway.KeyDown(new FirmwareKeyCommand(10, keyCode));
    gateway.KeyUp();
    Assert.IsTrue(gateway.IsDisplayVisible(), gateway.DisplayText);
    StringAssert.Contains(gateway.DisplayText, "7");

    Soak(gateway, ticks: 210);
    Assert.IsFalse(
      gateway.IsDisplayVisible(),
      $"display should blank after ~2s at 10ms cadence, got '{gateway.DisplayText}'");
  }

  [TestMethod]
  public void IsNativeTeo01Pilot_RomReady()
  {
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeTeo01Pilot("HP-01"));
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeTeo01Pilot("01"));
  }

  [TestMethod]
  public void IsNativeTeo01Pilot_ExcludesOthers()
  {
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo01Pilot("HP-67"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo01Pilot("HP-19C"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo01Pilot("HP-25"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeTeo67Pilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeWoodstockPilot("HP-01"));
    Assert.IsFalse(CalcFirmwareBootstrap.IsNativeClassicPilot("HP-01"));
  }

  [TestMethod]
  public void Teo01Cpu_Is_CpuBase_Not_ActCpuBase()
  {
    Assert.IsTrue(typeof(CpuBase).IsAssignableFrom(typeof(Teo01Cpu)));
    Assert.IsTrue(typeof(ICpu).IsAssignableFrom(typeof(Teo01Cpu)));
    Assert.IsFalse(typeof(TeoCalc.Core.Engine.Act.ActCpuBase).IsAssignableFrom(typeof(Teo01Cpu)));
  }

  [TestMethod]
  public void RomWordCount_Is_2304()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "T-01", "Model.json"));
    Assert.AreEqual(2304, model.Hardware.RomWordCount);
    string romPath = Path.Combine(engineRoot, "T-01", model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    MicrocodeRom rom = MicrocodeRom.LoadBinary(romPath);
    Assert.AreEqual(2304, rom.WordCount);
  }
}
