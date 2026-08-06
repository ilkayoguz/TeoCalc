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
/// App Settings as a true <see cref="ImGui.BeginPopupModal"/> (Default/Cancel/OK).
/// OpenPopup is owned by a fullscreen capture host so the modal stays above
/// NoBringToFrontOnFocus calculator/launcher windows.
/// </summary>
public static class CalcSettingsModal
{
  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

  private static string s_saveAsName = "";
  private static string? s_saveAsError;
  private static bool s_showSaveAs;
  private static bool s_open;
  private static bool s_pendingOpen;
  private static bool s_openPopup;
  private static SettingsSession<CalcSettingsForm>? s_session;
  private static string s_snapshotProfileId = string.Empty;
  private static CalcExplorerSession? s_drawSession;

  public static bool IsOpen => s_open || s_pendingOpen;

  public static void RequestOpen() => s_pendingOpen = true;

  /// <summary>Apply a pending open while still inside the host frame (before End).</summary>
  public static void PrepareOpen()
  {
    if (!s_pendingOpen || s_open)
      return;

    CalcLocalization.EnsureInitialized();
    s_pendingOpen = false;
    s_open = true;
    s_openPopup = true;
    s_showSaveAs = false;
    s_saveAsError = null;
    s_snapshotProfileId = CalcSessionProfiles.ActiveProfileId;
    s_session = CreateSession();
    s_session.Open();
  }

