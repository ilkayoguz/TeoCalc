namespace TeoCalc.Formats;

/// <summary>Classic mag-card style program + register snapshot (<c>.hp65</c> ASCII interop).</summary>
public sealed record Hp65CardSnapshot(
  IReadOnlyList<byte> ProgramCodes,
  IReadOnlyList<double> Registers)
{
  public const int DefaultProgramCapacity = 100;

  public const int DefaultRegisterCount = 10;
}
