using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Core.Catalog;

public sealed class MicrocodeMapCatalog
{
  [JsonPropertyName("Model")]
  public string Model { get; init; } = "";

  [JsonPropertyName("WordCount")]
  public int WordCount { get; init; }

  [JsonPropertyName("Entries")]
  public List<MicrocodeMapEntry> Entries { get; init; } = [];

  public static MicrocodeMapCatalog Load(string path)
  {
    string json = File.ReadAllText(path);
    MicrocodeMapCatalog? catalog = JsonSerializer.Deserialize<MicrocodeMapCatalog>(json, JsonOptions);
    return catalog ?? throw new InvalidDataException($"Failed to load {path}");
  }

  public MicrocodeMapEntry? TryGetAddress(int address)
  {
    return Entries.Find(entry => entry.Address == address);
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };
}

public sealed class MicrocodeMapEntry
{
  [JsonPropertyName("Address")]
  public int Address { get; init; }

  [JsonPropertyName("AddressHex")]
  public string AddressHex { get; init; } = "";

  [JsonPropertyName("RomWord")]
  public int RomWord { get; init; }

  [JsonPropertyName("RomWordHex")]
  public string RomWordHex { get; init; } = "";

  [JsonPropertyName("Mnemonic")]
  public string Mnemonic { get; init; } = "";

  [JsonPropertyName("HandlerId")]
  public string HandlerId { get; init; } = "";

  [JsonPropertyName("Title")]
  public string Title { get; init; } = "";

  [JsonPropertyName("PanamatikAlias")]
  public string PanamatikAlias { get; init; } = "";
}
