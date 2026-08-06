using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Teo.Locale;
using Teo.Settings;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;
using Teo.Theme;
using SilkWindow = Silk.NET.Windowing.Window;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// App Settings in a dedicated OS window (not an ImGui popup inside launcher/T65).
/// Default / Cancel / OK; Cancel restores the open snapshot.
/// </summary>
public static class CalcSettingsModal
{
  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

  private static Host? s_host;
  private static bool s_pendingOpen;
  private static IGLContext? s_shareContext;
  private static CalcExplorerSession? s_boundSession;

  public static bool IsOpen => s_host is { IsClosing: false } || s_pendingOpen;

  /// <summary>Open or focus the Settings OS window. Call from any title-bar gear.</summary>
  public static void RequestOpen(CalcExplorerSession? session = null)
  {
    s_boundSession = session;

    if (s_host is { IsClosing: false })
    {
      s_host.BindSession(session);
      s_host.Focus();
      return;
    }

    s_pendingOpen = true;
  }

  /// <summary>Legacy no-op — Settings no longer opens inside the parent ImGui context.</summary>
  public static void PrepareOpen()
  {
  }

  /// <summary>Legacy no-op — content is drawn by <see cref="Pump"/>.</summary>
  public static void Draw(CalcExplorerSession? session = null)
  {
    if (session is not null)
      s_boundSession = session;
  }

  /// <summary>Create / pump / dispose the Settings OS window from the explorer loop.</summary>
  public static void Pump(IGLContext? shareContext)
  {
    if (shareContext is not null)
      s_shareContext = shareContext;

    if (s_pendingOpen && s_host is null)
    {
      s_pendingOpen = false;
      s_host = Host.Create(s_shareContext, s_boundSession);
      s_host.Initialize();
    }

    if (s_host is null)
      return;

    if (s_host.IsClosing)
    {
      s_host.Dispose();
      s_host = null;
      return;
    }

    s_host.PumpUpdate();
    s_host.PumpRender();
  }

  /// <summary>Process exit — skip GPU teardown stalls.</summary>
  public static void DisposeForAppExit()
  {
    s_pendingOpen = false;
    s_host?.DisposeForAppExit();
    s_host = null;
  }

  private sealed class Host
  {
    private readonly IWindow _window;
    private readonly bool _ownsGl;
    private GL? _gl;
    private IInputContext? _input;
    private ImGuiController? _controller;
    private bool _loaded;
    private bool _disposed;
    private bool _closeRequested;
    private double _lastFrameTime;
    private CalcExplorerSession? _session;
    private SettingsSession<CalcSettingsForm>? _edit;
    private string _snapshotProfileId = string.Empty;
    private string _saveAsName = "";
    private string? _saveAsError;
    private bool _showSaveAs;

    private Host(IWindow window, bool ownsGl, CalcExplorerSession? session)
    {
      _window = window;
      _ownsGl = ownsGl;
      _session = session;
      Wire();
    }

    public bool IsClosing => _closeRequested || _window.IsClosing;

    public static Host Create(IGLContext? sharedContext, CalcExplorerSession? session)
    {
      WindowOptions options = WindowOptions.Default;
      options.Title = CalcUiText.SettingsTitle(CalcLocalization.Preference);
      options.Size = new Vector2D<int>(420, 560);
      options.VSync = true;
      options.WindowBorder = WindowBorder.Fixed;
      if (sharedContext is not null)
        options.SharedContext = sharedContext;

      if (TryCenter(out int x, out int y))
        options.Position = new Vector2D<int>(x, y);

      IWindow native = SilkWindow.Create(options);
      return new Host(native, ownsGl: sharedContext is null, session);
    }

    public void Initialize()
    {
      if (_loaded)
        return;
      _window.Initialize();
    }

    public void BindSession(CalcExplorerSession? session) => _session = session;

    public void Focus()
    {
      if (_window.IsClosing)
        return;
      _window.WindowState = WindowState.Normal;
      _window.Focus();
    }

    public void PumpUpdate()
    {
      if (_window.IsClosing)
        return;
      _window.DoUpdate();
    }

