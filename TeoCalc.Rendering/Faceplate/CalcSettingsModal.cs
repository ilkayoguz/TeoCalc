using System.Numerics;
using ImGuiNET;
using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// App Settings modal. Opens in the ImGui context that requested it
/// (launcher or a calculator host) so the dialog stays on that window.
/// </summary>
public static class CalcSettingsModal
{
  private static IntPtr s_openForContext;

  /// <summary>Queue open for the current ImGui context (call while that window is current).</summary>
  public static void RequestOpen()
  {
    IntPtr ctx = ImGui.GetCurrentContext();
    if (ctx == IntPtr.Zero)
    {
      return;
    }

    s_openForContext = ctx;
  }

  public static void Draw()
  {
    CalcAppTheme.EnsureInitialized();

    IntPtr ctx = ImGui.GetCurrentContext();
    if (ctx != IntPtr.Zero && s_openForContext == ctx)
    {
      ImGui.OpenPopup("##teo-settings");
      s_openForContext = IntPtr.Zero;
    }

    CalcAppDialogStyle.PushModal();
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(12f, 12f));
    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f));

    bool open = true;
    if (!ImGui.BeginPopupModal(
          "##teo-settings",
          ref open,
          ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
    {
      ImGui.PopStyleVar(2);
      CalcAppDialogStyle.PopModal();
      return;
    }

    // Keep the dialog from collapsing into a tight strip.
    ImGui.Dummy(new Vector2(300f, 0f));

    ImGui.TextUnformatted("Settings");
    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    ImGui.TextUnformatted("Appearance");
    ImGui.Spacing();

    AppThemePreference current = CalcAppTheme.Preference;
    int mode = (int)current;
    ImGui.RadioButton("System##app_theme", ref mode, (int)AppThemePreference.System);
    ImGui.RadioButton("Light##app_theme", ref mode, (int)AppThemePreference.Light);
    ImGui.RadioButton("Dark##app_theme", ref mode, (int)AppThemePreference.Dark);

    AppThemePreference next = (AppThemePreference)mode;
    if (next != current)
    {
      CalcAppTheme.SetPreference(next);
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();
    CalcAppDialogStyle.PushAffirmative();
    if (ImGui.Button("Close", new Vector2(140f, 0f)))
    {
      ImGui.CloseCurrentPopup();
    }

    CalcAppDialogStyle.PopButton();

    ImGui.EndPopup();
    ImGui.PopStyleVar(2);
    CalcAppDialogStyle.PopModal();
  }
}
