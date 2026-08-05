using Teo.Surface.Immediate;
using TeoTheme;

namespace TeoCalc.Rendering;

/// <summary>
/// ImGui modal chrome matching TeoCave overlay dialogs — thin wrapper over
/// <see cref="ImGuiDialogStyle"/> with Calc's active <see cref="ThemePalette"/>.
/// </summary>
internal static class CalcAppDialogStyle
{
  public static void PushModal()
  {
    CalcAppTheme.EnsureInitialized();
    ImGuiDialogStyle.PushModal(CalcAppTheme.Current);
  }

  public static void PopModal() => ImGuiDialogStyle.PopModal();

  public static void PushAffirmative()
  {
    CalcAppTheme.EnsureInitialized();
    ImGuiDialogStyle.PushAffirmative(CalcAppTheme.Current);
  }

  public static void PushNeutral()
  {
    CalcAppTheme.EnsureInitialized();
    ImGuiDialogStyle.PushNeutral(CalcAppTheme.Current);
  }

  public static void PushDestructive()
  {
    CalcAppTheme.EnsureInitialized();
    ImGuiDialogStyle.PushDestructive(CalcAppTheme.Current);
  }

  public static void PushDismiss()
  {
    CalcAppTheme.EnsureInitialized();
    ImGuiDialogStyle.PushDismiss(CalcAppTheme.Current);
  }

  public static void PopButton() => ImGuiDialogStyle.PopButton();
}
