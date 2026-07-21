using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Firmware;

/// <summary>UI/session boundary for calculator firmware (ClassicCpu or emulator adapter).</summary>
public interface ICalcFirmwareGateway
{
  event EventHandler<FirmwareDisplayChangedEventArgs>? DisplayChanged;

  event EventHandler<FirmwareKeyProcessedEventArgs>? KeyProcessed;

  event EventHandler<FirmwareKeyStateChangedEventArgs>? KeyStateChanged;

  event EventHandler<FirmwareBatchCompletedEventArgs>? BatchCompleted;

  bool PowerOn { get; set; }

  bool ProgramMode { get; }

  string DisplayText { get; }

  FirmwareDisplaySnapshot DisplaySnapshot { get; }

  FirmwareBatchSnapshot LastBatch { get; }

  FirmwareKeyCommand? ActiveKey { get; }

  bool KeyLineHeld { get; }

  /// <summary>True when this gateway can import/export a Classic program card snapshot.</summary>
  bool SupportsCardProgram { get; }

  /// <summary>Live printer strip lines (empty when the model has no printer).</summary>
  IReadOnlyList<string> PrintLines { get; }

  void PowerOnResume();

  void PowerOff();

  bool IsDisplayVisible();

  void EndDisplayFrame();

  void SetProgramMode(bool programMode);

  void ToggleProgramMode();

  void Tick(float deltaSeconds);

  void Step();

  /// <summary>
  /// When true, timer <see cref="Tick"/> batches are skipped so DEBUG/TRACE can single-step.
  /// Key presses still run instruction batches.
  /// </summary>
  bool ExecutionPaused { get; set; }

  /// <summary>True when this gateway can single-step microcode (native engines).</summary>
  bool SupportsInstructionStep { get; }

  /// <summary>Execute one microcode instruction and pause continuous run.</summary>
  void StepInto();

  /// <summary>
  /// Step over a subroutine call when the next executed op is JSB; otherwise same as <see cref="StepInto"/>.
  /// </summary>
  void StepOver(int maxInstructions = 50_000);

  /// <summary>Resume timer-driven execution after a break / step.</summary>
  void ContinueExecution();

  /// <summary>Text snapshot of CPU / batch / optional registers for bug reports.</summary>
  string CaptureDebugDump();

  /// <summary>Working register digests when the native CPU exposes them; otherwise null.</summary>
  FirmwareDebugRegisters? TryGetDebugRegisters();

  void KeyDown(FirmwareKeyCommand key);

  void KeyUp(FirmwareKeyCommand? key = null);

  void SetKeyLineHeld(bool held);

  bool TryExportCardProgram(out byte[] programCodes, out double[] registers);

  bool TryImportCardProgram(IReadOnlyList<byte> programCodes, IReadOnlyList<double> registers);

  void ClearPrintLines();

  void AppendTestPrint(string line);
}

public sealed record FirmwareDisplaySnapshot(
  string Text,
  bool Visible,
  bool BlankPulse,
  long Revision,
  long StepCount,
  int ProgramCounter);

public sealed record FirmwareDisplayChangedEventArgs(FirmwareDisplaySnapshot Snapshot)
{
  public string Text => Snapshot.Text;

  public bool Visible => Snapshot.Visible;
}

public readonly record struct FirmwareKeyCommand(int KeyChartIndex, byte KeyCode);

public sealed record FirmwareKeyStateChangedEventArgs(FirmwareKeyCommand? Key, bool Held);

public sealed record FirmwareKeyProcessedEventArgs(
  FirmwareKeyCommand Key,
  string DisplayText,
  bool DisplayVisible);

/// <summary>ClassicCpu-only batch diagnostics; null on emulator / non-Classic gateways.</summary>
public sealed record ClassicFirmwareDiagnostics(
  ClassicCpuFlags Flags,
  ClassicKeyInputState KeyInputState,
  int BranchOffset,
  int KeysToRomAddressCount,
  int BufferToRomAddressCount,
  bool KeyAvailable);

public sealed record FirmwareBatchSnapshot(
  int StepCount,
  int ProgramCounter,
  ushort Status,
  byte KeyBuffer,
  string? LastHandlerId,
  bool KeyLineHeld,
  FirmwareKeyCommand? ActiveKey,
  FirmwareDisplaySnapshot? Display,
  byte Rom,
  byte Grp,
  byte P,
  ClassicFirmwareDiagnostics? Classic = null);

public sealed record FirmwareBatchCompletedEventArgs(FirmwareBatchSnapshot Snapshot);
