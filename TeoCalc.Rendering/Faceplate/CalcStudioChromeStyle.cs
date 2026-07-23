using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>TeoTheme Control* / PrimaryButton* for Studio toolbar (follows AppTheme).</summary>
internal static class CalcStudioChromeStyle
{
  private const int ToolbarColorCount = 4;
  private const int PrimaryColorCount = 4;

  public static void PushToolbar()
  {
    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;
    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
    ImGui.PushStyleColor(ImGuiCol.Button, CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBackColor));
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlHoverBackColor));
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlActiveBackColor));
    ImGui.PushStyleColor(ImGuiCol.Border, CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBorderColor));
  }

  public static void PopToolbar()
  {
    ImGui.PopStyleColor(ToolbarColorCount);
    ImGui.PopStyleVar(2);
  }

  public static void PushPrimary()
  {
    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;
    ImGui.PushStyleColor(ImGuiCol.Button, CalcAppThemeColors.ToVector4(palette, ThemeTokens.PrimaryButtonBackColor));
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, CalcAppThemeColors.ToVector4(palette, ThemeTokens.PrimaryButtonHoverBackColor));
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, CalcAppThemeColors.ToVector4(palette, ThemeTokens.PrimaryButtonActiveBackColor));
    ImGui.PushStyleColor(ImGuiCol.Text, CalcAppThemeColors.ToVector4(palette, ThemeTokens.DialogIconInkColor));
  }

  public static void PopPrimary() => ImGui.PopStyleColor(PrimaryColorCount);
}
