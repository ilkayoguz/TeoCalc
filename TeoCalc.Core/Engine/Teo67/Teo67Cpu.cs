using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Teo67;

/// <summary>HP-67 CPU (ACT ISA via <see cref="ActCpuBase"/>; Panamatik ACThp67).</summary>
public sealed class Teo67Cpu : ActCpuBase
{
  public Teo67Cpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }

  /// <summary>Panamatik HP-67 <c>Getopcode</c> bank-window remap into the 5120-word ROM.</summary>
  protected override int ResolveOpcodeFetchAddress()
  {
    int address = State.ResolveFetchAddress();
    if (address < 4096)
    {
      return address;
    }

    return address < 5120 || address >= 6144
      ? address - 4096
      : address - 1024;
  }
}
