namespace TeoCalc.Core.Engine.Classic;

/// <summary>Panamatik HPClassic.init_ops dispatch table (study reference).</summary>
public static class ClassicDispatchTable
{
  public static Dictionary<int, string> Build()
  {
    Dictionary<int, string> op = new();
    for (int i = 0; i < 1024; i += 4)
    {
      op[i] = "op_unknown";
      op[i + 1] = "op_jsb";
      op[i + 2] = "op_arith";
      op[i + 3] = "op_goto";
    }

    for (int i = 0; i < 16; i++)
    {
      op[4 + (i << 6)] = "op_set_s";
      op[20 + (i << 6)] = "op_test_s_eq_0";
      op[32 + (i << 6)] = "op_set_f";
      op[36 + (i << 6)] = "op_clr_s";
    }

    op[52] = "op_clear_s";
    for (int i = 0; i < 8; i++)
    {
      op[116 + (i << 7)] = "op_del_sel_rom";
    }

    op[564] = "op_del_sel_grp";
    op[692] = "op_del_sel_grp";
    for (int i = 0; i < 16; i++)
    {
      op[12 + (i << 6)] = "op_set_p";
      op[44 + (i << 6)] = "op_test_p";
    }

    op[28] = "op_dec_p";
    op[60] = "op_inc_p";
    for (int i = 0; i < 10; i++)
    {
      op[24 + (i << 6)] = "op_load_constant";
    }

    for (int i = 0; i < 2; i++)
    {
      op[40] = "op_display_toggle";
      op[168] = "op_c_exch_m";
      op[296] = "op_c_to_stack";
      op[424] = "op_stack_to_a";
      op[552] = "op_display_off";
      op[680] = "op_m_to_c";
      op[808] = "op_down_rotate";
      op[936] = "op_clear_regs";
      for (int j = 0; j <= 3; j++)
      {
        op[232 + (j << 8) + (i << 4)] = "op_data_to_c";
      }
    }

    for (int i = 0; i <= 7; i++)
    {
      op[16 + (i << 7)] = "op_sel_rom";
      op[48] = "op_return";
      if ((i & 1) != 0)
      {
        op[208] = "op_keys_to_rom_addr";
      }
    }

    op[624] = "op_c_to_addr";
    op[752] = "op_c_to_data";
    op[0] = "op_nop";
    op[120] = "op_memoryfull";
    op[64] = "op_buf_to_rom_addr";
    op[128] = "op_memoryinsert";
    op[256] = "op_markandsearch";
    op[384] = "op_memorydelete";
    op[512] = "op_rom_addr_to_buf";
    op[640] = "op_searchforlabel";
    op[768] = "op_pointeradvance";
    op[896] = "op_memoryinitialize";
    return op;
  }
}
