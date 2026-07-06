namespace TeoCalc.Rendering;

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

  void PowerOnResume();

  void PowerOff();

  bool IsDisplayVisible();

  void EndDisplayFrame();

  void SetProgramMode(bool programMode);

  void ToggleProgramMode();

  void Tick(float deltaSeconds);

  void Step();

  void KeyDown(FirmwareKeyCommand key);

  void KeyUp(FirmwareKeyCommand? key = null);

  void SetKeyLineHeld(bool held);
}
