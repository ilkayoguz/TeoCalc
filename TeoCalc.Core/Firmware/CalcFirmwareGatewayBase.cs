namespace TeoCalc.Core.Firmware;

/// <summary>
/// Shared firmware life-cycle: power, tick cadence, key line, display snapshot events.
/// Family gateways override batch / display / power-off hooks.
/// </summary>
public abstract class CalcFirmwareGatewayBase : ICalcFirmwareGateway
{
  protected const int KeyRunSteps = 200;
  /// <summary>Match Panamatik / emulator timer cadence (most families use 50ms / 200 steps).</summary>
  protected const float RunTickSeconds = 0.05f;

  /// <summary>Instructions executed per timer batch. HP-01 overrides to Panamatik's 100.</summary>
  protected virtual int InstructionStepsPerBatch => KeyRunSteps;

  /// <summary>Gateway tick quantum. HP-01 overrides to Panamatik's 10ms timer1 interval.</summary>
  protected virtual float TimerTickSeconds => RunTickSeconds;

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

  public bool PowerOn { get; set; }

  public abstract bool ProgramMode { get; }

  public string DisplayText => _displayText;

  public FirmwareDisplaySnapshot DisplaySnapshot { get; private set; } =
    new(string.Empty, Visible: false, BlankPulse: false, Revision: 0, StepCount: 0, ProgramCounter: 0);

  public FirmwareBatchSnapshot LastBatch { get; protected set; } =
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

  public virtual bool SupportsCardProgram => false;

  public virtual IReadOnlyList<string> PrintLines => [];

  protected float RunAccumulator
  {
    get => _runAccumulator;
    set => _runAccumulator = value;
  }

  public void PowerOnResume()
  {
    PowerOn = true;
    RunInstructionBatch(InstructionStepsPerBatch);
  }

  public virtual void PowerOff()
  {
    OnPowerOffCpu();
    PowerOn = false;
    ClearKeyLine();
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public abstract bool IsDisplayVisible();

  public void EndDisplayFrame() =>
    _displayBlankPulse = false;

  public abstract void SetProgramMode(bool programMode);

  public void ToggleProgramMode() =>
    SetProgramMode(!ProgramMode);

  public void Tick(float deltaSeconds)
  {
    if (!CanRunBatch())
    {
      return;
    }

    _runAccumulator += deltaSeconds;
    float tick = TimerTickSeconds;
    while (_runAccumulator >= tick)
    {
      RunInstructionBatch(InstructionStepsPerBatch);
      _runAccumulator -= tick;
    }
  }

  public abstract void Step();

  public void KeyDown(FirmwareKeyCommand key)
  {
    if (!CanRunBatch())
    {
      return;
    }

    _activeKey = key;
    SetKeyLineHeld(true);
    OnKeyDown(key);
    RunInstructionBatch(InstructionStepsPerBatch);
    KeyProcessed?.Invoke(this, new FirmwareKeyProcessedEventArgs(key, _displayText, IsDisplayVisible()));
  }

  public void KeyUp(FirmwareKeyCommand? key = null)
  {
    if (key is not null && _activeKey is not null && _activeKey.Value != key.Value)
    {
      return;
    }

    ClearKeyLine();
    OnKeyUp();
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

  public virtual bool TryExportCardProgram(out byte[] programCodes, out double[] registers)
  {
    programCodes = [];
    registers = [];
    return false;
  }

  public virtual bool TryImportCardProgram(IReadOnlyList<byte> programCodes, IReadOnlyList<double> registers)
  {
    _ = programCodes;
    _ = registers;
    return false;
  }

  public virtual void ClearPrintLines()
  {
  }

  public virtual void AppendTestPrint(string line) =>
    _ = line;

  protected void ResetSessionState()
  {
    PowerOn = false;
    _runAccumulator = 0f;
    _activeKey = null;
    _keyLineHeld = false;
    SetDisplayState(string.Empty, blankPulse: false);
  }

  protected void ClearKeyLine()
  {
    FirmwareKeyCommand? key = _activeKey;
    _activeKey = null;
    if (_keyLineHeld)
    {
      _keyLineHeld = false;
      KeyStateChanged?.Invoke(this, new FirmwareKeyStateChangedEventArgs(key, Held: false));
    }
  }

  protected void SetDisplayState(string text, bool blankPulse)
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
        CurrentStepCount,
        CurrentProgramCounter);
      DisplayChanged?.Invoke(this, new FirmwareDisplayChangedEventArgs(DisplaySnapshot));
    }
  }

  protected bool DisplayBlankPulse => _displayBlankPulse;

  protected void RaiseBatchCompleted() =>
    BatchCompleted?.Invoke(this, new FirmwareBatchCompletedEventArgs(LastBatch));

  protected abstract bool CanRunBatch();

  protected abstract int CurrentStepCount { get; }

  protected abstract int CurrentProgramCounter { get; }

  protected abstract void OnPowerOffCpu();

  protected abstract void OnKeyDown(FirmwareKeyCommand key);

  protected abstract void OnKeyUp();

  protected abstract void RunInstructionBatch(int steps);
}
