namespace TeoCalc.Core.Engine;

/// <summary>Little-endian ushort firmware ROM image shared by all native CPU families.</summary>
public interface IMicrocodeRom
{
  int WordCount { get; }

  ReadOnlySpan<ushort> Words { get; }

  ushort ReadWord(int linearAddress);
}
