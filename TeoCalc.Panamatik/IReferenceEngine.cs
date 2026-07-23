namespace TeoCalc.ReferenceEmulator;

public interface IReferenceEngine : IDisposable
{
  string ModelId { get; }

  bool PowerOn { get; set; }

  bool ProgramMode { get; }

  string DisplayText { get; }

  bool DisplayOn { get; }

  ReferenceEngineSnapshot Snapshot { get; }

  void PowerOnResume();

  void PowerOff();

  void SetProgramMode(bool programMode);

  void RunTimerBatch();

  void PressKey(byte keyCode);

  void ReleaseKey();
}

public readonly record struct ReferenceEngineSnapshot(
  ushort ProgramCounter,
  ushort Status,
  byte KeyBuffer,
  byte Flags,
  byte P,
  byte Rom,
  byte Grp);
