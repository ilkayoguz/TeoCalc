namespace TeoCalc.Rendering;

/// <summary>Named session knobs (speed today; HW/feature flags later).</summary>
public sealed class CalcSessionProfile
{
  public const string StandardId = "standard";
  public const string SlowId = "slow";
  public const string FastId = "fast";
  public const string MaxId = "max";

  public required string Id { get; init; }

  public required string Name { get; set; }

  /// <summary>Index into <see cref="CalcExplorerSession"/> execution-speed steps (0.25×…16×).</summary>
  public int ExecutionSpeedIndex { get; set; } = 2;

  /// <summary>When true, selecting/applying this profile sets free-run speed.</summary>
  public bool ControlExecutionSpeed { get; set; } = true;

  public bool IsBuiltIn { get; init; }

  public static IReadOnlyList<CalcSessionProfile> BuiltIns { get; } =
  [
    new()
    {
      Id = SlowId,
      Name = "Slow",
      ExecutionSpeedIndex = 0,
      ControlExecutionSpeed = true,
      IsBuiltIn = true,
    },
    new()
    {
      Id = StandardId,
      Name = "Standard",
      ExecutionSpeedIndex = 2,
      ControlExecutionSpeed = true,
      IsBuiltIn = true,
    },
    new()
    {
      Id = FastId,
      Name = "Fast",
      ExecutionSpeedIndex = 4,
      ControlExecutionSpeed = true,
      IsBuiltIn = true,
    },
    new()
    {
      Id = MaxId,
      Name = "Max",
      ExecutionSpeedIndex = 6,
      ControlExecutionSpeed = true,
      IsBuiltIn = true,
    },
  ];

  public CalcSessionProfile Clone(string? newId = null, string? newName = null, bool? builtIn = null) =>
    new()
    {
      Id = newId ?? Id,
      Name = newName ?? Name,
      ExecutionSpeedIndex = ExecutionSpeedIndex,
      ControlExecutionSpeed = ControlExecutionSpeed,
      IsBuiltIn = builtIn ?? IsBuiltIn,
    };
}
