namespace TeoCalc.Rendering;

public sealed record ReferenceCalculatorEntry(
  string ModelId,
  string ReferenceModelName,
  string ProjectPath,
  string TargetFramework,
  string? ExecutablePath)
{
  public bool HasProject => File.Exists(ProjectPath);

  public bool CanLaunch => ExecutablePath is not null && File.Exists(ExecutablePath);

  public bool CanBuildOnDemand =>
    HasProject && string.Equals(TargetFramework, "net481", StringComparison.OrdinalIgnoreCase);

  public bool CanOpen => CanLaunch || CanBuildOnDemand;

  public string Status =>
    CanLaunch
      ? "Reference available"
      : CanBuildOnDemand
        ? $"Reference project available ({TargetFramework}, builds on demand)"
        : HasProject ? $"Reference project found ({TargetFramework})" : "Reference pending";
}
