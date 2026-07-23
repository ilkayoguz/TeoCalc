using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering;

/// <summary>
/// ImGui modal chrome matching TeoCave overlay dialogs:
/// <see cref="ThemeTokens.OverlayPopupBackColor"/> / border, scrim, Dialog* button roles.
/// </summary>
internal static class CalcAppDialogStyle
{
  private const int ModalColorCount = 5;
  private const int ModalVarCount = 4;
  private const int ButtonColorCount = 5;

  public static void PushModal()
  {
    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;
    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
    ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 8f);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2f);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(22f, 18f));
    ImGui.PushStyleColor(ImGuiCol.PopupBg, CalcAppThemeColors.ToVector4(palette, ThemeTokens.OverlayPopupBackColor));
    ImGui.PushStyleColor(ImGuiCol.Border, CalcAppThemeColors.ToVector4(palette, ThemeTokens.OverlayPopupBorderColor));
    ImGui.PushStyleColor(ImGuiCol.Text, CalcAppThemeColors.ToVector4(palette, ThemeTokens.TextColor));
    ImGui.PushStyleColor(ImGuiCol.TextDisabled, CalcAppThemeColors.ToVector4(palette, ThemeTokens.PopupTextDisabledColor));
    ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, CalcAppThemeColors.ToVector4(palette, ThemeTokens.ScrimColor));
  }

  public static void PopModal()
  {
    ImGui.PopStyleColor(ModalColorCount);
    ImGui.PopStyleVar(ModalVarCount);
  }

  /// <summary>OK / Save / Close — TeoCave affirmative role.</summary>
  public static void PushAffirmative() =>
    PushDialogButton(
      ThemeTokens.DialogAffirmativeBackColor,
      ThemeTokens.DialogAffirmativeBorderColor,
      ThemeTokens.DialogAffirmativeColor);

  /// <summary>Cancel / secondary.</summary>
  public static void PushNeutral() =>
    PushDialogButton(
      ThemeTokens.DialogNeutralBackColor,
      ThemeTokens.DialogNeutralBorderColor,
      ThemeTokens.DialogNeutralColor);

  /// <summary>Don't Save / destructive confirm.</summary>
  public static void PushDestructive() =>
    PushDialogButton(
      ThemeTokens.DialogDestructiveBackColor,
      ThemeTokens.DialogDestructiveBorderColor,
      ThemeTokens.DialogDestructiveColor);

  /// <summary>Dismiss / soft warning action.</summary>
  public static void PushDismiss() =>
    PushDialogButton(
      ThemeTokens.DialogDismissBackColor,
      ThemeTokens.DialogDismissBorderColor,
      ThemeTokens.DialogDismissColor);

  public static void PopButton()
  {
    ImGui.PopStyleColor(ButtonColorCount);
    ImGui.PopStyleVar(1);
  }

  private static void PushDialogButton(string backToken, string borderToken, string inkToken)
  {
    CalcAppTheme.EnsureInitialized();
    ThemePalette palette = CalcAppTheme.Current;
    Vector4 back = CalcAppThemeColors.ToVector4(palette, backToken);
    Vector4 hover = new(
      MathF.Min(1f, back.X * 1.04f + 0.02f),
      MathF.Min(1f, back.Y * 1.04f + 0.02f),
      MathF.Min(1f, back.Z * 1.04f + 0.02f),
      back.W);
    Vector4 active = new(back.X * 0.92f, back.Y * 0.92f, back.Z * 0.92f, back.W);
    ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.5f);
    ImGui.PushStyleColor(ImGuiCol.Button, back);
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hover);
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, active);
    ImGui.PushStyleColor(ImGuiCol.Border, CalcAppThemeColors.ToVector4(palette, borderToken));
    ImGui.PushStyleColor(ImGuiCol.Text, CalcAppThemeColors.ToVector4(palette, inkToken));
  }
}
