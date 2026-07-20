using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// Native Classic-family firmware life-cycle (power / key / batch / display).
/// Mirrors Panamatik Classic timing shape without calling Panamatik at runtime.
/// </summary>
public sealed class ClassicFirmwareGateway : CalcFirmwareGatewayBase
{
  private const int IoStepInterval = 50;

  private int _ioStepsUntilNext;
  private bool _programMode;

  public ClassicCpu? Cpu { get; private set; }

  public override bool ProgramMode => _programMode;

  public void AttachCpu(ClassicCpu? cpu)
  {
    Cpu = cpu;
    _programMode = false;
    _ioStepsUntilNext = 0;
    ResetSessionState();
  }

  public override bool IsDisplayVisible() =>
    PowerOn && !DisplayBlankPulse && DisplayText.Length > 0;

  public override void SetProgramMode(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    _programMode = programMode;
    RunInstructionBatch(KeyRunSteps);
  }

  public override void Step()
  {
    if (Cpu is null)
    {
      return;
    }

    Cpu.Step();
    RefreshDisplayFromCpu();
  }

  public override void PowerOff()
  {
    if (Cpu is not null)
    {
      Cpu.Reset();
    }

    _ioStepsUntilNext = 0;
    base.PowerOff();
  }

  protected override bool CanRunBatch() =>
    Cpu is not null && PowerOn;

  protected override int CurrentStepCount =>
    Cpu?.StepCount ?? 0;

  protected override int CurrentProgramCounter =>
    Cpu?.State.ProgramCounter ?? 0;

  protected override void OnPowerOffCpu()
  {
    // Classic PowerOff handled in override above (Reset + io clear).
  }

  protected override void OnKeyDown(FirmwareKeyCommand key)
  {
    Cpu!.State.Flags &= ~ClassicCpuFlags.DisplayOn;
    SetDisplayState(string.Empty, blankPulse: true);
    Cpu.PressKey(key.KeyCode);
  }

  protected override void OnKeyUp()
  {
    // Classic HeadlessReleaseKey: clear key line only (no batch).
  }

  protected override void RunInstructionBatch(int steps)
  {
    if (Cpu is null)
    {
      return;
    }

    bool firmwareKeyHeld = KeyLineHeld;
    string? lastHandlerId = null;
    int keysToRomAddressCount = 0;
    int bufferToRomAddressCount = 0;
    for (int step = 0; step < steps; step++)
    {
      lastHandlerId = Cpu.Step().HandlerId;
      _ioStepsUntilNext--;
      if (_ioStepsUntilNext <= 0)
      {
        Cpu.State.HandleIo();
        _ioStepsUntilNext = IoStepInterval;
      }

      Cpu.State.ApplyKeyInput();

      if (KeyLineHeld)
      {
        Cpu.State.Status |= 1;
      }

      if (lastHandlerId == "ClassicCpu.KeysToRomAddress")
      {
        keysToRomAddressCount++;
      }
      else if (lastHandlerId == "ClassicCpu.BufferToRomAddress")
      {
        bufferToRomAddressCount++;
      }

      if (ProgramMode)
      {
        Cpu.State.Status |= 8;
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
      Cpu.State.Rom,
      Cpu.State.Grp,
      Cpu.State.P,
      new ClassicFirmwareDiagnostics(
        Cpu.State.Flags,
        Cpu.State.KeyInputState,
        Cpu.State.BranchOffset,
        keysToRomAddressCount,
        bufferToRomAddressCount,
        Cpu.State.KeyAvailable));
    RaiseBatchCompleted();
  }

  private void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      SetDisplayState(string.Empty, blankPulse: false);
      return;
    }

    string text = ClassicFirmwareDisplay.BuildLedText(
      Cpu.State,
      ProgramMode,
      Cpu.Program.EndState);

    SetDisplayState(text, blankPulse: false);
  }
}
