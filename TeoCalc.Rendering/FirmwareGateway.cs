using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Rendering;

public sealed class FirmwareGateway : ICalcFirmwareGateway
{
  private const int KeyRunSteps = 200;
  private const int IoStepInterval = 50;
  private const float RunTickSeconds = 0.01f;

  private float _runAccumulator;
  private int _ioStepsUntilNext;
  private string _displayText = string.Empty;
  private bool _displayBlankPulse;
  private bool _keyLineHeld;
  private long _displayRevision;
  private FirmwareKeyCommand? _activeKey;

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged;

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed;

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged;

  public event EventHandler<FirmwareBatchCompletedEventArgs>? BatchCompleted;

  public ClassicCpu? Cpu { get; private set; }

  public bool PowerOn { get; set; }

  public bool ProgramMode { get; private set; }

  public string DisplayText => _displayText;

  public FirmwareDisplaySnapshot DisplaySnapshot { get; private set; } =
    new(string.Empty, Visible: false, BlankPulse: false, Revision: 0, StepCount: 0, ProgramCounter: 0);

  public FirmwareBatchSnapshot LastBatch { get; private set; } =
    new(
      StepCount: 0,
      ProgramCounter: 0,
      Status: 0,
      KeyBuffer: 0,
      LastHandlerId: null,
      KeyLineHeld: false,
      ActiveKey: null,
      Display: null,
      Rom: 0,
      Grp: 0,
      P: 0,
      Flags: ClassicCpuFlags.None,
      BranchOffset: 0,
      KeyInputState: ClassicKeyInputState.Idle,
      KeyAvailable: false,
      KeysToRomAddressCount: 0,
      BufferToRomAddressCount: 0);

  public FirmwareKeyCommand? ActiveKey => _activeKey;

  public bool KeyLineHeld => _keyLineHeld;

  public void AttachCpu(ClassicCpu? cpu)
  {
    Cpu = cpu;
    PowerOn = false;
    _runAccumulator = 0f;
    _ioStepsUntilNext = 0;
    _activeKey = null;
    _keyLineHeld = false;
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public void PowerOnResume()
  {
    PowerOn = true;
    RunInstructionBatch(KeyRunSteps);
  }

  public void PowerOff()
  {
    if (Cpu is not null)
    {
      Cpu.Reset();
    }

    PowerOn = false;
    _ioStepsUntilNext = 0;
    ClearKeyLine();
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public bool IsDisplayVisible() =>
    PowerOn && !_displayBlankPulse && _displayText.Length > 0;

  public void EndDisplayFrame()
  {
    _displayBlankPulse = false;
  }

  public void SetProgramMode(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    ProgramMode = programMode;
    RunInstructionBatch(KeyRunSteps);
  }

  public void ToggleProgramMode() =>
    SetProgramMode(!ProgramMode);

  public void Tick(float deltaSeconds)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    _runAccumulator += deltaSeconds;
    while (_runAccumulator >= RunTickSeconds)
    {
      RunInstructionBatch(KeyRunSteps);
      _runAccumulator -= RunTickSeconds;
    }
  }

  public void Step()
  {
    if (Cpu is null)
    {
      return;
    }

    Cpu.Step();
    RefreshDisplayFromCpu();
  }

  public void KeyDown(FirmwareKeyCommand key)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    _activeKey = key;
    SetKeyLineHeld(true);
    Cpu.State.Flags &= ~ClassicCpuFlags.DisplayOn;
    SetDisplayState(string.Empty, blankPulse: true);
    Cpu.PressKey(key.KeyCode);
    RunInstructionBatch(KeyRunSteps);
    KeyProcessed?.Invoke(this, new FirmwareKeyProcessedEventArgs(key, _displayText, IsDisplayVisible()));
  }

  public void KeyUp(FirmwareKeyCommand? key = null)
  {
    if (key is not null && _activeKey is not null && _activeKey.Value != key.Value)
    {
      return;
    }

    ClearKeyLine();
  }

  public void SetKeyLineHeld(bool held)
  {
    if (_keyLineHeld == held)
    {
      return;
    }

    _keyLineHeld = held;
    KeyStateChanged?.Invoke(this, new FirmwareKeyStateChangedEventArgs(_activeKey, held));
  }

  private void ClearKeyLine()
  {
    FirmwareKeyCommand? key = _activeKey;
    _activeKey = null;
    if (_keyLineHeld)
    {
      _keyLineHeld = false;
      KeyStateChanged?.Invoke(this, new FirmwareKeyStateChangedEventArgs(key, Held: false));
    }
  }

  private void RunInstructionBatch(int steps)
  {
    if (Cpu is null)
    {
      return;
    }

    bool firmwareKeyHeld = _keyLineHeld;
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

      if (_keyLineHeld)
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
      _activeKey,
      DisplaySnapshot,
      Cpu.State.Rom,
      Cpu.State.Grp,
      Cpu.State.P,
      Cpu.State.Flags,
      Cpu.State.BranchOffset,
      Cpu.State.KeyInputState,
      Cpu.State.KeyAvailable,
      keysToRomAddressCount,
      bufferToRomAddressCount);
    BatchCompleted?.Invoke(this, new FirmwareBatchCompletedEventArgs(LastBatch));
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

  private void SetDisplayState(string text, bool blankPulse)
  {
    bool changed = _displayText != text || _displayBlankPulse != blankPulse;
    _displayText = text;
    _displayBlankPulse = blankPulse;
    if (changed)
    {
      DisplaySnapshot = new FirmwareDisplaySnapshot(
        text,
        IsDisplayVisible(),
        blankPulse,
        ++_displayRevision,
        Cpu?.StepCount ?? 0,
        Cpu?.State.ProgramCounter ?? 0);
      DisplayChanged?.Invoke(this, new FirmwareDisplayChangedEventArgs(DisplaySnapshot));
    }
  }
}
