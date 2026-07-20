using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Engine.Act;

/// <summary>Shared ACT-family CPU body (Woodstock / Spice). Mirrors Panamatik HP25/HPSpice.</summary>
public abstract class ActCpuBase : CpuBase, IActCpu
{
  private static readonly byte[] PSetMap =
  [
    14, 4, 7, 8, 11, 2, 10, 12, 1, 3,
    13, 6, 0, 9, 5, 14,
  ];

  private static readonly byte[] PTestMap =
  [
    4, 8, 12, 2, 9, 1, 6, 3, 1, 13,
    5, 0, 11, 10, 7, 4,
  ];

  private static readonly string[] Op0000 =
  [
    "op_nop", "op_keys_to_rom_addr", "op_sel_rom", "op_unknown", "op_crc_test_motor_on", "op_keys_to_a", "op_sel_rom", "op_unknown", "op_unknown", "op_a_to_rom_addr",
    "op_sel_rom", "op_crc_motor_on", "op_crc_test_f1", "op_display_reset_twf", "op_sel_rom", "op_crc_motor_off", "op_crc_set_f2", "op_binary", "op_sel_rom", "op_unknown",
    "op_crc_test_f2", "op_circulate_a_left", "op_sel_rom", "op_crc_test_card_in", "op_crc_set_f3", "op_dec_p", "op_sel_rom", "op_unknown", "op_crc_test_f3", "op_inc_p",
    "op_sel_rom", "op_crc_test_prot", "op_crc_set_f4", "op_return", "op_sel_rom", "op_bank_switch", "op_crc_test_f4", "op_pik_home", "op_sel_rom", "op_c_to_addr",
    "op_crc_set_f0", "op_pik_cr", "op_sel_rom", "op_clear_data_regs", "op_crc_clear_f0", "op_pik_keys", "op_sel_rom", "op_c_to_data", "op_crc_set_f1", "op_pik_c4",
    "op_sel_rom", "op_rom_selftest", "op_crc_clear_f1", "op_pik_d4", "op_sel_rom", "op_unknown", "op_unknown", "op_pik_e4", "op_sel_rom", "op_unknown",
    "op_crc_write_prot", "op_pik_print", "op_sel_rom", "op_nop",
  ];

  private static readonly string[] Op0100 = ["op_set_s", "op_test_s_eq_1", "op_test_p_eq", "op_del_sel_rom"];

  private static readonly string[] Op02xx = ["op_nop", "op_load_constant", "op_c_to_register", "op_register_to_c"];

  private static readonly string[] Op0300 = ["op_clr_s", "op_test_s_eq_0", "op_test_p_ne", "op_set_p"];

  private static readonly string[] Op0200 =
  [
    "op_clear_reg", "op_clear_s", "op_display_toggle", "op_display_off", "op_mx", "op_mx", "op_mx", "op_mx", "op_stack_to_a", "op_down_rotate",
    "op_y_to_a", "op_c_to_stack", "op_decimal", "op_unknown", "op_f_to_a", "op_f_exch_a",
  ];

  private bool _programMode;

  protected ActCpuBase(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers) : base(rom, handlers)
  {
    State = new ActCpuState();
  }

  public ActCpuState State { get; }

  public bool ProgramMode
  {
    get => _programMode;
    set => _programMode = value;
  }

  public override void Reset()
  {
    State.Reset();
    StepCount = 0;
  }

  public override void PressKey(byte keyCode) =>
    State.PressKey(keyCode);

  public void ReleaseKey() =>
    State.ReleaseKey();

  /// <summary>One Panamatik <c>act_execute_instruction</c> (cycles until ST.norm).</summary>
  public override MicrocodeHandlerEntry Step()
  {
    MicrocodeHandlerEntry? last = null;
    do
    {
      last = ExecuteCycle();
    }
    while (State.InstructionState != ActInstructionState.Norm);

    StepCount++;
    return last ?? Handlers.ResolveByPanamatikAlias("op_nop");
  }

