namespace TeoCalc.Core.Engine.Spice;

/// <summary>Panamatik HP25 <c>F</c> flags.</summary>
[Flags]
public enum SpiceCpuFlags : byte
{
  None = 0,
  Null = 1,
  Carry = 2,
  PrevCarry = 4,
  DelRom = 8,
  Key = 0x10,
  DisplayOn = 0x20,
  PCarry = 0x40,
  Bank = 0x80,
}
