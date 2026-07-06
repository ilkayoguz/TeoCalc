using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Panamatik.Calc.HP70;

public class HPClassic : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int EXPSIZE = 3;

	private const int BUTTONS = 35;

	private const int RAMSIZE = 448;

	private F act_flags;

	private byte[] src;

	private byte[] dest;

	private byte[] src2;

	private byte first;

	private byte last;

	private byte act_key_buf;

	private byte act_ram_addr;

	private byte act_del_rom;

	private byte act_del_grp;

	private byte act_rom;

	private byte act_grp;

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

	private bool mouseDown;

	private bool transparent;

	private Size OriginalSize;

	private Point lastLocation;

	private int FirstCol = 25;

	private int FirstRow = 127;

	private int LastCol = 192;

	private int LastRow = 436;

	private int RowSize;

	private int ColSize;

	private int FirstCol2;

	private int ColSize2;

	private int SliderY;

	private int SliderLeft;

	private int SliderRight;

	private char[] HPClassicKeyChartable = new char[40]
	{
		'n', 'i', '$', 'p', 'f', 't', '%', 'e', 'x', ' ',
		'y', 'r', 's', 'k', 'd', '\r', '\r', 'c', 'm', '#',
		'-', '7', '8', '9', '\0', '+', '4', '5', '6', '\0',
		'*', '1', '2', '3', '\0', '/', '0', '.', '\b', '\0'
	};

	private byte[] HPClassicKeytable = new byte[40]
	{
		6, 4, 3, 2, 0, 46, 44, 43, 42, 40,
		14, 12, 11, 10, 8, 62, 62, 59, 58, 56,
		54, 52, 51, 50, 0, 22, 20, 19, 18, 0,
		30, 28, 27, 26, 0, 38, 36, 35, 34, 0
	};

	public ushort[] opcodeint = new ushort[2048]
	{
		793, 272, 272, 272, 372, 7, 500, 35, 400, 929,
		1001, 953, 13, 33, 102, 71, 183, 254, 942, 206,
		985, 942, 752, 206, 993, 942, 206, 1001, 17, 206,
		997, 434, 923, 490, 490, 5, 750, 1022, 718, 9,
		33, 206, 985, 5, 451, 206, 993, 942, 206, 997,
		175, 929, 1001, 953, 13, 33, 102, 239, 343, 206,
		985, 942, 752, 206, 989, 942, 206, 1001, 17, 206,
		997, 434, 923, 490, 490, 5, 206, 510, 590, 13,
		33, 206, 985, 5, 451, 206, 989, 191, 929, 989,
		942, 206, 993, 434, 923, 5, 33, 434, 451, 206,
		985, 942, 752, 206, 1001, 434, 923, 953, 13, 33,
		475, 0, 422, 467, 244, 467, 206, 459, 206, 985,
		942, 5, 451, 0, 0, 0, 929, 1005, 942, 206,
		997, 17, 206, 989, 9, 430, 451, 206, 989, 434,
		923, 942, 206, 997, 434, 923, 5, 254, 942, 206,
		985, 942, 752, 206, 1005, 434, 923, 254, 752, 871,
		0, 0, 0, 0, 0, 0, 0, 0, 929, 1005,
		434, 923, 750, 994, 5, 206, 977, 942, 752, 206,
		989, 942, 206, 993, 434, 923, 5, 206, 977, 942,
		25, 206, 482, 9, 490, 490, 0, 451, 400, 929,
		1005, 434, 923, 206, 993, 434, 923, 942, 206, 997,
		434, 923, 5, 206, 985, 942, 752, 884, 39, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		372, 471, 168, 780, 68, 260, 206, 48, 362, 362,
		750, 1022, 718, 48, 482, 482, 482, 482, 482, 482,
		482, 482, 482, 624, 760, 48, 151, 939, 195, 199,
		0, 528, 528, 0, 132, 319, 291, 271, 808, 467,
		424, 296, 942, 467, 1006, 1006, 1006, 107, 953, 999,
		0, 0, 1006, 1006, 1006, 147, 953, 975, 0, 0,
		263, 204, 299, 528, 953, 983, 500, 899, 343, 967,
		756, 39, 628, 735, 528, 528, 1006, 1006, 1006, 75,
		953, 991, 1012, 523, 283, 254, 164, 319, 296, 641,
		475, 637, 475, 644, 164, 323, 1012, 559, 1012, 575,
		148, 311, 427, 676, 1015, 676, 718, 531, 0, 0,
		0, 196, 942, 206, 600, 780, 624, 942, 752, 424,
		942, 500, 35, 0, 724, 411, 884, 1011, 276, 1019,
		116, 1023, 270, 270, 812, 447, 455, 750, 874, 168,
		938, 168, 641, 452, 529, 758, 468, 495, 296, 452,
		206, 46, 558, 366, 190, 510, 558, 703, 414, 548,
		579, 506, 516, 340, 571, 490, 543, 40, 20, 547,
		36, 28, 812, 583, 552, 532, 567, 270, 356, 208,
		210, 370, 218, 906, 643, 206, 52, 398, 780, 298,
		394, 442, 683, 170, 378, 619, 938, 272, 658, 861,
		529, 726, 414, 812, 767, 142, 494, 76, 274, 60,
		418, 731, 404, 775, 28, 172, 691, 388, 695, 658,
		614, 799, 861, 529, 503, 236, 695, 861, 645, 716,
		50, 529, 812, 843, 490, 809, 645, 716, 50, 529,
		841, 746, 942, 398, 844, 278, 362, 638, 919, 630,
		879, 202, 48, 28, 490, 2, 911, 726, 934, 48,
		552, 212, 395, 467, 196, 424, 48, 756, 71, 372,
		7, 372, 11, 372, 15, 372, 19, 482, 624, 48,
		656, 0, 144, 283, 291, 254, 46, 1018, 1018, 506,
		506, 74, 51, 942, 934, 422, 67, 942, 550, 74,
		99, 654, 1002, 14, 99, 71, 378, 378, 746, 862,
		638, 127, 163, 518, 143, 254, 814, 782, 0, 187,
		910, 354, 155, 718, 60, 876, 159, 490, 766, 780,
		46, 610, 227, 270, 362, 622, 199, 206, 298, 910,
		638, 167, 934, 398, 46, 780, 596, 3, 404, 419,
		420, 427, 204, 170, 46, 330, 350, 311, 190, 806,
		750, 812, 159, 102, 363, 84, 471, 210, 370, 218,
		0, 247, 562, 934, 379, 482, 790, 375, 918, 278,
		28, 44, 379, 398, 138, 187, 500, 515, 500, 791,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 473,
		168, 268, 418, 599, 40, 194, 168, 680, 588, 98,
		551, 206, 510, 510, 590, 624, 760, 254, 752, 206,
		750, 510, 510, 510, 590, 624, 760, 942, 752, 40,
		603, 168, 52, 324, 206, 244, 647, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 398, 680, 942, 767, 726,
		1002, 711, 844, 826, 314, 28, 172, 847, 810, 890,
		767, 750, 874, 46, 516, 76, 314, 390, 278, 610,
		723, 548, 908, 394, 122, 711, 28, 172, 835, 759,
		874, 819, 727, 890, 735, 290, 28, 754, 394, 918,
		895, 726, 1022, 532, 899, 60, 726, 54, 814, 994,
		994, 278, 890, 919, 754, 882, 726, 532, 1007, 2,
		963, 979, 866, 60, 812, 951, 554, 746, 442, 1003,
		170, 378, 554, 814, 144, 0, 0, 0, 372, 283,
		372, 159, 372, 187, 372, 471, 580, 750, 388, 838,
		27, 718, 382, 27, 510, 302, 83, 722, 894, 79,
		766, 910, 404, 215, 866, 67, 818, 274, 926, 775,
		460, 155, 510, 782, 139, 910, 558, 206, 358, 280,
		486, 590, 364, 283, 396, 754, 844, 558, 942, 408,
		243, 994, 302, 382, 83, 722, 942, 278, 942, 894,
		219, 814, 994, 19, 598, 270, 0, 143, 556, 363,
		332, 216, 216, 524, 558, 404, 267, 718, 60, 60,
		339, 910, 382, 335, 942, 278, 942, 155, 620, 403,
		460, 216, 216, 24, 536, 344, 588, 307, 684, 455,
		588, 216, 88, 24, 88, 472, 600, 536, 88, 652,
		307, 748, 515, 408, 600, 216, 88, 280, 472, 88,
		536, 24, 344, 408, 716, 307, 812, 307, 206, 152,
		216, 24, 152, 344, 536, 344, 24, 600, 216, 780,
		942, 404, 651, 334, 26, 599, 334, 814, 28, 270,
		108, 603, 942, 446, 635, 230, 490, 40, 716, 11,
		302, 390, 698, 727, 506, 718, 490, 671, 446, 703,
		814, 782, 238, 718, 612, 155, 1002, 634, 767, 790,
		715, 918, 270, 362, 719, 718, 210, 938, 683, 998,
		751, 830, 598, 1022, 71, 760, 3, 552, 52, 936,
		46, 506, 506, 168, 780, 624, 930, 752, 930, 482,
		831, 206, 780, 482, 624, 482, 590, 482, 490, 752,
		206, 979, 206, 750, 997, 997, 1009, 997, 1009, 997,
		1009, 997, 1009, 997, 1009, 206, 296, 296, 296, 168,
		198, 168, 244, 467, 0, 0, 0, 482, 624, 48,
		942, 752, 942, 48, 0, 0, 0, 0, 756, 539,
		67, 148, 223, 1013, 989, 942, 296, 760, 116, 451,
		148, 279, 1013, 985, 47, 148, 107, 1013, 977, 47,
		905, 126, 611, 807, 588, 98, 139, 482, 1005, 977,
		331, 0, 148, 347, 1013, 973, 47, 588, 837, 446,
		19, 756, 675, 87, 148, 459, 1013, 981, 47, 905,
		126, 403, 780, 98, 267, 825, 780, 98, 267, 482,
		1005, 989, 331, 905, 126, 663, 716, 98, 323, 825,
		716, 98, 323, 482, 1005, 985, 942, 752, 244, 255,
		905, 126, 559, 524, 98, 391, 825, 524, 98, 391,
		482, 1005, 973, 331, 780, 98, 267, 889, 588, 837,
		446, 515, 524, 837, 446, 527, 116, 355, 905, 126,
		535, 652, 98, 503, 825, 652, 98, 503, 482, 1005,
		981, 331, 116, 207, 0, 116, 39, 652, 98, 503,
		889, 873, 175, 524, 98, 391, 889, 873, 588, 837,
		446, 603, 756, 155, 756, 223, 588, 98, 139, 889,
		873, 524, 837, 446, 655, 756, 355, 756, 427, 716,
		98, 323, 873, 588, 837, 446, 719, 524, 837, 446,
		727, 116, 675, 116, 507, 116, 799, 676, 168, 889,
		873, 588, 837, 446, 779, 1005, 1012, 39, 524, 837,
		446, 863, 644, 767, 0, 588, 98, 139, 825, 123,
		204, 482, 48, 222, 418, 855, 510, 48, 0, 168,
		372, 471, 780, 418, 863, 48, 716, 418, 863, 48,
		168, 222, 204, 418, 963, 354, 418, 959, 354, 418,
		955, 510, 482, 482, 48, 482, 482, 482, 482, 482,
		482, 482, 482, 624, 48, 168, 780, 942, 206, 48,
		0, 272, 272, 272, 372, 7, 500, 35, 400, 424,
		942, 296, 17, 362, 362, 116, 451, 424, 942, 296,
		9, 808, 296, 362, 362, 5, 63, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 933, 1001,
		953, 13, 206, 1005, 558, 206, 977, 558, 752, 942,
		25, 206, 993, 17, 63, 933, 1001, 434, 331, 953,
		13, 206, 1005, 558, 206, 977, 558, 752, 942, 25,
		206, 482, 9, 206, 997, 17, 206, 1001, 362, 362,
		5, 63, 1005, 942, 206, 997, 17, 63, 933, 1001,
		953, 13, 206, 1005, 254, 558, 206, 977, 558, 752,
		942, 25, 206, 989, 17, 63, 933, 1001, 434, 331,
		953, 13, 206, 1005, 254, 558, 206, 977, 558, 752,
		942, 25, 750, 994, 9, 206, 997, 17, 206, 1001,
		362, 362, 5, 63, 933, 1001, 434, 847, 953, 13,
		206, 1005, 558, 206, 977, 142, 752, 942, 25, 206,
		510, 590, 9, 206, 985, 942, 752, 206, 1001, 953,
		942, 206, 989, 17, 206, 985, 5, 63, 933, 1005,
		254, 942, 206, 977, 942, 752, 206, 1001, 434, 827,
		953, 13, 206, 977, 942, 25, 750, 1022, 718, 9,
		206, 985, 942, 752, 206, 1001, 362, 362, 942, 206,
		993, 17, 206, 985, 5, 63, 993, 942, 206, 1005,
		819, 989, 831, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 168, 68, 780, 206, 48, 362, 362,
		750, 1022, 718, 48, 482, 482, 482, 482, 482, 482,
		482, 482, 482, 624, 760, 48, 0, 272, 272, 272,
		372, 7, 500, 35, 400, 917, 676, 510, 482, 690,
		590, 942, 206, 965, 17, 206, 985, 942, 5, 206,
		957, 942, 752, 442, 315, 398, 13, 442, 227, 206,
		985, 750, 994, 5, 254, 942, 206, 957, 942, 752,
		206, 965, 942, 206, 957, 942, 222, 25, 206, 482,
		9, 383, 206, 985, 942, 206, 965, 9, 13, 206,
		957, 942, 752, 942, 206, 985, 5, 490, 490, 490,
		442, 339, 644, 339, 750, 206, 994, 965, 5, 383,
		206, 985, 750, 994, 13, 760, 17, 206, 957, 942,
		5, 206, 981, 942, 752, 660, 411, 743, 676, 750,
		206, 994, 981, 430, 743, 13, 206, 985, 254, 558,
		206, 957, 558, 752, 942, 25, 206, 961, 942, 752,
		206, 750, 994, 981, 13, 206, 957, 942, 752, 206,
		985, 942, 206, 961, 17, 206, 981, 17, 206, 957,
		5, 752, 750, 206, 994, 961, 9, 752, 206, 957,
		9, 752, 206, 965, 942, 206, 981, 17, 206, 961,
		9, 206, 957, 5, 752, 750, 994, 9, 206, 981,
		17, 752, 206, 957, 17, 750, 861, 760, 750, 869,
		415, 168, 268, 194, 168, 680, 588, 98, 791, 206,
		985, 254, 752, 206, 750, 981, 942, 752, 942, 490,
		490, 0, 116, 451, 0, 0, 0, 0, 0, 0,
		0, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 422, 743,
		442, 915, 970, 743, 48, 168, 268, 88, 168, 780,
		708, 292, 68, 206, 48, 510, 510, 510, 510, 510,
		510, 510, 510, 510, 590, 624, 760, 48, 276, 1023,
		912, 48, 0, 272, 272, 272, 372, 7, 416, 35,
		400, 925, 1001, 942, 206, 216, 408, 490, 490, 780,
		5, 206, 660, 99, 985, 103, 989, 17, 558, 206,
		997, 949, 814, 17, 206, 981, 942, 752, 206, 750,
		216, 408, 490, 490, 398, 344, 780, 5, 206, 981,
		17, 296, 206, 989, 296, 206, 981, 660, 475, 206,
		977, 808, 808, 752, 206, 985, 942, 206, 660, 283,
		981, 287, 977, 9, 206, 1001, 17, 206, 973, 942,
		752, 206, 216, 408, 660, 343, 347, 344, 12, 280,
		780, 942, 206, 660, 383, 981, 387, 977, 17, 206,
		973, 5, 660, 435, 206, 993, 942, 752, 676, 251,
		296, 206, 977, 296, 206, 993, 296, 206, 752, 981,
		116, 451, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 148, 535, 559, 942, 925, 1009, 13, 873,
		599, 942, 206, 1009, 587, 942, 206, 1005, 942, 660,
		611, 752, 244, 255, 296, 760, 475, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 210, 370, 218, 906, 875, 206,
		52, 398, 780, 298, 394, 442, 915, 170, 378, 851,
		938, 48, 168, 206, 780, 68, 260, 708, 48, 362,
		362, 750, 1022, 718, 48, 510, 510, 510, 510, 510,
		510, 510, 510, 510, 590, 624, 760, 48
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
			op_fcn0[32 + (i << 6)] = op_set_f;
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
			op_fcn0[936] = op_clear_regs;
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
			act_pc = (ushort)(((act_grp << 11) | (act_rom << 8) | (opcode >> 2)) + over);
			over = 0;
			act_rom = act_del_rom;
			act_grp = act_del_grp;
		}
	}

	private void op_jsb()
	{
		act_stack[0] = act_pc;
		act_rom = act_del_rom;
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | (opcode >> 2));
		if ((act_f & 0x80) != 0)
		{
			buffer = act_pc & 0x3F;
			act_f &= 127;
		}
	}

	private void op_return()
	{
		act_pc = act_stack[0];
	}

	private void op_clear_s()
	{
		act_s = 0;
	}

	private void op_del_sel_rom()
	{
		act_del_rom = (byte)(opcode >> 7);
	}

	private void op_del_sel_grp()
	{
		act_del_grp = (byte)((opcode >> 7) & 1);
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
		act_grp = act_del_grp;
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
		for (byte b = 0; b < 7; b++)
		{
			byte b2 = act_ram[act_ram_addr * 14 / 2 + b];
			act_c[b * 2] = (byte)(b2 & 0xF);
			act_c[b * 2 + 1] = (byte)(b2 >> 4);
		}
	}

	private int pointerpos()
	{
		int num = ((memlen == 103) ? 57 : 61);
		int i;
		for (i = 1; i < memlen && act_ram[112 + i] != num; i++)
		{
		}
		if (i == memlen)
		{
			return 1;
		}
		return i;
	}

	private int label_pos(int n, int n2)
	{
		while (n < memlen && (act_ram[112 + n - 1] != 43 || act_ram[112 + n] != n2))
		{
			n++;
		}
		if (n == memlen)
		{
			n = 1;
			while (n < memlen && (act_ram[112 + n - 1] != 43 || act_ram[112 + n] != n2))
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
		buffer = act_ram[112 + num - 1];
		endstate = 0;
		if (act_ram[112 + memlen - 1] != 0)
		{
			endstate = 1;
		}
		if (num == memlen - 1)
		{
			endstate = 2;
		}
		busy = n;
	}

	private void insert_at(int n, int n2)
	{
		while (n < memlen)
		{
			int num = act_ram[112 + n];
			act_ram[112 + n] = (byte)n2;
			n2 = num;
			n++;
		}
	}

	private void delete_at(int n)
	{
		for (n++; n < memlen; n++)
		{
			act_ram[112 + n - 1] = act_ram[112 + n];
		}
		act_ram[112 + n - 1] = 0;
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
		int n = act_ram[112 + num];
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
		int num2 = act_ram[112 + num];
		if (num < memlen - 1)
		{
			act_ram[112 + num] = act_ram[112 + num + 1];
			act_ram[112 + num + 1] = (byte)num2;
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
		act_ram[112] = 63;
		act_ram[113] = 61;
		for (int i = 2; i < memlen; i++)
		{
			act_ram[112 + i] = 0;
		}
		cleanup(7);
	}

	private void op_sel_rom()
	{
		act_rom = (byte)(opcode >> 7);
		act_grp = act_del_grp;
		act_del_rom = act_rom;
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | (byte)act_pc);
	}

	private void op_keys_to_rom_addr()
	{
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | act_key_buf);
		if ((act_f & 0x80) != 0)
		{
			buffer = act_pc & 0x3F;
			act_f &= 127;
		}
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

	private void op_set_s()
	{
		act_s |= (ushort)(1 << (opcode >> 6));
		if (opcode >> 6 == 0)
		{
			act_grp = act_del_grp;
		}
	}

	private void op_set_f()
	{
		int num = opcode >> 7;
		if ((opcode & 0x40) == 0)
		{
			act_f |= (byte)(1 << num);
			return;
		}
		if ((act_f & (1 << num)) != 0)
		{
			act_s |= 2048;
			act_f &= (byte)(~(1 << num));
		}
		if (num == 5)
		{
			act_s |= 2048;
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

	private void op_clr_s()
	{
		ushort num = (ushort)(1 << (opcode >> 6));
		act_s &= (ushort)(~num);
	}

	private void op_a_to_rom_addr()
	{
		act_pc &= 65280;
		handle_del_rom();
		act_pc += (ushort)((act_a[2] << 4) + act_a[1]);
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
			for (byte b = 0; b < 7; b++)
			{
				byte b2 = act_ram[addr * 14 / 2 + b];
				act_c[b * 2] = (byte)(b2 & 0xF);
				act_c[b * 2 + 1] = (byte)(b2 >> 4);
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
			for (byte b = 0; b < 7; b++)
			{
				act_ram[addr * 14 / 2 + b] = (byte)(act_c[b * 2] | (act_c[b * 2 + 1] << 4));
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
			for (int i = b * 14 / 2; i < b * 14 / 2 + 112; i++)
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

	private void op_clear_regs()
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
		ushort pc = (ushort)((act_grp << 11) | (act_rom << 8) | (byte)act_pc);
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
		act_del_grp = 0;
		act_rom = 0;
		act_grp = 0;
		act_base = 10;
		act_sp = 0;
		act_key_buf = 0;
		act_pc = 0;
		act_p = 0;
		act_s = 0;
		op_clear_regs();
		act_clear_memory();
	}

	public HPClassic()
	{
		InitializeComponent();
		CalculateKeyboard();
		Text = "HP-70";
		labelHPType.Text = "HP-70 Emulator";
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
		OriginalSize = pictureBox1.Size;
		Size size;
		try
		{
			ReadKeyboardFile("hp70.kml");
			if (transparent)
			{
				base.FormBorderStyle = FormBorderStyle.None;
				BackColor = Color.LimeGreen;
				base.TransparencyKey = Color.LimeGreen;
				pictureBox1.BackColor = Color.Transparent;
			}
			pictureBox1.Size = pictureBox1.Image.Size;
			size = pictureBox1.Size;
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
		}
		catch
		{
			size = pictureBox1.Size;
			Point location = textBoxDisplay.Location;
			location.Y = location.Y * size.Height / OriginalSize.Height;
			textBoxDisplay.Location = location;
			textBoxDisplay.Width = textBoxDisplay.Width * size.Width / OriginalSize.Width;
			textBoxDisplay.Font = new Font(textBoxDisplay.Font.Name, textBoxDisplay.Font.Size * (float)size.Width / (float)OriginalSize.Width);
		}
		size.Width += 184;
		size.Height += 40;
		MaximumSize = size;
		size.Width = size.Width - 184 + 16;
		MinimumSize = size;
		base.Size = size;
		timer1.Interval = 50;
		ACTClassic();
	}

	private void CalculateKeyboard()
	{
		RowSize = (LastRow - FirstRow) / 7;
		ColSize = (LastCol - FirstCol) / 4;
		ColSize2 = (LastCol - FirstCol) * 4 / 11;
		FirstCol2 = LastCol - 3 * ColSize2;
		SliderY = FirstRow - RowSize * 3 / 2;
		SliderLeft = FirstCol + ColSize / 2;
		SliderRight = LastCol - ColSize / 2;
	}

	private void ReadKeyboardFile(string Filename)
	{
		char[] separator = new char[2] { ' ', ',' };
		string[] array = File.ReadAllLines(Filename);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);
			if (array[i] == "" || array[i][0] == ';' || array2[0] == ";")
			{
				continue;
			}
			if (array2[0] == "IMAGE")
			{
				pictureBox1.Image = Image.FromFile(array2[1]);
				if (array2.Length > 2 && array2[2] == "TRANSPARENT")
				{
					transparent = true;
				}
			}
			else if (array2[0] == "BUTTONS")
			{
				FirstCol = Convert.ToInt32(array2[1]);
				FirstRow = Convert.ToInt32(array2[2]);
				LastCol = Convert.ToInt32(array2[3]);
				LastRow = Convert.ToInt32(array2[4]);
				CalculateKeyboard();
			}
			else if (array2[0] == "DISPLAY")
			{
				Point location = textBoxDisplay.Location;
				Size size = textBoxDisplay.Size;
				location.X = Convert.ToInt32(array2[1]);
				location.Y = Convert.ToInt32(array2[2]);
				textBoxDisplay.Location = location;
				size.Width = Convert.ToInt32(array2[3]) - location.X;
				size.Height = Convert.ToInt32(array2[4]) - location.Y;
				textBoxDisplay.Size = size;
			}
			else if (array2[0] == "FONT")
			{
				int num;
				int num2;
				if ((num = array[i].IndexOf('"')) > 0 && (num2 = array[i].IndexOf('"', num + 1)) > 0)
				{
					string familyName = array[i].Substring(num + 1, num2 - num - 1);
					string value = array[i].Substring(num2 + 1);
					textBoxDisplay.Font = new Font(familyName, (float)Convert.ToDouble(value, CultureInfo.InvariantCulture));
					textBoxDisplay.Font = new Font(textBoxDisplay.Font, FontStyle.Bold);
					if (textBoxDisplay.Font.Name == "HP Classic LED Set")
					{
						SegmentFont = true;
					}
					else
					{
						SegmentFont = false;
					}
				}
			}
			else if (array2[0] == "COLOR")
			{
				int red = Convert.ToInt32(array2[1]);
				int green = Convert.ToInt32(array2[2]);
				int blue = Convert.ToInt32(array2[3]);
				textBoxDisplay.ForeColor = Color.FromArgb(255, red, green, blue);
				red = Convert.ToInt32(array2[4]);
				green = Convert.ToInt32(array2[5]);
				blue = Convert.ToInt32(array2[6]);
				textBoxDisplay.BackColor = Color.FromArgb(255, red, green, blue);
			}
		}
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
			act_grp);
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
		act_flags &= (F)(-33);
		ShowDisplay();
		act_press_key(code);
		Run();
	}

	private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
	{
		int num = e.X;
		int num2 = e.Y;
		if (num >= pictureBox1.Width / 10 && num < pictureBox1.Width * 9 / 10 && num2 >= SliderY - RowSize / 2 && num2 < SliderY + RowSize / 2)
		{
			if (num < (SliderLeft + SliderRight) / 2)
			{
				if (num < SliderLeft)
				{
					if (transparent)
					{
						Application.Exit();
					}
					Stop();
					Reset();
					act_flags &= (F)(-33);
					ShowDisplay();
				}
				else
				{
					Run();
				}
			}
			else
			{
					}
		}
		else if (num2 >= FirstRow - RowSize / 2 && num2 < FirstRow + 7 * RowSize + RowSize / 2 && num >= FirstCol - ColSize / 2 && num < FirstCol + 4 * ColSize + ColSize / 2)
		{
			int num3 = (num2 - FirstRow + RowSize / 2) / RowSize;
			int num4 = ((num3 > 3) ? ((num - FirstCol2 + ColSize2 / 2) / ColSize2) : ((num - FirstCol + ColSize / 2) / ColSize));
			byte code = HPClassicKeytable[num3 * 5 + num4];
			press_key(code);
			buttonpressed = true;
		}
		else if (transparent)
		{
			mouseDown = true;
			lastLocation = e.Location;
		}
	}

	private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
	{
		if (mouseDown)
		{
			base.Location = new Point(base.Location.X - lastLocation.X + e.X, base.Location.Y - lastLocation.Y + e.Y);
			Update();
		}
	}

	private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
	{
		buttonpressed = false;
		mouseDown = false;
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
		}
		e.Handled = true;
	}

	private void textBoxDisplay_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		ShowDisplay();
	}

	private void buttonLoad_Click(object sender, EventArgs e)
	{
	}

	private void buttonSave_Click(object sender, EventArgs e)
	{
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
		this.textBoxDisplay.BackColor = System.Drawing.Color.FromArgb(20, 0, 0);
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
		this.label3.Text = "Version 1.04";
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
		this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseMove);
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
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		this.MaximumSize = new System.Drawing.Size(400, 500);
		this.MinimumSize = new System.Drawing.Size(236, 500);
		base.Name = "HPClassic";
		this.Text = "HP-";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