  private MicrocodeHandlerEntry ExecuteCycle()
  {
    int address = State.ResolveFetchAddress();
    if (address < 0 || address >= Rom.WordCount)
    {
      throw new InvalidOperationException($"ROM address out of range: {address:X4}");
    }

    ushort opcode = Rom.ReadWord(address);
    if ((State.Flags & ActCpuFlags.Carry) != 0)
    {
      State.Flags |= ActCpuFlags.PrevCarry;
    }
    else
    {
      State.Flags &= ~ActCpuFlags.PrevCarry;
    }

    State.Flags &= ~ActCpuFlags.Carry;
    State.ProgramCounter++;

    string alias = "op_nop";
    switch (State.InstructionState)
    {
      case ActInstructionState.Norm:
        alias = ResolveNormAlias(opcode);
        Execute(alias, opcode);
        break;
      case ActInstructionState.Branch:
        alias = "op_branch_target";
        State.InstructionState = ActInstructionState.Norm;
        if ((State.Flags & ActCpuFlags.PrevCarry) == 0)
        {
          State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFC00) | opcode);
        }

        break;
      case ActInstructionState.SelfTest:
        alias = "op_rom_selftest";
        if (opcode == 1060)
        {
          State.Flags ^= ActCpuFlags.Bank;
        }

        if ((State.ProgramCounter & 0x3FF) == 0)
        {
          State.InstructionState = ActInstructionState.Norm;
          Execute("op_return", opcode);
        }

