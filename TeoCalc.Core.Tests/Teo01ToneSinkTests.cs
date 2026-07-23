using TeoCalc.Core.Engine.Teo01;
using TeoCalc.Core.Firmware;
using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class Teo01ToneSinkTests
{
  [TestMethod]
  public void Bootstrap_PassesConfiguredToneSink_ToTeo01GatewayCpu()
  {
    RecordingToneSink tones = new();
    ITeo01ToneSink previous = CalcFirmwareBootstrap.Teo01ToneSink;
    try
    {
      CalcFirmwareBootstrap.Teo01ToneSink = tones;
      ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-01");
      Teo01FirmwareGateway teo01 = (Teo01FirmwareGateway)gateway;
      Assert.IsNotNull(teo01.Cpu);

      teo01.PowerOnResume();
      for (int i = 0; i < 6; i++)
      {
        teo01.Cpu.State.Cl[i] = 0;
        teo01.Cpu.State.Al[i] = 0;
      }

      teo01.Cpu.State.Al[0] = 1;
      teo01.Cpu.State.ExtraFlags |= Teo01ExtraFlags.AlarmActive;
      teo01.Cpu.State.TickCnt = 0x1F;
      typeof(Teo01Cpu)
        .GetField("_second", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .SetValue(teo01.Cpu, (byte)255);

      teo01.Tick(0.01f);

      Assert.AreEqual(1, tones.AlarmCount);
    }
    finally
    {
      CalcFirmwareBootstrap.Teo01ToneSink = previous;
    }
  }

  private sealed class RecordingToneSink : ITeo01ToneSink
  {
    public int BeepCount { get; private set; }

    public int AlarmCount { get; private set; }

    public void Beep() => BeepCount++;

    public void Alarm() => AlarmCount++;
  }
}
