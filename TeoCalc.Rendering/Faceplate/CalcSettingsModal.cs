using System.Numerics;
using ImGuiNET;
using Teo.Theme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// App Settings modal. Opens in the ImGui context that requested it
/// (launcher or a calculator host) so the dialog stays on that window.
/// </summary>
public static class CalcSettingsModal
{
  private static IntPtr s_openForContext;
  private static string s_saveAsName = "";
  private static string? s_saveAsError;
  private static bool s_showSaveAs;

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

  public static void Draw(CalcExplorerSession? session = null)
  {
    CalcAppTheme.EnsureInitialized();

    IntPtr ctx = ImGui.GetCurrentContext();
    if (ctx != IntPtr.Zero && s_openForContext == ctx)
    {
      ImGui.OpenPopup("##teo-settings");
      s_openForContext = IntPtr.Zero;
      s_showSaveAs = false;
      s_saveAsError = null;
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
    ImGui.Dummy(new Vector2(340f, 0f));

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

    DrawSessionProfiles(session);

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

  private static void DrawSessionProfiles(CalcExplorerSession? session)
  {
    ImGui.TextUnformatted("Session profile");
    ImGui.Spacing();

    IReadOnlyList<CalcSessionProfile> profiles = CalcSessionProfiles.List();
    CalcSessionProfile active = CalcSessionProfiles.Active;
    int selected = 0;
    for (int i = 0; i < profiles.Count; i++)
    {
      if (string.Equals(profiles[i].Id, CalcSessionProfiles.ActiveProfileId, StringComparison.Ordinal))
      {
        selected = i;
        break;
      }
    }

    ImGui.SetNextItemWidth(200f);
    if (ImGui.BeginCombo("##session_profile", active.Name))
    {
      for (int i = 0; i < profiles.Count; i++)
      {
        bool isSelected = i == selected;
        if (ImGui.Selectable(profiles[i].Name, isSelected))
        {
          CalcSessionProfiles.Select(profiles[i].Id, session);
          active = CalcSessionProfiles.Active;
        }

        if (isSelected)
        {
          ImGui.SetItemDefaultFocus();
        }
      }

      ImGui.EndCombo();
    }

    ImGui.SameLine();
    if (ImGui.Button("Save as##profile"))
    {
      s_showSaveAs = !s_showSaveAs;
      s_saveAsError = null;
      s_saveAsName = active.IsBuiltIn ? $"{active.Name} copy" : active.Name;
    }

    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("Save current speed and feature toggles as a new profile");
    }

    ImGui.Spacing();
    ImGui.TextUnformatted("Features");
    ImGui.Spacing();

    bool controlSpeed = active.ControlExecutionSpeed;
    if (ImGui.Checkbox("Execution speed##profile_feat_speed", ref controlSpeed))
    {
      CalcSessionProfiles.SetControlExecutionSpeed(controlSpeed);
      if (controlSpeed && session is not null)
      {
        CalcSessionProfiles.ApplyTo(session);
      }
    }

    if (controlSpeed)
    {
      ImGui.SameLine();
      string speedText = session is not null
        ? session.ExecutionSpeedLabel
        : CalcSessionProfiles.FormatSpeedLabel(active.ExecutionSpeedIndex);
      ImGui.TextDisabled($"({speedText})");
    }

    ImGui.TextDisabled("Speed toggles apply when a calculator session is open.");

    if (s_showSaveAs)
    {
      ImGui.Spacing();
      ImGui.SetNextItemWidth(200f);
      ImGui.InputText("##profile_save_name", ref s_saveAsName, 64u);
      ImGui.SameLine();
      if (ImGui.Button("Create##profile_save"))
      {
        if (CalcSessionProfiles.TrySaveAs(s_saveAsName, session, out string? error))
        {
          s_showSaveAs = false;
          s_saveAsError = null;
        }
        else
        {
          s_saveAsError = error;
        }
      }

      if (!string.IsNullOrEmpty(s_saveAsError))
      {
        ImGui.TextColored(new Vector4(0.9f, 0.35f, 0.3f, 1f), s_saveAsError);
      }
    }
  }
}
