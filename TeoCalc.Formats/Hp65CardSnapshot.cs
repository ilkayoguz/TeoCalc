namespace TeoCalc.Formats;

/// <summary>HP-65 mag-card style program + register snapshot (Panamatik <c>.hp65</c> ASCII).</summary>
public sealed record Hp65CardSnapshot(
  IReadOnlyList<byte> ProgramCodes,
  IReadOnlyList<double> Registers)
{
  public const int DefaultProgramCapacity = 100;

  public const int DefaultRegisterCount = 10;
}
