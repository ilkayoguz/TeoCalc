using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public sealed class FirmwareGateway
{
  private const int KeyRunSteps = 200;
  private const float RunTickSeconds = 0.01f;

  private float _runAccumulator;
  private string _displayText = string.Empty;
  private bool _displayBlankPulse;
  private int _keyHoldBatchesRemaining;
  private bool _keyLineHeld;
  private FirmwareKeyCommand? _activeKey;

  public event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged;

  public event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed;

  public event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged;

  public ClassicCpu? Cpu { get; private set; }

  public bool PowerOn { get; set; }

  public bool ProgramMode { get; private set; }

  public string DisplayText => _displayText;

  public FirmwareKeyCommand? ActiveKey => _activeKey;

  public bool KeyLineHeld => _keyLineHeld;

  public void AttachCpu(ClassicCpu? cpu)
  {
    Cpu = cpu;
    PowerOn = false;
    _runAccumulator = 0f;
    _keyHoldBatchesRemaining = 0;
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
    _keyHoldBatchesRemaining = 0;
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
    _keyHoldBatchesRemaining = Math.Max(_keyHoldBatchesRemaining, 12);
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

    bool firmwareKeyHeld = _keyLineHeld || _keyHoldBatchesRemaining > 0;
    for (int step = 0; step < steps; step++)
    {
      Cpu.Step();
      if (firmwareKeyHeld)
      {
        Cpu.State.Status |= 1;
      }

      if (ProgramMode)
      {
        Cpu.State.Status |= 8;
      }
    }

    if (_keyHoldBatchesRemaining > 0)
    {
      _keyHoldBatchesRemaining--;
    }

    RefreshDisplayFromCpu();
  }

  private void RefreshDisplayFromCpu()
  {
    if (Cpu is null || !PowerOn)
    {
      SetDisplayState(string.Empty, blankPulse: false);
      return;
    }

    string? text = ClassicFirmwareDisplay.TryBuildLedText(
      Cpu.State,
      ProgramMode,
      Cpu.Program.EndState);

    if (text is not null)
    {
      SetDisplayState(text, blankPulse: false);
      return;
    }

    if (_displayBlankPulse)
    {
      SetDisplayState(string.Empty, blankPulse: true);
    }
  }

  private void SetDisplayState(string text, bool blankPulse)
  {
    bool changed = _displayText != text || _displayBlankPulse != blankPulse;
    _displayText = text;
    _displayBlankPulse = blankPulse;
    if (changed)
    {
      DisplayChanged?.Invoke(this, new FirmwareDisplayChangedEventArgs(text, IsDisplayVisible()));
    }
  }
}

public sealed record FirmwareDisplayChangedEventArgs(string Text, bool Visible);

public readonly record struct FirmwareKeyCommand(int KeyChartIndex, byte KeyCode);

public sealed record FirmwareKeyStateChangedEventArgs(FirmwareKeyCommand? Key, bool Held);

public sealed record FirmwareKeyProcessedEventArgs(
  FirmwareKeyCommand Key,
  string DisplayText,
  bool DisplayVisible);
