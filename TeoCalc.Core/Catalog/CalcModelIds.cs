namespace TeoCalc.Core.Catalog;

/// <summary>
/// Single registry for museum catalog ids, engine resource folders, and emulator bindings.
/// Catalog may use suffixes (HP-29C); engine/emulator ids are short (HP-29).
/// </summary>
public static class CalcModelIds
{
  private static readonly Dictionary<string, string> CatalogToEngine = new(StringComparer.OrdinalIgnoreCase)
  {
    ["HP-29C"] = "HP-29",
    ["HP-31E"] = "HP-31",
    ["HP-32E"] = "HP-32",
    ["HP-33C"] = "HP-33",
    ["HP-33E"] = "HP-33",
    ["HP-34C"] = "HP-34",
    ["HP-37E"] = "HP-37",
    ["HP-38E"] = "HP-38",
  };

  /// <summary>
  /// Build catalog/engine/short/product/family identity once.
  /// <paramref name="catalogOrEngineId"/> is the launcher / open id when known (e.g. HP-31E).
  /// </summary>
  public static CalcModelIdentity Resolve(string catalogOrEngineId, string? family = null)
  {
    string catalogId = string.IsNullOrWhiteSpace(catalogOrEngineId)
      ? string.Empty
      : catalogOrEngineId.Trim();
    string engineId = ToEngineId(catalogId);
    string shortId = ToShortId(catalogId);
    string productLabel = ToProductLabel(catalogId);
    string resolvedFamily = !string.IsNullOrWhiteSpace(family)
      ? family.Trim()
      : InferFamily(catalogId);
    return new CalcModelIdentity(catalogId, engineId, shortId, productLabel, resolvedFamily);
  }

  /// <summary>Maps a catalog / display id to the engine folder and emulator binding id.</summary>
  public static string ToEngineId(string catalogOrEngineId)
  {
    if (string.IsNullOrWhiteSpace(catalogOrEngineId))
    {
      return catalogOrEngineId;
    }

    return CatalogToEngine.TryGetValue(catalogOrEngineId.Trim(), out string? engineId)
      ? engineId
      : catalogOrEngineId.Trim();
  }

  /// <summary>Product label for UI (e.g. T-65 from HP-65 / HP-29C → T-29C short form via numeric id).</summary>
  public static string ToProductLabel(string catalogOrEngineId) =>
    $"T-{ToShortId(catalogOrEngineId)}";

  /// <summary>Short id for faceplate logo strip (65, 29C, 31E, …).</summary>
  public static string ToShortId(string catalogOrEngineId)
  {
    if (string.IsNullOrWhiteSpace(catalogOrEngineId))
    {
      return catalogOrEngineId;
    }

    string id = catalogOrEngineId.Trim();
    if (id.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      return id[3..];
    }

    return id;
  }

  public static string InferFamily(string engineOrCatalogId)
  {
    string modelId = ToEngineId(engineOrCatalogId).ToUpperInvariant();
    return modelId switch
    {
      "HP-01" => "HP01",
      "HP-19C" => "HP19C",
      "HP-67" => "Classic",
      "HP-35" or "HP-45" or "HP-55" or "HP-65" or "HP-70" or "HP-80" => "Classic",
      var id when id.StartsWith("HP-3", StringComparison.Ordinal) && id is not "HP-35" => "Spice",
      var id when id.StartsWith("HP-2", StringComparison.Ordinal) => "Woodstock",
      _ => "Classic",
    };
  }
}
