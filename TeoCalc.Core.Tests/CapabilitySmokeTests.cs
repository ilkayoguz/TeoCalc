using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Hp01;
using TeoCalc.Core.Engine.Hp19;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CapabilitySmokeTests
{
  [TestMethod]
  public void Hp19_OnPikPrint_AppendsToPrinterBuffer()
  {
    Hp19Cpu cpu = CreateHp19Cpu();
    cpu.State.Registers.C[0] = 4;
    cpu.State.Registers.C[1] = 5;
    cpu.State.Registers.C[2] = 6;
    cpu.State.Registers.C[3] = 15;

    cpu.PrintAndFlushCRegister(alpha: false);

    Assert.AreEqual(1, cpu.PrintLines.Count);
    Assert.AreEqual("654", cpu.PrintLines[0]);
  }

  [TestMethod]
  public void Hp19_Gateway_ExposesPikPrintBuffer()
  {
    Hp19FirmwareGateway gateway = (Hp19FirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-19C");
    gateway.AppendTestPrint("Printer ready.");
    Assert.AreEqual(1, gateway.PrintLines.Count);

    Hp19Cpu cpu = gateway.Cpu!;
    cpu.State.Registers.C[0] = 1;
    cpu.State.Registers.C[1] = 2;
    cpu.State.Registers.C[2] = 3;
    cpu.State.Registers.C[3] = 15;
    cpu.PrintAndFlushCRegister(alpha: false);

    Assert.AreEqual(2, gateway.PrintLines.Count);
    Assert.AreEqual("321", gateway.PrintLines[1]);
  }

  [TestMethod]
  public void Hp01_Stopwatch_StartStop_UpdatesFlags()
  {
    RecordingToneSink tones = new();
    Hp01Cpu cpu = CreateHp01Cpu(tones);
    Assert.AreEqual(Hp01ExtraFlags.None, cpu.State.ExtraFlags & Hp01ExtraFlags.SwStarted);

    cpu.InvokeOpcodeAliasForTests("op_swstrt");
    Assert.AreNotEqual(Hp01ExtraFlags.None, cpu.State.ExtraFlags & Hp01ExtraFlags.SwStarted);

    for (int i = 0; i < 5; i++)
    {
      cpu.ServicePeripherals();
    }

    cpu.InvokeOpcodeAliasForTests("op_swstop");
    Assert.AreEqual(Hp01ExtraFlags.None, cpu.State.ExtraFlags & Hp01ExtraFlags.SwStarted);
    Assert.AreEqual(0, tones.BeepCount);
    Assert.AreEqual(0, tones.AlarmCount);
  }

  [TestMethod]
  public void Hp01_AlarmMatch_SetsBlinkAndInvokesToneSink()
  {
    RecordingToneSink tones = new();
    Hp01Cpu cpu = CreateHp01Cpu(tones);
    // Hp01Clinc increments Cl before comparing to Al — prime Al one tick ahead.
    for (int i = 0; i < 6; i++)
    {
      cpu.State.Cl[i] = 0;
      cpu.State.Al[i] = 0;
    }

    cpu.State.Al[0] = 1;
    cpu.State.ExtraFlags |= Hp01ExtraFlags.AlarmActive;
    cpu.State.TickCnt = 0x1F;
    typeof(Hp01Cpu)
      .GetField("_second", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
      .SetValue(cpu, (byte)255);

    cpu.ServicePeripherals();

    Assert.AreEqual(Hp01ExtraFlags.None, cpu.State.ExtraFlags & Hp01ExtraFlags.AlarmActive);
    Assert.AreNotEqual(Hp01ExtraFlags.None, cpu.State.ExtraFlags & Hp01ExtraFlags.Blink);
    Assert.AreEqual(1, tones.AlarmCount);
  }

  [TestMethod]
  public void EmulatorGateway_Hp01Fallback_Uses10msCadence()
  {
    // When native assets are present CreateGateway returns Hp01FirmwareGateway;
    // construct the emulator path directly to lock the fallback timer contract.
    IPanamatikEngine engine = PanamatikEngineFactory.Create("HP-01");
    using EmulatorFirmwareGateway gateway = new(engine, runTickSeconds: 0.01f, stepsPerBatch: 100);
    Assert.AreEqual(0.01f, gateway.RunTickSeconds);
    Assert.AreEqual(100, gateway.StepsPerBatch);
  }

  private static Hp19Cpu CreateHp19Cpu()
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-19C", "Model.json"));
    return Hp19CpuFactory.Create(model, engineRoot);
  }

  private static Hp01Cpu CreateHp01Cpu(IHp01ToneSink tones)
  {
    string engineRoot = TeoCalcPaths.ResourcePath("Engine");
    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(Path.Combine(engineRoot, "HP-01", "Model.json"));
    string modelDir = Path.Combine(engineRoot, model.Model);
    MicrocodeRom rom = MicrocodeRom.LoadBinary(
      Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar)));
    MicrocodeHandlerCatalog handlers = MicrocodeHandlerCatalog.Load(
      Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar)));
    Hp01Cpu cpu = new(rom, handlers, tones);
    cpu.Reset();
    return cpu;
  }

  private sealed class RecordingToneSink : IHp01ToneSink
  {
    public int BeepCount { get; private set; }

    public int AlarmCount { get; private set; }

    public void Beep() => BeepCount++;

    public void Alarm() => AlarmCount++;
  }
}
