using TeoCalc.Core.Firmware;

namespace TeoCalc.Panamatik;

/// <summary>Routes TeoCalc UI through headless emulator engines in this adapter assembly.</summary>
public sealed class EmulatorFirmwareGateway : ICalcFirmwareGateway, IDisposable
{
  private const float RunTickSeconds = 0.05f;

  private readonly IPanamatikEngine _engine;
  private float _runAccumulator;
  private long _stepCount;
  private long _displayRevision;
  private string _displayText = string.Empty;
  private bool _displayBlankPulse;
  private bool _keyLineHeld;
  private FirmwareKeyCommand? _activeKey;

  public EmulatorFirmwareGateway(IPanamatikEngine engine)
  {
    _engine = engine;
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged;

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed;

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged;

  public event EventHandler<FirmwareBatchCompletedEventArgs>? BatchCompleted;

  public bool PowerOn { get; set; }

  public bool ProgramMode => _engine.ProgramMode;

  public string DisplayText => _displayText;

  public FirmwareDisplaySnapshot DisplaySnapshot { get; private set; } =
    new(string.Empty, Visible: false, BlankPulse: false, Revision: 0, StepCount: 0, ProgramCounter: 0);

  public FirmwareBatchSnapshot LastBatch { get; private set; } =
    new(
      StepCount: 0,
      ProgramCounter: 0,
      Status: 0,
      KeyBuffer: 0,
      LastHandlerId: "Emulator.Engine",
      KeyLineHeld: false,
      ActiveKey: null,
      Display: null,
      Rom: 0,
      Grp: 0,
      P: 0,
      Classic: null);

  public FirmwareKeyCommand? ActiveKey => _activeKey;

  public bool KeyLineHeld => _keyLineHeld;

  public void PowerOnResume()
  {
    PowerOn = true;
    _engine.PowerOnResume();
    RunTimerBatch();
  }

  public void PowerOff()
  {
    _engine.PowerOff();
    PowerOn = false;
    _runAccumulator = 0f;
    ClearKeyLine();
    SetDisplayState(string.Empty, blankPulse: false);
  }

  public bool IsDisplayVisible() =>
    PowerOn && _engine.DisplayOn && _displayText.Length > 0;

  public void EndDisplayFrame()
  {
  }

  public void SetProgramMode(bool programMode)
  {
    if (!PowerOn || ProgramMode == programMode)
    {
      return;
    }

    _engine.SetProgramMode(programMode);
    RunTimerBatch();
  }

  public void ToggleProgramMode() =>
    SetProgramMode(!ProgramMode);

  public void Tick(float deltaSeconds)
  {
    if (!PowerOn)
    {
      return;
    }

    _runAccumulator += deltaSeconds;
    while (_runAccumulator >= RunTickSeconds)
    {
      RunTimerBatch();
      _runAccumulator -= RunTickSeconds;
    }
  }

  public void Step()
  {
    if (!PowerOn)
    {
      return;
    }

    RunTimerBatch();
  }

  public void KeyDown(FirmwareKeyCommand key)
  {
    if (!PowerOn)
    {
      return;
    }

    _activeKey = key;
    SetKeyLineHeld(true);
    _engine.PressKey(key.KeyCode);
    RunTimerBatch();
    KeyProcessed?.Invoke(this, new FirmwareKeyProcessedEventArgs(key, _displayText, IsDisplayVisible()));
  }

  public void KeyUp(FirmwareKeyCommand? key = null)
  {
    if (key is not null && _activeKey is not null && _activeKey.Value != key.Value)
    {
      return;
    }

    _engine.ReleaseKey();
    ClearKeyLine();
    RunTimerBatch();
  }

  public void SetKeyLineHeld(bool held)
  {
    if (_keyLineHeld == held)
    {
      return;
    }

    _keyLineHeld = held;
    if (!held)
    {
      _engine.ReleaseKey();
      RunTimerBatch();
    }

    KeyStateChanged?.Invoke(this, new FirmwareKeyStateChangedEventArgs(_activeKey, held));
  }

  public void Dispose() =>
    _engine.Dispose();

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

  private void RunTimerBatch()
  {
    _engine.RunTimerBatch();
    _stepCount += 200;
    RefreshDisplayFromEngine();
    PublishBatch();
  }

  private void RefreshDisplayFromEngine()
  {
    string text = _engine.DisplayText;
    bool blankPulse = PowerOn && !_engine.DisplayOn;
    SetDisplayState(text, blankPulse);
  }

  private void PublishBatch()
  {
    PanamatikEngineSnapshot snapshot = _engine.Snapshot;
    LastBatch = new FirmwareBatchSnapshot(
      (int)_stepCount,
      snapshot.ProgramCounter,
      snapshot.Status,
      snapshot.KeyBuffer,
      "Emulator.Engine",
      _keyLineHeld,
      _activeKey,
      DisplaySnapshot,
      snapshot.Rom,
      snapshot.Grp,
      snapshot.P,
      Classic: null);
    BatchCompleted?.Invoke(this, new FirmwareBatchCompletedEventArgs(LastBatch));
  }

  private void SetDisplayState(string text, bool blankPulse)
  {
    bool changed = _displayText != text || _displayBlankPulse != blankPulse;
    _displayText = text;
    _displayBlankPulse = blankPulse;
    if (changed)
    {
      PanamatikEngineSnapshot snapshot = _engine.Snapshot;
      DisplaySnapshot = new FirmwareDisplaySnapshot(
        text,
        IsDisplayVisible(),
        blankPulse,
        ++_displayRevision,
        _stepCount,
        snapshot.ProgramCounter);
      DisplayChanged?.Invoke(this, new FirmwareDisplayChangedEventArgs(DisplaySnapshot));
    }
  }
}