    public void PumpRender()
    {
      if (_window.IsClosing)
        return;
      _window.DoRender();
      if (_closeRequested && !_window.IsClosing)
        _window.Close();
    }

    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      _closeRequested = true;
      TearDownGraphics();
      CloseWindow();
    }

    public void DisposeForAppExit()
    {
      if (_disposed)
        return;
      _disposed = true;
      _closeRequested = true;
      _controller = null;
      _input = null;
      _gl = null;
      CloseWindow();
    }

    private void TearDownGraphics()
    {
      if (_controller is not null)
      {
        try
        {
          _window.MakeCurrent();
          _controller.MakeCurrent();
          CalcFaceplateFonts.UnregisterCurrentContext();
          _controller.Dispose();
        }
        catch
        {
          // Platform may already be tearing down.
        }

        _controller = null;
      }

      _input?.Dispose();
      _input = null;
      if (_ownsGl)
        _gl?.Dispose();
      _gl = null;
    }

    private void CloseWindow()
    {
      if (!_window.IsClosing)
      {
        try
        {
          _window.Close();
        }
        catch
        {
          // ignore
        }
      }

      try
      {
        _window.Dispose();
      }
      catch
      {
        // ignore
      }
    }

    private void Wire()
    {
      _window.Load += () =>
      {
        try
        {
          _gl = _window.CreateOpenGL();
          _input = _window.CreateInput();
          _controller = new ImGuiController(_gl, _window, _input, onConfigureIO: CalcFaceplateFonts.Configure);
          BeginEditSession();
          _loaded = true;
        }
        catch (Exception exception)
        {
          FatalErrorDialog.Show(exception, "TeoCalc — Settings");
          _closeRequested = true;
        }
      };

      _window.Update += _ =>
      {
        double time = _window.Time;
        float delta = _lastFrameTime > 0d ? (float)(time - _lastFrameTime) : 0.016f;
        _lastFrameTime = time;
        _window.MakeCurrent();
        _controller?.Update(delta);
      };

      _window.Render += _ =>
      {
        if (_gl is null || _controller is null)
          return;

        try
        {
          _window.MakeCurrent();
          _gl.Viewport(_window.FramebufferSize);
          _gl.ClearColor(0.12f, 0.12f, 0.14f, 1f);
          _gl.Clear(ClearBufferMask.ColorBufferBit);
          _controller.MakeCurrent();
          CalcAppTheme.ApplyImGuiStyle();
          DrawBody();
          _controller.Render();
        }
        catch (Exception exception)
        {
          FatalErrorDialog.Show(exception, "TeoCalc — Settings");
          _closeRequested = true;
        }
      };

      _window.Closing += () =>
      {
        if (_edit is not null)
        {
          _edit.Revert();
          RestoreProfile();
          _edit = null;
        }
      };
    }

    private void BeginEditSession()
    {
      CalcLocalization.EnsureInitialized();
      CalcAppTheme.EnsureInitialized();
      _snapshotProfileId = CalcSessionProfiles.ActiveProfileId;
      _showSaveAs = false;
      _saveAsError = null;
      _edit = CreateSession();
      _edit.Open();
      _window.Title = CalcUiText.SettingsTitle(_edit.Current.Language);
    }

