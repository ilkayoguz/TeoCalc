namespace TeoCalc.Core.Firmware;

/// <summary>Compact working-register digests for the DEBUG/TRACE panel.</summary>
public sealed record FirmwareRegisterDigest(
  string Name,
  string DigitsHex);

/// <summary>Optional register bank exposed by native gateways for debug UI / DUMP.</summary>
public sealed record FirmwareDebugRegisters(
  IReadOnlyList<FirmwareRegisterDigest> Working);
