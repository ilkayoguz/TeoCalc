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
    string primary = CalcFaceplateLayout.LabelForKey(key, vocabulary, family, modelId);
    // HP-65/67 A–E (indices 10–14) use card-slot overlays instead of JSON gold.
    // Other Classic models (HP-35 CapAbove on STO/RCL/…) must still load those indices.
    bool skipJsonForClassicAe =
      IsHp65Or67(modelId)
      && key.Index is >= 10 and <= 14;
    KeyFaceplateEntry? entry = skipJsonForClassicAe ? null : TryGetEntry(modelId, key.Index);
    return new HpCalcKeyVisual(
      primary,
      entry?.Gold,
      entry?.Blue,
      entry?.GoldInverse,
      entry?.GoldRight,
      entry?.Black,
      labelStyle);
  }

  private static bool IsHp65Or67(string modelId) =>
    string.Equals(modelId, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-67", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-67BE", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "67", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "67BE", StringComparison.OrdinalIgnoreCase);

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
    string engineId = CalcModelIds.ToEngineId(modelId);
    if (!string.Equals(engineId, modelId, StringComparison.OrdinalIgnoreCase))
    {
      yield return engineId;
    }

    if (modelId.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      yield return modelId["HP-".Length..];
    }
    else if (!modelId.StartsWith("HP", StringComparison.OrdinalIgnoreCase))
    {
      yield return "HP-" + modelId;
    }

    if (engineId.StartsWith("HP-", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(engineId, modelId, StringComparison.OrdinalIgnoreCase))
    {
      yield return engineId["HP-".Length..];
    }
  }

  private static Dictionary<int, KeyFaceplateEntry> Load(string modelId)
  {
    string path = Path.Combine(
      TeoCalcPaths.ResourcePath("Engine"),
      CalcModelIds.ToEngineId(modelId),
      "Program",
      "key.faceplate.json");
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

    /// <summary>Second CapAbove gold legend, drawn right-aligned (HP-31E ENTER: PREFIX).</summary>
    [JsonPropertyName("GoldRight")]
    public string? GoldRight { get; init; }

    /// <summary>HP-34C h-shift CapSkirt (black).</summary>
    [JsonPropertyName("Black")]
    public string? Black { get; init; }
  }
}
