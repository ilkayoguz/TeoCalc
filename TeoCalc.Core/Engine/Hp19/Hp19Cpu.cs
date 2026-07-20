using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Hp19;

/// <summary>HP-19C CPU (ACT ISA variant via <see cref="ActCpuBase"/>; Panamatik ACThp19C).</summary>
public sealed class Hp19Cpu : ActCpuBase
{
  public Hp19Cpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }

  /// <summary>HP-19C has no bank-OR on fetch; status bit 0 remaps high ROM.</summary>
  protected override int ResolveOpcodeFetchAddress()
  {
    int addr = State.ProgramCounter;
    if ((State.Status & 1) != 0 && addr >= 3072)
    {
      addr += 1024;
    }

    return addr;
  }

  protected override ushort TransformFetchedOpcode(ushort opcode) =>
    (ushort)(opcode ^ 3040);

  /// <summary>After XOR 3040, low bits are rotated vs Woodstock (<c>bits ^ 2</c>).</summary>
  protected override string ResolveNormAlias(ushort opcode)
  {
    int remapped = (opcode & ~3) | (((byte)opcode & 3) ^ 2);
    return ResolveStandardNormAlias((ushort)remapped);
  }

  protected override void ApplyBranchTarget(ushort opcode) =>
    State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFC00) | (opcode ^ 2));

  /// <summary>
  /// HP-19C flag layout differs: INCP=0x80 (Woodstock Bank), BANK=0x10 (Woodstock Key).
  /// </summary>
  protected override void AfterCycle(ushort opcode)
  {
    _ = opcode;
    if ((State.Flags & ActCpuFlags.Bank) != 0)
    {
      State.Flags &= ~ActCpuFlags.Key;
    }
    else
    {
      State.Flags &= ~ActCpuFlags.Bank;
    }
  }

  protected override void ToggleBank() =>
    State.Flags ^= ActCpuFlags.Key;

  protected override void OnIncP() =>
    // HP-19C F.INCP == 0x80 == ActCpuFlags.Bank bit position.
    State.Flags |= ActCpuFlags.Bank;
}
