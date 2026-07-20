using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine;

/// <summary>Family-neutral CPU contract: reset, key, one instruction step.</summary>
public interface ICpu
{
  int StepCount { get; }

  void Reset();

  void PressKey(byte keyCode);

  MicrocodeHandlerEntry Step();
}
