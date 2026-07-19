using System.Text.Json;
using System.Text.Json.Serialization;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

public static class ClassicKeyFaceplateLegend
{
  private static readonly Dictionary<string, Dictionary<int, KeyFaceplateEntry>> Cache = new(StringComparer.OrdinalIgnoreCase);

  public static HpCalcKeyVisual Resolve(
    string modelId,
    string family,
    ProgramKeyEntry key,
    ProgramVocabulary vocabulary,
    FaceplateLabelStyle labelStyle)
  {
    string primary = CalcFaceplateLayout.LabelForKey(key, vocabulary, family);
    // HP-65 A–E (indices 10–14) use CalcEnterRowLabels / card-slot overlays instead of JSON gold.
    // Other models (e.g. HP-01 operator-row gold DW/21/…) must still load those indices.
    bool skipJsonForClassicAe =
      string.Equals(family, "Classic", StringComparison.OrdinalIgnoreCase)
      && key.Index is >= 10 and <= 14;
    KeyFaceplateEntry? entry = skipJsonForClassicAe ? null : TryGetEntry(modelId, key.Index);
    return new HpCalcKeyVisual(
      primary,
      entry?.Gold,
      entry?.Blue,
      entry?.GoldInverse,
      labelStyle);
  }

  private static KeyFaceplateEntry? TryGetEntry(string modelId, int keyIndex)
  {
    foreach (string id in CandidateModelIds(modelId))
    {
      if (!Cache.TryGetValue(id, out Dictionary<int, KeyFaceplateEntry>? entries))
      {
        entries = Load(id);
        Cache[id] = entries;
      }

      if (entries.TryGetValue(keyIndex, out KeyFaceplateEntry? entry))
      {
        return entry;
      }
    }

    return null;
  }

  private static IEnumerable<string> CandidateModelIds(string modelId)
  {
    yield return modelId;
    if (modelId.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      yield return modelId["HP-".Length..];
    }
    else if (!modelId.StartsWith("HP", StringComparison.OrdinalIgnoreCase))
    {
      yield return "HP-" + modelId;
    }
  }

  private static Dictionary<int, KeyFaceplateEntry> Load(string modelId)
  {
    string path = Path.Combine(TeoCalcPaths.ResourcePath("Engine"), modelId, "Program", "key.faceplate.json");
    if (!File.Exists(path))
    {
      return [];
    }

    string json = File.ReadAllText(path);
    KeyFaceplateDocument? document = JsonSerializer.Deserialize<KeyFaceplateDocument>(json, JsonOptions);
    if (document?.Keys is null)
    {
      return [];
    }

    Dictionary<int, KeyFaceplateEntry> entries = [];
    foreach (KeyValuePair<string, KeyFaceplateEntry> pair in document.Keys)
    {
      if (int.TryParse(pair.Key, out int index))
      {
        entries[index] = pair.Value;
      }
    }

    return entries;
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private sealed class KeyFaceplateDocument
  {
    [JsonPropertyName("Keys")]
    public Dictionary<string, KeyFaceplateEntry>? Keys { get; init; }
  }

  private sealed class KeyFaceplateEntry
  {
    [JsonPropertyName("Gold")]
    public string? Gold { get; init; }

    [JsonPropertyName("Blue")]
    public string? Blue { get; init; }

    [JsonPropertyName("GoldInverse")]
    public string? GoldInverse { get; init; }
  }
}
