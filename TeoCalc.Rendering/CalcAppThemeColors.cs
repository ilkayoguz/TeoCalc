using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering;

/// <summary>Converts TeoTheme colors for ImGui / draw-list use.</summary>
internal static class CalcAppThemeColors
{
  public static Vector4 ToVector4(ThemePalette palette, string token) =>
    ToVector4(palette.Get(token));

  public static Vector4 ToVector4(ThemeColor color) =>
    new(color.R, color.G, color.B, color.A);

  public static uint ToImGui(ThemePalette palette, string token) =>
    ImGui.ColorConvertFloat4ToU32(ToVector4(palette, token));

  public static uint ToImGui(ThemeColor color) =>
    ImGui.ColorConvertFloat4ToU32(ToVector4(color));
}
