namespace TeoCalc.Formats;

/// <summary>Classic mag-card style program + register snapshot (T-65 / HP-65 engine packing).</summary>
public sealed record ClassicCardSnapshot(
  IReadOnlyList<byte> ProgramCodes,
  IReadOnlyList<double> Registers)
{
  public const int DefaultProgramCapacity = 100;

  public const int DefaultRegisterCount = 10;
}
