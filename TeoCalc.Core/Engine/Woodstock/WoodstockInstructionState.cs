namespace TeoCalc.Core.Engine.Woodstock;

/// <summary>Panamatik HP25 <c>ST</c> multi-cycle instruction state.</summary>
public enum WoodstockInstructionState
{
  Norm,
  Branch,
  LongBranch,
  SelfTest,
}
