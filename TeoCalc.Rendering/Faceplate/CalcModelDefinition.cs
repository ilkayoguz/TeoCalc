using TeoCalc.Core.Catalog;

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

  /// <summary>Named annotation pack id from Model.json (semantic).</summary>
  public string? AnnotationStyleId { get; init; }

  /// <summary>Named switch bank id from Model.json (semantic).</summary>
  public string? SwitchBankId { get; init; }

  /// <summary>Card-slot presence from Model.json when set.</summary>
  public bool? HasCardSlot { get; init; }

  /// <summary>Printer capability from Model.json when set (title-bar icon only).</summary>
  public bool? HasPrinter { get; init; }

  /// <summary>Catalog/engine/short/product/family identity built by <see cref="CalcModelCatalog.Resolve"/>.</summary>
  public CalcModelIdentity? Identity { get; init; }

  public IReadOnlyDictionary<string, string> PaletteOverrides { get; init; }
    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

  /// <summary>Center brand caption on the logo plate (natural width, not stretched).</summary>
  public string LogoCaption => "Teo \u00A9 2026";

  /// <summary>Right-side product id on the logo plate (e.g. T-65).</summary>
  public string ProductLabel => Identity?.ProductLabel ?? $"T-{Id}";
}
