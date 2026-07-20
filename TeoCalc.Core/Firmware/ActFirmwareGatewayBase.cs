using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Firmware;

/// <summary>Shared ACT (Woodstock/Spice) gateway life-cycle on top of <see cref="CalcFirmwareGatewayBase"/>.</summary>
public abstract class ActFirmwareGatewayBase<TCpu> : CalcFirmwareGatewayBase
  where TCpu : ActCpuBase
{
  public TCpu? Cpu { get; private set; }

  public override bool ProgramMode
  {
    get => Cpu?.ProgramMode ?? false;
  }

  public void AttachCpu(TCpu? cpu)
  {
    Cpu = cpu;
    ResetSessionState();
  }

  public override bool IsDisplayVisible() =>
    PowerOn
    && Cpu is not null
    && (Cpu.State.Flags & ActCpuFlags.DisplayOn) != 0
    && DisplayText.Length > 0;

  public override void SetProgramMode(bool programMode)
  {
    if (Cpu is null || !PowerOn || ProgramMode == programMode)
    {
      return;
    }

    Cpu.ProgramMode = programMode;
    RunInstructionBatch(KeyRunSteps);
  }

  public override void Step()
  {
    if (!CanRunBatch())
    {
      return;
    }

    RunInstructionBatch(KeyRunSteps);
  }

  protected override bool CanRunBatch() =>
    Cpu is not null && PowerOn;

  protected override int CurrentStepCount =>
    Cpu?.StepCount ?? 0;

  protected override int CurrentProgramCounter =>
    Cpu?.State.ProgramCounter ?? 0;

  protected override void OnPowerOffCpu()
  {
    if (Cpu is null)
    {
      return;
    }

    Cpu.Reset();
    Cpu.State.Flags &= ~ActCpuFlags.DisplayOn;
  }

  protected override void OnKeyDown(FirmwareKeyCommand key) =>
    Cpu!.PressKey(key.KeyCode);

  protected override void OnKeyUp() =>
    // Panamatik HeadlessReleaseKey only clears the held line; it does not call act_release_key.
    RunInstructionBatch(KeyRunSteps);

  protected override void RunInstructionBatch(int steps)
  {
    if (Cpu is null)
    {
      return;
    }

    bool firmwareKeyHeld = KeyLineHeld;
    string? lastHandlerId = null;
    for (int step = 0; step < steps; step++)
    {
      if (PulseStatusBeforeStep)
      {
        ApplyBatchStatusPulse();
      }

      lastHandlerId = Cpu.Step().HandlerId;

      if (!PulseStatusBeforeStep)
      {
        ApplyBatchStatusPulse();
      }
    }

    RefreshDisplayFromCpu();
    LastBatch = new FirmwareBatchSnapshot(
      Cpu.StepCount,
      Cpu.State.ProgramCounter,
      Cpu.State.Status,
      Cpu.State.KeyBuffer,
      lastHandlerId,
      firmwareKeyHeld,
      ActiveKey,
      DisplaySnapshot,
      Rom: Cpu.State.DelRom,
      Grp: (byte)((Cpu.State.Flags & ActCpuFlags.Bank) != 0 ? 1 : 0),
      P: Cpu.State.P,
      Classic: null);
    RaiseBatchCompleted();
  }

  /// <summary>When true, status pulses run before each Step (HP-19C timer1_Tick order).</summary>
  protected virtual bool PulseStatusBeforeStep => false;

  /// <summary>
  /// Panamatik HeadlessRunTimerBatch status pulse (Woodstock/Spice).
  /// HP-67 omits the continuous S3/S5 pulses — override accordingly.
  /// </summary>
  protected virtual void ApplyBatchStatusPulse()
  {
    Cpu!.State.Status |= 32;
    if (!ProgramMode)
    {
      Cpu.State.Status |= 8;
    }

    if (KeyLineHeld)
    {
      Cpu.State.Status |= 0x8000;
    }
  }

  protected void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      SetDisplayState(string.Empty, blankPulse: false);
      return;
    }

    string text = ActFirmwareDisplay.BuildLedText(Cpu.State);
    bool blankPulse = PowerOn && (Cpu.State.Flags & ActCpuFlags.DisplayOn) == 0;
    SetDisplayState(text, blankPulse);
  }
}
