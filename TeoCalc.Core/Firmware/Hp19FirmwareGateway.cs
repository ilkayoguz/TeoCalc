using TeoCalc.Core.Engine.Hp19;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// HP-19C gateway (ACT variant). Status pulses match Panamatik <c>timer1_Tick</c>
/// (before each instruction; battery + run-mode bits).
/// </summary>
public sealed class Hp19FirmwareGateway : ActFirmwareGatewayBase<Hp19Cpu>
{
  protected override bool PulseStatusBeforeStep => true;

  protected override void ApplyBatchStatusPulse()
  {
    if (Cpu is null)
    {
      return;
    }

    // batteryok
    Cpu.State.Status |= 32;
    // Default power-on: act_switch==4 → pulse S3 when status bit0 clear, or when not in PRGM
    if ((Cpu.State.Status & 1) == 0 || !ProgramMode)
    {
      Cpu.State.Status |= 8;
    }

    if (KeyLineHeld)
    {
      Cpu.State.Status |= 0x8000;
    }
  }
}
