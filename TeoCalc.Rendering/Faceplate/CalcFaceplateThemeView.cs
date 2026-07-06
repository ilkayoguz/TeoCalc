namespace TeoCalc.Rendering.Faceplate;

public static class CalcFaceplateThemeView
{
  public static void DrawThemeCombo()
  {
    IReadOnlyList<CalcThemePack> themes = CalcThemeCatalog.LoadAll();
    if (themes.Count == 0)
    {
      return;
    }

    string preview = CalcFaceplateTheme.Current.DisplayName;
    if (ImGuiNET.ImGui.BeginCombo("Theme", preview))
    {
      foreach (CalcThemePack theme in themes)
      {
        bool selected = string.Equals(theme.Id, CalcFaceplateTheme.Current.Id, StringComparison.OrdinalIgnoreCase);
        if (ImGuiNET.ImGui.Selectable(theme.DisplayName, selected))
        {
          CalcFaceplateThemeState.SetUserTheme(theme.Id);
          CalcFaceplateTheme.SetTheme(theme.Id);
        }

        if (selected)
        {
          ImGuiNET.ImGui.SetItemDefaultFocus();
        }
      }

      ImGuiNET.ImGui.EndCombo();
    }
  }
}
