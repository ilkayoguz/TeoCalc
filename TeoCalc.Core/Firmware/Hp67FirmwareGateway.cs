using TeoCalc.Core.Engine.Hp67;

namespace TeoCalc.Core.Firmware;

/// <summary>T-67 / ACT gateway. Timer batch matches reference emulator — key line only, no S3/S5 pulses.</summary>
public sealed class Hp67FirmwareGateway : ActFirmwareGatewayBase<Hp67Cpu>
{
  public override bool SupportsCardProgram => Cpu is not null;

  protected override void ApplyBatchStatusPulse()
  {
    if (KeyLineHeld && Cpu is not null)
    {
      Cpu.State.Status |= 0x8000;
    }
  }

  public override bool TryExportCardProgram(out byte[] programCodes, out double[] registers)
  {
    if (Cpu is null)
    {
      programCodes = [];
      registers = [];
      return false;
    }

    Hp67CardProgramIo.Export(Cpu.State, out programCodes, out registers);
    return true;
  }

  public override bool TryImportCardProgram(IReadOnlyList<byte> programCodes, IReadOnlyList<double> registers)
  {
    if (Cpu is null)
    {
      return false;
    }

    Hp67CardProgramIo.Import(Cpu.State, programCodes, registers);
    return true;
  }

  public bool TryExportCardMode(out Hp67CardMode mode)
  {
    if (Cpu is null)
    {
      mode = new Hp67CardMode("DEG", "FIX", 2, 0, 0);
      return false;
    }

    mode = Hp67CardProgramIo.ExportMode(Cpu.State);
    return true;
  }

  public bool TryImportCardMode(Hp67CardMode mode)
  {
    if (Cpu is null)
    {
      return false;
    }

    Hp67CardProgramIo.ImportMode(Cpu.State, mode);
    return true;
  }
}
