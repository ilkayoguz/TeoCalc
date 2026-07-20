namespace TeoCalc.Core.Engine.Hp01;

/// <summary>Panamatik <c>ACThp01.init_ops01</c> → full-opcode alias map.</summary>
public static class Hp01DispatchTable
{
  public static Dictionary<int, string> Build()
  {
    string[] fcn = new string[256];
    for (int i = 0; i < 256; i++)
    {
      fcn[i] = "op_unknown";
    }

    for (int i = 0; i < 16; i++)
    {
      fcn[(4 | (i << 6)) >> 2] = "op_set_s";
      fcn[(0xC | (i << 6)) >> 2] = "op_set_p";
      fcn[(0x14 | (i << 6)) >> 2] = "op_test_s_eq_0";
      fcn[(0x18 | (i << 6)) >> 2] = "op_load_constant";
      fcn[(0x20 | (i << 6)) >> 2] = "op_sel_rom";
      fcn[(0x24 | (i << 6)) >> 2] = "op_clr_s";
      fcn[(0x2C | (i << 6)) >> 2] = "op_test_p_ne";
      fcn[(0x34 | (i << 6)) >> 2] = "op_del_sel_rom";
    }

    fcn[0] = "op_nop";
    fcn[4] = "op_clr_s17";
    fcn[20] = "op_clr_s815";
    fcn[36] = "op_gokeys";
    fcn[52] = "op_inc_p";
    fcn[68] = "op_dec_p";
    fcn[84] = "op_return";
    fcn[100] = "op_sleep";
    fcn[7] = "op_clear_reg";
    fcn[23] = "op_cdex";
    fcn[39] = "op_mtoc";
    fcn[55] = "op_dtoc";
    fcn[71] = "op_ctom";
    fcn[119] = "op_display_on";
    fcn[135] = "op_display_off";
    fcn[151] = "op_blink";
    fcn[167] = "op_ftoap";
    fcn[183] = "op_aptof";
    fcn[199] = "op_enscwp";
    fcn[215] = "op_dsscwp";
    fcn[247] = "op_dsptoa";
    fcn[15] = "op_cltoa";
    fcn[31] = "op_atoclrs";
    fcn[47] = "op_atocl";
    fcn[63] = "op_cltodsp";
    fcn[79] = "op_atoal";
    fcn[95] = "op_swtoa";
    fcn[111] = "op_atosw";
    fcn[127] = "op_swtodsp";
    fcn[143] = "op_swdec";
    fcn[159] = "op_altodsp";
    fcn[175] = "op_swinc";
    fcn[191] = "op_atodsp";
    fcn[207] = "op_altoa";
    fcn[223] = "op_swstrt";
    fcn[239] = "op_swstop";
    fcn[255] = "op_altog";

    Dictionary<int, string> op = new(1024);
    for (int opcode = 0; opcode < 1024; opcode++)
    {
      op[opcode] = (opcode & 3) switch
      {
        0 => fcn[opcode >> 2],
        1 => "op_jsb",
        2 => "op_arith",
        _ => "op_goto",
      };
    }

    return op;
  }
}
