using TeoCalc.Core.Engine.Hp67;

namespace TeoCalc.Core.Firmware;

/// <summary>HP-67 gateway (ACT). Timer batch matches Panamatik — key line only, no S3/S5 pulses.</summary>
public sealed class Hp67FirmwareGateway : ActFirmwareGatewayBase<Hp67Cpu>
{
  protected override void ApplyBatchStatusPulse()
  {
    if (KeyLineHeld && Cpu is not null)
    {
      Cpu.State.Status |= 0x8000;
    }
  }
}
