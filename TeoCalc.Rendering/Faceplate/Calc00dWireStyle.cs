using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Outer chrome from Catalog/Documents/00d.png (687×1176).
/// Outside→in: black 1q fitil → dark-gray band → black 1q fitil → gray 1q fitil → light-gray 1q fitil → body.
/// A fitil is a 3D bead: outer crest is brighter (specular), not a separate thin white stroke.
/// Geometry scale constants stay here; colors resolve from the active theme palette.
/// </summary>
public static class Calc00dWireStyle
{
  /// <summary>1q — width of every outer-body fitil (~4px in 00d.png).</summary>
  public const float FitilWidthRef = 4f;

  /// <summary>Flat dark-gray band between the two outer black fitils (~23px).</summary>
  public const float DarkGrayBandWidthRef = 23f;

  /// <summary>Display surround: thin black fitil (~2px).</summary>
  public const float DisplayFitilWidthRef = 2f;

  /// <summary>Display surround: thin dark-gray band. Fitil(2)+band(4)+fitil(2)=8 matches the glass inset.</summary>
  public const float DisplayBandWidthRef = 4f;

  /// <summary>Outer crest of a fitil that catches light (~45% of fitil width).</summary>
  public const float FitilShineFraction = 0.45f;

  public const float OuterRadiusRef = 18f;

  public const float FaceplateRadiusRef = 12f;

  /// <summary>Outer bezel radius = glass radius(5) + frame thickness(8) so the bead corners nest concentrically.</summary>
  public const float DisplayBezelRadiusRef = 13f;

  public const float DisplayGlassRadiusRef = 5f;

  public const float SwitchTrackRadiusRef = 5.5f;

  public const float SwitchKnobRadiusRef = 2.5f;

  public const float LogoStripRadiusRef = 3f;

  public static uint BlackFitilFill => CalcChassisPalette.ChromeBlackFitil;

  public static uint BlackFitilShine => CalcChassisPalette.ChromeBlackFitilShine;

  public static uint DarkGrayBandFill => CalcChassisPalette.ChromeDarkGrayBand;

  public static uint GrayFitilFill => CalcChassisPalette.ChromeGrayFitil;

  public static uint GrayFitilShine => CalcChassisPalette.ChromeGrayFitilShine;

  public static uint LightGrayFitilFill => CalcChassisPalette.ChromeLightGrayFitil;

  /// <summary>Bright crest of the last fitil — the “white” reflection, not a line.</summary>
  public static uint LightGrayFitilShine => CalcChassisPalette.ChromeLightGrayFitilShine;

  /// <summary>Inner body, slightly lighter than the last fitil body.</summary>
  public static uint InnerBodyFill => CalcChassisPalette.ChromeInnerBody;

  /// <summary>Switch panel well — slightly recessed vs inner body.</summary>
  public static uint SwitchPanelFill => CalcChassisPalette.ChromeSwitchPanel;

  /// <summary>Display dark-gray band.</summary>
  public static uint DisplayBandFill => CalcChassisPalette.ChromeDisplayBand;

  public static uint DisplayGlassFill => CalcChassisPalette.DisplayGlass;

  public static uint SwitchTrackFill => CalcChassisPalette.ChromeSwitchTrack;

  public static uint SwitchKnobFill => CalcChassisPalette.ChromeSwitchKnob;

  public static uint LogoStripFill => CalcChassisPalette.ChromeLogoStrip;

  /// <summary>Brushed aluminum plate — darker left/right edges.</summary>
  public static uint LogoStripEdge => CalcChassisPalette.ChromeLogoStripEdge;

  /// <summary>Brushed aluminum plate — bright horizontal center.</summary>
  public static uint LogoStripCenter => CalcChassisPalette.ChromeLogoStripCenter;

  /// <summary>Teo logo-plate caption ink.</summary>
  public static uint LogoCaptionInk => CalcChassisPalette.LogoCaption;

  /// <summary>Switch legend ink — cream/white on faceplate (classic HP legend).</summary>
  public static uint SwitchLabelInk => CalcChassisPalette.SwitchLabel;

  public static uint SwitchKnobEdge => CalcChassisPalette.ChromeSwitchKnobEdge;

  /// <summary>Logo-plate groove — slightly darker than aluminum center (embedded, low contrast).</summary>
  public static uint LogoDivider => CalcChassisPalette.ChromeLogoDivider;

  /// <summary>Logo-plate groove highlight — slightly lighter than aluminum center.</summary>
  public static uint LogoDividerHighlight => CalcChassisPalette.ChromeLogoDividerHighlight;

  /// <summary>Knurl ridge spacing on switch knobs (~3px in 00d.png).</summary>
  public const float SwitchKnurlPitchRef = 3f;

  public const float SwitchKnobEdgeWidthRef = 1.5f;

  public const float LogoDividerWidthRef = 1.25f;

  public const float SwitchTrackWidthRefPx = 80f;

  public const float SwitchTrackHeightRefPx = 11f;

  public const float SwitchKnobWidthRefPx = 28f;

  public const float SwitchKnobHeightRefPx = 18f;

  public const float SwitchPanelRadiusRef = 5f;

  public const float SwitchPanelPadXRef = 14f;

  public const float SwitchPanelPadYRef = 10f;

  /// <summary>4×1q fitils + dark-gray band.</summary>
  public const float OuterStackInsetRef =
    FitilWidthRef * 4f + DarkGrayBandWidthRef;

  public static float Px(float referencePixels, float scale) =>
    MathF.Max(1f, referencePixels * scale);
}
