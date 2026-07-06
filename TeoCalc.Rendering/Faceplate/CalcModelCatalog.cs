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

  public static CalcModelDefinition Resolve(string displayName) =>
    displayName.Equals("HP-65", StringComparison.OrdinalIgnoreCase) ? Hp65 : new()
    {
      Id = displayName.Replace("HP-", string.Empty, StringComparison.OrdinalIgnoreCase),
      DisplayName = displayName,
      ThemeId = CalcThemeCatalog.DefaultThemeId,
      BodyLayoutId = CalcBodyLayoutCatalog.DefaultLayoutId,
      ModifierKeys = [CalcModifierKey.F, CalcModifierKey.G],
    };
}
