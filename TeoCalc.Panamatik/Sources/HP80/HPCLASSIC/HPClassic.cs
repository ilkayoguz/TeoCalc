using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP80;

public class HPClassic : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int EXPSIZE = 3;

	private const int NROFIMAGES = 1;

	private const int BUTTONS = 35;

	private const int RAMSIZE = 448;

	private const int FIRSTROW = 127;

	private const int FIRSTCOL = 25;

	private const int FIRSTCOL2 = 12;

	private const int ROWSIZE = 44;

	private const int COLSIZE = 42;

	private const int COLSIZE2 = 60;

	private F act_flags;

	private byte[] src;

	private byte[] dest;

	private byte[] src2;

	private byte first;

	private byte last;

	private byte act_key_buf;

	private byte act_ram_addr;

	private byte act_del_rom;

	private byte rom_addr;

	private ushort[] act_stack;

	public byte[] act_a;

	public byte[] act_b;

	public byte[] act_c;

	public byte[] act_y;

	public byte[] act_z;

	public byte[] act_t;

	public byte[] act_m;

	public byte act_base;

	public byte act_sp;

	public byte act_p;

	public byte act_f;

	public ushort act_pc;

	public ushort opcode;

	public ushort act_s;

	public byte[] act_ram;

	private op_fcn[] op_fcn0;

	private byte act_rom;

	public byte act_ram_size = 10;

	private byte endstate;

	private int memlen = 102;

	private int buffer;

	private int busy;

	private int over;

	private bool running = true;

	private bool prgmmode;

	private bool timermode;

	private bool focus;

	private bool buttonpressed;

	private bool SegmentFont;

	private string[] ImageTable;

	private int ImageNr;

	private Size OriginalSize;

	private string[] HP55Mnemonics = new string[0];

	private string[] HP65Mnemonics = new string[0];

	private char[] HPClassicKeyChartable = new char[40]
	{
		'z', 'g', 'l', 'e', 'c', 'q', 'a', 's', 'c', 't',
		'r', 'y', 'd', 'o', 'r', '\r', '\r', 'n', 'x', '\b',
		'-', '7', '8', '9', '\0', '+', '4', '5', '6', '\0',
		'*', '1', '2', '3', '\0', '/', '0', '.', 'p', '\0'
	};

	private byte[] HPClassicKeytable = new byte[40]
	{
		30, 28, 27, 26, 24, 46, 44, 43, 42, 40,
		14, 12, 11, 10, 8, 62, 62, 59, 58, 56,
		54, 52, 51, 50, 0, 22, 20, 19, 18, 0,
		6, 4, 3, 2, 0, 38, 36, 35, 34, 0
	};

	public ushort[] opcodeint = new ushort[1792]
	{
		713, 371, 1002, 1002, 1002, 147, 424, 144, 808, 643,
		144, 1015, 808, 319, 424, 135, 0, 0, 1002, 1002,
		1002, 11, 424, 144, 708, 123, 123, 644, 123, 48,
		468, 195, 400, 144, 144, 324, 144, 0, 424, 144,
		784, 0, 528, 528, 424, 144, 452, 267, 452, 319,
		1002, 1002, 1002, 75, 254, 89, 206, 273, 987, 532,
		307, 311, 296, 740, 676, 484, 292, 323, 468, 571,
		276, 295, 571, 296, 296, 251, 296, 680, 484, 260,
		548, 398, 302, 780, 214, 482, 482, 442, 511, 234,
		442, 583, 324, 922, 391, 206, 321, 206, 370, 218,
		958, 323, 292, 612, 780, 36, 28, 812, 427, 552,
		596, 487, 356, 266, 208, 580, 76, 340, 491, 498,
		463, 40, 20, 463, 423, 298, 663, 633, 746, 1002,
		266, 814, 522, 503, 298, 874, 623, 236, 603, 144,
		28, 551, 484, 292, 327, 394, 633, 1002, 874, 615,
		362, 563, 559, 710, 593, 28, 582, 541, 144, 48,
		144, 538, 663, 1002, 890, 202, 204, 812, 967, 558,
		420, 413, 758, 276, 707, 296, 420, 935, 936, 52,
		132, 295, 1002, 6, 923, 236, 767, 206, 510, 510,
		844, 594, 6, 815, 780, 610, 815, 746, 874, 610,
		815, 262, 795, 298, 558, 942, 356, 409, 558, 202,
		502, 12, 354, 60, 354, 875, 274, 855, 810, 302,
		340, 899, 388, 815, 404, 731, 28, 172, 767, 771,
		590, 558, 409, 206, 46, 844, 216, 548, 362, 554,
		847, 610, 679, 354, 60, 671, 254, 414, 394, 415,
		0, 0, 0, 168, 680, 267, 0, 0, 68, 219,
		68, 311, 612, 272, 921, 311, 272, 516, 468, 91,
		484, 1009, 311, 528, 215, 68, 196, 311, 25, 307,
		484, 433, 311, 484, 16, 48, 276, 195, 111, 0,
		831, 367, 311, 468, 111, 123, 484, 251, 296, 468,
		259, 227, 942, 171, 52, 208, 0, 0, 68, 196,
		132, 311, 484, 429, 808, 296, 362, 362, 913, 309,
		921, 362, 362, 309, 808, 362, 362, 750, 1022, 718,
		942, 895, 424, 16, 222, 218, 674, 355, 146, 494,
		446, 355, 598, 662, 942, 414, 675, 468, 695, 276,
		683, 400, 202, 212, 607, 490, 682, 682, 148, 607,
		84, 627, 643, 254, 302, 1018, 1018, 506, 506, 74,
		467, 942, 614, 991, 491, 710, 614, 995, 378, 378,
		746, 958, 862, 638, 531, 982, 862, 272, 326, 543,
		182, 390, 272, 0, 0, 0, 482, 202, 370, 558,
		942, 28, 315, 0, 0, 0, 0, 84, 619, 490,
		148, 635, 490, 490, 16, 387, 16, 808, 468, 935,
		484, 869, 296, 35, 420, 16, 254, 484, 260, 424,
		548, 296, 433, 808, 398, 921, 276, 735, 254, 424,
		296, 942, 285, 276, 763, 382, 433, 808, 424, 433,
		296, 808, 808, 311, 285, 429, 558, 808, 942, 913,
		516, 1009, 424, 296, 942, 311, 808, 808, 254, 808,
		808, 895, 0, 558, 808, 424, 558, 296, 142, 552,
		979, 276, 71, 400, 950, 927, 272, 330, 272, 424,
		869, 296, 942, 913, 808, 921, 424, 429, 869, 795,
		468, 119, 903, 942, 74, 491, 1002, 479, 102, 43,
		48, 0, 16, 910, 382, 7, 942, 278, 942, 243,
		424, 296, 942, 750, 388, 838, 3, 718, 382, 3,
		510, 302, 91, 722, 894, 87, 766, 910, 404, 363,
		866, 75, 818, 274, 926, 903, 460, 243, 1002, 634,
		895, 790, 147, 918, 270, 362, 151, 718, 210, 938,
		446, 215, 814, 782, 238, 718, 548, 243, 510, 782,
		227, 910, 558, 206, 358, 280, 486, 590, 364, 839,
		396, 754, 844, 558, 942, 408, 391, 748, 667, 408,
		600, 216, 88, 280, 472, 88, 536, 24, 344, 408,
		716, 863, 994, 302, 382, 91, 722, 942, 278, 942,
		894, 367, 814, 994, 551, 598, 270, 231, 620, 467,
		460, 216, 216, 24, 536, 344, 588, 863, 684, 303,
		588, 216, 88, 24, 88, 472, 600, 536, 88, 652,
		863, 918, 354, 519, 718, 60, 876, 523, 490, 766,
		780, 610, 587, 270, 362, 622, 559, 206, 938, 698,
		603, 1014, 938, 638, 531, 934, 398, 532, 891, 404,
		667, 420, 596, 919, 206, 28, 344, 927, 812, 863,
		206, 152, 216, 24, 152, 344, 536, 344, 24, 600,
		216, 780, 942, 404, 803, 334, 26, 751, 334, 814,
		28, 270, 108, 755, 942, 446, 787, 230, 490, 40,
		716, 523, 302, 390, 698, 159, 506, 718, 490, 823,
		195, 556, 427, 332, 216, 216, 524, 558, 404, 415,
		718, 60, 60, 11, 144, 998, 183, 830, 598, 1022,
		79, 808, 296, 204, 458, 350, 947, 190, 814, 750,
		318, 812, 523, 6, 3, 946, 987, 482, 790, 983,
		918, 278, 28, 44, 987, 414, 942, 551, 281, 942,
		909, 83, 516, 144, 548, 39, 516, 144, 917, 281,
		724, 247, 429, 25, 808, 25, 281, 359, 424, 296,
		869, 379, 740, 819, 275, 811, 439, 0, 398, 808,
		983, 276, 979, 208, 0, 808, 168, 808, 296, 269,
		429, 424, 296, 942, 281, 429, 17, 808, 296, 808,
		808, 942, 909, 680, 429, 676, 740, 311, 0, 942,
		425, 814, 909, 63, 0, 144, 740, 291, 144, 708,
		837, 269, 429, 865, 315, 16, 17, 281, 942, 660,
		371, 425, 808, 808, 808, 724, 363, 942, 909, 424,
		424, 424, 917, 231, 660, 151, 292, 528, 490, 909,
		281, 429, 424, 479, 144, 144, 144, 724, 899, 740,
		660, 751, 837, 254, 296, 424, 808, 909, 168, 808,
		296, 281, 429, 814, 917, 168, 424, 296, 837, 808,
		425, 429, 168, 909, 168, 808, 168, 424, 281, 429,
		296, 296, 142, 168, 17, 808, 296, 917, 142, 808,
		808, 909, 808, 808, 281, 425, 680, 909, 808, 808,
		425, 424, 296, 558, 429, 808, 558, 808, 558, 909,
		680, 917, 168, 429, 168, 398, 945, 634, 843, 551,
		917, 869, 490, 708, 403, 424, 942, 909, 424, 296,
		942, 281, 942, 909, 17, 281, 425, 490, 490, 229,
		724, 287, 660, 295, 254, 293, 0, 144, 724, 231,
		808, 808, 808, 3, 144, 206, 482, 510, 690, 598,
		490, 48, 660, 923, 467, 144, 48, 144, 942, 808,
		296, 296, 296, 731, 680, 490, 490, 1002, 1002, 1002,
		1002, 48, 528, 909, 269, 429, 808, 424, 424, 558,
		942, 660, 63, 43, 16, 0, 0, 400, 48, 57,
		452, 292, 281, 429, 135, 424, 296, 48, 808, 808,
		808, 48, 340, 19, 656, 468, 23, 127, 3, 0,
		656, 987, 324, 656, 3, 276, 927, 808, 292, 424,
		808, 398, 61, 425, 45, 281, 363, 87, 468, 203,
		276, 439, 292, 615, 452, 276, 287, 292, 808, 599,
		400, 942, 206, 216, 408, 490, 490, 48, 425, 45,
		656, 144, 808, 227, 144, 452, 45, 942, 281, 429,
		808, 296, 917, 808, 558, 808, 814, 429, 808, 424,
		429, 296, 61, 227, 429, 680, 917, 429, 45, 142,
		917, 45, 942, 227, 168, 269, 680, 942, 168, 651,
		144, 144, 144, 45, 296, 909, 808, 281, 429, 142,
		808, 917, 424, 909, 429, 808, 296, 425, 429, 814,
		429, 57, 281, 425, 424, 909, 429, 808, 814, 425,
		424, 45, 142, 917, 424, 429, 254, 168, 206, 296,
		680, 296, 275, 281, 429, 296, 296, 424, 424, 398,
		61, 917, 680, 429, 45, 275, 680, 909, 808, 424,
		425, 45, 142, 425, 168, 281, 429, 296, 168, 13,
		168, 281, 425, 45, 917, 808, 281, 429, 45, 942,
		13, 424, 168, 281, 425, 680, 917, 168, 281, 425,
		808, 254, 296, 917, 808, 424, 424, 168, 425, 680,
		917, 227, 917, 168, 424, 45, 229, 780, 909, 808,
		229, 344, 780, 909, 45, 425, 61, 424, 942, 296,
		259, 144, 0, 144, 144, 292, 168, 808, 296, 296,
		281, 429, 558, 917, 168, 942, 909, 168, 139, 208,
		292, 168, 269, 942, 296, 909, 424, 808, 168, 835,
		0, 0, 528, 808, 296, 942, 296, 206, 88, 536,
		152, 344, 24, 490, 490, 780, 48, 206, 88, 536,
		55, 660, 99, 48, 784, 0, 0, 324, 277, 439,
		249, 708, 302, 398, 433, 814, 909, 168, 265, 942,
		909, 13, 909, 122, 371, 233, 680, 808, 680, 277,
		429, 249, 942, 711, 424, 356, 400, 808, 808, 808,
		48, 808, 424, 296, 48, 0, 528, 324, 283, 528,
		680, 917, 265, 942, 909, 245, 680, 917, 265, 942,
		909, 808, 724, 219, 223, 808, 398, 634, 67, 874,
		262, 351, 424, 680, 429, 249, 69, 909, 277, 425,
		558, 168, 254, 917, 277, 871, 144, 144, 144, 686,
		909, 168, 265, 13, 909, 122, 619, 245, 277, 686,
		942, 429, 142, 909, 808, 424, 808, 254, 9, 341,
		942, 277, 429, 9, 229, 249, 425, 429, 680, 917,
		808, 53, 808, 398, 680, 917, 808, 424, 909, 424,
		429, 808, 425, 223, 245, 69, 909, 808, 917, 277,
		686, 429, 168, 277, 53, 429, 142, 168, 909, 429,
		229, 277, 425, 168, 917, 808, 1019, 9, 277, 425,
		865, 229, 425, 865, 808, 942, 909, 680, 917, 249,
		429, 424, 233, 142, 245, 680, 429, 168, 142, 53,
		53, 53, 422, 827, 442, 195, 724, 1007, 740, 808,
		296, 341, 942, 277, 425, 923, 144, 429, 424, 909,
		277, 425, 680, 909, 1011, 0, 0, 144, 0, 144,
		142, 917, 680, 917, 233, 398, 233, 917, 277, 686,
		942, 429, 142, 909, 249, 917, 808, 424, 917, 296,
		191, 680, 53, 398, 429, 223, 16, 260, 947, 620,
		563, 48, 814, 172, 551, 998, 358, 426, 63, 998,
		358, 48, 216, 408, 344, 276, 95, 1002, 48, 152,
		344, 48, 0, 0, 25, 814, 454, 28, 44, 115,
		202, 398, 468, 587, 424, 942, 627, 468, 183, 276,
		195, 484, 296, 296, 424, 468, 211, 296, 942, 943,
		428, 15, 48, 0, 342, 247, 182, 558, 424, 808,
		254, 421, 766, 206, 332, 88, 536, 942, 534, 407,
		296, 142, 417, 356, 484, 16, 60, 362, 535, 942,
		6, 3, 25, 454, 814, 276, 371, 127, 518, 127,
		3, 750, 562, 65, 524, 562, 451, 142, 417, 299,
		750, 324, 452, 780, 144, 910, 354, 439, 654, 28,
		236, 443, 878, 479, 1006, 1002, 60, 590, 620, 483,
		216, 268, 562, 590, 28, 1004, 511, 426, 3, 812,
		323, 3, 0, 300, 219, 48, 748, 575, 48, 870,
		486, 48, 354, 808, 596, 935, 276, 7, 612, 947,
		12, 582, 482, 619, 106, 3, 446, 655, 166, 982,
		460, 206, 472, 216, 24, 344, 86, 3, 524, 65,
		46, 558, 614, 719, 3, 998, 654, 28, 739, 482,
		782, 735, 910, 44, 723, 614, 775, 910, 362, 106,
		787, 878, 268, 216, 12, 558, 206, 938, 60, 1002,
		198, 29, 774, 839, 847, 614, 811, 902, 966, 262,
		998, 810, 206, 378, 974, 814, 844, 28, 270, 492,
		887, 910, 60, 270, 812, 907, 1002, 942, 307, 276,
		235, 292, 362, 963, 582, 202, 106, 3, 46, 398,
		524, 210, 152, 88, 524, 82, 3, 88, 600, 524,
		338, 383
	};

	private IContainer components;

	private PictureBox pictureBox1;

	private global::System.Windows.Forms.Timer timer1;

	private TextBox textBoxDisplay;

	private Label labelHPType;

	private Label label2;

	private Label label3;

	private Button buttonLoad;

	private Button buttonSave;

	private TextBox textBox1;

	public void ACTClassic()
	{
		op_fcn0 = new op_fcn[1024];
		init_ops();
		act_a = new byte[14];
		act_b = new byte[14];
		act_c = new byte[14];
		act_y = new byte[14];
		act_z = new byte[14];
		act_t = new byte[14];
		act_m = new byte[14];
		act_stack = new ushort[2];
		act_ram = new byte[448];
		act_reset();
	}

	private void init_ops()
	{
		for (int i = 0; i < 1024; i += 4)
		{
			op_fcn0[i] = op_unknown;
			op_fcn0[i + 1] = op_jsb;
			op_fcn0[i + 2] = op_arith;
			op_fcn0[i + 3] = op_goto;
		}
		for (int i = 0; i < 16; i++)
		{
			op_fcn0[4 + (i << 6)] = op_set_s;
			op_fcn0[20 + (i << 6)] = op_test_s_eq_0;
			op_fcn0[32 + (i << 6)] = op_setf;
			op_fcn0[36 + (i << 6)] = op_clr_s;
			op_fcn0[52] = op_clear_s;
		}
		for (int i = 0; i <= 7; i++)
		{
			op_fcn0[116 + (i << 7)] = op_del_sel_rom;
		}
		op_fcn0[564] = op_del_sel_grp;
		op_fcn0[692] = op_del_sel_grp;
		for (int i = 0; i <= 15; i++)
		{
			op_fcn0[12 + (i << 6)] = op_set_p;
			op_fcn0[44 + (i << 6)] = op_test_p;
			op_fcn0[28] = op_dec_p;
			op_fcn0[60] = op_inc_p;
		}
		for (int i = 0; i <= 9; i++)
		{
			op_fcn0[24 + (i << 6)] = op_load_constant;
		}
		for (int i = 0; i <= 1; i++)
		{
			op_fcn0[40] = op_display_toggle;
			op_fcn0[168] = op_c_exch_m;
			op_fcn0[296] = op_c_to_stack;
			op_fcn0[424] = op_stack_to_a;
			op_fcn0[552] = op_display_off;
			op_fcn0[680] = op_m_to_c;
			op_fcn0[808] = op_down_rotate;
			op_fcn0[936] = op_clear_reg;
			for (int j = 0; j <= 3; j++)
			{
				op_fcn0[232 + (j << 8) + (i << 4)] = op_data_to_c;
			}
		}
		for (int i = 0; i <= 7; i++)
		{
			op_fcn0[16 + (i << 7)] = op_sel_rom;
			op_fcn0[48] = op_return;
			if ((i & 1) != 0)
			{
				op_fcn0[208] = op_keys_to_rom_addr;
			}
		}
		op_fcn0[624] = op_c_to_addr;
		op_fcn0[752] = op_c_to_data;
		op_fcn0[0] = op_nop;
		op_fcn0[120] = op_memoryfull;
		op_fcn0[64] = op_buf_to_rom_addr;
		op_fcn0[128] = op_memoryinsert;
		op_fcn0[256] = op_markandsearch;
		op_fcn0[384] = op_memorydelete;
		op_fcn0[512] = op_rom_addr_to_buf;
		op_fcn0[640] = op_searchforlabel;
		op_fcn0[768] = op_pointeradvance;
		op_fcn0[896] = op_memoryinitialize;
	}

	private void op_setfield()
	{
		switch ((byte)((opcode >> 2) & 7))
		{
		case 0:
			first = act_p;
			last = act_p;
			break;
		case 1:
			first = 3;
			last = 12;
			break;
		case 2:
			first = 0;
			last = 2;
			break;
		case 3:
			first = 0;
			last = 13;
			break;
		case 4:
			first = 0;
			last = act_p;
			break;
		case 5:
			first = 3;
			last = 13;
			break;
		case 6:
			first = 2;
			last = 2;
			break;
		case 7:
			first = 13;
			last = 13;
			break;
		}
	}

	private void op_arith()
	{
		op_setfield();
		switch ((byte)(opcode >> 5))
		{
		case 0:
			src = act_b;
			dest = null;
			reg_test_nonequal();
			break;
		case 1:
			dest = act_b;
			reg_zero();
			break;
		case 2:
			dest = null;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 3:
			src = act_c;
			dest = null;
			reg_test_equal();
			break;
		case 4:
			dest = act_c;
			src = act_b;
			reg_copy();
			break;
		case 5:
			dest = act_c;
			src = null;
			src2 = act_c;
			reg_sub();
			break;
		case 6:
			dest = act_c;
			reg_zero();
			break;
		case 7:
			act_flags |= F.CARRY;
			dest = act_c;
			src = null;
			src2 = act_c;
			reg_sub();
			break;
		case 8:
			dest = act_a;
			reg_shift_left();
			break;
		case 9:
			dest = act_b;
			src = act_a;
			reg_copy();
			break;
		case 10:
			dest = act_c;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 11:
			act_flags |= F.CARRY;
			dest = act_c;
			src = act_c;
			src2 = null;
			reg_sub();
			break;
		case 12:
			dest = act_a;
			src = act_c;
			reg_copy();
			break;
		case 13:
			src = act_c;
			dest = null;
			reg_test_nonequal();
			break;
		case 14:
			dest = act_c;
			src = act_a;
			reg_add();
			break;
		case 15:
			dest = act_c;
			reg_inc();
			break;
		case 16:
			dest = null;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 17:
			dest = act_b;
			src = act_c;
			reg_exch();
			break;
		case 18:
			dest = act_c;
			reg_shift_right();
			break;
		case 19:
			src = act_a;
			dest = null;
			reg_test_equal();
			break;
		case 20:
			dest = act_b;
			reg_shift_right();
			break;
		case 21:
			dest = act_c;
			src = act_c;
			reg_add();
			break;
		case 22:
			dest = act_a;
			reg_shift_right();
			break;
		case 23:
			dest = act_a;
			reg_zero();
			break;
		case 24:
			dest = act_a;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 25:
			dest = act_a;
			src = act_b;
			reg_exch();
			break;
		case 26:
			dest = act_a;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 27:
			act_flags |= F.CARRY;
			dest = act_a;
			src = act_a;
			src2 = null;
			reg_sub();
			break;
		case 28:
			dest = act_a;
			src = act_b;
			reg_add();
			break;
		case 29:
			dest = act_a;
			src = act_c;
			reg_exch();
			break;
		case 30:
			dest = act_a;
			src = act_c;
			reg_add();
			break;
		case 31:
			dest = act_a;
			reg_inc();
			break;
		}
	}

	private void op_goto()
	{
		if ((act_flags & F.PREV_CARRY) == 0)
		{
			act_rom = act_del_rom;
			act_pc = (ushort)(((act_rom << 8) | (opcode >> 2)) + over);
			over = 0;
		}
	}

	private void op_jsb()
	{
		act_stack[0] = (byte)act_pc;
		act_rom = act_del_rom;
		act_pc = (ushort)((act_rom << 8) | (opcode >> 2));
	}

	private void op_return()
	{
		act_pc = (ushort)((act_rom << 8) | act_stack[0]);
	}

	private void op_clear_s()
	{
		act_s = 0;
	}

	private void op_del_sel_rom()
	{
		act_del_rom = (byte)((act_del_rom & 8) | (opcode >> 7));
	}

	private void op_del_sel_grp()
	{
		act_del_rom = (byte)((act_del_rom & 7) | ((opcode >> 4) & 8));
	}

	private void op_set_p()
	{
		act_p = (byte)(opcode >> 6);
	}

	private void op_test_p()
	{
		if (act_p == opcode >> 6)
		{
			act_flags |= F.CARRY;
		}
		else
		{
			act_flags &= (F)(-3);
		}
	}

	private void op_dec_p()
	{
		act_p = (byte)((act_p - 1) & 0xF);
	}

	private void op_inc_p()
	{
		act_p = (byte)((act_p + 1) & 0xF);
	}

	private void op_load_constant()
	{
		if (act_p < 14 && opcode >> 6 < 10)
		{
			act_c[act_p] = (byte)(opcode >> 6);
		}
		op_dec_p();
	}

	private void op_c_exch_m()
	{
		for (byte b = 0; b < 14; b++)
		{
			byte b2 = act_c[b];
			act_c[b] = act_m[b];
			act_m[b] = b2;
		}
	}

	private void op_m_to_c()
	{
		for (int i = 0; i < 14; i++)
		{
			act_c[i] = act_m[i];
		}
	}

	private void op_data_to_c()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_c[b] = act_ram[act_ram_addr * 14 + b];
		}
	}

	private int pointerpos()
	{
		int num = ((memlen == 103) ? 57 : 61);
		int i;
		for (i = 1; act_ram[i] != num; i++)
		{
		}
		return i;
	}

	private int label_pos(int n, int n2)
	{
		while (n < memlen && (act_ram[n - 1] != 43 || act_ram[n] != n2))
		{
			n++;
		}
		if (n == memlen)
		{
			n = 1;
			while (n < memlen && (act_ram[n - 1] != 43 || act_ram[n] != n2))
			{
				n++;
			}
			if (n == memlen)
			{
				n = 0;
			}
		}
		return n;
	}

	private void cleanup(int n)
	{
		int num = pointerpos();
		buffer = act_ram[num - 1];
		endstate = 0;
		if (act_ram[memlen - 1] != 0)
		{
			endstate = 1;
		}
		if (num == memlen - 1)
		{
			endstate += 2;
		}
		busy = n;
	}

	private void insert_at(int n, int n2)
	{
		while (n < memlen)
		{
			int num = act_ram[n];
			act_ram[n] = (byte)n2;
			n2 = num;
			n++;
		}
	}

	private void delete_at(int n)
	{
		for (n++; n < memlen; n++)
		{
			act_ram[n - 1] = act_ram[n];
		}
		act_ram[n - 1] = 0;
	}

	private void op_rom_addr_to_buf()
	{
	}

	private void op_memoryfull()
	{
		act_a[0] = (byte)(endstate & 1);
		if (endstate >= 2)
		{
			act_f |= 4;
		}
	}

	private void op_buf_to_rom_addr()
	{
		over = buffer;
	}

	private void op_memoryinsert()
	{
		int num = pointerpos();
		if (num < memlen - 1)
		{
			insert_at(num, buffer);
			cleanup(10);
		}
		else
		{
			busy = 5;
		}
	}

	private void op_markandsearch()
	{
		int n = pointerpos();
		if (memlen == 103)
		{
			delete_at(n);
		}
		memlen = 103;
		if ((n = label_pos(n, buffer)) == 0)
		{
			memlen = 102;
		}
		else
		{
			insert_at(n + 1, 57);
		}
		cleanup(12);
	}

	private void op_memorydelete()
	{
		int num = pointerpos();
		if (num > 1)
		{
			delete_at(num - 1);
		}
		cleanup(10);
	}

	private void op_searchforlabel()
	{
		int num = pointerpos();
		int n = act_ram[num];
		delete_at(num);
		num = label_pos(num, buffer);
		if (num == 0 && memlen == 103)
		{
			memlen = 102;
		}
		else
		{
			insert_at(num + 1, n);
		}
		cleanup(12);
	}

	private void op_pointeradvance()
	{
		int num = pointerpos();
		int num2 = act_ram[num];
		if (num < memlen - 1)
		{
			act_ram[num] = act_ram[num + 1];
			act_ram[num + 1] = (byte)num2;
		}
		else
		{
			insert_at(1, num2);
		}
		cleanup(7);
	}

	private void op_memoryinitialize()
	{
		memlen = 102;
		act_ram[0] = 63;
		act_ram[1] = 61;
		for (int i = 2; i < memlen; i++)
		{
			act_ram[i] = 0;
		}
		cleanup(7);
	}

	private void op_sel_rom()
	{
		act_rom = act_del_rom;
		act_rom = (byte)((act_rom & 8) | (opcode >> 7));
		act_pc = (ushort)((act_rom << 8) | (byte)act_pc);
		act_del_rom = act_rom;
	}

	private void op_keys_to_rom_addr()
	{
		act_pc = (ushort)((act_rom << 8) | act_key_buf);
	}

	private void op_c_to_addr()
	{
		if (act_ram_size <= 10)
		{
			act_ram_addr = act_c[12];
		}
		else
		{
			act_ram_addr = (byte)(act_c[12] * 10 + act_c[11]);
		}
	}

	private void op_setf()
	{
		int num = opcode >> 7;
		if ((opcode & 0x40) == 0)
		{
			act_f |= (byte)(1 << num);
		}
		else
		{
			act_f &= (byte)(254 << num);
		}
	}

	private void op_unknown()
	{
	}

	private void op_nop()
	{
	}

	private void reg_zero()
	{
		for (byte b = first; b <= last; b++)
		{
			dest[b] = 0;
		}
	}

	private void reg_copy()
	{
		for (byte b = first; b <= last; b++)
		{
			dest[b] = src[b];
		}
	}

	private void reg_exch()
	{
		for (byte b = first; b <= last; b++)
		{
			byte b2 = dest[b];
			dest[b] = src[b];
			src[b] = b2;
		}
	}

	private void reg_shift_right()
	{
		for (byte b = first; b <= last; b++)
		{
			dest[b] = (byte)((b != last) ? dest[b + 1] : 0);
		}
	}

	private void reg_shift_left()
	{
		for (sbyte b = (sbyte)last; b >= first; b--)
		{
			dest[b] = (byte)((b != first) ? dest[b - 1] : 0);
		}
	}

	private void handle_del_rom()
	{
		if ((act_flags & F.DEL_ROM) != 0)
		{
			act_pc = (ushort)((act_del_rom << 8) | (byte)act_pc);
			act_flags &= (F)(-9);
		}
	}

	private void reg_test_equal()
	{
		act_flags |= F.CARRY;
		for (byte b = first; b <= last; b++)
		{
			byte b2 = (byte)((dest != null) ? dest[b] : 0);
			if (src[b] != b2)
			{
				act_flags &= (F)(-3);
				break;
			}
		}
	}

	private void reg_test_nonequal()
	{
		act_flags &= (F)(-3);
		for (byte b = first; b <= last; b++)
		{
			byte b2 = (byte)((dest != null) ? dest[b] : 0);
			if (src[b] != b2)
			{
				act_flags |= F.CARRY;
				break;
			}
		}
	}

	private void reg_inc()
	{
		act_flags |= F.CARRY;
		src = null;
		reg_add();
	}

	private void reg_add()
	{
		for (byte b = first; b <= last; b++)
		{
			byte b2 = (byte)((src != null) ? src[b] : 0);
			byte b3 = (byte)(dest[b] + b2 + (((act_flags & F.CARRY) != 0) ? 1 : 0));
			if (b3 >= act_base)
			{
				b3 -= act_base;
				act_flags |= F.CARRY;
			}
			else
			{
				act_flags &= (F)(-3);
			}
			dest[b] = b3;
		}
	}

	private void op_circulate_a_left()
	{
		byte b = act_a[13];
		for (byte b2 = 13; b2 >= 1; b2--)
		{
			act_a[b2] = act_a[b2 - 1];
		}
		act_a[0] = b;
	}

	private void op_test_s_eq_0()
	{
		if ((act_s & (1 << (opcode >> 6))) != 0)
		{
			act_flags |= F.CARRY;
		}
		else
		{
			act_flags &= (F)(-3);
		}
	}

	private void op_test_s_eq_1()
	{
		if ((act_s & (1 << (opcode >> 6))) != 0)
		{
			act_flags &= (F)(-3);
		}
		else
		{
			act_flags |= F.CARRY;
		}
	}

	private void op_set_s()
	{
		act_s |= (ushort)(1 << (opcode >> 6));
	}

	private void op_clr_s()
	{
		ushort num = (ushort)(1 << (opcode >> 6));
		act_s &= (ushort)(~num);
	}

	private void op_a_to_rom_addr()
	{
		act_pc &= 65280;
		handle_del_rom();
		rom_addr = (byte)((act_a[2] << 4) + act_a[1]);
		act_pc += rom_addr;
	}

	private void op_y_to_a()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_a[b] = act_y[b];
		}
	}

	private void reg_sub()
	{
		for (byte b = first; b <= last; b++)
		{
			byte b2 = (byte)((src != null) ? src[b] : 0);
			byte b3 = (byte)((src2 != null) ? src2[b] : 0);
			sbyte b4 = (sbyte)(b2 - b3 - (((act_flags & F.CARRY) != 0) ? 1 : 0));
			if (b4 < 0)
			{
				b4 += (sbyte)act_base;
				act_flags |= F.CARRY;
			}
			else
			{
				act_flags &= (F)(-3);
			}
			if (dest != null)
			{
				dest[b] = (byte)b4;
			}
		}
	}

	private void op_register_to_c()
	{
		if (opcode >> 6 != 0)
		{
			act_ram_addr &= 240;
			act_ram_addr += (byte)(opcode >> 6);
		}
		register_to_c(act_ram_addr);
	}

	private void op_f_to_a()
	{
		act_a[0] = act_f;
	}

	private void op_f_exch_a()
	{
		byte b = act_a[0];
		act_a[0] = act_f;
		act_f = b;
	}

	private void register_to_c(byte addr)
	{
		if (addr < 64)
		{
			for (byte b = 0; b < 14; b++)
			{
				act_c[b] = act_ram[addr * 14 + b];
			}
		}
		else if (addr == byte.MaxValue)
		{
			act_c[0] = act_key_buf;
		}
	}

	private void c_to_register(byte addr)
	{
		if (addr < 64)
		{
			for (byte b = 0; b < 14; b++)
			{
				act_ram[addr * 14 + b] = act_c[b];
			}
		}
	}

	private void op_c_to_data()
	{
		c_to_register(act_ram_addr);
	}

	private void op_c_to_register()
	{
		act_ram_addr &= 240;
		act_ram_addr += (byte)(opcode >> 6);
		op_c_to_data();
	}

	private void op_clear_data_regs()
	{
		byte b = (byte)(act_ram_addr & 0xF0);
		if (b < 64)
		{
			for (int i = b * 14; i < b * 14 + 224; i++)
			{
				act_ram[i] = 0;
			}
		}
	}

	private void op_c_to_stack()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_t[b] = act_z[b];
			act_z[b] = act_y[b];
			act_y[b] = act_c[b];
		}
	}

	private void op_stack_to_a()
	{
		op_y_to_a();
		for (byte b = 0; b < 14; b++)
		{
			act_y[b] = act_z[b];
			act_z[b] = act_t[b];
		}
	}

	private void op_down_rotate()
	{
		for (byte b = 0; b < 14; b++)
		{
			byte b2 = act_c[b];
			act_c[b] = act_y[b];
			act_y[b] = act_z[b];
			act_z[b] = act_t[b];
			act_t[b] = b2;
		}
	}

	private void op_clear_reg()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_a[b] = (act_b[b] = (act_c[b] = (act_y[b] = (act_z[b] = (act_t[b] = (act_m[0] = 0))))));
		}
	}

	private void op_display_off()
	{
		act_flags &= (F)(-33);
	}

	private void op_display_toggle()
	{
		act_flags ^= F.DISPLAY_ON;
	}

	private bool classic_execute_instruction()
	{
		ushort pc = (ushort)((act_rom << 8) | (byte)act_pc);
		opcode = Getopcode(pc);
		if ((act_flags & F.CARRY) != 0)
		{
			act_flags &= (F)(-3);
			act_flags |= F.PREV_CARRY;
		}
		else
		{
			act_flags &= (F)(-5);
		}
		act_pc++;
		op_fcn0[opcode]();
		return true;
	}

	private void act_press_key(byte keycode)
	{
		act_key_buf = keycode;
	}

	private void act_clear_memory()
	{
		for (ushort num = 0; num < 448; num++)
		{
			act_ram[num] = 0;
		}
	}

	private void act_reset()
	{
		act_flags = (F)0;
		act_del_rom = 0;
		act_rom = 0;
		act_base = 10;
		act_sp = 0;
		act_key_buf = 0;
		act_pc = 0;
		act_p = 0;
		act_s = 0;
		op_clear_reg();
		act_clear_memory();
	}

	public HPClassic()
	{
		InitializeComponent();
		Text = "HP-80";
		labelHPType.Text = "HP-80 Emulator";
		buttonLoad.Visible = false;
		buttonSave.Visible = false;
		try
		{
			textBoxDisplay.Font = new Font("HP Classic LED Set", 12.75f);
			if (textBoxDisplay.Font.Name == "HP Classic LED Set")
			{
				SegmentFont = true;
			}
			else
			{
				textBoxDisplay.Font = new Font("Lucida Console", 14f);
			}
			textBoxDisplay.Font = new Font(textBoxDisplay.Font, FontStyle.Bold);
		}
		catch
		{
		}
		ImageTable = new string[1] { "classic.bmp" };
		ImageNr = 0;
		try
		{
			OriginalSize = pictureBox1.Size;
			pictureBox1.Image = Image.FromFile(ImageTable[ImageNr]);
			pictureBox1.Size = pictureBox1.Image.Size;
			Size size = pictureBox1.Size;
			Point location = buttonLoad.Location;
			location.X += size.Width - OriginalSize.Width;
			buttonLoad.Location = location;
			location = buttonSave.Location;
			location.X += size.Width - OriginalSize.Width;
			buttonSave.Location = location;
			location = labelHPType.Location;
			location.X += size.Width - OriginalSize.Width;
			labelHPType.Location = location;
			location = label2.Location;
			location.X += size.Width - OriginalSize.Width;
			label2.Location = location;
			location = label3.Location;
			location.X += size.Width - OriginalSize.Width;
			label3.Location = location;
			location = textBoxDisplay.Location;
			location.Y = location.Y * size.Height / OriginalSize.Height;
			textBoxDisplay.Location = location;
			textBoxDisplay.Width = textBoxDisplay.Width * size.Width / OriginalSize.Width;
			textBoxDisplay.Font = new Font(textBoxDisplay.Font.Name, textBoxDisplay.Font.Size * (float)size.Width / (float)OriginalSize.Width);
			size.Width += 184;
			size.Height += 40;
			MaximumSize = size;
			size.Width = size.Width - 184 + 16;
			MinimumSize = size;
			base.Size = size;
		}
		catch
		{
		}
		timer1.Interval = 50;
		ACTClassic();
	}

	private void Reset()
	{
		act_reset();
	}

	private ushort Getopcode(ushort pc)
	{
		return opcodeint[pc];
	}

	private void ShowDisplay()
	{
		string text = null;
		if ((act_flags & F.DISPLAY_ON) != 0)
		{
			int num = 14;
			for (int i = 0; i < num; i++)
			{
				byte b = act_a[13 - i];
				byte b2 = act_b[13 - i];
				if (i == 0 && timermode)
				{
					continue;
				}
				if (b2 <= 7)
				{
					char c = ((i != 0 && i != num - 3) ? ((char)(b + 48)) : ((b >= 8) ? '-' : ' '));
					text += c;
					if ((b2 & 2) != 0)
					{
						c = (SegmentFont ? ';' : '.');
						text += c;
					}
				}
				else
				{
					text += ' ';
				}
			}
		}
		else
		{
			text = "";
		}
		textBoxDisplay.Text = text;
	}

	private void Run()
	{
		running = true;
	}

	private void Stop()
	{
		running = false;
	}

	
	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		Reset();
		act_flags &= (F)(-33);
		buttonpressed = false;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
		buttonpressed = true;
	}

	public void HeadlessReleaseKey()
	{
		buttonpressed = false;
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
		prgmmode = programMode;
	}

	public bool HeadlessProgramMode => prgmmode;

	public void HeadlessRunTimerBatch()
	{
		if (!running)
		{
			return;
		}

		for (int i = 0; i < 200; i++)
		{
			classic_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 1;
			}
		}

		ShowDisplay();
	}

	public string HeadlessDisplayText => textBoxDisplay.Text;

	public bool HeadlessDisplayOn => (act_flags & F.DISPLAY_ON) != 0;

	public PanamatikHeadlessSnapshot HeadlessSnapshot =>
		new(
			act_pc,
			act_s,
			act_key_buf,
			(byte)act_flags,
			act_p,
			act_rom,
			0);
	private void timer1_Tick(object sender, EventArgs e)
	{
		if (!running)
		{
			return;
		}
		for (int i = 0; i < 200; i++)
		{
			classic_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 1;
			}
		}
		if (!focus)
		{
			focus = true;
			textBox1.Focus();
		}
		ShowDisplay();
	}

	private void press_key(byte code)
	{
		ShowDisplay();
		act_press_key(code);
		Run();
	}

	private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
	{
		int num = e.X * OriginalSize.Width / pictureBox1.Width;
		int num2 = e.Y * OriginalSize.Height / pictureBox1.Height;
		if (num2 >= 78 && num2 < 98)
		{
			switch (num)
			{
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
			case 25:
			case 26:
			case 27:
			case 28:
			case 29:
			case 30:
			case 31:
			case 32:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
			case 39:
			case 40:
			case 41:
			case 42:
			case 43:
			case 44:
			case 45:
			case 46:
			case 47:
			case 48:
			case 49:
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
				Stop();
				Reset();
				act_flags &= (F)(-33);
				ShowDisplay();
				break;
			case 58:
			case 59:
			case 60:
			case 61:
			case 62:
			case 63:
			case 64:
			case 65:
			case 66:
			case 67:
			case 68:
			case 69:
			case 70:
			case 71:
			case 72:
			case 73:
			case 74:
			case 75:
			case 76:
			case 77:
			case 78:
			case 79:
			case 80:
			case 81:
			case 82:
			case 83:
			case 84:
			case 85:
			case 86:
			case 87:
			case 88:
			case 89:
			case 90:
			case 91:
			case 92:
			case 93:
			case 94:
			case 95:
			case 96:
			case 97:
			case 98:
			case 99:
			case 100:
			case 101:
			case 102:
			case 103:
				Run();
				break;
			}
			if (num >= 102 && num < 214)
			{
						prgmmode = false;
				if (num < 160)
				{
					prgmmode = true;
				}
			}
		}
		if (num2 >= 105 && num2 < 457 && num >= 4 && num < 214)
		{
			int num3 = (num2 - 127 + 22) / 44;
			int num4 = ((num3 > 3) ? ((num - 12 + 30) / 60) : ((num - 25 + 21) / 42));
			byte code = HPClassicKeytable[num3 * 5 + num4];
			press_key(code);
			buttonpressed = true;
		}
	}

	private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
	{
		buttonpressed = false;
	}

	private void textBox1_KeyDown(object sender, KeyEventArgs e)
	{
		buttonpressed = true;
	}

	private void textBox1_KeyUp(object sender, KeyEventArgs e)
	{
		buttonpressed = false;
	}

	private void txtBox1_KeyPress(object sender, KeyPressEventArgs e)
	{
		char c = char.ToLower(e.KeyChar);
		for (int i = 0; i < 40; i++)
		{
			if (c == HPClassicKeyChartable[i])
			{
				press_key(HPClassicKeytable[i]);
				break;
			}
		}
		switch (c)
		{
		case 'm':
			prgmmode = !prgmmode;
			break;
		case ',':
			press_key(HPClassicKeytable[37]);
			break;
		case 'I':
			if (++ImageNr >= 1)
			{
				ImageNr = 0;
			}
			pictureBox1.Image = Image.FromFile(ImageTable[ImageNr]);
			break;
		}
		e.Handled = true;
	}

	private void textBoxDisplay_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		ShowDisplay();
	}

	private double GetRegisterValue(int n)
	{
		int num = 0;
		for (int num2 = 2; num2 >= 0; num2--)
		{
			byte b = act_ram[n * 7 + num2 / 2];
			b = (((num2 & 1) == 0) ? ((byte)(b & 0xF)) : ((byte)(b >> 4)));
			num *= 10;
			num += b;
		}
		if (num >= 100)
		{
			num -= 1000;
		}
		long num3 = 0L;
		for (int num2 = 12; num2 >= 3; num2--)
		{
			byte b = act_ram[n * 7 + num2 / 2];
			b = (((num2 & 1) == 0) ? ((byte)(b & 0xF)) : ((byte)(b >> 4)));
			num3 *= 10;
			num3 += b;
		}
		if (act_ram[n * 7 + 7 - 1] >> 4 == 9)
		{
			num3 = -num3;
		}
		return (double)num3 / 1000000000.0 * Math.Pow(10.0, num);
	}

	private void SetRegisterValue(byte[] buf, string s)
	{
		bool flag = true;
		for (int i = 0; i < 14; i++)
		{
			buf[i] = 0;
		}
		int num = 0;
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < s.Length; i++)
		{
			if (num == 0 && s[i] == '-')
			{
				buf[13] = 9;
				continue;
			}
			if (s[i] == '.' || s[i] == ',')
			{
				if (num == 0)
				{
					num++;
				}
				num2 = num - 1;
				continue;
			}
			if (s[i] == 'E' || s[i] == 'e')
			{
				num3 = Convert.ToInt32(s.Substring(i + 1));
				break;
			}
			if (s[i] >= '0' && s[i] <= '9' && num < 10)
			{
				buf[12 - num] = (byte)(s[i] - 48);
				num++;
				if (s[i] != '0')
				{
					flag = false;
				}
			}
		}
		if (flag)
		{
			return;
		}
		if (num2 < 0)
		{
			num2 = num - 1;
		}
		num3 += num2;
		for (int i = 0; i < 10; i++)
		{
			if (buf[12] != 0)
			{
				break;
			}
			for (num = 0; num < 10; num++)
			{
				buf[12 - num] = buf[12 - num - 1];
			}
			num3--;
		}
		if (num3 < 0)
		{
			num3 = 1000 + num3;
		}
		buf[2] = (byte)(num3 / 100);
		num3 -= buf[2] * 100;
		buf[1] = (byte)(num3 / 10);
		num3 -= buf[1] * 10;
		buf[0] = (byte)num3;
	}

	private int GetProgramCode(int j)
	{
		return 0;
	}

	private void SetProgramCode(int j, byte code)
	{
	}

	private void WriteProgram(string FileName)
	{
	}

	private bool ReadProgram(string FileName)
	{
		return false;
	}

	private void buttonLoad_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "hp65 files (*.hp65)|*.hp65|all files (*.*)|*.*";
		openFileDialog.FilterIndex = 2;
		openFileDialog.RestoreDirectory = true;
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		try
		{
			if (!ReadProgram(openFileDialog.FileName))
			{
				throw new Exception("No program file");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "HPClassic", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void buttonSave_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "hp65 files (*.hp65)|*.hp65|all files (*.*)|*.*";
		saveFileDialog.FilterIndex = 2;
		saveFileDialog.RestoreDirectory = true;
		if (saveFileDialog.ShowDialog() == DialogResult.OK)
		{
			try
			{
				WriteProgram(saveFileDialog.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "HPClassic", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HPClassic));
		this.timer1 = new System.Windows.Forms.Timer(this.components);
		this.textBoxDisplay = new System.Windows.Forms.TextBox();
		this.labelHPType = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.buttonLoad = new System.Windows.Forms.Button();
		this.buttonSave = new System.Windows.Forms.Button();
		this.textBox1 = new System.Windows.Forms.TextBox();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.timer1.Enabled = true;
		this.timer1.Interval = 10;
		this.timer1.Tick += new System.EventHandler(timer1_Tick);
		this.textBoxDisplay.BackColor = System.Drawing.Color.FromArgb(64, 0, 0);
		this.textBoxDisplay.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBoxDisplay.Cursor = System.Windows.Forms.Cursors.IBeam;
		this.textBoxDisplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 16f);
		this.textBoxDisplay.ForeColor = System.Drawing.Color.Red;
		this.textBoxDisplay.Location = new System.Drawing.Point(5, 15);
		this.textBoxDisplay.Name = "textBoxDisplay";
		this.textBoxDisplay.ReadOnly = true;
		this.textBoxDisplay.Size = new System.Drawing.Size(210, 25);
		this.textBoxDisplay.TabIndex = 25;
		this.textBoxDisplay.TabStop = false;
		this.textBoxDisplay.Click += new System.EventHandler(textBoxDisplay_Click);
		this.labelHPType.AutoSize = true;
		this.labelHPType.Location = new System.Drawing.Point(249, 29);
		this.labelHPType.Name = "labelHPType";
		this.labelHPType.Size = new System.Drawing.Size(81, 13);
		this.labelHPType.TabIndex = 26;
		this.labelHPType.Text = "HP-25 Emulator";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(233, 53);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(111, 13);
		this.label2.TabIndex = 27;
		this.label2.Text = "(c) PANAMATIK 2016";
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(253, 77);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(66, 13);
		this.label3.TabIndex = 28;
		this.label3.Text = "Version 1.01";
		this.buttonLoad.Location = new System.Drawing.Point(238, 218);
		this.buttonLoad.Name = "buttonLoad";
		this.buttonLoad.Size = new System.Drawing.Size(98, 23);
		this.buttonLoad.TabIndex = 34;
		this.buttonLoad.Text = "Load Program";
		this.buttonLoad.UseVisualStyleBackColor = true;
		this.buttonLoad.Click += new System.EventHandler(buttonLoad_Click);
		this.buttonSave.Location = new System.Drawing.Point(238, 248);
		this.buttonSave.Name = "buttonSave";
		this.buttonSave.Size = new System.Drawing.Size(98, 23);
		this.buttonSave.TabIndex = 35;
		this.buttonSave.Text = "Save Program";
		this.buttonSave.UseVisualStyleBackColor = true;
		this.buttonSave.Click += new System.EventHandler(buttonSave_Click);
		this.textBox1.Location = new System.Drawing.Point(10, 13);
		this.textBox1.Name = "textBox1";
		this.textBox1.Size = new System.Drawing.Size(190, 20);
		this.textBox1.TabIndex = 36;
		this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(textBox1_KeyDown);
		this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(txtBox1_KeyPress);
		this.textBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(textBox1_KeyUp);
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(0, 0);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(220, 461);
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseDown);
		this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseUp);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(220, 462);
		base.Controls.Add(this.buttonSave);
		base.Controls.Add(this.buttonLoad);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.labelHPType);
		base.Controls.Add(this.textBoxDisplay);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.textBox1);
		this.MaximumSize = new System.Drawing.Size(400, 500);
		this.MinimumSize = new System.Drawing.Size(236, 500);
		base.Name = "HPClassic";
		this.Text = "HP-";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