  /// <summary>Call after the fullscreen host End. Owns capture + BeginPopupModal.</summary>
  public static void Draw(CalcExplorerSession? session = null)
  {
    s_drawSession = session;
    CalcAppTheme.EnsureInitialized();
    PrepareOpen();

    if (!s_open && !s_openPopup && !ImGui.IsPopupOpen("##teo-settings"))
      return;

    CalcLocalization.EnsureInitialized();
    LanguagePreference uiLang = s_session?.Current.Language ?? CalcLocalization.Preference;
    Vector2 display = ImGui.GetIO().DisplaySize;

    // Parent window for OpenPopup / BeginPopupModal (must share ID stack).
    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(display);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.45f));
    ImGui.Begin(
      "##teo-settings-modal-host",
      ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoNavInputs
        | ImGuiWindowFlags.NoBringToFrontOnFocus);
    ImGui.InvisibleButton("##teo-settings-scrim", display);

    if (s_openPopup)
    {
      ImGui.OpenPopup("##teo-settings");
      s_openPopup = false;
    }

    ImGui.PushFont(CalcFaceplateFonts.Ui);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(12f, 12f));
    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f));

    bool open = s_open || ImGui.IsPopupOpen("##teo-settings");
    if (!ImGuiModalHost.Begin(
          "##teo-settings",
          CalcUiText.SettingsTitle(uiLang),
          CalcAppTheme.Current,
          ref open,
          minContentWidth: 380f))
    {
      ImGui.PopStyleVar(2);
      ImGui.PopFont();
      ImGui.End();
      ImGui.PopStyleColor();
      ImGui.PopStyleVar(2);
      if (!open && s_open)
        Dismiss(commit: false, session);
      return;
    }

    CalcSettingsForm draft = s_session?.Current ?? new CalcSettingsForm();

    ImGui.TextUnformatted(CalcUiText.Language(uiLang));
    ImGui.Spacing();
    int lang = (int)draft.Language;
    ImGui.RadioButton($"{CalcUiText.LanguageSystem(uiLang)}##app_lang", ref lang, (int)LanguagePreference.System);
    ImGui.RadioButton($"{CalcUiText.LanguageEnglish(uiLang)}##app_lang", ref lang, (int)LanguagePreference.English);
    ImGui.RadioButton($"{CalcUiText.LanguageTurkish(uiLang)}##app_lang", ref lang, (int)LanguagePreference.Turkish);
    LanguagePreference nextLang = (LanguagePreference)lang;
    if (nextLang != draft.Language)
    {
      draft.Language = nextLang;
      s_session?.Preview(CloneForm(draft));
      uiLang = nextLang;
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    ImGui.TextUnformatted(CalcUiText.Appearance(uiLang));
    ImGui.Spacing();

    int mode = (int)draft.Theme;
    ImGui.RadioButton($"{CalcUiText.ThemeSystem(uiLang)}##app_theme", ref mode, (int)AppThemePreference.System);
    ImGui.RadioButton($"{CalcUiText.ThemeLight(uiLang)}##app_theme", ref mode, (int)AppThemePreference.Light);
    ImGui.RadioButton($"{CalcUiText.ThemeDark(uiLang)}##app_theme", ref mode, (int)AppThemePreference.Dark);

    AppThemePreference nextTheme = (AppThemePreference)mode;
    if (nextTheme != draft.Theme)
    {
      draft.Theme = nextTheme;
      s_session?.Preview(CloneForm(draft));
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    DrawSessionProfiles(session, uiLang);

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.Spacing();

    Vector2 footerBtn = new(90f, 0f);
    bool reset = ImGuiModalHost.Button(
      DialogButtonRole.Neutral,
      CalcUiText.Default(uiLang),
      footerBtn);
    ImGui.SameLine();
    bool cancel = ImGuiModalHost.Button(
      DialogButtonRole.Neutral,
      CalcUiText.Cancel(uiLang),
      footerBtn);
    ImGui.SameLine();
    bool ok = ImGuiModalHost.Button(
      DialogButtonRole.Affirmative,
      CalcUiText.Ok(uiLang),
      footerBtn);

    if (ok)
    {
      Dismiss(commit: true, session);
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (cancel)
    {
      Dismiss(commit: false, session);
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (reset)
    {
      s_session?.ResetToDefaults();
    }

    ImGuiModalHost.End();
    ImGui.PopStyleVar(2);
    ImGui.PopFont();
    ImGui.End();
    ImGui.PopStyleColor();
    ImGui.PopStyleVar(2);

    s_open = open;
    if (!s_open)
      s_session = null;
  }

  private static void Dismiss(bool commit, CalcExplorerSession? session)
  {
    if (commit)
      s_session?.Commit();
    else
    {
      s_session?.Revert();
      RestoreProfile(session);
    }

    s_open = false;
    s_pendingOpen = false;
    s_openPopup = false;
    s_session = null;
  }

  private static void RestoreProfile(CalcExplorerSession? session)
  {
    session ??= s_drawSession;
    if (!string.Equals(CalcSessionProfiles.ActiveProfileId, s_snapshotProfileId, StringComparison.Ordinal))
      CalcSessionProfiles.Select(s_snapshotProfileId, session);
  }

  private static SettingsSession<CalcSettingsForm> CreateSession() =>
    new(
      new AdapterStore(),
      createDefaults: static () => new CalcSettingsForm(),
      serialize: static f => JsonSerializer.Serialize(f, JsonOptions),
      deserialize: static json => JsonSerializer.Deserialize<CalcSettingsForm>(json, JsonOptions),
      applyPreview: static form =>
      {
        CalcAppTheme.SetPreference(form.Theme, persist: false);
        CalcLocalization.SetPreference(form.Language, persist: false);
      },
      clone: CloneForm);

  private static CalcSettingsForm CloneForm(CalcSettingsForm form) => new()
  {
    Theme = form.Theme,
    Language = form.Language,
  };

  private sealed class AdapterStore : ISettingsStore
  {
    public string Location => "CalcUserSettingsStore";

    public bool TryLoad(out SettingsBlob blob)
    {
      CalcSettingsForm form = new()
      {
        Theme = CalcUserSettingsStore.LoadAppThemePreference(),
        Language = CalcUserSettingsStore.LoadLanguagePreference(),
      };
      blob = SettingsBlob.Create(JsonSerializer.Serialize(form, JsonOptions));
      return true;
    }

    public void Save(SettingsBlob blob)
    {
      CalcSettingsForm? form = JsonSerializer.Deserialize<CalcSettingsForm>(blob.Json, JsonOptions);
      if (form is null)
        return;
      CalcUserSettingsStore.SaveAppThemePreference(form.Theme);
      CalcUserSettingsStore.SaveLanguagePreference(form.Language);
      CalcAppTheme.SetPreference(form.Theme, persist: false);
      CalcLocalization.SetPreference(form.Language, persist: true);
    }
  }

  private sealed class CalcSettingsForm
  {
    public AppThemePreference Theme { get; set; } = AppThemePreference.System;
    public LanguagePreference Language { get; set; } = LanguagePreference.System;
  }

  private static void DrawSessionProfiles(CalcExplorerSession? session, LanguagePreference uiLang)
  {
    ImGui.TextUnformatted(CalcUiText.SessionProfile(uiLang));
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

    ImGui.SetNextItemWidth(220f);
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
    if (ImGui.Button($"{CalcUiText.SaveAs(uiLang)}##profile"))
    {
      s_showSaveAs = !s_showSaveAs;
      s_saveAsError = null;
      s_saveAsName = active.IsBuiltIn ? $"{active.Name} copy" : active.Name;
    }

    ImGuiPointerStyle.MarkLastItemClickable();
    if (ImGui.IsItemHovered())
      CalcAppTooltip.Set(CalcUiText.SaveAsTooltip(uiLang));

    ImGui.Spacing();
    ImGui.TextUnformatted(CalcUiText.Features(uiLang));
    ImGui.Spacing();

    bool controlSpeed = active.ControlExecutionSpeed;
    if (ImGui.Checkbox($"{CalcUiText.ExecutionSpeed(uiLang)}##profile_feat_speed", ref controlSpeed))
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

    ImGui.TextDisabled(CalcUiText.SpeedHint(uiLang));

    if (s_showSaveAs)
    {
      ImGui.Spacing();
      ImGui.SetNextItemWidth(220f);
      ImGui.InputText("##profile_save_name", ref s_saveAsName, 64u);
      ImGui.SameLine();
      if (ImGui.Button($"{CalcUiText.Create(uiLang)}##profile_save"))
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
