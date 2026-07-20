using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Hp19;

/// <summary>HP-19C CPU (ACT ISA variant via <see cref="ActCpuBase"/>; Panamatik ACThp19C).</summary>
public sealed class Hp19Cpu : ActCpuBase
{
  /// <summary>Panamatik <c>DefaultRAM</c> continuous-memory seed at register address 0x2E (offset 322).</summary>
  private static readonly byte[] DefaultRamSeed = [0, 2, 64, 40, 52, 73, 3];

  /// <summary>
  /// Panamatik HP-19C <c>op_fcn_0000</c>: no <c>op_rom_selftest</c>; print slots are alpha/num.
  /// </summary>
  private static readonly string[] Op0000Hp19 =
  [
    "op_nop", "op_keys_to_rom_addr", "op_sel_rom", "op_unknown", "op_crc_test_motor_on", "op_keys_to_a", "op_sel_rom", "op_unknown", "op_unknown", "op_a_to_rom_addr",
    "op_sel_rom", "op_crc_motor_on", "op_crc_test_f1", "op_display_reset_twf", "op_sel_rom", "op_crc_motor_off", "op_crc_set_f2", "op_binary", "op_sel_rom", "op_unknown",
    "op_crc_test_f2", "op_circulate_a_left", "op_sel_rom", "op_crc_test_card_in", "op_crc_set_f3", "op_dec_p", "op_sel_rom", "op_unknown", "op_crc_test_f3", "op_inc_p",
    "op_sel_rom", "op_crc_test_prot", "op_crc_set_f4", "op_return", "op_sel_rom", "op_bank_switch", "op_crc_test_f4", "op_pik_home", "op_sel_rom", "op_c_to_addr",
    "op_crc_set_f0", "op_pik_cr", "op_sel_rom", "op_clear_data_regs", "op_crc_clear_f0", "op_pik_keys", "op_sel_rom", "op_c_to_data", "op_crc_set_f1", "op_pik_c4",
    "op_sel_rom", "op_unknown", "op_crc_clear_f1", "op_pik_d4", "op_sel_rom", "op_unknown", "op_unknown", "op_pik_e4", "op_sel_rom", "op_pik_print_alpha",
    "op_crc_write_prot", "op_pik_print_num", "op_sel_rom", "op_nop",
  ];

  /// <summary>Panamatik <c>act_switch</c> cold-start value (ON / run position).</summary>
  private byte _powerSwitch = 4;

  private bool _buttonPressed;

  public Hp19Cpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }

  public override void Reset()
  {
    base.Reset();
    _powerSwitch = 4;
    _buttonPressed = false;
    SuppressNextStatusPulse = false;
    SeedDefaultRam();
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

  protected override string ResolveOp0000Alias(ushort opcode) =>
    Op0000Hp19[opcode >> 4];

  protected override void ApplyBranchTarget(ushort opcode) =>
    State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFC00) | (opcode ^ 2));

  /// <summary>
  /// HP-19C flag layout differs: INCP=0x80 (Woodstock Bank), BANK=0x10 (Woodstock Key).
  /// Panamatik clears INCP one cycle after op_inc_p; otherwise clears PCARRY.
  /// </summary>
  protected override void AfterCycle(ushort opcode)
  {
    _ = opcode;
    if ((State.Flags & ActCpuFlags.Bank) != 0)
    {
      // Clear INCP (mapped onto ActCpuFlags.Bank).
      State.Flags &= ~ActCpuFlags.Bank;
    }
    else
    {
      State.Flags &= ~ActCpuFlags.PCarry;
    }
  }

  protected override void ToggleBank() =>
    State.Flags ^= ActCpuFlags.Key;

  protected override void OnIncP() =>
    // HP-19C F.INCP == 0x80 == ActCpuFlags.Bank bit position.
    State.Flags |= ActCpuFlags.Bank;

  /// <summary>Panamatik HP-19C key→C uses C[2]/C[1] (Woodstock uses C[0]).</summary>
  protected override void LoadKeyBufferIntoC()
  {
    State.Registers.C[2] = (byte)(State.KeyBuffer & 0xF);
    State.Registers.C[1] = (byte)(State.KeyBuffer >> 4);
  }

  /// <summary>Panamatik HP-19C <c>op_keys_to_a</c> reads <c>act_switch</c>, not the key buffer.</summary>
  protected override void LoadKeysToA()
  {
    State.Registers.A[2] = (byte)(_powerSwitch >> 4);
    State.Registers.A[1] = (byte)(_powerSwitch & 0xF);
  }

  /// <summary>Panamatik <c>op_pik_keys</c>: S3 reflects a pending key edge; suppress next timer S3 pulse.</summary>
  protected override void OnPikKeys()
  {
    if (_buttonPressed)
    {
      State.Status |= 8;
      _buttonPressed = false;
    }
    else
    {
      State.Status &= 65527;
    }

    SuppressNextStatusPulse = true;
  }

  /// <summary>Panamatik <c>op_pik_home</c> with motor idle: set S3 and suppress next timer S3 pulse.</summary>
  protected override void OnPikHome()
  {
    State.Status |= 8;
    SuppressNextStatusPulse = true;
  }

  /// <summary>Panamatik <c>op_pik_cr</c> with empty print buffer: set S3 and suppress next timer S3 pulse.</summary>
  protected override void OnPikCr()
  {
    State.Status |= 8;
    SuppressNextStatusPulse = true;
  }

  /// <summary>Latch a key-press edge for <c>op_pik_keys</c> (Panamatik <c>buttonpressed</c>).</summary>
  public void NotifyButtonPressed() =>
    _buttonPressed = true;

  private void SeedDefaultRam()
  {
    for (int i = 0; i < DefaultRamSeed.Length; i++)
    {
      State.Ram[322 + i] = DefaultRamSeed[i];
    }
  }
}
