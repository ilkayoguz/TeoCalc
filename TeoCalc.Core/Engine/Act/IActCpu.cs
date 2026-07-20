namespace TeoCalc.Core.Engine.Act;

/// <summary>ACT-family extensions beyond <see cref="ICpu"/>.</summary>
public interface IActCpu : ICpu
{
  bool ProgramMode { get; set; }

  void ReleaseKey();

  ActCpuState State { get; }
}
