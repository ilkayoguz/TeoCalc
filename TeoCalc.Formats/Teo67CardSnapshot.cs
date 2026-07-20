namespace TeoCalc.Formats;

/// <summary>Mag-card style program + register snapshot for ACT HasCardSlot models (.hp67 ASCII).</summary>
public sealed record Teo67CardSnapshot(
  IReadOnlyList<byte> ProgramCodes,
  IReadOnlyList<double> Registers,
  Teo67CardModeSnapshot? Mode = null)
{
  public const int DefaultProgramCapacity = 224;

  public const int DefaultRegisterCount = 26;
}

/// <summary>Optional MODE line from .hp67 ASCII cards.</summary>
public sealed record Teo67CardModeSnapshot(
  string Angle,
  string Display,
  int Digits,
  byte FlagsHi,
  byte FlagsLo);