        break;
    }

    if ((opcode & 0x3D0) != 464)
    {
      State.Flags &= ~ActCpuFlags.PCarry;
    }

    MicrocodeHandlerEntry handler = Handlers.ResolveByPanamatikAlias(alias);
    State.LastOpcode = opcode;
    State.LastHandlerId = handler.HandlerId;
    return handler;
  }

  private static string ResolveNormAlias(ushort opcode) =>
    ((byte)opcode & 3) switch
    {
      0 => ((byte)opcode & 0xC) switch
      {
        0 => Op0000[opcode >> 4],
        4 => Op0100[((byte)opcode >> 4) & 3],
        8 => ((opcode >> 4) & 3) != 0
          ? Op02xx[((byte)opcode >> 4) & 3]
          : Op0200[opcode >> 6],
        _ => Op0300[((byte)opcode >> 4) & 3],
      },
      1 => "op_jsb",
      2 => "op_arith",
      _ => "op_goto",
    };

  private void Execute(string alias, ushort opcode)
  {
    switch (alias)
    {
      case "op_nop":
      case "op_unknown":
      case "op_crc_test_motor_on":
      case "op_crc_write_prot":
      case "op_crc_motor_on":
      case "op_crc_motor_off":
      case "op_crc_test_card_in":
      case "op_crc_test_prot":
      case "op_display_reset_twf":
      case "op_pik_home":
      case "op_pik_cr":
      case "op_pik_c4":
      case "op_pik_d4":
      case "op_pik_e4":
      case "op_pik_print":
      case "op_pik_keys":
      case "op_rom_addr_to_buffer":
        break;
      case "op_binary":
        State.Base = 16;
        break;
      case "op_decimal":
        State.Base = 10;
        break;
      case "op_goto":
        Goto(opcode);
        break;
      case "op_jsb":
        Jsb(opcode);
        break;
      case "op_return":
        Return();
        break;
      case "op_arith":
        ActCpuArithmetic.Execute(opcode, State);
        break;
      case "op_set_s":
        State.Status |= (ushort)(1 << (opcode >> 6));
        break;
      case "op_clr_s":
        State.Status &= (ushort)~(1 << (opcode >> 6));
        break;
      case "op_test_s_eq_0":
        State.InstructionState = ActInstructionState.Branch;
        SetCarry((State.Status & (1 << (opcode >> 6))) != 0);
        break;
      case "op_test_s_eq_1":
        State.InstructionState = ActInstructionState.Branch;
        SetCarry((State.Status & (1 << (opcode >> 6))) == 0);
        break;
      case "op_clear_s":
        State.Status &= 32806;
        break;
      case "op_dec_p":
        State.P = State.P != 0 ? (byte)(State.P - 1) : (byte)13;
        break;
      case "op_inc_p":
        State.P++;
        if (State.P >= 14)
        {
          State.P = 0;
          State.Flags |= ActCpuFlags.PCarry;
        }

        break;
      case "op_load_constant":
        State.Registers.C[State.P] = (byte)(opcode >> 6);
        Execute("op_dec_p", opcode);
        break;
      case "op_sel_rom":
        State.ProgramCounter = (ushort)(((opcode & 0x3C0) << 2) | (byte)State.ProgramCounter);
        break;
      case "op_del_sel_rom":
        State.DelRom = (byte)(opcode >> 6);
        State.Flags |= ActCpuFlags.DelRom;
        break;
      case "op_mx":
        ActCpuStack.Mx(State.Registers, opcode);
        break;
      case "op_a_to_rom_addr":
        State.ProgramCounter &= 0xFF00;
        HandleDelRom();
        State.RomAddress = (byte)((State.Registers.A[2] << 4) + State.Registers.A[1]);
        State.ProgramCounter += State.RomAddress;
        break;
      case "op_y_to_a":
        ActCpuStack.YToA(State.Registers);
        break;
      case "op_register_to_c":
        ActCpuDataRam.RegisterToCOpcode(State, opcode);
        break;
      case "op_c_to_register":
        ActCpuDataRam.CToRegisterOpcode(State, opcode);
        break;
      case "op_c_to_addr":
        ActCpuDataRam.CToAddress(State);
        break;
      case "op_c_to_data":
        ActCpuDataRam.CToData(State);
        break;
      case "op_clear_data_regs":
        ActCpuDataRam.ClearDataRegs(State);
        break;
      case "op_c_to_stack":
        ActCpuStack.CToStack(State.Registers);
        break;
      case "op_stack_to_a":
        ActCpuStack.StackToA(State.Registers);
        break;
      case "op_down_rotate":
        ActCpuStack.DownRotate(State.Registers);
        break;
      case "op_clear_reg":
        ActCpuStack.ClearRegisters(State.Registers);
        break;
      case "op_set_p":
        State.P = PSetMap[opcode >> 6];
        break;
      case "op_test_p_eq":
        State.InstructionState = ActInstructionState.Branch;
        SetCarry(State.P != PTestMap[opcode >> 6]);
        break;
      case "op_test_p_ne":
        TestPNe(opcode);
        break;
      case "op_keys_to_rom_addr":
        State.ProgramCounter &= 0xFF00;
        HandleDelRom();
        State.ProgramCounter += State.KeyBuffer;
        State.RomAddress = State.KeyBuffer;
        break;
      case "op_keys_to_a":
        State.Registers.A[2] = (byte)(State.KeyBuffer >> 4);
        State.Registers.A[1] = (byte)(State.KeyBuffer & 0xF);
        break;
      case "op_display_off":
        State.Flags &= ~ActCpuFlags.DisplayOn;
        break;
      case "op_display_toggle":
        State.Flags ^= ActCpuFlags.DisplayOn;
        break;
      case "op_f_to_a":
        State.Registers.A[0] = State.F;
        break;
      case "op_f_exch_a":
      {
        byte tmp = State.Registers.A[0];
        State.Registers.A[0] = State.F;
        State.F = tmp;
        break;
      }
      case "op_bank_switch":
        State.Flags ^= ActCpuFlags.Bank;
        break;
      case "op_rom_selftest":
        State.InstructionState = ActInstructionState.SelfTest;
        State.ProgramCounter &= 0xFC00;
        break;
      case "op_circulate_a_left":
        CirculateALeft();
        break;
      case "op_crc_clear_f0":
        if ((State.CrcFlags & 1) != 0)
        {
          State.Status |= 8;
          State.CrcFlags &= 254;
        }

        break;
      case "op_crc_clear_f1":
        if ((State.CrcFlags & 2) != 0)
        {
          State.Status |= 8;
          State.CrcFlags &= 253;
        }

        break;
      case "op_crc_set_f0":
        State.CrcFlags |= 1;
        break;
      case "op_crc_set_f1":
        State.CrcFlags |= 2;
        break;
      case "op_crc_set_f2":
        State.CrcFlags |= 4;
        break;
      case "op_crc_set_f3":
        State.CrcFlags |= 8;
        break;
      case "op_crc_set_f4":
        State.CrcFlags |= 16;
        break;
      case "op_crc_test_f1":
        if (_programMode)
        {
          State.Status |= 8;
        }

        State.CrcFlags &= 253;
        break;
      case "op_crc_test_f2":
        CrcTestFlag(4, 251);
        break;
      case "op_crc_test_f3":
        CrcTestFlag(8, 247);
        break;
      case "op_crc_test_f4":
        CrcTestFlag(16, 239);
        break;
    }
  }

  private void CrcTestFlag(byte flagBit, byte clearMask)
  {
    State.CrcFlags &= 127;
    if ((State.Status & 8) != 0)
    {
      State.CrcFlags |= 128;
    }

    State.Status &= 65527;
    if ((State.CrcFlags & flagBit) != 0)
    {
      State.Status |= 8;
    }

    State.CrcFlags = (byte)(State.CrcFlags & clearMask);
    if ((State.CrcFlags & 0x80) != 0)
    {
      State.CrcFlags |= flagBit;
    }
  }

  private void TestPNe(ushort opcode)
  {
    State.InstructionState = ActInstructionState.Branch;
    byte expected = PTestMap[opcode >> 6];
    if ((State.Flags & ActCpuFlags.PCarry) != 0 && State.P == 1 && expected == 0)
    {
      State.Flags |= ActCpuFlags.Carry;
    }
    else if (State.P != expected)
    {
      State.Flags &= ~ActCpuFlags.Carry;
    }
    else
    {
      State.Flags |= ActCpuFlags.Carry;
    }
  }

  private void CirculateALeft()
  {
    byte top = State.Registers.A[13];
    for (byte i = 13; i >= 1; i--)
    {
      State.Registers.A[i] = State.Registers.A[i - 1];
    }

    State.Registers.A[0] = top;
  }

  private void Goto(ushort opcode)
  {
    if ((State.Flags & ActCpuFlags.PrevCarry) == 0)
    {
      State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFF00) | (opcode >> 2));
      HandleDelRom();
    }
  }

  private void Jsb(ushort opcode)
  {
    State.ReturnStack[State.StackPointer] = State.ProgramCounter;
    State.StackPointer = (byte)((State.StackPointer + 1) & 1);
    State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFF00) | (opcode >> 2));
    HandleDelRom();
  }

  private void Return()
  {
    State.StackPointer = (byte)((State.StackPointer - 1) & 1);
    State.ProgramCounter = State.ReturnStack[State.StackPointer];
  }

  private void HandleDelRom()
  {
    if ((State.Flags & ActCpuFlags.DelRom) == 0)
    {
      return;
    }

    State.ProgramCounter = (ushort)((State.DelRom << 8) | (byte)State.ProgramCounter);
    State.Flags &= ~ActCpuFlags.DelRom;
  }

  private void SetCarry(bool carry)
  {
    if (carry)
    {
      State.Flags |= ActCpuFlags.Carry;
    }
    else
    {
      State.Flags &= ~ActCpuFlags.Carry;
    }
  }
}
