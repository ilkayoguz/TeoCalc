using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP45;

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
		6, 4, 3, 2, 0, 46, 44, 43, 42, 40,
		14, 12, 11, 10, 8, 62, 62, 59, 58, 56,
		54, 52, 51, 50, 0, 22, 20, 19, 18, 0,
		30, 28, 27, 26, 0, 38, 36, 35, 34, 0
	};

	public ushort[] opcodeint = new ushort[2048]
	{
		173, 784, 297, 814, 998, 314, 890, 910, 746, 905,
		905, 746, 805, 403, 532, 327, 541, 942, 398, 11,
		656, 541, 942, 528, 144, 212, 99, 296, 206, 624,
		0, 760, 942, 633, 793, 649, 793, 766, 760, 446,
		183, 894, 181, 83, 144, 942, 532, 971, 254, 969,
		84, 179, 660, 423, 235, 144, 398, 144, 942, 398,
		506, 283, 426, 283, 206, 12, 344, 780, 458, 943,
		660, 471, 942, 227, 202, 12, 344, 842, 634, 819,
		1023, 297, 909, 46, 818, 814, 270, 917, 452, 805,
		206, 216, 408, 780, 633, 537, 503, 490, 482, 527,
		206, 140, 280, 942, 7, 144, 452, 276, 463, 686,
		478, 614, 459, 254, 665, 68, 87, 144, 532, 427,
		541, 68, 660, 235, 975, 942, 400, 362, 362, 452,
		354, 660, 635, 651, 676, 452, 292, 388, 680, 446,
		571, 260, 510, 583, 420, 206, 490, 84, 771, 276,
		903, 484, 404, 523, 391, 683, 660, 651, 708, 100,
		46, 663, 708, 100, 144, 144, 708, 254, 100, 164,
		46, 1018, 1018, 506, 506, 74, 715, 942, 934, 422,
		731, 942, 550, 74, 763, 654, 1002, 14, 763, 735,
		144, 1023, 404, 903, 484, 276, 523, 511, 424, 296,
		48, 708, 144, 144, 550, 654, 1002, 823, 750, 396,
		48, 724, 867, 686, 452, 627, 144, 780, 847, 724,
		999, 708, 468, 815, 740, 48, 46, 818, 654, 652,
		910, 28, 300, 923, 48, 206, 482, 660, 223, 423,
		84, 507, 942, 206, 354, 624, 206, 752, 503, 212,
		963, 660, 1019, 528, 656, 272, 574, 975, 814, 193,
		760, 942, 193, 760, 942, 596, 51, 942, 340, 107,
		126, 71, 548, 222, 665, 752, 661, 609, 181, 760,
		16, 942, 665, 660, 875, 750, 994, 294, 934, 362,
		658, 442, 135, 722, 490, 151, 718, 654, 752, 558,
		283, 558, 268, 891, 752, 942, 418, 215, 174, 398,
		138, 815, 39, 16, 340, 119, 254, 958, 79, 658,
		894, 255, 510, 818, 466, 814, 302, 850, 259, 942,
		760, 590, 958, 814, 274, 752, 1022, 1022, 175, 206,
		42, 726, 713, 866, 760, 268, 657, 396, 621, 524,
		621, 140, 536, 652, 621, 569, 621, 817, 270, 621,
		142, 813, 817, 686, 660, 471, 596, 471, 942, 254,
		617, 817, 686, 16, 817, 686, 686, 597, 686, 941,
		817, 652, 625, 569, 524, 629, 140, 536, 396, 625,
		268, 625, 625, 814, 590, 844, 344, 1011, 396, 536,
		408, 344, 152, 280, 600, 84, 875, 48, 750, 994,
		16, 272, 270, 662, 558, 647, 510, 782, 643, 910,
		272, 272, 330, 272, 482, 846, 675, 974, 270, 28,
		594, 44, 679, 215, 482, 790, 715, 918, 278, 28,
		44, 719, 215, 28, 918, 879, 16, 378, 378, 746,
		862, 638, 795, 272, 518, 811, 254, 814, 782, 272,
		206, 716, 472, 536, 344, 216, 600, 536, 88, 408,
		216, 344, 16, 48, 16, 906, 891, 354, 510, 44,
		751, 938, 746, 98, 923, 718, 590, 554, 202, 780,
		699, 272, 658, 658, 382, 947, 466, 786, 562, 142,
		894, 955, 958, 760, 942, 30, 11, 270, 946, 752,
		658, 382, 784, 830, 1022, 598, 274, 75, 424, 665,
		398, 532, 267, 750, 838, 3, 718, 382, 3, 510,
		302, 601, 866, 71, 818, 926, 7, 460, 437, 524,
		629, 588, 625, 1017, 652, 625, 501, 716, 625, 893,
		625, 741, 625, 985, 942, 334, 26, 191, 334, 814,
		28, 270, 108, 195, 942, 446, 227, 230, 490, 716,
		789, 596, 27, 340, 595, 985, 669, 595, 985, 945,
		741, 716, 621, 893, 652, 621, 501, 588, 621, 1017,
		524, 621, 621, 621, 396, 754, 844, 558, 942, 408,
		571, 148, 379, 1002, 634, 779, 790, 359, 918, 270,
		362, 371, 718, 210, 938, 446, 435, 814, 782, 238,
		718, 558, 206, 358, 148, 475, 280, 486, 487, 408,
		108, 471, 590, 590, 148, 595, 48, 460, 216, 216,
		24, 536, 344, 24, 600, 939, 601, 994, 302, 382,
		539, 722, 942, 278, 942, 894, 547, 814, 994, 817,
		144, 722, 894, 599, 766, 910, 48, 144, 718, 558,
		643, 910, 382, 639, 942, 278, 942, 439, 204, 458,
		350, 687, 190, 806, 750, 812, 791, 102, 731, 84,
		3, 656, 879, 0, 562, 934, 144, 548, 408, 600,
		216, 88, 280, 472, 88, 923, 998, 403, 910, 354,
		787, 718, 60, 876, 791, 490, 766, 780, 46, 610,
		859, 270, 362, 622, 831, 206, 298, 910, 638, 799,
		934, 398, 46, 780, 491, 588, 216, 88, 24, 88,
		472, 600, 536, 24, 344, 344, 216, 887, 942, 302,
		390, 698, 379, 506, 718, 490, 971, 415, 206, 780,
		152, 216, 24, 152, 344, 519, 332, 507, 528, 0,
		299, 359, 363, 0, 275, 272, 528, 784, 411, 419,
		808, 511, 424, 296, 507, 255, 1006, 1006, 1006, 107,
		528, 541, 270, 331, 1006, 1006, 1006, 48, 528, 208,
		548, 528, 131, 204, 48, 528, 151, 324, 401, 267,
		307, 159, 528, 0, 389, 291, 0, 16, 1006, 1006,
		1006, 75, 528, 0, 665, 519, 751, 775, 46, 912,
		296, 669, 519, 389, 612, 199, 389, 750, 994, 295,
		528, 528, 612, 95, 401, 580, 324, 199, 270, 1006,
		270, 168, 938, 168, 459, 0, 0, 548, 580, 389,
		132, 31, 541, 323, 196, 676, 784, 784, 68, 391,
		580, 423, 612, 132, 541, 469, 397, 596, 455, 985,
		507, 752, 468, 255, 511, 12, 610, 1023, 459, 541,
		469, 528, 0, 0, 942, 669, 452, 533, 981, 859,
		254, 676, 547, 718, 414, 548, 595, 506, 516, 340,
		587, 490, 559, 40, 20, 563, 36, 28, 812, 599,
		552, 532, 583, 270, 356, 660, 127, 528, 0, 210,
		370, 218, 906, 671, 206, 52, 398, 780, 298, 394,
		442, 711, 170, 378, 647, 938, 276, 39, 810, 42,
		533, 812, 543, 266, 763, 260, 724, 115, 718, 946,
		795, 718, 276, 531, 946, 250, 398, 442, 815, 218,
		170, 844, 278, 362, 638, 947, 630, 819, 202, 533,
		726, 414, 812, 895, 142, 494, 76, 274, 60, 418,
		879, 942, 236, 919, 202, 388, 795, 404, 931, 28,
		658, 793, 28, 490, 2, 939, 708, 726, 934, 276,
		847, 677, 519, 758, 468, 999, 296, 452, 206, 366,
		190, 510, 558, 48, 0, 531, 371, 803, 355, 784,
		389, 363, 611, 267, 548, 67, 131, 16, 389, 1023,
		389, 55, 523, 523, 523, 656, 0, 419, 727, 0,
		523, 523, 523, 0, 0, 443, 385, 656, 835, 459,
		455, 0, 463, 324, 377, 263, 303, 159, 591, 675,
		389, 263, 0, 0, 1006, 1006, 451, 656, 0, 431,
		389, 983, 1006, 255, 0, 0, 878, 12, 315, 400,
		385, 548, 808, 296, 362, 362, 532, 655, 647, 377,
		400, 400, 270, 60, 876, 315, 168, 930, 168, 400,
		0, 0, 324, 400, 541, 400, 676, 400, 68, 391,
		196, 644, 784, 784, 784, 644, 196, 399, 388, 260,
		471, 420, 260, 471, 388, 467, 1006, 784, 784, 420,
		292, 148, 511, 676, 400, 676, 656, 0, 559, 0,
		400, 389, 424, 487, 676, 535, 644, 612, 400, 424,
		296, 942, 48, 596, 579, 385, 942, 487, 405, 752,
		571, 377, 424, 196, 676, 311, 385, 808, 296, 665,
		275, 0, 0, 656, 208, 228, 663, 228, 144, 144,
		254, 16, 377, 196, 292, 446, 699, 260, 808, 545,
		614, 215, 210, 482, 213, 100, 541, 398, 657, 752,
		813, 401, 657, 760, 669, 890, 890, 890, 791, 378,
		965, 398, 261, 967, 389, 132, 23, 206, 624, 0,
		760, 48, 596, 847, 639, 385, 87, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 16, 0, 0, 0, 656,
		0, 0, 0, 0, 0, 0, 0, 0, 48, 784,
		784, 482, 482, 482, 482, 482, 334, 624, 0, 760,
		276, 1023, 942, 532, 671, 667, 400, 0, 0, 0,
		0, 915, 260, 676, 657, 9, 749, 181, 398, 13,
		143, 0, 0, 0, 267, 749, 201, 17, 749, 206,
		482, 398, 5, 749, 67, 206, 624, 0, 760, 48,
		808, 296, 398, 48, 548, 612, 661, 0, 0, 0,
		0, 0, 0, 0, 0, 400, 676, 292, 13, 398,
		657, 5, 126, 3, 661, 9, 942, 665, 296, 5,
		942, 206, 482, 665, 424, 661, 261, 296, 13, 398,
		5, 645, 292, 468, 387, 296, 398, 17, 296, 398,
		13, 507, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 749, 942, 400,
		0, 0, 0, 881, 596, 471, 400, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		371, 228, 663, 228, 144, 144, 254, 16, 0, 0,
		0, 0, 0, 0, 0, 0, 210, 370, 218, 272,
		707, 210, 370, 218, 906, 751, 206, 398, 780, 298,
		394, 442, 787, 170, 378, 727, 950, 760, 942, 752,
		48, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		276, 903, 404, 667, 671, 404, 663, 659, 206, 382,
		140, 152, 168, 206, 750, 780, 354, 624, 942, 296,
		752, 942, 482, 482, 947, 507, 206, 408, 939, 0,
		0, 0, 0, 0, 0, 48, 755, 31, 52, 511,
		506, 505, 977, 206, 52, 324, 398, 680, 942, 103,
		726, 1002, 59, 844, 826, 314, 28, 172, 183, 750,
		874, 46, 516, 76, 314, 390, 278, 610, 71, 548,
		908, 394, 122, 59, 28, 172, 171, 95, 874, 155,
		75, 890, 83, 290, 28, 754, 394, 918, 235, 726,
		1022, 1002, 532, 239, 60, 726, 54, 890, 634, 263,
		95, 1018, 814, 994, 994, 278, 890, 279, 754, 882,
		726, 814, 532, 343, 938, 42, 442, 339, 170, 378,
		938, 340, 1019, 515, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 723, 659, 398, 206, 354,
		624, 942, 398, 1007, 0, 0, 0, 0, 0, 0,
		519, 473, 52, 144, 468, 487, 296, 206, 48, 0,
		676, 401, 400, 101, 400, 270, 270, 473, 890, 543,
		499, 890, 567, 152, 344, 280, 507, 890, 615, 280,
		344, 216, 344, 600, 152, 216, 472, 362, 505, 216,
		472, 536, 344, 280, 88, 88, 472, 536, 280, 507,
		12, 270, 60, 812, 663, 766, 942, 624, 164, 760,
		942, 740, 46, 84, 1007, 407, 302, 942, 206, 624,
		142, 942, 752, 403, 126, 31, 122, 31, 106, 787,
		28, 815, 28, 236, 803, 19, 362, 769, 272, 114,
		31, 938, 716, 426, 855, 362, 106, 19, 270, 942,
		750, 994, 174, 811, 942, 590, 510, 780, 310, 974,
		895, 846, 270, 974, 911, 1022, 814, 985, 716, 985,
		142, 50, 654, 814, 918, 887, 942, 554, 490, 809,
		548, 272, 2, 1003, 658, 1002, 48, 660, 1019, 528,
		400, 0, 23, 651, 23, 23, 23, 55, 23, 71,
		23, 671, 23, 23, 23, 659, 23, 600, 591, 663,
		195, 127, 280, 503, 23, 536, 591, 675, 491, 499,
		88, 503, 23, 344, 591, 159, 579, 839, 24, 503,
		23, 39, 23, 183, 23, 23, 23, 191, 23, 223,
		408, 591, 63, 95, 472, 503, 23, 1015, 750, 53,
		467, 571, 459, 400, 596, 247, 52, 126, 1019, 362,
		362, 442, 1019, 295, 582, 490, 291, 942, 680, 88,
		216, 70, 1019, 408, 652, 66, 1019, 524, 722, 460,
		408, 460, 66, 1019, 168, 332, 722, 722, 722, 722,
		814, 882, 746, 524, 866, 716, 994, 994, 894, 814,
		36, 165, 165, 20, 563, 667, 644, 659, 10, 547,
		42, 31, 0, 0, 216, 591, 152, 591, 400, 20,
		7, 532, 435, 552, 548, 780, 206, 208, 810, 874,
		810, 663, 516, 135, 660, 459, 676, 624, 603, 624,
		660, 623, 942, 398, 582, 752, 667, 760, 780, 418,
		643, 206, 942, 278, 181, 189, 189, 189, 189, 221,
		680, 552, 40, 660, 511, 76, 1010, 511, 396, 994,
		651, 460, 994, 66, 739, 655, 754, 588, 994, 55,
		652, 994, 66, 775, 663, 210, 754, 716, 994, 811,
		750, 780, 994, 671, 70, 823, 39, 750, 716, 994,
		103, 680, 210, 140, 10, 867, 280, 871, 408, 168,
		206, 780, 46, 624, 558, 760, 98, 915, 941, 752,
		558, 482, 887, 710, 942, 941, 507, 434, 1015, 942,
		270, 524, 274, 396, 274, 274, 274, 780, 1002, 610,
		1011, 874, 262, 991, 942, 48, 206, 301
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
		Text = "HP-45";
		labelHPType.Text = "HP-45 Emulator";
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
				textBoxDisplay.Font = new Font("Lucida Console", 15f);
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
				if (b2 <= 7)
				{
					char c = ((i != 0 && i != num - 3) ? ((char)(b + 48)) : ((b >= 8) ? '-' : ' '));
					text += c;
					if (b2 == 2)
					{
						text += '.';
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
		if (num2 >= 78 && num2 < 18)
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
				Stop();
				Reset();
				act_flags &= (F)(-33);
				ShowDisplay();
				break;
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
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
				Run();
				break;
			}
			switch (num)
			{
			case 96:
			case 97:
			case 98:
			case 99:
			case 100:
			case 101:
			case 102:
			case 103:
			case 104:
			case 105:
			case 106:
			case 107:
			case 108:
			case 109:
			case 110:
			case 111:
			case 112:
			case 113:
			case 114:
			case 115:
			case 116:
			case 117:
			case 118:
			case 119:
			case 120:
			case 121:
			case 122:
			case 123:
			case 124:
			case 125:
			case 126:
			case 127:
			case 128:
			case 129:
			case 130:
			case 131:
			case 132:
			case 133:
			case 134:
			case 135:
			case 136:
			case 137:
			case 138:
			case 139:
			case 140:
			case 141:
				prgmmode = true;
				break;
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
			case 150:
			case 151:
			case 152:
			case 153:
			case 154:
			case 155:
			case 156:
			case 157:
			case 158:
			case 159:
			case 160:
			case 161:
			case 162:
			case 163:
			case 164:
			case 165:
			case 166:
			case 167:
			case 168:
			case 169:
			case 170:
			case 171:
			case 172:
			case 173:
			case 174:
			case 175:
			case 176:
			case 177:
			case 178:
			case 179:
			case 180:
			case 181:
			case 182:
			case 183:
				prgmmode = false;
				break;
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
		this.textBoxDisplay.Font = new System.Drawing.Font("HP Classic LED Set", 14f, System.Drawing.FontStyle.Bold);
		this.textBoxDisplay.ForeColor = System.Drawing.Color.Red;
		this.textBoxDisplay.Location = new System.Drawing.Point(10, 15);
		this.textBoxDisplay.Name = "textBoxDisplay";
		this.textBoxDisplay.ReadOnly = true;
		this.textBoxDisplay.Size = new System.Drawing.Size(200, 19);
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
		this.Text = "HP-2x";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
