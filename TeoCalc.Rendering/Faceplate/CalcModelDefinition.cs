namespace TeoCalc.Rendering.Faceplate;

/// <summary>Per-model faceplate metadata. <see cref="Id"/> is the short model number for the logo strip (e.g. 65, 67, 34C).</summary>
public sealed class CalcModelDefinition
{
  public required string Id { get; init; }

  public required string DisplayName { get; init; }

  public string ThemeId { get; init; } = CalcThemeCatalog.DefaultThemeId;

  public string BodyLayoutId { get; init; } = CalcBodyLayoutCatalog.DefaultLayoutId;

  public IReadOnlyList<CalcModifierKey> ModifierKeys { get; init; } = [];

  /// <summary>
  /// Modifier → label-slot → ink bindings for this model.
  /// Empty falls back to <see cref="CalcModifierPlacement.ClassicFg"/>.
  /// </summary>
  public IReadOnlyList<CalcModifierAnnotationStyle> AnnotationStyles { get; init; } = [];

  public IReadOnlyDictionary<string, string> PaletteOverrides { get; init; }
    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  public string LogoCaption => $"HEWLETT-PACKARD {Id}";
}
