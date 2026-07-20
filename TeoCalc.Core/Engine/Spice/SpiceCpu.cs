using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Spice;

/// <summary>Spice-family CPU (thin ACT shell; HP-31/32/33/34/37/38).</summary>
public sealed class SpiceCpu : ActCpuBase
{
  public SpiceCpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }
}
