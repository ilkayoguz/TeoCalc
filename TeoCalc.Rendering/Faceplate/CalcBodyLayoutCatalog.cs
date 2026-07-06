namespace TeoCalc.Rendering.Faceplate;

public static class CalcBodyLayoutCatalog
{
  public const string DefaultLayoutId = Hp65CalcBodyLayout.LayoutId;

  private static readonly Dictionary<string, CalcBodyLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

  public static CalcBodyLayout Resolve(CalcModelDefinition model) =>
    Resolve(model.BodyLayoutId);

  public static CalcBodyLayout Resolve(string layoutId)
  {
    if (Cache.TryGetValue(layoutId, out CalcBodyLayout? cached))
    {
      return cached;
    }

    CalcBodyLayout loaded = layoutId.ToLowerInvariant() switch
    {
      Hp65CalcBodyLayout.LayoutId => Hp65CalcBodyLayout.Instance,
      _ => Hp65CalcBodyLayout.Instance,
    };

    Cache[loaded.Id] = loaded;
    return loaded;
  }
}
