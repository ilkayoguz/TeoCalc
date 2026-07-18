namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Outer chrome from Catalog/Documents/00d.png (687×1176).
/// Outside→in: black 1q fitil → dark-gray band → black 1q fitil → gray 1q fitil → light-gray 1q fitil → body.
/// A fitil is a 3D bead: outer crest is brighter (specular), not a separate thin white stroke.
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

  public const uint BlackFitilFill = 0xFF080808;

  public const uint BlackFitilShine = 0xFF2A2A2A;

  public const uint DarkGrayBandFill = 0xFF323232;

  public const uint GrayFitilFill = 0xFF464646;

  public const uint GrayFitilShine = 0xFF646464;

  public const uint LightGrayFitilFill = 0xFF545454;

  /// <summary>Bright crest of the last fitil (~RGB 126) — the “white” reflection, not a line.</summary>
  public const uint LightGrayFitilShine = 0xFF7E7E7E;

  /// <summary>Inner body, slightly lighter than the last fitil body.</summary>
  public const uint InnerBodyFill = 0xFF585858;

  /// <summary>Switch panel well — slightly recessed vs inner body.</summary>
  public const uint SwitchPanelFill = 0xFF4C4C4C;

  /// <summary>Display dark-gray band — RGB(40,40,40).</summary>
  public const uint DisplayBandFill = 0xFF282828;

  /// <summary>RGB(40,11,13) — ImGui ABGR.</summary>
  public const uint DisplayGlassFill = 0xFF0D0B28;

  public const uint SwitchTrackFill = 0xFF141414;

  public const uint SwitchKnobFill = 0xFF2A2A2A;

  public const uint LogoStripFill = 0xFFC8C8C8;

  /// <summary>Brushed aluminum plate — darker left/right edges.</summary>
  public const uint LogoStripEdge = 0xFF8E8E8E;

  /// <summary>Brushed aluminum plate — bright horizontal center.</summary>
  public const uint LogoStripCenter = 0xFFD8D8D8;

  /// <summary>HEWLETT-PACKARD caption ink.</summary>
  public const uint LogoCaptionInk = 0xFF2E2E2E;

  /// <summary>Switch legend ink — cream/white on faceplate (classic HP legend).</summary>
  public const uint SwitchLabelInk = 0xFFF5F0E8;

  public const uint SwitchKnobEdge = 0xFF4A4A4A;

  public const uint LogoDivider = 0xFF464646;

  /// <summary>Knurl ridge spacing on switch knobs (~3px in 00d.png).</summary>
  public const float SwitchKnurlPitchRef = 3f;

  public const float SwitchKnobEdgeWidthRef = 1.5f;

  public const float LogoDividerWidthRef = 2.5f;

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
