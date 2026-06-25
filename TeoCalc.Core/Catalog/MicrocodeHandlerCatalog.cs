using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Core.Catalog;

public sealed class MicrocodeHandlerCatalog
{
  [JsonPropertyName("CpuFamily")]
  public string CpuFamily { get; init; } = "";

  [JsonPropertyName("Handlers")]
  public List<MicrocodeHandlerEntry> Handlers { get; init; } = [];

  public static MicrocodeHandlerCatalog Load(string path)
  {
    string json = File.ReadAllText(path);
    MicrocodeHandlerCatalog? catalog = JsonSerializer.Deserialize<MicrocodeHandlerCatalog>(json, JsonOptions);
    return catalog ?? throw new InvalidDataException($"Failed to load {path}");
  }

  public MicrocodeHandlerEntry ResolveByPanamatikAlias(string alias)
  {
    MicrocodeHandlerEntry? entry = Handlers.Find(h => h.PanamatikAlias == alias);
    return entry ?? Handlers.Find(h => h.PanamatikAlias == "op_unknown")
      ?? throw new KeyNotFoundException(alias);
  }

  public MicrocodeHandlerEntry ResolveByDispatchIndex(int index, IReadOnlyDictionary<int, string> dispatchTable)
  {
    string alias = dispatchTable.TryGetValue(index, out string? name) ? name : "op_unknown";
    return ResolveByPanamatikAlias(alias);
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };
}

public sealed class MicrocodeHandlerEntry
{
  [JsonPropertyName("HandlerId")]
  public string HandlerId { get; init; } = "";

  [JsonPropertyName("Mnemonic")]
  public string Mnemonic { get; init; } = "";

  [JsonPropertyName("PanamatikAlias")]
  public string PanamatikAlias { get; init; } = "";

  [JsonPropertyName("Title")]
  public string Title { get; init; } = "";
}
