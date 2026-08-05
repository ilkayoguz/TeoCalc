using System.Text.Json;
using System.Text.Json.Serialization;
using Teo.Theme;

namespace TeoCalc.Rendering;

/// <summary>Persists TeoCalc user preferences under LocalApplicationData (immediate write).</summary>
public static class CalcUserSettingsStore
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  };

  private static UserSettingsDocument? _cache;

  public static void Initialize() => _cache = LoadFromDisk();

  public static AppThemePreference LoadAppThemePreference() =>
    TryRead(() => ParseAppThemePreference(_cache!.Display.AppTheme), AppThemePreference.System);

  public static void SaveAppThemePreference(AppThemePreference preference) =>
    Update(settings => settings.Display.AppTheme = FormatAppThemePreference(preference));

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

  public static string SettingsPath()
  {
    string directory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TeoCalc");
    Directory.CreateDirectory(directory);
    return Path.Combine(directory, "UserSettings.json");
  }

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
    WriteToDisk(_cache!);
  }

  private static void EnsureCache()
  {
    if (_cache is null)
    {
      Initialize();
    }
  }

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

  private static UserSettingsDocument LoadFromDisk()
  {
    string path = SettingsPath();
    if (!File.Exists(path))
    {
      UserSettingsDocument defaults = new();
      WriteToDisk(defaults);
      return defaults;
    }

    try
    {
      string json = File.ReadAllText(path);
      return JsonSerializer.Deserialize<UserSettingsDocument>(json, JsonOptions) ?? new UserSettingsDocument();
    }
    catch (JsonException)
    {
      return new UserSettingsDocument();
    }
  }

  private static void WriteToDisk(UserSettingsDocument settings)
  {
    string path = SettingsPath();
    string json = JsonSerializer.Serialize(settings, JsonOptions);
    File.WriteAllText(path, json);
  }

  private sealed class UserSettingsDocument
  {
    public DisplaySettings Display { get; set; } = new();

    public SessionSettings Session { get; set; } = new();
  }

  private sealed class DisplaySettings
  {
    public string AppTheme { get; set; } = "System";
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
