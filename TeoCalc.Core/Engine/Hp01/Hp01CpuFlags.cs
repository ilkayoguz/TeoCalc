namespace TeoCalc.Core.Engine.Hp01;

/// <summary>Panamatik <c>F</c> flags for ACThp01.</summary>
[Flags]
public enum Hp01CpuFlags : byte
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
