using TeoTheme;

namespace TeoCalc.Rendering.Faceplate;

public static class CalcFaceplateTheme
{
  private static readonly Dictionary<string, CalcThemePack> ThemeCache = new(StringComparer.OrdinalIgnoreCase);

  private static CalcThemePack _current = CalcThemeCatalog.LoadDefault();

  public static CalcThemePack Current => _current;

  public static void SetTheme(CalcThemePack theme)
  {
    _current = theme ?? throw new ArgumentNullException(nameof(theme));
    ThemeCache[theme.Id] = theme;
  }

  public static void SetTheme(string themeId) =>
    SetTheme(GetTheme(themeId));

  public static CalcThemePack GetTheme(string themeId)
  {
    if (ThemeCache.TryGetValue(themeId, out CalcThemePack? cached))
    {
      return cached;
    }

    CalcThemePack loaded = CalcThemeCatalog.Load(themeId);
    ThemeCache[themeId] = loaded;
    return loaded;
  }

  public static uint Resolve(string token, CalcModelDefinition? model = null)
  {
    if (model?.PaletteOverrides is not null
        && model.PaletteOverrides.TryGetValue(token, out string? overrideValue)
        && !string.IsNullOrWhiteSpace(overrideValue))
    {
      return ToImGuiColor(ThemeColorParser.ParseString(overrideValue, token));
    }

    ThemePalette palette = model is null ? _current.Palette : GetTheme(model.ThemeId).Palette;
    return ToImGuiColor(palette.Get(token));
  }

  public static uint Resolve(CalcColorToken token, CalcModelDefinition? model = null) =>
    Resolve(token.Name, model);

  public static string ResolveAnnotationToken(CalcModifierKey modifier, CalcLabelAnchor anchor, CalcModelDefinition model)
  {
    if (CalcModifierPlacement.TryGetInkToken(model, modifier, anchor, out CalcColorToken ink))
    {
      return ink.Name;
    }

    return CalcFaceplateTokens.ModifierLabelColor;
  }

  public static uint ResolveAnnotation(
    CalcModifierKey modifier,
    CalcLabelAnchor anchor,
    CalcModelDefinition model) =>
    Resolve(ResolveAnnotationToken(modifier, anchor, model), model);

  private static uint ToImGuiColor(ThemeColor color)
  {
    (byte r, byte g, byte b, byte a) = color.ToBytes();
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }
}
