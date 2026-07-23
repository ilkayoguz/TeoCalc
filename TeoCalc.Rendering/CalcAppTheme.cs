using System.Numerics;
using ImGuiNET;
using TeoCalc.Core;
using TeoTheme;
using TeoTheme.Windows;

namespace TeoCalc.Rendering;

/// <summary>
/// Loads portable AppTheme pack, resolves System/Light/Dark, and applies shell chrome.
/// Faceplate / CalcTheme (Retro/Modern) stays independent.
/// </summary>
public static class CalcAppTheme
{
  private static AppThemeService? _service;

  public static bool IsInitialized => _service is not null;

  public static AppThemeService Service =>
    _service ?? throw new InvalidOperationException("CalcAppTheme.Initialize must run before use.");

  public static ThemePalette Current => Service.Current;

  public static ThemeAppearance Appearance => Service.Appearance;

  public static AppThemePreference Preference => Service.Preference;

  public static void Initialize(AppThemePreference? preference = null)
  {
    AppThemePreference pref = preference ?? CalcUserSettingsStore.LoadAppThemePreference();
    string themeDir = TeoCalcPaths.ResourcePath(Path.Combine("Config", "AppTheme"));
    ThemePack pack = ThemeCatalog.LoadDefault(themeDir);
    _service?.Dispose();
    _service = new AppThemeService(
      pack,
      pref,
      OperatingSystem.IsWindows() ? WindowsHostThemeResolver.Instance : null,
      OperatingSystem.IsWindows() ? new WindowsHostThemeWatcher() : null);
  }

  public static void EnsureInitialized()
  {
    if (_service is null)
    {
      CalcUserSettingsStore.Initialize();
      Initialize();
    }
  }

  public static void SetPreference(AppThemePreference preference)
  {
    EnsureInitialized();
    if (_service!.Preference == preference)
    {
      return;
    }

    _service.Preference = preference;
    CalcUserSettingsStore.SaveAppThemePreference(preference);
  }

  /// <summary>Call after each ImGui context is current (launcher and each calc window).</summary>
  public static void ApplyImGuiStyle()
  {
    EnsureInitialized();
    if (ImGui.GetCurrentContext() == IntPtr.Zero)
    {
      return;
    }

    ThemePalette palette = Current;
    ImGuiStylePtr style = ImGui.GetStyle();
    style.WindowRounding = 6f;
    style.FrameRounding = 4f;
    style.PopupRounding = 6f;
    style.PopupBorderSize = 1f;
    style.Colors[(int)ImGuiCol.Text] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.TextColor);
    style.Colors[(int)ImGuiCol.TextDisabled] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.TextDisabledColor);
    style.Colors[(int)ImGuiCol.WindowBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.WindowBackColor);
    style.Colors[(int)ImGuiCol.ChildBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.PanelBackColor);
    style.Colors[(int)ImGuiCol.PopupBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.PopupBackColor);
    style.Colors[(int)ImGuiCol.Border] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBorderColor);
    style.Colors[(int)ImGuiCol.FrameBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBackColor);
    style.Colors[(int)ImGuiCol.FrameBgHovered] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlHoverBackColor);
    style.Colors[(int)ImGuiCol.FrameBgActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlActiveBackColor);
    style.Colors[(int)ImGuiCol.TitleBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.WindowTitleBarBackColor);
    style.Colors[(int)ImGuiCol.TitleBgActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.WindowTitleBarBackColor);
    style.Colors[(int)ImGuiCol.Button] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBackColor);
    style.Colors[(int)ImGuiCol.ButtonHovered] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlHoverBackColor);
    style.Colors[(int)ImGuiCol.ButtonActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlActiveBackColor);
    style.Colors[(int)ImGuiCol.Header] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlActiveBackColor);
    style.Colors[(int)ImGuiCol.HeaderHovered] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlHoverBackColor);
    style.Colors[(int)ImGuiCol.HeaderActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.RowSelectedBackColor);
    style.Colors[(int)ImGuiCol.Separator] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBorderColor);
    style.Colors[(int)ImGuiCol.CheckMark] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.AccentColor);
    style.Colors[(int)ImGuiCol.SliderGrab] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.SliderGrabBackColor);
    style.Colors[(int)ImGuiCol.SliderGrabActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.SliderGrabActiveBackColor);
    style.Colors[(int)ImGuiCol.ScrollbarBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ScrollbarTrackBackColor);
    style.Colors[(int)ImGuiCol.ScrollbarGrab] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ScrollbarThumbBackColor);
    style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.SliderGrabBackColor);
    style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.SliderGrabActiveBackColor);
    style.Colors[(int)ImGuiCol.TableHeaderBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ControlBackColor);
    style.Colors[(int)ImGuiCol.TableRowBg] = Vector4.Zero;
    Vector4 rowSel = CalcAppThemeColors.ToVector4(palette, ThemeTokens.RowSelectedBackColor);
    style.Colors[(int)ImGuiCol.TableRowBgAlt] = rowSel with { W = 0.35f };
    style.Colors[(int)ImGuiCol.ModalWindowDimBg] = CalcAppThemeColors.ToVector4(palette, ThemeTokens.ScrimColor);
  }

  public static uint TitleBarBack => ImGuiColor(ThemeTokens.WindowTitleBarBackColor);

  public static uint TitleBarInk => ImGuiColor(ThemeTokens.WindowTitleBarColor);

  public static uint PanelBack => ImGuiColor(ThemeTokens.PanelBackColor);

  public static uint PanelBorder => ImGuiColor(ThemeTokens.PanelBorderColor);

  public static uint WindowBack => ImGuiColor(ThemeTokens.WindowBackColor);

  public static uint CloseHoverBack => ImGuiColor(ThemeTokens.CloseButtonHoverBackColor);

  public static uint TitleButtonHoverFill =>
    Appearance == ThemeAppearance.Light ? 0x22000000u : 0x33FFFFFFu;

  public static uint TitleButtonActiveFill =>
    Appearance == ThemeAppearance.Light ? 0x33000000u : 0x55FFFFFFu;

  private static uint ImGuiColor(string token)
  {
    EnsureInitialized();
    return CalcAppThemeColors.ToImGui(Current, token);
  }
}
