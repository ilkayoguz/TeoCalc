namespace TeoCalc.Rendering.Faceplate;

public static class CalcModelCatalog
{
  public static CalcModelDefinition Hp65 { get; } = new()
  {
    Id = "65",
    DisplayName = "HP-65",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Hp65CalcBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
  };

  public static CalcModelDefinition Hp21 { get; } = new()
  {
    Id = "21",
    DisplayName = "HP-21",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Hp21CalcBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    AnnotationStyles = CalcModifierPlacement.ClassicFg,
  };

  public static CalcModelDefinition Resolve(string displayName) =>
    displayName.ToUpperInvariant() switch
    {
      "HP-65" => Hp65,
      "HP-21" => Hp21,
      _ => new()
      {
        Id = displayName.Replace("HP-", string.Empty, StringComparison.OrdinalIgnoreCase),
        DisplayName = displayName,
        ThemeId = CalcThemeCatalog.DefaultThemeId,
        BodyLayoutId = displayName.ToUpperInvariant() switch
        {
          "HP-65" or "HP-67" or "HP-35" or "HP-19C" => Hp65CalcBodyLayout.LayoutId,
          "HP-01" => Hp21CalcBodyLayout.LayoutId,
          "HP-21" => Hp21CalcBodyLayout.LayoutId,
          _ when displayName.StartsWith("HP-2", StringComparison.OrdinalIgnoreCase)
            || displayName.StartsWith("HP-3", StringComparison.OrdinalIgnoreCase) => Hp21CalcBodyLayout.LayoutId,
          _ => Hp65CalcBodyLayout.LayoutId,
        },
        ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
        AnnotationStyles = CalcModifierPlacement.ClassicFg,
      },
    };
}
