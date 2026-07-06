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
  };

  public static CalcModelDefinition Hp21 { get; } = new()
  {
    Id = "21",
    DisplayName = "HP-21",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    BodyLayoutId = Hp21CalcBodyLayout.LayoutId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
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
        BodyLayoutId = displayName.StartsWith("HP-2", StringComparison.OrdinalIgnoreCase)
          ? Hp21CalcBodyLayout.LayoutId
          : CalcBodyLayoutCatalog.DefaultLayoutId,
        ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
      },
    };
}
