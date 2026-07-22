using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// TeoCave Dark control chrome for Studio toolbar buttons (opaque fills + border).
/// Values match <c>TeoTheme.json</c> Dark <c>Control*</c> / <c>PrimaryButton*</c>.
/// </summary>
internal static class CalcStudioChromeStyle
{
  // TeoTheme Dark Control*
  private static readonly Vector4 ControlBack = HexRgb(0x3B3B3B);
  private static readonly Vector4 ControlHover = HexRgb(0x444444);
  private static readonly Vector4 ControlActive = HexRgb(0x4C5C70);
  private static readonly Vector4 ControlBorder = HexRgb(0x767676);

  // TeoTheme Dark PrimaryButton*
  private static readonly Vector4 PrimaryBack = HexRgb(0x0067C0);
  private static readonly Vector4 PrimaryHover = HexRgb(0x1D78C8);
  private static readonly Vector4 PrimaryActive = HexRgb(0x0052A0);

  private const int ToolbarColorCount = 4;
  private const int PrimaryColorCount = 3;

  public static void PushToolbar()
  {
    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
    ImGui.PushStyleColor(ImGuiCol.Button, ControlBack);
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ControlHover);
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ControlActive);
    ImGui.PushStyleColor(ImGuiCol.Border, ControlBorder);
  }

  public static void PopToolbar()
  {
    ImGui.PopStyleColor(ToolbarColorCount);
    ImGui.PopStyleVar(2);
  }

  public static void PushPrimary()
  {
    ImGui.PushStyleColor(ImGuiCol.Button, PrimaryBack);
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PrimaryHover);
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, PrimaryActive);
  }

  public static void PopPrimary() => ImGui.PopStyleColor(PrimaryColorCount);

  private static Vector4 HexRgb(uint rgb) =>
    new(
      ((rgb >> 16) & 0xFF) / 255f,
      ((rgb >> 8) & 0xFF) / 255f,
      (rgb & 0xFF) / 255f,
      1f);
}
