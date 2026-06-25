namespace TeoCalc.Core.Engine.Classic;

[Flags]
public enum ClassicCpuFlags : byte
{
  None = 0,
  Null = 1,
  Carry = 2,
  PrevCarry = 4,
  DelRom = 8,
  Key = 0x10,
  DisplayOn = 0x20,
}
