namespace TeoCalc.Core.Firmware;

/// <summary>Compact working-register digests for the DEBUG/TRACE panel.</summary>
public sealed record FirmwareRegisterDigest(
  string Name,
  string DigitsHex);

/// <summary>Optional register bank exposed by native gateways for debug UI / DUMP.</summary>
public sealed record FirmwareDebugRegisters(
  IReadOnlyList<FirmwareRegisterDigest> Working);

/// <summary>One hardware return-stack slot.</summary>
/// <param name="Index">Slot index (0 = Ret0 / Stack[0]).</param>
/// <param name="Address">Saved return address.</param>
/// <param name="IsTop">True for the slot Return would pop next (when known).</param>
public sealed record FirmwareCallStackSlot(
  int Index,
  ushort Address,
  bool IsTop);

/// <summary>Hardware return stack for Machine debug (Classic/Act/Teo01: 2 slots).</summary>
public sealed record FirmwareCallStackSnapshot(
  IReadOnlyList<FirmwareCallStackSlot> Slots,
  int? StackPointer)
{
  public static FirmwareCallStackSnapshot FromHardware(
    IReadOnlyList<ushort> slots,
    int? stackPointer = null)
  {
    List<FirmwareCallStackSlot> list = new(slots.Count);
    int topIndex = stackPointer is int sp && slots.Count > 0
      ? (sp - 1) & (slots.Count - 1)
      : 0;
    for (int i = 0; i < slots.Count; i++)
    {
      list.Add(new FirmwareCallStackSlot(i, slots[i], IsTop: i == topIndex));
    }

    return new FirmwareCallStackSnapshot(list, stackPointer);
  }
}
