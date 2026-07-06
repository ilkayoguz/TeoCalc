namespace TeoCalc.Rendering.Faceplate;

/// <summary>Per-model faceplate metadata. <see cref="Id"/> is the short model number for the logo strip (e.g. 65, 67, 34C).</summary>
public sealed class CalcModelDefinition
{
  public required string Id { get; init; }

  public required string DisplayName { get; init; }

  public IReadOnlyList<CalcModifierKey> ModifierKeys { get; init; } = [];

  public IReadOnlyList<CalcModifierAnnotationStyle> AnnotationStyles { get; init; } = [];

  public string LogoCaption => $"HEWLETT-PACKARD {Id}";
}