    private void DrawBody()
    {
      if (_edit is null)
        return;

      LanguagePreference uiLang = _edit.Current.Language;
      Vector2 display = ImGui.GetIO().DisplaySize;
      ImGui.SetNextWindowPos(Vector2.Zero);
      ImGui.SetNextWindowSize(display);
      ImGui.PushFont(CalcFaceplateFonts.Ui);
      ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 16f));
      ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(12f, 12f));
      ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f));
      ImGui.Begin(
        "##teo-settings-body",
        ImGuiWindowFlags.NoTitleBar
          | ImGuiWindowFlags.NoResize
          | ImGuiWindowFlags.NoMove
          | ImGuiWindowFlags.NoCollapse
          | ImGuiWindowFlags.NoSavedSettings);

      CalcSettingsForm draft = _edit.Current;

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
        _edit.Preview(CloneForm(draft));
        uiLang = nextLang;
        _window.Title = CalcUiText.SettingsTitle(uiLang);
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
        _edit.Preview(CloneForm(draft));
      }

      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();
      DrawSessionProfiles(uiLang);

      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();

      Vector2 footerBtn = new(90f, 0f);
      ThemePalette palette = CalcAppTheme.Current;
      bool reset = ImGuiModalHost.Button(palette, DialogButtonRole.Neutral, CalcUiText.Default(uiLang), footerBtn);
      ImGui.SameLine();
      bool cancel = ImGuiModalHost.Button(palette, DialogButtonRole.Neutral, CalcUiText.Cancel(uiLang), footerBtn);
      ImGui.SameLine();
      bool ok = ImGuiModalHost.Button(palette, DialogButtonRole.Affirmative, CalcUiText.Ok(uiLang), footerBtn);

      if (ok)
      {
        _edit.Commit();
        _edit = null;
        _closeRequested = true;
      }
      else if (cancel)
      {
        _edit.Revert();
        RestoreProfile();
        _edit = null;
        _closeRequested = true;
      }
      else if (reset)
      {
        _edit.ResetToDefaults();
        _window.Title = CalcUiText.SettingsTitle(_edit.Current.Language);
      }

      ImGui.End();
      ImGui.PopStyleVar(3);
      ImGui.PopFont();
    }

    private void DrawSessionProfiles(LanguagePreference uiLang)
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
            CalcSessionProfiles.Select(profiles[i].Id, _session);
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
        _showSaveAs = !_showSaveAs;
        _saveAsError = null;
        _saveAsName = active.IsBuiltIn ? $"{active.Name} copy" : active.Name;
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
        if (controlSpeed && _session is not null)
          CalcSessionProfiles.ApplyTo(_session);
      }

      if (controlSpeed)
      {
        ImGui.SameLine();
        string speedText = _session is not null
          ? _session.ExecutionSpeedLabel
          : CalcSessionProfiles.FormatSpeedLabel(active.ExecutionSpeedIndex);
        ImGui.TextDisabled($"({speedText})");
      }

      ImGui.TextDisabled(CalcUiText.SpeedHint(uiLang));

      if (!_showSaveAs)
        return;

      ImGui.Spacing();
      ImGui.SetNextItemWidth(220f);
      ImGui.InputText("##profile_save_name", ref _saveAsName, 64u);
      ImGui.SameLine();
      if (ImGui.Button($"{CalcUiText.Create(uiLang)}##profile_save"))
      {
        if (CalcSessionProfiles.TrySaveAs(_saveAsName, _session, out string? error))
        {
          _showSaveAs = false;
          _saveAsError = null;
        }
        else
        {
          _saveAsError = error;
        }
      }

      ImGuiPointerStyle.MarkLastItemClickable();
      if (!string.IsNullOrEmpty(_saveAsError))
        ImGui.TextColored(new Vector4(0.9f, 0.35f, 0.3f, 1f), _saveAsError);
    }

    private void RestoreProfile()
    {
      if (!string.Equals(CalcSessionProfiles.ActiveProfileId, _snapshotProfileId, StringComparison.Ordinal))
        CalcSessionProfiles.Select(_snapshotProfileId, _session);
    }

    private SettingsSession<CalcSettingsForm> CreateSession() =>
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

    private static bool TryCenter(out int x, out int y)
    {
      x = 120;
      y = 120;
      if (!OperatingSystem.IsWindows())
        return false;

      try
      {
        // Mirror faceplate: SPI work area without Silk monitor enumeration quirks.
        RECT rect = default;
        if (!SystemParametersInfo(SpiGetWorkArea, 0, ref rect, 0))
          return false;
        int workW = rect.Right - rect.Left;
        int workH = rect.Bottom - rect.Top;
        x = rect.Left + Math.Max(0, (workW - 420) / 2);
        y = rect.Top + Math.Max(0, (workH - 560) / 2);
        return workW > 0 && workH > 0;
      }
      catch
      {
        return false;
      }
    }

    private const uint SpiGetWorkArea = 0x0030;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }
  }

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
}
