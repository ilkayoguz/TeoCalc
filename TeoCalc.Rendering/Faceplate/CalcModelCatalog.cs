using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

public static class CalcModelCatalog
{
  public static CalcModelDefinition Hp65 { get; } = new()
  {
    Id = "65",
    DisplayName = "HP-65",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Calc00dBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
  };

  public static CalcModelDefinition Hp21 { get; } = new()
  {
    Id = "21",
    DisplayName = "HP-21",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Calc00dBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
  };

  /// <summary>
  /// Resolve faceplate metadata from engine <see cref="TeoCalcModelDefinition"/>.
  /// <paramref name="catalogOrEngineId"/> is the launcher / open id (e.g. HP-31E) when known —
  /// product logo label matches that 1:1 with the launcher.
  /// </summary>
  public static CalcModelDefinition Resolve(TeoCalcModelDefinition model, string? catalogOrEngineId = null)
  {
    string modelDisplay = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Model : model.DisplayName;
    // Prefer the catalog id used to open (T-31E), then Faceplate.ShortId, then Model.json DisplayName.
    string productSource = !string.IsNullOrWhiteSpace(catalogOrEngineId)
      ? catalogOrEngineId!
      : modelDisplay;
    string shortId = model.Faceplate?.ShortId is { Length: > 0 } sid
      ? sid
      : CalcModelIds.ToShortId(productSource);

    string bodyLayoutId = model.Faceplate?.BodyLayoutId is { Length: > 0 } layout
      ? layout
      : Calc00dBodyLayout.LayoutId;

    string themeId = model.Faceplate?.ThemeId is { Length: > 0 } theme
      ? theme
      : CalcThemeCatalog.DefaultThemeId;

    string engineId = CalcModelIds.ToEngineId(catalogOrEngineId ?? modelDisplay);
    string catalogId = productSource;
    bool hp34 = string.Equals(engineId, "HP-34", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-34C", StringComparison.OrdinalIgnoreCase);
    bool hp35 = string.Equals(engineId, "HP-35", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-35", StringComparison.OrdinalIgnoreCase);
    bool hp45 = string.Equals(engineId, "HP-45", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-45", StringComparison.OrdinalIgnoreCase);
    bool hp55 = string.Equals(engineId, "HP-55", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-55", StringComparison.OrdinalIgnoreCase);
    bool hp67 = string.Equals(engineId, "HP-67", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-67", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-67BE", StringComparison.OrdinalIgnoreCase);
    bool hp70 = string.Equals(engineId, "HP-70", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-70", StringComparison.OrdinalIgnoreCase);
    bool hp80 = string.Equals(engineId, "HP-80", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-80", StringComparison.OrdinalIgnoreCase);

    return new CalcModelDefinition
    {
      Id = shortId,
      DisplayName = catalogId,
      ThemeId = themeId,
      BodyLayoutId = bodyLayoutId,
      ModifierKeys = hp70
        ? []
        : hp34 || hp67
          ? [CalcModifierKey.F, CalcModifierKey.G, CalcModifierKey.H]
          : hp35 || hp45 || hp80
            ? [CalcModifierKey.F]
            : [CalcModifierKey.F, CalcModifierKey.G],
      AnnotationStyles = hp70
        ? CalcModifierPlacement.None
        : hp67
          ? CalcModifierPlacement.ClassicHp67Fgh
          : hp34
            ? CalcModifierPlacement.SpiceFgh
            : hp35
              ? CalcModifierPlacement.Hp35WhiteCapAbove
              : hp45 || hp80
                ? CalcModifierPlacement.ClassicGoldOnly
                : hp55
                  ? CalcModifierPlacement.ClassicDualCapAbove
                  : CalcModifierPlacement.ClassicFg,
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
      CalcModelIds.ToEngineId(displayName));
}
