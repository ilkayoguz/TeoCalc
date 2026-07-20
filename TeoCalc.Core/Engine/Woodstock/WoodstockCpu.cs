using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Woodstock;

/// <summary>Woodstock-family CPU (thin ACT shell; HP-21/22/25/27/29).</summary>
public sealed class WoodstockCpu : ActCpuBase
{
  public WoodstockCpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }
}
