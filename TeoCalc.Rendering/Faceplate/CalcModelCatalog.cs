using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

public static class CalcModelCatalog
{
  public static CalcModelDefinition Hp65 { get; } = new()
  {
    Id = "65",
    DisplayName = "T-65",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Calc00dBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
    AnnotationStyleId = CalcAnnotationStyleCatalog.ClassicFgId,
    SwitchBankId = "Classic65",
    HasCardSlot = true,
    HasPrinter = false,
    Identity = CalcModelIds.Resolve("HP-65", "Classic"),
  };

  public static CalcModelDefinition Hp21 { get; } = new()
  {
    Id = "21",
    DisplayName = "T-21",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Calc00dBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
    AnnotationStyleId = CalcAnnotationStyleCatalog.ClassicFgId,
    SwitchBankId = "WoodstockAngle",
    HasCardSlot = false,
    HasPrinter = false,
    Identity = CalcModelIds.Resolve("HP-21", "Woodstock"),
  };

  /// <summary>
  /// Resolve faceplate metadata from engine <see cref="TeoCalcModelDefinition"/>.
  /// <paramref name="catalogOrEngineId"/> is the launcher / open id (e.g. HP-31E) when known —
  /// product logo label matches that 1:1 with the launcher.
  /// </summary>
  public static CalcModelDefinition Resolve(TeoCalcModelDefinition model, string? catalogOrEngineId = null)
  {
    string modelDisplay = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Model : model.DisplayName;
    string productSource = !string.IsNullOrWhiteSpace(catalogOrEngineId)
      ? catalogOrEngineId!
      : modelDisplay;

    string? familyHint = string.IsNullOrWhiteSpace(model.Family) ? null : model.Family;
    CalcModelIdentity identity = CalcModelIds.Resolve(productSource, familyHint);

    string shortId = model.Faceplate?.ShortId is { Length: > 0 } sid
      ? sid
      : identity.ShortId;
    if (!string.Equals(shortId, identity.ShortId, StringComparison.Ordinal))
    {
      identity = identity with
      {
        ShortId = shortId,
        ProductLabel = $"T-{shortId}",
      };
    }

    string bodyLayoutId = model.Faceplate?.BodyLayoutId is { Length: > 0 } layout
      ? layout
      : Calc00dBodyLayout.LayoutId;

    string themeId = model.Faceplate?.ThemeId is { Length: > 0 } theme
      ? theme
      : CalcThemeCatalog.DefaultThemeId;

    string annotationStyleId = model.Faceplate?.AnnotationStyleId is { Length: > 0 } ann
      ? ann
      : CalcAnnotationStyleCatalog.HeuristicId(identity.EngineId, identity.CatalogId);

    if (!CalcAnnotationStyleCatalog.TryResolve(
          annotationStyleId,
          out IReadOnlyList<CalcModifierAnnotationStyle> styles,
          out IReadOnlyList<CalcModifierKey> modifierKeys))
    {
      annotationStyleId = CalcAnnotationStyleCatalog.HeuristicId(identity.EngineId, identity.CatalogId);
      CalcAnnotationStyleCatalog.TryResolve(annotationStyleId, out styles, out modifierKeys);
    }

    string? switchBankId = model.Faceplate?.SwitchBankId;
    if (string.IsNullOrWhiteSpace(switchBankId))
    {
      switchBankId = CalcSwitchCatalog.HeuristicBankId(shortId, identity.CatalogId);
    }

    bool? hasCardSlot = model.Faceplate?.HasCardSlot;
    hasCardSlot ??= CalcCardSlotComponent.HeuristicHasCardSlot(shortId);
    bool? hasPrinter = model.Faceplate?.HasPrinter;

    return new CalcModelDefinition
    {
      Id = shortId,
      DisplayName = identity.CatalogId,
      ThemeId = themeId,
      BodyLayoutId = bodyLayoutId,
      ModifierKeys = modifierKeys,
      AnnotationStyles = styles,
      AnnotationStyleId = annotationStyleId,
      SwitchBankId = switchBankId,
      HasCardSlot = hasCardSlot,
      HasPrinter = hasPrinter,
      Identity = identity,
    };
  }

  public static CalcModelDefinition Resolve(string displayName) =>
    Resolve(
      new TeoCalcModelDefinition
      {
        Model = displayName,
        DisplayName = displayName,
        Family = CalcModelIds.InferFamily(displayName),
      },
      displayName);
}
