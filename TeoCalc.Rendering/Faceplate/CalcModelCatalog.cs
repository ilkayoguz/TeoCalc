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

  /// <summary>Resolve faceplate metadata from engine <see cref="TeoCalcModelDefinition"/> when available.</summary>
  public static CalcModelDefinition Resolve(TeoCalcModelDefinition model, string? engineModelId = null)
  {
    string catalogId = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Model : model.DisplayName;
    string shortId = model.Faceplate?.ShortId is { Length: > 0 } sid
      ? sid
      : CalcModelIds.ToShortId(catalogId);

    string bodyLayoutId = model.Faceplate?.BodyLayoutId is { Length: > 0 } layout
      ? layout
      : Calc00dBodyLayout.LayoutId;

    string themeId = model.Faceplate?.ThemeId is { Length: > 0 } theme
      ? theme
      : CalcThemeCatalog.DefaultThemeId;

    string engineId = CalcModelIds.ToEngineId(engineModelId ?? catalogId);
    bool hp34 = string.Equals(engineId, "HP-34", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-34C", StringComparison.OrdinalIgnoreCase);
    bool hp35 = string.Equals(engineId, "HP-35", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-35", StringComparison.OrdinalIgnoreCase);

    return new CalcModelDefinition
    {
      Id = shortId,
      DisplayName = catalogId,
      ThemeId = themeId,
      BodyLayoutId = bodyLayoutId,
      ModifierKeys = hp34
        ? [CalcModifierKey.F, CalcModifierKey.G, CalcModifierKey.H]
        : hp35
          ? [CalcModifierKey.F]
          : [CalcModifierKey.F, CalcModifierKey.G],
      AnnotationStyles = hp34
        ? CalcModifierPlacement.SpiceFgh
        : hp35
          ? CalcModifierPlacement.Hp35WhiteCapAbove
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
