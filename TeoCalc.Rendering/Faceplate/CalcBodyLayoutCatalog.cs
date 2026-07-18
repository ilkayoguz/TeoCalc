namespace TeoCalc.Rendering.Faceplate;

public static class CalcBodyLayoutCatalog
{
  public const string DefaultLayoutId = Hp65CalcBodyLayout.LayoutId;

  private static readonly Dictionary<string, CalcBodyLayout> GeometryCache = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, CalcBodyLayout> ModelCache = new(StringComparer.OrdinalIgnoreCase);

  public static CalcBodyLayout Resolve(CalcModelDefinition model)
  {
    string modelKey = $"{model.BodyLayoutId}|{model.Id}|{model.DisplayName}";
    if (ModelCache.TryGetValue(modelKey, out CalcBodyLayout? cached))
    {
      return cached;
    }

    CalcBodyLayout geometry = ResolveGeometry(model.BodyLayoutId, model.DisplayName);
    CalcBodyLayout withSwitches = geometry.WithSwitches(CalcSwitchCatalog.ForModel(model));
    ModelCache[modelKey] = withSwitches;
    return withSwitches;
  }

  public static CalcBodyLayout ResolveForFaceplate(CalcModelDefinition model, string family, string modelId) =>
    CalcModernBody.IsActive
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
      if (displayName.StartsWith("HP-2", StringComparison.OrdinalIgnoreCase)
          || displayName.StartsWith("HP-3", StringComparison.OrdinalIgnoreCase))
      {
        return Hp21CalcBodyLayout.Instance;
      }
    }

    return Hp65CalcBodyLayout.Instance;
  }
}
