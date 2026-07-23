using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering;

/// <summary>Theme-backed hover tooltips (TeoCave <c>TooltipView</c> pattern).</summary>
internal static class CalcAppTooltip
{
  public const float MaxWrapWidth = 360f;

  public static void Set(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return;
    }

    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;
    ImGui.PushStyleColor(ImGuiCol.PopupBg, CalcAppThemeColors.ToVector4(palette, ThemeTokens.TooltipBackColor));
    ImGui.PushStyleColor(ImGuiCol.Border, CalcAppThemeColors.ToVector4(palette, ThemeTokens.TooltipBorderColor));
    ImGui.PushStyleColor(ImGuiCol.Text, CalcAppThemeColors.ToVector4(palette, ThemeTokens.TooltipTextColor));
    ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);
    ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1f);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f));
    ImGui.BeginTooltip();
    ImGui.PushTextWrapPos(MaxWrapWidth);
    ImGui.TextUnformatted(text);
    ImGui.PopTextWrapPos();
    ImGui.EndTooltip();
    ImGui.PopStyleVar(3);
    ImGui.PopStyleColor(3);
  }
}
