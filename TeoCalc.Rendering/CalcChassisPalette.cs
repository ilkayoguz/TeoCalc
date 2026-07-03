namespace TeoCalc.Rendering;

public static class CalcChassisPalette
{
  // Tuned against hp65_470 reference (olive charcoal shell, warm grey keys).
  public static uint Frame => Rgba(74, 76, 72);

  public static uint FrameEdge => Rgba(58, 60, 54);

  public static uint Faceplate => Rgba(18, 18, 16);

  public static uint FaceplateGrain => Rgba(34, 32, 30, 70);

  public static uint KeyWell => Rgba(6, 6, 7);

  public static uint KeyWellEdge => Rgba(2, 2, 3);

  public static uint DisplayBezel => Rgba(12, 12, 14);

  public static uint DisplayGlass => Rgba(26, 8, 12);

  public static uint DisplayDigit => Rgba(255, 72, 32);

  public static uint DisplayDigitGlow => Rgba(120, 12, 0, 90);

  // Body.svg inner frame / switch recess (#61645A).
  public static uint BodyFrame => Rgba(97, 100, 90);

  public static uint SliderTrack => BodyFrame;

  public static uint Footer => Rgba(176, 174, 168);

  public static uint FooterText => Rgba(48, 46, 42);

  public static uint FooterBrandText => Rgba(232, 230, 224);

  public static uint GoldLabel => Rgba(220, 176, 58);

  public static uint BlueLabel => Rgba(168, 210, 248);

  /// <summary>Dark blue skirt ink on white/grey caps (readable on KeyGreySkirt).</summary>
  public static uint SkirtBlueDark => Rgba(0, 96, 168);

  public static uint SkirtLabelBand => Rgba(58, 86, 112);

  /// <summary>Behind blue skirt labels on grey keys — matches cap skirt, not extra-dark band.</summary>
  public static uint GreySkirtLabelBand => KeyGreySkirt;

  public static uint GoldRule => Rgba(200, 158, 48, 120);

  public static uint CardSlot => Rgba(8, 7, 6);

  public static uint CardSlotLabel => Rgba(236, 234, 228);

  public static uint SwitchTrack => BodyFrame;

  public static uint SwitchKnob => Rgba(0, 0, 0);

  public static uint SwitchLabel => Rgba(245, 243, 238);

  public static uint KeyHighlight => Rgba(255, 255, 255, 42);

  /// <summary>KeyCap-black.svg highlight stroke.</summary>
  public static uint KeyBlackTop => Rgba(74, 80, 86);

  /// <summary>KeyCap-black.svg face fill.</summary>
  public static uint KeyBlackFace => Rgba(44, 47, 49);

  /// <summary>KeyCap-black.svg skirt fill.</summary>
  public static uint KeyBlackSkirt => Rgba(20, 22, 24);

  public static uint KeyGreyTop => Rgba(204, 200, 188);

  /// <summary>KeyCap-grey.svg face fill.</summary>
  public static uint KeyGreyFace => Rgba(184, 180, 170);

  public static uint KeyCapGreyFace => KeyGreyFace;

  public static uint KeyCapGreyHighlight => KeyGreyTop;

  /// <summary>KeyCap-grey.svg skirt fill.</summary>
  public static uint KeyGreySkirt => Rgba(162, 158, 146);

  public static uint KeyWhiteTop => Rgba(252, 250, 246);

  public static uint KeyWhiteFace => Rgba(244, 242, 236);

  /// <summary>Key2.svg body tone behind white face.</summary>
  public static uint KeyWhiteSkirt => Rgba(176, 172, 164);

  /// <summary>KeyCap.svg highlight stroke.</summary>
  public static uint KeyOrangeTop => Rgba(247, 216, 118);

  public static uint KeyCapGoldFace => Rgba(232, 176, 64);

  /// <summary>KeyCap.svg face fill.</summary>
  public static uint KeyOrangeFace => Rgba(232, 176, 64);

  /// <summary>KeyCap.svg skirt / body fill.</summary>
  public static uint KeyOrangeSkirt => Rgba(161, 111, 34);

  /// <summary>KeyCap-blue.svg highlight stroke.</summary>
  public static uint KeyBlueTop => Rgba(148, 215, 248);

  public static uint KeyBlueFace => Rgba(126, 191, 231);

  public static uint KeyCapBlueFace => KeyBlueFace;

  public static uint KeyCapBlueHighlight => KeyBlueTop;

  /// <summary>KeyCap-blue.svg skirt fill.</summary>
  public static uint KeyBlueSkirt => Rgba(98, 157, 199);

  public static uint KeyText => Rgba(244, 244, 248);

  /// <summary>Primary labels on gold/grey key caps (dark ink).</summary>
  public static uint KeyCapDarkText => Rgba(18, 16, 14);

  public static uint KeyCapBezel => Rgba(26, 26, 26);

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
}
