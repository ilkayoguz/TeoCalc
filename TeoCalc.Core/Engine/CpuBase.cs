using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine;

/// <summary>Shared CPU fields (ROM + handler catalog + step count).</summary>
public abstract class CpuBase : ICpu
{
  protected CpuBase(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
  {
    Rom = rom;
    Handlers = handlers;
  }

  protected IMicrocodeRom Rom { get; }

  protected MicrocodeHandlerCatalog Handlers { get; }

  public int StepCount { get; protected set; }

  public abstract void Reset();

  public abstract void PressKey(byte keyCode);

  public abstract MicrocodeHandlerEntry Step();
}
