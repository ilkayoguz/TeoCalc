using System.Text;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// Native Classic-family firmware life-cycle (power / key / batch / display).
/// Mirrors reference Classic timing shape without calling the emulator adapter at runtime.
/// </summary>
public sealed class ClassicFirmwareGateway : CalcFirmwareGatewayBase
{
  private const int IoStepInterval = 50;

  private int _ioStepsUntilNext;
  private bool _programMode;

  public ClassicCpu? Cpu { get; private set; }

  public override bool ProgramMode => _programMode;

  public override bool SupportsCardProgram => Cpu is not null;

  public void AttachCpu(ClassicCpu? cpu)
  {
    Cpu = cpu;
    _programMode = false;
    _ioStepsUntilNext = 0;
    ResetSessionState();
  }

  public override bool TryExportCardProgram(out byte[] programCodes, out double[] registers)
  {
    if (Cpu is null)
    {
      programCodes = [];
      registers = [];
      return false;
    }

    ClassicCardProgramIo.Export(Cpu, out programCodes, out registers);
    return true;
  }

  public override bool TryImportCardProgram(IReadOnlyList<byte> programCodes, IReadOnlyList<double> registers)
  {
    if (Cpu is null)
    {
      return false;
    }

    ClassicCardProgramIo.Import(Cpu, programCodes, registers);
    return true;
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

  public override void Step() =>
    StepInto();

  public override FirmwareDebugRegisters? TryGetDebugRegisters()
  {
    if (Cpu is null)
    {
      return null;
    }

    ClassicRegisterFile r = Cpu.State.Registers;
    return FirmwareDebugOpcodes.FromClassicStyle(r.A, r.B, r.C, r.Y, r.Z, r.T, r.M);
  }

  protected override void AppendFamilyDebugDump(StringBuilder text)
  {
    if (Cpu is null)
    {
      return;
    }

    ClassicCpuState state = Cpu.State;
    text.AppendLine(
      $"Fetch={state.FetchAddress:X4}  PC.reg={state.ProgramCounter:X4}  Flags={(byte)state.Flags:X2}  F={state.F:X2}");
    text.AppendLine(
      $"Ret0={state.ReturnStack[0]:X4}  Ret1={state.ReturnStack[1]:X4}  KeyState={state.KeyInputState}");
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
    Cpu?.State.FetchAddress ?? 0;

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
      Cpu.State.FetchAddress,
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
