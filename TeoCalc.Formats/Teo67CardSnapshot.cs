namespace TeoCalc.Formats;

/// <summary>Mag-card style program + register snapshot for ACT HasCardSlot models (T-67 packing).</summary>
public sealed record Teo67CardSnapshot(
  IReadOnlyList<byte> ProgramCodes,
  IReadOnlyList<double> Registers,
  Teo67CardModeSnapshot? Mode = null)
{
  public const int DefaultProgramCapacity = 224;

  public const int DefaultRegisterCount = 26;
}

/// <summary>Optional calculator mode carried with a T-67 card snapshot.</summary>
public sealed record Teo67CardModeSnapshot(
  string Angle,
  string Display,
  int Digits,
  byte FlagsHi,
  byte FlagsLo);
