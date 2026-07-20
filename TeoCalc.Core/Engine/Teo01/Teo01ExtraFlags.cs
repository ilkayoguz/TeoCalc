namespace TeoCalc.Core.Engine.Teo01;

/// <summary>Panamatik <c>F01</c> watch/peripheral flags (ACThp01 only).</summary>
[Flags]
public enum Teo01ExtraFlags : byte
{
  None = 0,
  Sleep = 1,
  Blink = 2,
  Scwp = 4,
  SwDec = 8,
  SwStarted = 0x10,
  AlarmActive = 0x20,
  Wakeup = 0x40,
  DisplayOnHold = 0x80,
}
