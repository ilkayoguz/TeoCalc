namespace TeoCalc.Core.Engine.Act;

/// <summary>Panamatik HP25 <c>ST</c> multi-cycle instruction state.</summary>
public enum ActInstructionState
{
  Norm,
  Branch,
  LongBranch,
  SelfTest,
}
