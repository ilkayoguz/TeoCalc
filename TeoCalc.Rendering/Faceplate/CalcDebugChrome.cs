using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Shared Machine-debug / ROM-watch chrome — theme tokens, not hardcoded purple.</summary>
public static class CalcDebugChrome
{
  public static void SectionHeader(string title)
  {
    CalcAppTheme.EnsureInitialized();
    ImGui.PushStyleColor(
      ImGuiCol.Text,
      CalcAppThemeColors.ToVector4(CalcAppTheme.Current, ThemeTokens.TextSubtitleColor));
    ImGui.TextUnformatted(title);
    ImGui.PopStyleColor();
  }

  public static void Muted(string text)
  {
    CalcAppTheme.EnsureInitialized();
    ImGui.PushStyleColor(
      ImGuiCol.Text,
      CalcAppThemeColors.ToVector4(CalcAppTheme.Current, ThemeTokens.TextSecondaryColor));
    ImGui.TextUnformatted(text);
    ImGui.PopStyleColor();
  }

  public static void DrawExecutionStatus(bool paused, int programCounter, int stepCount, string? handlerId)
  {
    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;

    ImGui.TextUnformatted($"PC={programCounter:X4}");
    ImGui.SameLine();
    Muted($"steps={stepCount}");
    ImGui.SameLine();
    if (paused)
    {
      ImGui.PushStyleColor(
        ImGuiCol.Text,
        CalcAppThemeColors.ToVector4(palette, ThemeTokens.TextWarningColor));
      ImGui.TextUnformatted("PAUSED");
      ImGui.PopStyleColor();
    }
    else
    {
      ImGui.PushStyleColor(
        ImGuiCol.Text,
        CalcAppThemeColors.ToVector4(palette, ThemeTokens.TextAccentColor));
      ImGui.TextUnformatted("RUN");
      ImGui.PopStyleColor();
    }

    string handler = string.IsNullOrEmpty(handlerId) ? "-" : ShortHandler(handlerId);
    Muted(handler);
    if (!string.IsNullOrEmpty(handlerId) && ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set(handlerId);
    }
  }

  public static uint PcRowBackColor()
  {
    CalcAppTheme.EnsureInitialized();
    Vector4 accent = CalcAppThemeColors.ToVector4(CalcAppTheme.Current, ThemeTokens.PopupRowFocusFillColor);
    return ImGui.ColorConvertFloat4ToU32(accent);
  }

  public static uint SelectedRowBackColor()
  {
    CalcAppTheme.EnsureInitialized();
    return CalcAppThemeColors.ToImGui(CalcAppTheme.Current, ThemeTokens.RowSelectedBackColor);
  }

  private static string ShortHandler(string handlerId)
  {
    int dot = handlerId.LastIndexOf('.');
    return dot >= 0 && dot + 1 < handlerId.Length ? handlerId[(dot + 1)..] : handlerId;
  }
}
