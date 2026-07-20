using TeoCalc.Core.Engine.Hp01;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// HP-01 gateway (ACThp01 ISA). Life-cycle matches <see cref="CalcFirmwareGatewayBase"/>
/// (0.05s / 200 steps) with Panamatik sleep/time side effects.
/// </summary>
public sealed class Hp01FirmwareGateway : CalcFirmwareGatewayBase
{
  public Hp01Cpu? Cpu { get; private set; }

  public override bool ProgramMode => false;

  public void AttachCpu(Hp01Cpu? cpu)
  {
    Cpu = cpu;
    ResetSessionState();
  }

  public override bool IsDisplayVisible() =>
    PowerOn
    && Cpu is not null
    && (Cpu.State.Flags & Hp01CpuFlags.DisplayOn) != 0
    && DisplayText.Length > 0;

  public override void SetProgramMode(bool programMode)
  {
    _ = programMode;
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
    Cpu.State.Flags &= ~Hp01CpuFlags.DisplayOn;
  }

  protected override void OnKeyDown(FirmwareKeyCommand key) =>
    Cpu!.PressKey(key.KeyCode);

  protected override void OnKeyUp() =>
    RunInstructionBatch(KeyRunSteps);

  protected override void RunInstructionBatch(int steps)
  {
    if (Cpu is null)
    {
      return;
    }

    bool firmwareKeyHeld = KeyLineHeld;
    string? lastHandlerId = null;
    Cpu.ServicePeripherals();

    if (!Cpu.Running)
    {
      RefreshDisplayFromCpu();
      LastBatch = BuildBatchSnapshot(lastHandlerId, firmwareKeyHeld);
      RaiseBatchCompleted();
      return;
    }

    for (int step = 0; step < steps; step++)
    {
      lastHandlerId = Cpu.Step().HandlerId;
      if (!Cpu.Running)
      {
        break;
      }
    }

    RefreshDisplayFromCpu();
    LastBatch = BuildBatchSnapshot(lastHandlerId, firmwareKeyHeld);
    RaiseBatchCompleted();
  }

  private FirmwareBatchSnapshot BuildBatchSnapshot(string? lastHandlerId, bool firmwareKeyHeld) =>
    new(
      Cpu!.StepCount,
      Cpu.State.ProgramCounter,
      Cpu.State.Status,
      Cpu.State.KeyBuffer,
      lastHandlerId,
      firmwareKeyHeld,
      ActiveKey,
      DisplaySnapshot,
      Rom: Cpu.State.Rom,
      Grp: 0,
      P: Cpu.State.P,
      Classic: null);

  private void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      SetDisplayState(string.Empty, blankPulse: false);
      return;
    }

    string text = Hp01FirmwareDisplay.BuildLedText(Cpu.State);
    bool blankPulse = PowerOn && (Cpu.State.Flags & Hp01CpuFlags.DisplayOn) == 0;
    SetDisplayState(text, blankPulse);
    Cpu.ClearDisplayRefresh();
  }
}
