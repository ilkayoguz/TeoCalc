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
    foreach (CalcModifierAnnotationStyle style in model.AnnotationStyles)
    {
      if (style.Modifier == modifier && style.Anchor == anchor)
      {
        return style.Ink.Name;
      }
    }

    return DefaultAnnotationToken(modifier, anchor);
  }

  public static uint ResolveAnnotation(
    CalcModifierKey modifier,
    CalcLabelAnchor anchor,
    CalcModelDefinition model) =>
    Resolve(ResolveAnnotationToken(modifier, anchor, model), model);

  private static string DefaultAnnotationToken(CalcModifierKey modifier, CalcLabelAnchor anchor) =>
    (modifier, anchor) switch
    {
      (CalcModifierKey.F, CalcLabelAnchor.CapAbove) => CalcFaceplateTokens.ModifierFCapAboveColor,
      (CalcModifierKey.F, CalcLabelAnchor.CapBelow) => CalcFaceplateTokens.ModifierFCapAboveColor,
      (CalcModifierKey.G, CalcLabelAnchor.CapSkirt) => CalcFaceplateTokens.ModifierGCapSkirtColor,
      (CalcModifierKey.G, CalcLabelAnchor.CapAbove) => CalcFaceplateTokens.ModifierGCapSkirtColor,
      (CalcModifierKey.H, CalcLabelAnchor.CapFace) => CalcFaceplateTokens.ModifierHCapFaceColor,
      _ => CalcFaceplateTokens.ModifierLabelColor,
    };

  private static uint ToImGuiColor(ThemeColor color)
  {
    (byte r, byte g, byte b, byte a) = color.ToBytes();
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }
}
