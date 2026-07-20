using System.Text.Json;
using System.Text.Json.Serialization;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class ClassicKeyFaceplateLegend
{
  private static readonly Dictionary<string, Dictionary<int, KeyFaceplateEntry>> Cache = new(StringComparer.OrdinalIgnoreCase);

  public static KeyLegendVisual Resolve(
    string modelId,
    string family,
    ProgramKeyEntry key,
    ProgramVocabulary vocabulary,
    FaceplateLabelStyle labelStyle)
  {
    // HP-65 A–E card-slot overlays skip JSON gold on the f/h/STO/RCL/g row (10–14).
    // HP-67 uses CapAbove/CapSkirt JSON on that row (f,g,STO,RCL,h) and must not skip.
    bool skipJsonShiftForClassicAe =
      IsHp65(modelId)
      && key.Index is >= 10 and <= 14;
    KeyFaceplateEntry? entry = TryGetEntry(modelId, key.Index);

    // Prefer CapFace from JSON when present so Resolve does not recurse through LabelForKey ladders.
    string primary = entry?.CapFace is not null
      ? entry.CapFace
      : CalcFaceplateLayout.LabelForKey(key, vocabulary, family, modelId);

    return new KeyLegendVisual(
      primary,
      skipJsonShiftForClassicAe ? null : entry?.Gold,
      skipJsonShiftForClassicAe ? null : entry?.Blue,
      skipJsonShiftForClassicAe ? null : entry?.GoldInverse,
      skipJsonShiftForClassicAe ? null : entry?.GoldRight,
      skipJsonShiftForClassicAe ? null : entry?.Black,
      labelStyle);
  }

  public static bool TryGetStyle(string modelId, int keyIndex, out CalcButtonStyle style)
  {
    style = default;
    KeyFaceplateEntry? entry = TryGetEntry(modelId, keyIndex);
    return entry is not null && CalcKeyStyleResolver.TryParse(entry.Style, out style);
  }

  public static KeyFaceplateEntry? TryGetEntryPublic(string modelId, int keyIndex) =>
    TryGetEntry(modelId, keyIndex);

  /// <summary>Invalidate cache after migrating faceplate JSON on disk.</summary>
  public static void ClearCache() => Cache.Clear();

  private static bool IsHp65(string modelId) =>
    string.Equals(modelId, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "65", StringComparison.OrdinalIgnoreCase);

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

  public sealed class KeyFaceplateEntry
  {
    [JsonPropertyName("CapFace")]
    public string? CapFace { get; init; }

    [JsonPropertyName("Style")]
    public string? Style { get; init; }

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
