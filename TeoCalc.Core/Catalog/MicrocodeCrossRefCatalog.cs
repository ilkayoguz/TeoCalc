using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Core.Catalog;

public sealed class MicrocodeCrossRefCatalog
{
  [JsonPropertyName("Handlers")]
  public List<MicrocodeCrossRefEntry> Handlers { get; init; } = [];

  public static MicrocodeCrossRefCatalog Load(string path)
  {
    string json = File.ReadAllText(path);
    MicrocodeCrossRefCatalog? catalog = JsonSerializer.Deserialize<MicrocodeCrossRefCatalog>(json, JsonOptions);
    return catalog ?? throw new InvalidDataException($"Failed to load {path}");
  }

  public MicrocodeCrossRefEntry? TryGetHandler(string handlerId)
  {
    return Handlers.Find(entry => entry.HandlerId == handlerId);
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };
}

public sealed class MicrocodeCrossRefEntry
{
  [JsonPropertyName("HandlerId")]
  public string HandlerId { get; init; } = "";

  [JsonPropertyName("NonpareilMnemonic")]
  public string NonpareilMnemonic { get; init; } = "";

  [JsonPropertyName("PatentTerm")]
  public string PatentTerm { get; init; } = "";

  [JsonPropertyName("Verified")]
  public JsonElement Verified { get; init; }
}
