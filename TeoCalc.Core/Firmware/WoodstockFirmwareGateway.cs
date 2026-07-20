using TeoCalc.Core.Engine.Woodstock;

namespace TeoCalc.Core.Firmware;

/// <summary>
/// Native Woodstock-family firmware life-cycle (power / key / batch / display).
/// Mirrors Panamatik HP25 Headless* timing without calling Panamatik at runtime.
/// </summary>
public sealed class WoodstockFirmwareGateway : ICalcFirmwareGateway
{
  private const int KeyRunSteps = 200;
  /// <summary>Match <see cref="TeoCalc.Panamatik.EmulatorFirmwareGateway"/> timer cadence (Panamatik life-cycle).</summary>
  private const float RunTickSeconds = 0.05f;

  private float _runAccumulator;
  private string _displayText = string.Empty;
  private bool _displayBlankPulse;
  private bool _keyLineHeld;
  private long _displayRevision;
  private FirmwareKeyCommand? _activeKey;

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged;

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed;

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged;

  public event EventHandler<FirmwareBatchCompletedEventArgs>? BatchCompleted;

  public WoodstockCpu? Cpu { get; private set; }

  public bool PowerOn { get; set; }

  public bool ProgramMode
  {
    get => Cpu?.ProgramMode ?? false;
    private set
    {
      if (Cpu is not null)
      {
        Cpu.ProgramMode = value;
      }
    }
  }

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
      Classic: null);

  public FirmwareKeyCommand? ActiveKey => _activeKey;

  public bool KeyLineHeld => _keyLineHeld;

  public void AttachCpu(WoodstockCpu? cpu)
  {
    Cpu = cpu;
    PowerOn = false;
    _runAccumulator = 0f;
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
      Cpu.State.Flags &= ~WoodstockCpuFlags.DisplayOn;
    }

    PowerOn = false;
    ClearKeyLine();
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public bool IsDisplayVisible() =>
    PowerOn
    && Cpu is not null
    && (Cpu.State.Flags & WoodstockCpuFlags.DisplayOn) != 0
    && _displayText.Length > 0;

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
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    RunInstructionBatch(KeyRunSteps);
  }

  public void KeyDown(FirmwareKeyCommand key)
  {
    if (Cpu is null || !PowerOn)
    {
      return;
    }

    _activeKey = key;
    SetKeyLineHeld(true);
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

    // Panamatik HeadlessReleaseKey only clears the held line; it does not call act_release_key.
    ClearKeyLine();
    RunInstructionBatch(KeyRunSteps);
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
    for (int step = 0; step < steps; step++)
    {
      lastHandlerId = Cpu.Step().HandlerId;

      // Panamatik HeadlessRunTimerBatch status pulse.
      Cpu.State.Status |= 32;
      if (!ProgramMode)
      {
        Cpu.State.Status |= 8;
      }

      if (_keyLineHeld)
      {
        Cpu.State.Status |= 0x8000;
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
      Rom: Cpu.State.DelRom,
      Grp: (byte)((Cpu.State.Flags & WoodstockCpuFlags.Bank) != 0 ? 1 : 0),
      P: Cpu.State.P,
      Classic: null);
    BatchCompleted?.Invoke(this, new FirmwareBatchCompletedEventArgs(LastBatch));
  }

  private void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      SetDisplayState(string.Empty, blankPulse: false);
      return;
    }

    string text = WoodstockFirmwareDisplay.BuildLedText(Cpu.State);
    bool blankPulse = PowerOn && (Cpu.State.Flags & WoodstockCpuFlags.DisplayOn) == 0;
    SetDisplayState(text, blankPulse);
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
