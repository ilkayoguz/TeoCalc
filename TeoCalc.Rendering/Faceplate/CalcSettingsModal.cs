using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Teo.Locale;
using Teo.Settings;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;
using Teo.Theme;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// App Settings modal with SettingsSession: live preview, Default/Cancel/OK.
/// Theme and language persist only on OK (dual-write via CalcUserSettingsStore).
/// </summary>
public static class CalcSettingsModal
{
  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

  private static IntPtr s_openForContext;
  private static string s_saveAsName = "";
  private static string? s_saveAsError;
  private static bool s_showSaveAs;
  private static bool s_open;
  private static SettingsSession<CalcSettingsBag>? s_session;
  private static string s_snapshotProfileId = string.Empty;

  public static bool IsOpen => s_open || s_openForContext != IntPtr.Zero;

  public static void RequestOpen()
  {
    IntPtr ctx = ImGui.GetCurrentContext();
    if (ctx == IntPtr.Zero)
      return;

    s_openForContext = ctx;
  }

  public static void Draw(CalcExplorerSession? session = null)
  {
    CalcAppTheme.EnsureInitialized();

    IntPtr ctx = ImGui.GetCurrentContext();
    if (ctx != IntPtr.Zero && s_openForContext == ctx)
    {
      CalcLocalization.EnsureInitialized();
      ImGui.OpenPopup("##teo-settings");
      s_openForContext = IntPtr.Zero;
      s_open = true;
      s_showSaveAs = false;
      s_saveAsError = null;
      s_snapshotProfileId = CalcSessionProfiles.ActiveProfileId;
      s_session = CreateSession();
      s_session.Open();
    }

    if (!s_open && !ImGui.IsPopupOpen("##teo-settings"))
      return;

    CalcLocalization.EnsureInitialized();

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
        s_session?.Revert();
        RestoreProfile(session);
        s_open = false;
        s_session = null;
      }

      return;
    }

    CalcSettingsBag draft = s_session?.Current ?? new CalcSettingsBag();

    ImGui.TextUnformatted("Language");
    ImGui.Spacing();
    int lang = (int)draft.Language;
    ImGui.RadioButton("System##app_lang", ref lang, (int)LanguagePreference.System);
    ImGui.RadioButton("English##app_lang", ref lang, (int)LanguagePreference.English);
    ImGui.RadioButton("Turkish##app_lang", ref lang, (int)LanguagePreference.Turkish);
    LanguagePreference nextLang = (LanguagePreference)lang;
    if (nextLang != draft.Language)
    {
      draft.Language = nextLang;
      s_session?.Preview(CloneForm(draft));
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    ImGui.TextUnformatted("Appearance");
    ImGui.Spacing();

    int mode = (int)draft.Theme;
    ImGui.RadioButton("System##app_theme", ref mode, (int)AppThemePreference.System);
    ImGui.RadioButton("Light##app_theme", ref mode, (int)AppThemePreference.Light);
    ImGui.RadioButton("Dark##app_theme", ref mode, (int)AppThemePreference.Dark);

    AppThemePreference nextTheme = (AppThemePreference)mode;
    if (nextTheme != draft.Theme)
    {
      draft.Theme = nextTheme;
      s_session?.Preview(CloneForm(draft));
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    DrawSessionProfiles(session);

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    DialogResult footer = ImGuiModalHost.DrawDefaultCancelOkFooter(new Vector2(90f, 0f));
    if (footer is DialogResult.Ok)
    {
      s_session?.Commit();
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (footer is DialogResult.Cancel)
    {
      s_session?.Revert();
      RestoreProfile(session);
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (footer is DialogResult.Default)
    {
      s_session?.ResetToDefaults();
    }

    ImGuiModalHost.End();
    ImGui.PopStyleVar(2);
    s_open = open;
    if (!s_open)
      s_session = null;
  }

  private static void RestoreProfile(CalcExplorerSession? session)
  {
    if (!string.Equals(CalcSessionProfiles.ActiveProfileId, s_snapshotProfileId, StringComparison.Ordinal))
      CalcSessionProfiles.Select(s_snapshotProfileId, session);
  }

  private static SettingsSession<CalcSettingsBag> CreateSession() =>
    new(
      new AdapterStore(),
      createDefaults: static () => new CalcSettingsBag(),
      serialize: static f => JsonSerializer.Serialize(f, JsonOptions),
      deserialize: static json => JsonSerializer.Deserialize<CalcSettingsBag>(json, JsonOptions),
      applyPreview: static form =>
      {
        CalcAppTheme.SetPreference(form.Theme, persist: false);
        CalcLocalization.SetPreference(form.Language, persist: false);
      },
      clone: CloneForm);

  private static CalcSettingsBag CloneForm(CalcSettingsBag form) => new()
  {
    Theme = form.Theme,
    Language = form.Language,
  };

  private sealed class AdapterStore : ISettingsStore
  {
    public string Location => "CalcUserSettingsStore";

    public bool TryLoad(out SettingsBlob blob)
    {
      CalcSettingsBag form = new()
      {
        Theme = CalcUserSettingsStore.LoadAppThemePreference(),
        Language = CalcUserSettingsStore.LoadLanguagePreference(),
      };
      blob = SettingsBlob.Create(JsonSerializer.Serialize(form, JsonOptions));
      return true;
    }

    public void Save(SettingsBlob blob)
    {
      CalcSettingsBag? form = JsonSerializer.Deserialize<CalcSettingsBag>(blob.Json, JsonOptions);
      if (form is null)
        return;
      CalcUserSettingsStore.SaveAppThemePreference(form.Theme);
      CalcUserSettingsStore.SaveLanguagePreference(form.Language);
      CalcAppTheme.SetPreference(form.Theme, persist: false);
      CalcLocalization.SetPreference(form.Language, persist: true);
    }
  }

  private sealed class CalcSettingsBag
  {
    public AppThemePreference Theme { get; set; } = AppThemePreference.System;
    public LanguagePreference Language { get; set; } = LanguagePreference.System;
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
          ImGui.SetItemDefaultFocus();
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
      CalcAppTooltip.Set("Save current speed and feature toggles as a new profile");

    ImGui.Spacing();
    ImGui.TextUnformatted("Features");
    ImGui.Spacing();

    bool controlSpeed = active.ControlExecutionSpeed;
    if (ImGui.Checkbox("Execution Speed##profile_feat_speed", ref controlSpeed))
    {
      CalcSessionProfiles.SetControlExecutionSpeed(controlSpeed);
      if (controlSpeed && session is not null)
        CalcSessionProfiles.ApplyTo(session);
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
        ImGui.TextColored(new Vector4(0.9f, 0.35f, 0.3f, 1f), s_saveAsError);
    }
  }
}
