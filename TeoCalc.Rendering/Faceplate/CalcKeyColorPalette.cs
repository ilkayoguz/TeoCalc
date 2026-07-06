namespace TeoCalc.Rendering.Faceplate;

/// <summary>Model-independent color tokens. Modifier ink colors are config cross-references, not enum names.</summary>
public static class CalcKeyColorPalette
{
  public static CalcColorToken LabelOnDarkSurface { get; } = new("label.on-dark-surface");

  public static CalcColorToken LabelOnLightCap { get; } = new("label.on-light-cap");

  public static CalcColorToken LabelOnDarkCap { get; } = new("label.on-dark-cap");

  public static CalcColorToken ModifierFOnCapAbove { get; } = new("modifier.f.cap-above");

  public static CalcColorToken ModifierGOnCapSkirt { get; } = new("modifier.g.cap-skirt");

  public static CalcColorToken ModifierHOnCapFace { get; } = new("modifier.h.cap-face");

  public static CalcColorToken LogoMark { get; } = new("logo.mark");

  public static CalcColorToken LogoCaption { get; } = new("logo.caption");

  public static CalcColorToken LogoStrip { get; } = new("logo.strip");

  public static uint Resolve(CalcColorToken token, CalcModelDefinition model) =>
    Resolve(token, model.Id);

  public static uint Resolve(CalcColorToken token, string modelId) =>
    token.Name switch
    {
      "logo.mark" => 0xFF4A90D9u,
      "logo.caption" => 0xFFF0F0F0u,
      "logo.strip" => 0xFF8A8A8Au,
      "modifier.f.cap-above" => 0xFFD6A83Au,
      "modifier.g.cap-skirt" => 0xFF6EB5FFu,
      "modifier.h.cap-face" => 0xFFFFFFFFu,
      "label.on-dark-cap" => 0xFFE8E8E8u,
      "label.on-light-cap" => 0xFF1A1A1Au,
      _ => 0xFFFFFFFFu,
    };
}
