using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcChassisPalette
{
  public static uint Frame => T(CalcFaceplateTokens.FrameColor);

  public static uint FrameEdge => T(CalcFaceplateTokens.FrameEdgeColor);

  public static uint Faceplate => T(CalcFaceplateTokens.FaceplateColor);

  public static uint FaceplateGrain => T(CalcFaceplateTokens.FaceplateGrainColor);

  public static uint KeyWell => T(CalcFaceplateTokens.KeyWellColor);

  public static uint KeyWellEdge => T(CalcFaceplateTokens.KeyWellEdgeColor);

  public static uint DisplayBezel => T(CalcFaceplateTokens.DisplayBezelColor);

  public static uint DisplayGlass => T(CalcFaceplateTokens.DisplayGlassColor);

  public static uint DisplayDigit => T(CalcFaceplateTokens.DisplayDigitColor);

  public static uint DisplayDigitGlow => T(CalcFaceplateTokens.DisplayDigitGlowColor);

  public static uint BodyFrame => T(CalcFaceplateTokens.BodyFrameColor);

  public static uint SliderTrack => T(CalcFaceplateTokens.SliderTrackColor);

  public static uint Footer => T(CalcFaceplateTokens.FooterColor);

  public static uint FooterText => T(CalcFaceplateTokens.FooterTextColor);

  public static uint FooterBrandText => T(CalcFaceplateTokens.FooterBrandTextColor);

  public static uint GoldLabel => T(CalcFaceplateTokens.ModifierFCapAboveColor);

  public static uint BlueLabel => T(CalcFaceplateTokens.ModifierGCapSkirtColor);

  public static uint SkirtBlueDark => T(CalcFaceplateTokens.SkirtBlueDarkColor);

  public static uint SkirtLabelBand => T(CalcFaceplateTokens.SkirtLabelBandColor);

  public static uint GreySkirtLabelBand => T(CalcFaceplateTokens.GreySkirtLabelBandColor);

  public static uint GoldRule => T(CalcFaceplateTokens.GoldRuleColor);

  public static uint CardSlot => T(CalcFaceplateTokens.CardSlotColor);

  public static uint CardSlotLabel => T(CalcFaceplateTokens.CardSlotLabelColor);

  public static uint SwitchTrack => T(CalcFaceplateTokens.SwitchTrackColor);

  public static uint SwitchKnob => T(CalcFaceplateTokens.SwitchKnobColor);

  public static uint SwitchLabel => T(CalcFaceplateTokens.SwitchLabelColor);

  public static uint KeyHighlight => T(CalcFaceplateTokens.KeyHighlightColor);

  public static uint KeyBlackTop => T(CalcFaceplateTokens.KeyBlackTopColor);

  public static uint KeyBlackFace => T(CalcFaceplateTokens.KeyBlackFaceColor);

  public static uint KeyBlackSkirt => T(CalcFaceplateTokens.KeyBlackSkirtColor);

  public static uint KeyGreyTop => T(CalcFaceplateTokens.KeyGreyTopColor);

  public static uint KeyGreyFace => T(CalcFaceplateTokens.KeyGreyFaceColor);

  public static uint KeyCapGreyFace => KeyGreyFace;

  public static uint KeyCapGreyHighlight => KeyGreyTop;

  public static uint KeyGreySkirt => T(CalcFaceplateTokens.KeyGreySkirtColor);

  public static uint KeyLightGreyTop => T(CalcFaceplateTokens.KeyLightGreyTopColor);

  public static uint KeyLightGreyFace => T(CalcFaceplateTokens.KeyLightGreyFaceColor);

  public static uint KeyLightGreySkirt => T(CalcFaceplateTokens.KeyLightGreySkirtColor);

  public static uint KeyCementTop => T(CalcFaceplateTokens.KeyCementTopColor);

  public static uint KeyCementFace => T(CalcFaceplateTokens.KeyCementFaceColor);

  public static uint KeyCementSkirt => T(CalcFaceplateTokens.KeyCementSkirtColor);

  public static uint KeyDarkGreyTop => T(CalcFaceplateTokens.KeyDarkGreyTopColor);

  public static uint KeyDarkGreyFace => T(CalcFaceplateTokens.KeyDarkGreyFaceColor);

  public static uint KeyDarkGreySkirt => T(CalcFaceplateTokens.KeyDarkGreySkirtColor);

  public static uint KeyWhiteTop => T(CalcFaceplateTokens.KeyWhiteTopColor);

  public static uint KeyWhiteFace => T(CalcFaceplateTokens.KeyWhiteFaceColor);

  public static uint KeyWhiteSkirt => T(CalcFaceplateTokens.KeyWhiteSkirtColor);

  public static uint KeyOrangeTop => T(CalcFaceplateTokens.KeyOrangeTopColor);

  public static uint KeyCapGoldFace => T(CalcFaceplateTokens.KeyOrangeFaceColor);

  public static uint KeyOrangeFace => T(CalcFaceplateTokens.KeyOrangeFaceColor);

  public static uint KeyOrangeSkirt => T(CalcFaceplateTokens.KeyOrangeSkirtColor);

  public static uint KeyBlueTop => T(CalcFaceplateTokens.KeyBlueTopColor);

  public static uint KeyBlueFace => T(CalcFaceplateTokens.KeyBlueFaceColor);

  public static uint KeyCapBlueFace => KeyBlueFace;

  public static uint KeyCapBlueHighlight => KeyBlueTop;

  public static uint KeyBlueSkirt => T(CalcFaceplateTokens.KeyBlueSkirtColor);

  public static uint KeyOliveTop => T(CalcFaceplateTokens.KeyOliveTopColor);

  public static uint KeyOliveFace => T(CalcFaceplateTokens.KeyOliveFaceColor);

  public static uint KeyOliveSkirt => T(CalcFaceplateTokens.KeyOliveSkirtColor);

  public static uint KeyText => T(CalcFaceplateTokens.KeyTextColor);

  public static uint KeyCapDarkText => T(CalcFaceplateTokens.KeyCapDarkTextColor);

  public static uint KeyCapBezel => T(CalcFaceplateTokens.KeyCapBezelColor);

  private static uint T(string token) => CalcFaceplateTheme.Resolve(token);
}
