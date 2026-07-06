namespace TeoCalc.Rendering.Faceplate;

public static class CalcModelCatalog
{
  public static CalcModelDefinition Hp65 { get; } = new()
  {
    Id = "65",
    DisplayName = "HP-65",
    ThemeId = CalcThemeCatalog.DefaultThemeId,
    ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
  };

  public static CalcModelDefinition Resolve(string displayName) =>
    displayName.Equals("HP-65", StringComparison.OrdinalIgnoreCase) ? Hp65 : new()
    {
      Id = displayName.Replace("HP-", string.Empty, StringComparison.OrdinalIgnoreCase),
      DisplayName = displayName,
      ThemeId = CalcThemeCatalog.DefaultThemeId,
      ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    };
}
