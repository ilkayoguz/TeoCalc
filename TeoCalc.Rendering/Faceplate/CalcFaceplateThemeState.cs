namespace TeoCalc.Rendering.Faceplate;

public static class CalcFaceplateThemeState
{
  private static string? _userThemeId;

  public static string? UserThemeId => _userThemeId;

  public static void SetUserTheme(string? themeId) =>
    _userThemeId = string.IsNullOrWhiteSpace(themeId) ? null : themeId;

  public static void ApplyForModel(CalcModelDefinition model)
  {
    string themeId = _userThemeId ?? model.ThemeId;
    if (!string.Equals(CalcFaceplateTheme.Current.Id, themeId, StringComparison.OrdinalIgnoreCase))
    {
      CalcFaceplateTheme.SetTheme(themeId);
    }
  }
}
