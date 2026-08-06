using System.Numerics;
using ImGuiNET;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;
using Teo.Theme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// App Settings modal. Opens in the ImGui context that requested it
/// (launcher or a calculator host) so the dialog stays on that window.
/// OK keeps live edits; Cancel/X/ESC restores the snapshot taken on open.
/// </summary>
public static class CalcSettingsModal
{
  private static IntPtr s_openForContext;
  private static string s_saveAsName = "";
  private static string? s_saveAsError;
  private static bool s_showSaveAs;
  private static bool s_open;
  private static AppThemePreference s_snapshotPreference;
  private static string s_snapshotProfileId = string.Empty;

  public static bool IsOpen => s_open || s_openForContext != IntPtr.Zero;

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
      s_open = true;
      s_showSaveAs = false;
      s_saveAsError = null;
      s_snapshotPreference = CalcAppTheme.Preference;
      s_snapshotProfileId = CalcSessionProfiles.ActiveProfileId;
    }

    if (!s_open && !ImGui.IsPopupOpen("##teo-settings"))
    {
      return;
    }

    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(12f, 12f));
    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f));

    bool open = s_open || ImGui.IsPopupOpen("##teo-settings");
    if (!ImGuiModalHost.Begin(
          "##teo-settings",
          DialogStyles.SettingsTitle,
          CalcAppTheme.Current,
          ref open,
          minContentWidth: 340f))
    {
      ImGui.PopStyleVar(2);
      if (!open && s_open)
      {
        RestoreSnapshot(session);
        s_open = false;
      }

      return;
    }

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

    bool apply = ImGuiModalHost.OkButton(new Vector2(90f, 0f));
    ImGui.SameLine();
    bool cancel = ImGuiModalHost.CancelButton(new Vector2(90f, 0f));

    if (apply)
    {
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (cancel)
    {
      RestoreSnapshot(session);
      open = false;
      ImGui.CloseCurrentPopup();
    }

    ImGuiModalHost.End();
    ImGui.PopStyleVar(2);
    s_open = open;
  }

  private static void RestoreSnapshot(CalcExplorerSession? session)
  {
    if (CalcAppTheme.Preference != s_snapshotPreference)
    {
      CalcAppTheme.SetPreference(s_snapshotPreference);
    }

    if (!string.Equals(CalcSessionProfiles.ActiveProfileId, s_snapshotProfileId, StringComparison.Ordinal))
    {
      CalcSessionProfiles.Select(s_snapshotProfileId, session);
    }
  }

  private static void DrawSessionProfiles(CalcExplorerSession? session)
  {
    ImGui.TextUnformatted("Session Profile");
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
    if (ImGui.Button("Save As##profile"))
    {
      s_showSaveAs = !s_showSaveAs;
      s_saveAsError = null;
      s_saveAsName = active.IsBuiltIn ? $"{active.Name} copy" : active.Name;
    }

    ImGuiPointerStyle.MarkLastItemClickable();
    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("Save current speed and feature toggles as a new profile");
    }

    ImGui.Spacing();
    ImGui.TextUnformatted("Features");
    ImGui.Spacing();

    bool controlSpeed = active.ControlExecutionSpeed;
    if (ImGui.Checkbox("Execution Speed##profile_feat_speed", ref controlSpeed))
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

      ImGuiPointerStyle.MarkLastItemClickable();

      if (!string.IsNullOrEmpty(s_saveAsError))
      {
        ImGui.TextColored(new Vector4(0.9f, 0.35f, 0.3f, 1f), s_saveAsError);
      }
    }
  }
}
