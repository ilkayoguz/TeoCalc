namespace TeoCalc.Core.Engine.Spice;

/// <summary>Panamatik HP25 <c>ST</c> multi-cycle instruction state.</summary>
public enum SpiceInstructionState
{
  Norm,
  Branch,
  LongBranch,
  SelfTest,
}
