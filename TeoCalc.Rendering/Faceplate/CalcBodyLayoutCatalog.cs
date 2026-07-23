using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

public static class CalcBodyLayoutCatalog
{
  /// <summary>Canonical Modern geometry; Retro SVG layouts remain available by explicit id.</summary>
  public const string DefaultLayoutId = Calc00dBodyLayout.LayoutId;

  private static readonly Dictionary<string, CalcBodyLayout> GeometryCache = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, CalcBodyLayout> ModelCache = new(StringComparer.OrdinalIgnoreCase);

  public static CalcBodyLayout Resolve(CalcModelDefinition model)
  {
    string modelKey = $"{model.BodyLayoutId}|{model.Id}|{model.DisplayName}";
    if (ModelCache.TryGetValue(modelKey, out CalcBodyLayout? cached))
    {
      return cached;
    }

    CalcBodyLayout geometry =
      string.Equals(model.BodyLayoutId, Calc00dBodyLayout.LayoutId, StringComparison.OrdinalIgnoreCase)
        ? Calc00dBodyLayout.Resolve(CalcModelIds.InferFamily(model.DisplayName), model.Id, model)
        : ResolveGeometry(model.BodyLayoutId, model.DisplayName);

    CalcBodyLayout withSwitches = geometry.WithSwitches(CalcSwitchCatalog.ForModel(model));
    ModelCache[modelKey] = withSwitches;
    return withSwitches;
  }

  public static CalcBodyLayout ResolveForFaceplate(CalcModelDefinition model, string family, string modelId) =>
    CalcModernBody.IsActive || string.Equals(model.BodyLayoutId, Calc00dBodyLayout.LayoutId, StringComparison.OrdinalIgnoreCase)
      ? Calc00dBodyLayout.Resolve(family, modelId, model)
      : Resolve(model);

  public static CalcBodyLayout Resolve(string layoutId, string? displayName = null) =>
    ResolveGeometry(layoutId, displayName);

  private static CalcBodyLayout ResolveGeometry(string layoutId, string? displayName)
  {
    string cacheKey = layoutId;
    if (GeometryCache.TryGetValue(cacheKey, out CalcBodyLayout? cached))
    {
      return cached;
    }

    CalcBodyLayout loaded = layoutId.ToLowerInvariant() switch
    {
      Calc00dBodyLayout.LayoutId => Calc00dBodyLayout.Resolve(
        CalcModelIds.InferFamily(displayName ?? "T-65"),
        CalcModelIds.ToShortId(displayName ?? "T-65"),
        CalcModelCatalog.Resolve(displayName ?? "T-65")),
      CalcPrototypeBodyLayout.LayoutId => CalcPrototypeBodyLayout.Instance,
      Hp65CalcBodyLayout.LayoutId => Hp65CalcBodyLayout.Instance,
      Hp21CalcBodyLayout.LayoutId => Hp21CalcBodyLayout.Instance,
      _ => ResolveFamilyDefault(displayName),
    };

    GeometryCache[loaded.Id] = loaded;
    return loaded;
  }

  private static CalcBodyLayout ResolveFamilyDefault(string? displayName)
  {
    if (displayName is not null)
    {
      string engineId = CalcModelIds.ToEngineId(displayName);
      if (engineId.StartsWith("T-2", StringComparison.OrdinalIgnoreCase)
          || (engineId.StartsWith("T-3", StringComparison.OrdinalIgnoreCase)
              && !CalcModelIds.IsEngine(engineId, "T-35")))
      {
        return Hp21CalcBodyLayout.Instance;
      }
    }

    return Hp65CalcBodyLayout.Instance;
  }
}
