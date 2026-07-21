using System.Text;
using TeoCalc.Core.Engine.Teo01;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// T-01 gateway (ACThp01 ISA). Timer batch matches reference
/// <c>HeadlessRunTimerBatch</c> / <c>timer1</c> (10ms / 100 steps).
/// </summary>
public sealed class Teo01FirmwareGateway : CalcFirmwareGatewayBase
{
  /// <summary>Reference <c>timer1.Interval</c> for T-01.</summary>
  private const float Hp01TimerTickSeconds = 0.01f;

  /// <summary>Reference <c>timer1_Tick</c> instruction budget for T-01.</summary>
  private const int Hp01TimerBatchSteps = 100;

  public Teo01Cpu? Cpu { get; private set; }

  public override bool ProgramMode => false;

  protected override int InstructionStepsPerBatch => Hp01TimerBatchSteps;

  protected override float TimerTickSeconds => Hp01TimerTickSeconds;

  public void AttachCpu(Teo01Cpu? cpu)
  {
    Cpu = cpu;
    ResetSessionState();
  }

  public override bool IsDisplayVisible() =>
    PowerOn
    && Cpu is not null
    && (Cpu.State.Flags & Teo01CpuFlags.DisplayOn) != 0
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

    RunInstructionBatch(InstructionStepsPerBatch);
  }

  public override FirmwareDebugRegisters? TryGetDebugRegisters()
  {
    if (Cpu is null)
    {
      return null;
    }

    Teo01CpuState s = Cpu.State;
    return FirmwareDebugOpcodes.FromClassicStyle(s.A, s.B, s.C, s.Y, s.Z, s.T, s.M);
  }

  protected override void AppendFamilyDebugDump(StringBuilder text)
  {
    if (Cpu is null)
    {
      return;
    }

    Teo01CpuState s = Cpu.State;
    text.AppendLine($"Flags={(byte)s.Flags:X2}  Extra={(byte)s.ExtraFlags:X2}  F={s.F:X2}  Sp={s.Sp}");
    text.AppendLine($"Stack0={s.Stack[0]:X4}  Stack1={s.Stack[1]:X4}  Running={Cpu.Running}");
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
    Cpu.State.Flags &= ~Teo01CpuFlags.DisplayOn;
  }

  protected override void OnKeyDown(FirmwareKeyCommand key) =>
    Cpu!.PressKey(key.KeyCode);

  protected override void OnKeyUp() =>
    RunInstructionBatch(InstructionStepsPerBatch);

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

    string text = Teo01FirmwareDisplay.BuildLedText(Cpu.State);
    bool blankPulse = PowerOn && (Cpu.State.Flags & Teo01CpuFlags.DisplayOn) == 0;
    SetDisplayState(text, blankPulse);
    Cpu.ClearDisplayRefresh();
  }
}
