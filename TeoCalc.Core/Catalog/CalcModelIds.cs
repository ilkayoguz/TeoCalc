namespace TeoCalc.Core.Catalog;

/// <summary>
/// Single registry for museum catalog ids, engine resource folders, and emulator bindings.
/// Catalog may use suffixes (HP-29C); engine folders / bindings are <c>T-*</c> (T-29).
/// </summary>
public static class CalcModelIds
{
  /// <summary>Catalog / product-label aliases → engine folder id.</summary>
  private static readonly Dictionary<string, string> CatalogToEngine = new(StringComparer.OrdinalIgnoreCase)
  {
    ["HP-29C"] = "T-29",
    ["T-29C"] = "T-29",
    ["HP-31E"] = "T-31",
    ["T-31E"] = "T-31",
    ["HP-32E"] = "T-32",
    ["T-32E"] = "T-32",
    ["HP-33C"] = "T-33",
    ["T-33C"] = "T-33",
    ["HP-33E"] = "T-33",
    ["T-33E"] = "T-33",
    ["HP-34C"] = "T-34",
    ["T-34C"] = "T-34",
    ["HP-37E"] = "T-37",
    ["T-37E"] = "T-37",
    ["HP-38E"] = "T-38",
    ["T-38E"] = "T-38",
    ["HP-67BE"] = "T-67",
    ["T-67BE"] = "T-67",
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

  /// <summary>Maps a catalog / display id to the engine folder and emulator binding id (<c>T-*</c>).</summary>
  public static string ToEngineId(string catalogOrEngineId)
  {
    if (string.IsNullOrWhiteSpace(catalogOrEngineId))
    {
      return catalogOrEngineId;
    }

    string id = catalogOrEngineId.Trim();
    if (CatalogToEngine.TryGetValue(id, out string? mapped))
    {
      return mapped;
    }

    if (id.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      string asTeo = "T-" + id[3..];
      return CatalogToEngine.TryGetValue(asTeo, out mapped)
        || CatalogToEngine.TryGetValue(id, out mapped)
        ? mapped
        : asTeo;
    }

    if (id.StartsWith("T-", StringComparison.OrdinalIgnoreCase))
    {
      return CatalogToEngine.TryGetValue(id, out mapped) ? mapped : id;
    }

    if (id.Length > 0 && char.IsDigit(id[0]))
    {
      string asTeo = "T-" + id;
      return CatalogToEngine.TryGetValue(asTeo, out mapped)
        || CatalogToEngine.TryGetValue("HP-" + id, out mapped)
        ? mapped
        : asTeo;
    }

    return id;
  }

  /// <summary>Product label for UI (e.g. T-65 from HP-65 / HP-29C → T-29C).</summary>
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

    if (id.StartsWith("T-", StringComparison.OrdinalIgnoreCase))
    {
      return id[2..];
    }

    return id;
  }

  /// <summary>True when both ids resolve to the same engine folder (<c>T-*</c>).</summary>
  public static bool SameEngine(string? a, string? b)
  {
    if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
    {
      return false;
    }

    return string.Equals(ToEngineId(a), ToEngineId(b), StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>True when <paramref name="catalogOrEngineId"/> resolves to <paramref name="engineId"/> (e.g. T-65).</summary>
  public static bool IsEngine(string? catalogOrEngineId, string engineId) =>
    !string.IsNullOrWhiteSpace(catalogOrEngineId)
    && string.Equals(ToEngineId(catalogOrEngineId), ToEngineId(engineId), StringComparison.OrdinalIgnoreCase);

  public static string InferFamily(string engineOrCatalogId)
  {
    string modelId = ToEngineId(engineOrCatalogId).ToUpperInvariant();
    return modelId switch
    {
      "T-01" => "Teo01",
      "T-19C" => "Teo19",
      "T-67" => "Teo67",
      "T-35" or "T-45" or "T-55" or "T-65" or "T-70" or "T-80" => "Classic",
      var id when id.StartsWith("T-3", StringComparison.Ordinal) && id is not "T-35" => "Spice",
      var id when id.StartsWith("T-2", StringComparison.Ordinal) => "Woodstock",
      _ => "Classic",
    };
  }
}
