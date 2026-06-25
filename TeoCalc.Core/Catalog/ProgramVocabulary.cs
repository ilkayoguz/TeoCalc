using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Core.Catalog;

public sealed class ProgramVocabulary
{
  [JsonPropertyName("Model")]
  public string Model { get; init; } = "";

  [JsonPropertyName("Steps")]
  public List<ProgramStepEntry> Steps { get; init; } = [];

  [JsonPropertyName("KeyChart")]
  public List<ProgramKeyEntry> KeyChart { get; init; } = [];

  public static ProgramVocabulary Load(string path)
  {
    string json = File.ReadAllText(path);
    ProgramVocabulary? vocabulary = JsonSerializer.Deserialize<ProgramVocabulary>(json, JsonOptions);
    return vocabulary ?? throw new InvalidDataException($"Failed to load {path}");
  }

  public ProgramStepEntry ResolveCode(int code)
  {
    ProgramStepEntry? step = Steps.Find(s => s.Code == code);
    return step ?? throw new KeyNotFoundException($"Program code {code}");
  }

  public ProgramStepEntry? TryResolveMnemonic(string mnemonic)
  {
    return Steps.Find(s => string.Equals(s.Mnemonic, mnemonic, StringComparison.OrdinalIgnoreCase));
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };
}

public sealed class ProgramStepEntry
{
  [JsonPropertyName("Code")]
  public int Code { get; init; }

  [JsonPropertyName("Mnemonic")]
  public string Mnemonic { get; init; } = "";

  [JsonPropertyName("StepId")]
  public string StepId { get; init; } = "";

  [JsonPropertyName("Title")]
  public string Title { get; init; } = "";
}

public sealed class ProgramKeyEntry
{
  [JsonPropertyName("Index")]
  public int Index { get; init; }

  [JsonPropertyName("Char")]
  public string Char { get; init; } = "";

  [JsonPropertyName("KeyCode")]
  public int KeyCode { get; init; }
}
