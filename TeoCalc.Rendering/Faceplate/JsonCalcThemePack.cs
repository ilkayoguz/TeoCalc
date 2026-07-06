using System.Text.Json;
using System.Text.Json.Serialization;
using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

public static class JsonCalcThemePack
{
  public const string FormatId = "CalcTheme.Pack";
  public const int SchemaVersion = 2;

  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = null,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  };

  public static CalcThemePack LoadFile(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    return Load(File.ReadAllText(path));
  }

  public static CalcThemePack Load(string json)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(json);
    CalcThemePackDocument? document = JsonSerializer.Deserialize<CalcThemePackDocument>(json, Options)
      ?? throw new InvalidOperationException("Calc theme pack JSON is empty.");

    Validate(document);
    return new CalcThemePack
    {
      Id = document.Id,
      DisplayName = document.DisplayName ?? document.Id,
      Palette = ToPalette(document.Palette!),
    };
  }

  private static void Validate(CalcThemePackDocument document)
  {
    if (!string.Equals(document.Format, FormatId, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException($"Unsupported calc theme format '{document.Format}'.");
    }

    if (document.SchemaVersion is not (1 or 2))
    {
      throw new InvalidOperationException($"Unsupported calc theme schema version {document.SchemaVersion}.");
    }

    if (string.IsNullOrWhiteSpace(document.Id))
    {
      throw new InvalidOperationException("Calc theme pack Id is required.");
    }

    if (document.Palette is null)
    {
      throw new InvalidOperationException("Calc theme pack must define Palette.");
    }

    foreach (string token in CalcFaceplateTokens.All)
    {
      if (!document.Palette.ContainsKey(token))
      {
        throw new InvalidOperationException($"Calc theme pack is missing required token '{token}'.");
      }
    }
  }

  private static ThemePalette ToPalette(Dictionary<string, JsonElement> palette)
  {
    Dictionary<string, ThemeColor> colors = new(StringComparer.OrdinalIgnoreCase);
    foreach ((string token, JsonElement value) in palette)
    {
      colors[token] = ThemeColorParser.Parse(value, SchemaVersion, token);
    }

    return new ThemePalette(colors);
  }

  private sealed class CalcThemePackDocument
  {
    public string Format { get; set; } = FormatId;

    public int SchemaVersion { get; set; } = JsonCalcThemePack.SchemaVersion;

    public string Id { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public Dictionary<string, JsonElement>? Palette { get; set; }
  }
}
