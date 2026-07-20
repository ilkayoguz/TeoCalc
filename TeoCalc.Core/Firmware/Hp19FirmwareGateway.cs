using TeoCalc.Core.Engine.Hp19;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// HP-19C gateway (ACT variant). Status pulses match Panamatik <c>timer1_Tick</c>
/// (before each instruction; battery + run-mode bits; skip S3 after PIK ops).
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

    // Panamatik: skip S3 pulse when previous op set pikinstruction.
    if (!Cpu.SuppressNextStatusPulse
        && ((Cpu.State.Status & 1) == 0 || !ProgramMode))
    {
      Cpu.State.Status |= 8;
    }

    Cpu.SuppressNextStatusPulse = false;

    if (KeyLineHeld)
    {
      Cpu.State.Status |= 0x8000;
    }
  }

  protected override void OnKeyDown(FirmwareKeyCommand key)
  {
    base.OnKeyDown(key);
    Cpu?.NotifyButtonPressed();
  }
}
