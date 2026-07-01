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

  public static uint SliderTrack => Rgba(16, 14, 12);

  public static uint Footer => Rgba(176, 174, 168);

  public static uint FooterText => Rgba(48, 46, 42);

  public static uint GoldLabel => Rgba(220, 176, 58);

  public static uint BlueLabel => Rgba(96, 176, 220);

  public static uint GoldRule => Rgba(200, 158, 48, 120);

  public static uint CardSlot => Rgba(8, 7, 6);

  public static uint SwitchTrack => Rgba(10, 9, 8);

  public static uint SwitchKnob => Rgba(34, 32, 30);

  public static uint SwitchLabel => Rgba(196, 188, 170);

  public static uint KeyHighlight => Rgba(255, 255, 255, 42);

  public static uint KeyBlackTop => Rgba(42, 40, 38);

  public static uint KeyBlackFace => Rgba(28, 26, 24);

  public static uint KeyBlackSkirt => Rgba(14, 13, 12);

  public static uint KeyGreyTop => Rgba(204, 200, 188);

  public static uint KeyGreyFace => Rgba(184, 180, 170);

  public static uint KeyGreySkirt => Rgba(112, 108, 100);

  public static uint KeyOrangeTop => Rgba(232, 176, 64);

  public static uint KeyOrangeFace => Rgba(210, 148, 44);

  public static uint KeyOrangeSkirt => Rgba(148, 96, 28);

  public static uint KeyBlueTop => Rgba(148, 188, 214);

  public static uint KeyBlueFace => Rgba(118, 162, 196);

  public static uint KeyBlueSkirt => Rgba(72, 108, 138);

  public static uint KeyText => Rgba(244, 244, 248);

  private static uint Rgba(byte r, byte g, byte b, byte a = 255) =>
    (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
}
