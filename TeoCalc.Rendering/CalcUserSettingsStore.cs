using System.Text.Json;
using System.Text.Json.Serialization;
using Teo.Locale;
using Teo.Settings;
using Teo.Theme;

namespace TeoCalc.Rendering;

/// <summary>Persists TeoCalc preferences via Teo.Settings dual-write (SiDE leaf).</summary>
public static class CalcUserSettingsStore
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  };

  private static UserSettingsDocument? _cache;
  private static ISettingsStore? _store;

  public static void Initialize()
  {
    MigrateFromLegacyIfNeeded();
    _store = CreateStore();
    _cache = LoadFromStore();
  }

  public static AppThemePreference LoadAppThemePreference() =>
    TryRead(() => ParseAppThemePreference(_cache!.Display.AppTheme), AppThemePreference.System);

  public static void SaveAppThemePreference(AppThemePreference preference) =>
    Update(settings => settings.Display.AppTheme = FormatAppThemePreference(preference));

  public static LanguagePreference LoadLanguagePreference() =>
    TryRead(
      () => TextKeyword.ParsePreference(_cache!.Display.LanguageCode),
      LanguagePreference.System);

  public static void SaveLanguagePreference(LanguagePreference preference) =>
    Update(settings => settings.Display.LanguageCode = TextKeyword.FormatPreference(preference));

  public static string LoadActiveSessionProfileId() =>
    TryRead(
      () => string.IsNullOrWhiteSpace(_cache!.Session.ActiveProfileId)
        ? CalcSessionProfile.StandardId
        : _cache.Session.ActiveProfileId.Trim(),
      CalcSessionProfile.StandardId);

  public static void SaveActiveSessionProfileId(string profileId) =>
    Update(settings => settings.Session.ActiveProfileId = profileId);

  public static IReadOnlyList<CalcSessionProfile> LoadCustomSessionProfiles() =>
    TryRead(
      () => (IReadOnlyList<CalcSessionProfile>)_cache!.Session.CustomProfiles
        .Select(FromDto)
        .Where(p => !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name))
        .ToList(),
      Array.Empty<CalcSessionProfile>());

  public static void SaveCustomSessionProfiles(IEnumerable<CalcSessionProfile> profiles) =>
    Update(settings =>
    {
      settings.Session.CustomProfiles =
      [
        .. profiles
          .Where(p => !p.IsBuiltIn)
          .Select(ToDto),
      ];
    });

  public static string SettingsPath() =>
    SettingsPathGate.FilePath("TeoCalc", SettingsPathGate.TryResolveCoExVersion("TeoCalc"));

  public static string LegacySettingsPath() =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TeoCalc",
      "UserSettings.json");

  internal static AppThemePreference ParseAppThemePreference(string? value) =>
    value?.Trim() switch
    {
      "Light" => AppThemePreference.Light,
      "Dark" => AppThemePreference.Dark,
      _ => AppThemePreference.System,
    };

  internal static string FormatAppThemePreference(AppThemePreference preference) => preference switch
  {
    AppThemePreference.Light => "Light",
    AppThemePreference.Dark => "Dark",
    _ => "System",
  };

  private static CalcSessionProfile FromDto(SessionProfileDto dto) =>
    new()
    {
      Id = dto.Id?.Trim() ?? "",
      Name = dto.Name?.Trim() ?? "",
      ExecutionSpeedIndex = Math.Clamp(dto.ExecutionSpeedIndex, 0, CalcExplorerSession.ExecutionSpeedStepCount - 1),
      ControlExecutionSpeed = dto.ControlExecutionSpeed,
      IsBuiltIn = false,
    };

  private static SessionProfileDto ToDto(CalcSessionProfile profile) =>
    new()
    {
      Id = profile.Id,
      Name = profile.Name,
      ExecutionSpeedIndex = profile.ExecutionSpeedIndex,
      ControlExecutionSpeed = profile.ControlExecutionSpeed,
    };

  private static void Update(Action<UserSettingsDocument> edit)
  {
    EnsureCache();
    edit(_cache!);
    WriteToStore(_cache!);
  }

  private static void EnsureCache()
  {
    if (_cache is null)
      Initialize();
  }

  private static void EnsureStore() => _store ??= CreateStore();

  private static ISettingsStore CreateStore() =>
    SettingsPathGate.CreateDualStore("TeoCalc", SettingsPathGate.TryResolveCoExVersion("TeoCalc"));

  private static T TryRead<T>(Func<T> read, T fallback)
  {
    try
    {
      EnsureCache();
      return read();
    }
    catch (IOException)
    {
      return fallback;
    }
    catch (JsonException)
    {
      return fallback;
    }
  }

  private static UserSettingsDocument LoadFromStore()
  {
    EnsureStore();
    if (_store!.TryLoad(out SettingsBlob blob))
    {
      try
      {
        return JsonSerializer.Deserialize<UserSettingsDocument>(blob.Json, JsonOptions)
          ?? new UserSettingsDocument();
      }
      catch (JsonException)
      {
        // fall through
      }
    }

    UserSettingsDocument defaults = new();
    WriteToStore(defaults);
    return defaults;
  }

  private static void WriteToStore(UserSettingsDocument settings)
  {
    EnsureStore();
    string json = JsonSerializer.Serialize(settings, JsonOptions);
    _store!.Save(SettingsBlob.Create(json));
  }

  private static void MigrateFromLegacyIfNeeded()
  {
    string dualPath = SettingsPath();
    if (File.Exists(dualPath))
      return;

    string legacy = LegacySettingsPath();
    if (!File.Exists(legacy))
      return;

    try
    {
      string json = File.ReadAllText(legacy);
      UserSettingsDocument? doc = JsonSerializer.Deserialize<UserSettingsDocument>(json, JsonOptions);
      if (doc is null)
        return;
      CreateStore().Save(SettingsBlob.Create(JsonSerializer.Serialize(doc, JsonOptions)));
    }
    catch
    {
      // ignore
    }
  }

  private sealed class UserSettingsDocument
  {
    public DisplaySettings Display { get; set; } = new();

    public SessionSettings Session { get; set; } = new();
  }

  private sealed class DisplaySettings
  {
    public string AppTheme { get; set; } = "System";

    public string LanguageCode { get; set; } = "System";
  }

  private sealed class SessionSettings
  {
    public string ActiveProfileId { get; set; } = CalcSessionProfile.StandardId;

    public List<SessionProfileDto> CustomProfiles { get; set; } = [];
  }

  private sealed class SessionProfileDto
  {
    public string? Id { get; set; }

    public string? Name { get; set; }

    public int ExecutionSpeedIndex { get; set; } = 2;

    public bool ControlExecutionSpeed { get; set; } = true;
  }
}
