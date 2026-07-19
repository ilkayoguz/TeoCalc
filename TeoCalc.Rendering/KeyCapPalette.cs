namespace TeoCalc.Rendering;

/// <summary>Key2 layer fills — maps to legacy KeyCap.svg skirt / face / top tones.</summary>
public readonly record struct KeyCapPalette(uint Top, uint Face, uint Skirt)
{
  public static KeyCapPalette ForStyle(CalcButtonStyle style, bool hovered, bool pressed)
  {
    byte bump = (byte)(hovered && !pressed ? 10 : 0);
    byte drop = (byte)(pressed ? 16 : 0);
    byte skirtDrop = (byte)(pressed ? 18 : 0);
    return style switch
    {
      CalcButtonStyle.Grey => Pack(
        Tone(CalcChassisPalette.KeyGreyTop, bump, drop),
        Tone(CalcChassisPalette.KeyGreyFace, drop),
        Tone(CalcChassisPalette.KeyGreySkirt, skirtDrop)),
      CalcButtonStyle.White => Pack(
        Tone(CalcChassisPalette.KeyWhiteTop, bump, drop),
        Tone(CalcChassisPalette.KeyWhiteFace, drop),
        Tone(CalcChassisPalette.KeyWhiteSkirt, skirtDrop)),
      CalcButtonStyle.Orange => Pack(
        Tone(CalcChassisPalette.KeyOrangeTop, bump, drop),
        Tone(CalcChassisPalette.KeyOrangeFace, drop),
        Tone(CalcChassisPalette.KeyOrangeSkirt, skirtDrop)),
      CalcButtonStyle.Blue => Pack(
        Tone(CalcChassisPalette.KeyCapBlueHighlight, bump, drop),
        Tone(CalcChassisPalette.KeyCapBlueFace, drop),
        Tone(CalcChassisPalette.KeyBlueSkirt, skirtDrop)),
      CalcButtonStyle.Olive => Pack(
        Tone(CalcChassisPalette.KeyOliveTop, bump, drop),
        Tone(CalcChassisPalette.KeyOliveFace, drop),
        Tone(CalcChassisPalette.KeyOliveSkirt, skirtDrop)),
      _ => Pack(
        Tone(CalcChassisPalette.KeyBlackTop, bump, drop),
        Tone(CalcChassisPalette.KeyBlackFace, drop),
        Tone(CalcChassisPalette.KeyBlackSkirt, skirtDrop)),
    };
  }

  private static KeyCapPalette Pack(uint top, uint face, uint skirt) => new(top, face, skirt);

  private static uint Tone(uint color, byte subtract) => Subtract(color, subtract);

  private static uint Tone(uint color, byte add, byte subtract) => Subtract(Add(color, add), subtract);

  private static uint Add(uint color, byte amount)
  {
    byte r = (byte)Math.Min(255, (color & 0xFF) + amount);
    byte g = (byte)Math.Min(255, ((color >> 8) & 0xFF) + amount);
    byte b = (byte)Math.Min(255, ((color >> 16) & 0xFF) + amount);
    byte a = (byte)(color >> 24);
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }

  private static uint Subtract(uint color, byte amount)
  {
    byte r = (byte)Math.Max(0, (color & 0xFF) - amount);
    byte g = (byte)Math.Max(0, ((color >> 8) & 0xFF) - amount);
    byte b = (byte)Math.Max(0, ((color >> 16) & 0xFF) - amount);
    byte a = (byte)(color >> 24);
    return (uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r;
  }
}
