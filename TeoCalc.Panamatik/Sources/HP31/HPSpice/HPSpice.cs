using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP31;

public class HPSpice : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int EXPSIZE = 3;

	private const int NROFIMAGES = 1;

	private const int BUTTONS = 30;

	private const int RAMSIZE = 448;

	private const int FIRSTROW = 104;

	private const int FIRSTCOL = 25;

	private const int FIRSTCOL2 = 25;

	private const int ROWSIZE = 46;

	private const int COLSIZE = 42;

	private const int COLSIZE2 = 54;

	public byte[] act_a;

	public byte[] act_b;

	public byte[] act_c;

	public byte[] act_y;

	public byte[] act_z;

	public byte[] act_t;

	public byte[] act_m;

	public byte[] act_n;

	public byte act_base;

	public byte act_sp;

	public byte act_p;

	public byte act_f;

	public ushort act_pc;

	public ushort opcode;

	public ushort act_s;

	public byte[] act_ram;

	private F act_flags;

	private ST act_inst_state;

	private byte[] src;

	private byte[] dest;

	private byte[] src2;

	private byte first;

	private byte last;

	private byte act_key_buf;

	private byte crc_flags;

	private byte act_ram_addr;

	private byte act_del_rom;

	private byte rom_addr;

	private ushort[] act_stack;

	private op_fcn[] op_fcn_0000;

	private op_fcn[] op_fcn_0100;

	private op_fcn[] op_fcn_0200;

	private op_fcn[] op_fcn_0300;

	private op_fcn[] op_fcn_02xx;

	private byte[] p_set_map = new byte[16]
	{
		14, 4, 7, 8, 11, 2, 10, 12, 1, 3,
		13, 6, 0, 9, 5, 14
	};

	private byte[] p_test_map = new byte[16]
	{
		4, 8, 12, 2, 9, 1, 6, 3, 1, 13,
		5, 0, 11, 10, 7, 4
	};

	private bool running = true;

	private bool prgmmode;

	private bool focus;

	private bool buttonpressed;

	private bool SegmentFont;

	private string[] ImageTable;

	private int ImageNr;

	private Size OriginalSize;

	private string[] HP33Mnemonics = new string[0];

	private string[] HP34Mnemonics = new string[0];

	private char[] digittab = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'r', 'F', 'o', 'P', 'E', ' '
	};

	private char[] HP3xKeyChartable = new char[35]
	{
		'k', 'i', 'c', 't', 'g', 'y', 'd', 'e', 's', 'r',
		'\r', '\r', 'n', 'x', '\b', '-', '7', '8', '9', '\0',
		'+', '4', '5', '6', '\0', '*', '1', '2', '3', '\0',
		'/', '0', '.', ' ', '\0'
	};

	private byte[] HP3xKeytable = new byte[35]
	{
		52, 51, 50, 49, 48, 148, 147, 146, 145, 144,
		115, 115, 114, 113, 112, 163, 162, 161, 160, 0,
		99, 98, 97, 96, 0, 211, 210, 209, 208, 0,
		67, 66, 65, 64, 0
	};

	public ushort[] opcodeint = new ushort[2048]
	{
		436, 11, 816, 25, 752, 528, 886, 9, 282, 776,
		508, 746, 18, 622, 490, 618, 79, 494, 528, 362,
		95, 282, 111, 262, 614, 266, 708, 700, 528, 26,
		508, 418, 758, 257, 405, 163, 648, 590, 590, 173,
		436, 3, 520, 361, 58, 86, 233, 349, 508, 58,
		110, 294, 223, 494, 418, 150, 186, 528, 398, 636,
		562, 255, 658, 26, 275, 828, 255, 314, 610, 271,
		164, 77, 464, 954, 275, 494, 954, 528, 90, 261,
		328, 351, 264, 218, 261, 328, 366, 850, 75, 528,
		392, 456, 528, 186, 175, 758, 257, 411, 758, 257,
		520, 361, 417, 195, 58, 246, 562, 439, 658, 558,
		14, 18, 278, 508, 826, 120, 622, 474, 483, 482,
		538, 479, 314, 400, 236, 133, 446, 238, 270, 527,
		606, 826, 130, 474, 620, 120, 26, 154, 146, 238,
		755, 26, 434, 654, 954, 451, 520, 611, 154, 860,
		152, 690, 617, 195, 508, 58, 426, 426, 490, 490,
		782, 163, 154, 150, 758, 167, 154, 246, 782, 175,
		986, 430, 730, 175, 675, 618, 618, 14, 914, 850,
		183, 314, 351, 822, 187, 690, 90, 538, 508, 838,
		197, 270, 528, 272, 434, 776, 622, 834, 192, 454,
		775, 284, 204, 690, 753, 195, 882, 257, 361, 26,
		150, 853, 819, 122, 250, 378, 378, 410, 250, 286,
		186, 366, 899, 630, 366, 398, 828, 866, 230, 986,
		1018, 142, 282, 90, 700, 344, 1018, 979, 482, 922,
		955, 346, 474, 400, 998, 748, 239, 258, 154, 250,
		528, 313, 193, 163, 90, 520, 351, 436, 7, 282,
		634, 274, 164, 276, 804, 719, 868, 727, 292, 733,
		100, 737, 828, 472, 188, 528, 444, 408, 408, 536,
		408, 344, 152, 280, 600, 88, 88, 408, 508, 528,
		282, 508, 280, 344, 131, 26, 52, 361, 150, 250,
		957, 250, 516, 429, 250, 649, 250, 389, 684, 305,
		700, 472, 146, 250, 251, 122, 626, 299, 934, 154,
		478, 154, 594, 239, 90, 418, 52, 805, 511, 934,
		594, 295, 18, 314, 418, 239, 934, 418, 251, 154,
		957, 250, 282, 154, 52, 361, 52, 233, 278, 175,
		272, 498, 776, 538, 379, 314, 474, 1022, 250, 400,
		528, 494, 954, 746, 396, 850, 361, 654, 746, 386,
		278, 26, 366, 531, 262, 882, 382, 634, 266, 708,
		276, 383, 274, 436, 3, 954, 622, 519, 270, 754,
		394, 90, 538, 686, 286, 528, 154, 454, 454, 454,
		154, 591, 494, 538, 587, 314, 630, 615, 535, 154,
		462, 154, 474, 540, 403, 874, 374, 591, 282, 164,
		481, 634, 280, 506, 274, 1018, 868, 468, 292, 438,
		100, 448, 932, 456, 420, 461, 464, 528, 188, 837,
		24, 536, 344, 216, 88, 472, 444, 528, 956, 837,
		216, 24, 536, 280, 892, 528, 636, 837, 837, 252,
		528, 572, 837, 188, 528, 216, 216, 528, 892, 216,
		88, 24, 88, 472, 600, 536, 24, 280, 216, 316,
		528, 316, 408, 600, 216, 88, 280, 472, 88, 536,
		24, 344, 408, 508, 528, 282, 508, 152, 216, 24,
		152, 344, 536, 344, 24, 600, 152, 600, 600, 280,
		508, 528, 508, 762, 599, 882, 615, 52, 361, 750,
		645, 494, 26, 918, 750, 662, 934, 114, 700, 400,
		594, 71, 82, 286, 107, 954, 482, 82, 122, 272,
		338, 264, 146, 474, 954, 626, 131, 776, 264, 314,
		474, 594, 95, 178, 594, 338, 195, 90, 474, 235,
		356, 557, 498, 400, 90, 82, 474, 103, 626, 464,
		250, 116, 649, 954, 250, 263, 314, 610, 259, 882,
		568, 164, 666, 282, 793, 52, 753, 18, 476, 591,
		690, 660, 254, 404, 703, 52, 193, 436, 3, 668,
		257, 520, 854, 608, 712, 154, 116, 7, 850, 604,
		154, 52, 361, 282, 343, 668, 257, 520, 122, 842,
		604, 430, 590, 478, 854, 641, 846, 636, 146, 178,
		370, 370, 402, 754, 637, 260, 274, 250, 712, 250,
		31, 846, 622, 90, 371, 186, 578, 854, 651, 282,
		319, 52, 753, 270, 52, 417, 878, 659, 626, 146,
		270, 59, 452, 52, 753, 595, 750, 585, 634, 250,
		54, 116, 957, 154, 570, 714, 678, 570, 154, 250,
		746, 683, 698, 134, 400, 474, 364, 684, 508, 834,
		700, 470, 154, 146, 52, 273, 52, 349, 278, 319,
		430, 400, 723, 250, 116, 957, 250, 52, 449, 335,
		464, 622, 172, 710, 528, 408, 740, 273, 815, 252,
		813, 828, 344, 124, 536, 316, 528, 764, 813, 828,
		600, 444, 528, 124, 813, 892, 528, 380, 813, 252,
		528, 494, 494, 52, 449, 264, 116, 137, 52, 1007,
		154, 116, 137, 494, 494, 52, 417, 264, 244, 953,
		947, 648, 392, 456, 758, 832, 754, 771, 452, 644,
		274, 52, 417, 52, 37, 716, 620, 836, 890, 784,
		520, 456, 712, 282, 26, 58, 182, 754, 797, 260,
		860, 797, 668, 797, 652, 452, 268, 508, 746, 918,
		345, 412, 927, 937, 282, 122, 250, 954, 418, 646,
		219, 90, 154, 52, 753, 264, 154, 52, 317, 622,
		52, 853, 250, 90, 456, 154, 52, 425, 874, 927,
		90, 627, 490, 146, 858, 773, 90, 520, 250, 712,
		250, 52, 329, 372, 197, 90, 154, 456, 52, 185,
		520, 712, 328, 79, 852, 90, 528, 122, 730, 870,
		578, 858, 921, 154, 412, 869, 909, 282, 411, 953,
		154, 345, 282, 180, 797, 250, 953, 378, 1018, 250,
		668, 892, 941, 250, 154, 570, 154, 250, 282, 52,
		753, 18, 476, 896, 941, 314, 274, 788, 908, 494,
		494, 52, 449, 916, 908, 122, 986, 538, 52, 805,
		852, 597, 520, 712, 18, 154, 436, 3, 750, 857,
		90, 404, 257, 345, 52, 565, 909, 508, 264, 328,
		286, 494, 750, 940, 498, 400, 428, 931, 328, 431,
		264, 282, 498, 1018, 759, 154, 264, 482, 178, 264,
		986, 986, 594, 731, 18, 314, 154, 122, 922, 711,
		264, 498, 264, 90, 474, 400, 428, 957, 250, 328,
		52, 449, 18, 835, 494, 934, 878, 974, 264, 270,
		188, 250, 116, 13, 250, 879, 314, 610, 875, 954,
		258, 758, 873, 464, 855, 660, 999, 644, 528, 652,
		528, 954, 494, 878, 1001, 528, 508, 282, 472, 536,
		344, 216, 600, 536, 88, 408, 216, 216, 600, 472,
		344, 508, 528, 939, 201, 713, 52, 177, 436, 3,
		900, 251, 500, 533, 144, 201, 690, 469, 209, 75,
		201, 713, 52, 381, 19, 201, 622, 280, 344, 216,
		344, 600, 152, 216, 472, 528, 201, 52, 177, 209,
		469, 19, 644, 412, 85, 476, 124, 460, 995, 690,
		481, 655, 85, 75, 392, 456, 154, 282, 508, 88,
		536, 508, 528, 772, 555, 908, 780, 555, 388, 836,
		244, 67, 244, 953, 911, 505, 26, 150, 250, 244,
		953, 250, 180, 919, 644, 259, 668, 38, 652, 276,
		122, 260, 995, 456, 264, 156, 94, 712, 328, 19,
		263, 52, 177, 655, 272, 276, 111, 156, 106, 712,
		132, 260, 30, 598, 282, 528, 388, 186, 783, 85,
		11, 216, 152, 494, 52, 611, 268, 995, 452, 995,
		52, 363, 644, 451, 500, 553, 186, 58, 136, 972,
		980, 135, 436, 55, 12, 555, 0, 0, 455, 942,
		942, 154, 624, 56, 612, 90, 154, 164, 167, 804,
		46, 868, 45, 292, 97, 52, 381, 52, 25, 724,
		778, 752, 328, 19, 244, 953, 378, 378, 1018, 502,
		270, 363, 152, 344, 280, 494, 528, 464, 528, 505,
		180, 955, 0, 500, 533, 836, 388, 644, 520, 392,
		154, 26, 58, 150, 754, 207, 452, 660, 206, 260,
		274, 250, 788, 68, 924, 216, 154, 186, 1018, 922,
		116, 137, 250, 622, 874, 226, 622, 907, 494, 954,
		250, 264, 328, 378, 378, 378, 1018, 250, 874, 251,
		116, 429, 282, 250, 328, 378, 474, 796, 247, 954,
		1018, 250, 538, 331, 314, 250, 328, 250, 796, 324,
		878, 323, 474, 275, 508, 328, 660, 463, 860, 354,
		250, 456, 372, 945, 712, 456, 90, 52, 185, 284,
		278, 690, 436, 3, 400, 420, 260, 494, 99, 282,
		250, 116, 13, 250, 116, 389, 428, 287, 250, 264,
		954, 954, 282, 508, 88, 264, 700, 408, 447, 58,
		238, 508, 218, 366, 899, 218, 244, 933, 418, 251,
		464, 235, 52, 349, 58, 52, 855, 494, 874, 331,
		538, 967, 314, 52, 753, 18, 788, 344, 250, 328,
		250, 52, 449, 264, 244, 953, 52, 313, 622, 264,
		328, 494, 111, 474, 119, 282, 26, 418, 52, 805,
		91, 934, 934, 594, 407, 18, 264, 154, 570, 314,
		264, 122, 178, 610, 415, 154, 470, 154, 758, 381,
		626, 18, 954, 447, 494, 626, 499, 274, 264, 154,
		602, 328, 660, 392, 654, 90, 730, 452, 52, 449,
		412, 354, 122, 572, 290, 615, 474, 446, 619, 434,
		954, 122, 494, 474, 158, 52, 329, 197, 860, 441,
		250, 90, 456, 154, 52, 425, 154, 264, 398, 712,
		328, 154, 937, 520, 712, 456, 142, 14, 954, 264,
		52, 313, 52, 193, 895, 250, 90, 328, 154, 558,
		14, 954, 52, 449, 274, 395, 282, 404, 459, 634,
		266, 274, 91, 860, 351, 278, 887, 404, 469, 90,
		52, 565, 395, 860, 351, 250, 90, 456, 52, 185,
		945, 712, 456, 79, 218, 622, 919, 218, 235, 400,
		748, 481, 218, 259, 52, 193, 476, 495, 690, 52,
		27, 90, 538, 244, 909, 295, 956, 154, 674, 154,
		436, 723, 52, 825, 91, 8, 51, 551, 828, 280,
		154, 968, 282, 392, 8, 688, 140, 55, 132, 500,
		533, 200, 52, 25, 264, 328, 117, 20, 658, 874,
		549, 622, 563, 270, 599, 26, 904, 508, 274, 400,
		590, 135, 528, 14, 182, 954, 494, 159, 607, 532,
		559, 498, 400, 874, 566, 494, 746, 567, 380, 587,
		494, 958, 647, 336, 302, 850, 111, 590, 478, 239,
		596, 690, 308, 401, 636, 418, 299, 454, 464, 279,
		578, 484, 605, 612, 594, 212, 594, 434, 14, 122,
		400, 726, 602, 776, 229, 958, 178, 154, 90, 272,
		400, 6, 582, 264, 328, 58, 250, 146, 178, 508,
		722, 621, 280, 700, 400, 626, 435, 482, 473, 473,
		473, 482, 482, 447, 172, 183, 846, 636, 956, 408,
		250, 14, 776, 136, 972, 980, 640, 988, 643, 492,
		805, 328, 55, 828, 500, 43, 228, 655, 400, 498,
		103, 117, 516, 328, 274, 26, 182, 58, 102, 314,
		6, 858, 671, 886, 658, 850, 555, 178, 746, 678,
		654, 618, 272, 582, 540, 610, 174, 956, 454, 454,
		454, 395, 46, 775, 124, 454, 122, 956, 934, 934,
		934, 78, 572, 714, 705, 518, 707, 102, 14, 229,
		142, 46, 90, 52, 25, 612, 524, 391, 816, 8,
		506, 264, 282, 264, 392, 392, 584, 584, 584, 584,
		250, 90, 968, 968, 602, 858, 770, 508, 482, 624,
		752, 464, 494, 236, 735, 622, 624, 154, 56, 154,
		922, 858, 770, 622, 923, 508, 52, 9, 1021, 809,
		1021, 636, 152, 492, 758, 186, 378, 378, 154, 688,
		499, 482, 348, 548, 270, 264, 956, 43, 553, 388,
		975, 508, 282, 272, 634, 58, 464, 490, 364, 782,
		252, 920, 664, 664, 792, 664, 154, 474, 474, 474,
		975, 596, 609, 758, 949, 956, 710, 955, 923, 200,
		16, 533, 516, 116, 335, 404, 807, 533, 116, 159,
		435, 175, 999, 519, 412, 508, 553, 235, 553, 316,
		644, 975, 404, 511, 282, 491, 611, 783, 327, 404,
		185, 468, 1005, 52, 393, 699, 404, 89, 553, 700,
		452, 975, 426, 404, 8, 468, 145, 668, 576, 942,
		942, 968, 4, 804, 525, 308, 563, 426, 426, 426,
		823, 404, 970, 476, 873, 164, 1010, 392, 456, 691,
		404, 892, 533, 507, 243, 707, 883, 404, 130, 476,
		889, 164, 715, 712, 436, 43, 533, 388, 516, 180,
		3, 404, 824, 52, 119, 204, 268, 524, 588, 844,
		396, 460, 652, 716, 508, 528, 27, 299, 935, 983,
		404, 61, 625, 699, 404, 115, 52, 147, 520, 712,
		154, 528, 426, 426, 383, 404, 189, 476, 937, 164,
		1009, 392, 456, 690, 52, 585, 436, 3, 404, 521,
		553, 276, 797, 308, 401, 58, 88, 186, 582, 580,
		956, 70, 854, 609, 70, 588, 975, 404, 11, 553,
		196, 284, 851, 975, 533, 180, 999, 468, 777, 426,
		426, 426, 323, 404, 71, 476, 985, 164, 1008, 52,
		169, 699, 404, 518, 553, 596, 502, 758, 1011, 690,
		284, 512, 590, 436, 391, 404, 6, 553, 315, 172,
		839, 400, 400, 400, 400, 436, 503, 404, 59, 584,
		699, 404, 170, 533, 644, 625, 503, 359
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

	public void ACTSpice()
	{
		op_fcn_0000 = new op_fcn[64]
		{
			op_nop, op_keys_to_rom_addr, op_sel_rom, op_unknown, op_crc_test_motor_on, op_keys_to_a, op_sel_rom, op_unknown, op_unknown, op_a_to_rom_addr,
			op_sel_rom, op_crc_motor_on, op_crc_test_f1, op_display_reset_twf, op_sel_rom, op_crc_motor_off, op_crc_set_f2, op_binary, op_sel_rom, op_unknown,
			op_crc_test_f2, op_circulate_a_left, op_sel_rom, op_crc_test_card_in, op_crc_set_f3, op_dec_p, op_sel_rom, op_unknown, op_crc_test_f3, op_inc_p,
			op_sel_rom, op_crc_test_prot, op_crc_set_f4, op_return, op_sel_rom, op_bank_switch, op_crc_test_f4, op_pik_home, op_sel_rom, op_c_to_addr,
			op_crc_set_f0, op_pik_cr, op_sel_rom, op_clear_data_regs, op_crc_clear_f0, op_pik_keys, op_sel_rom, op_c_to_data, op_crc_set_f1, op_pik_c4,
			op_sel_rom, op_rom_selftest, op_crc_clear_f1, op_pik_d4, op_sel_rom, op_unknown, op_unknown, op_pik_e4, op_sel_rom, op_unknown,
			op_crc_write_prot, op_pik_print, op_sel_rom, op_nop
		};
		op_fcn_0100 = new op_fcn[4] { op_set_s, op_test_s_eq_1, op_test_p_eq, op_del_sel_rom };
		op_fcn_02xx = new op_fcn[4] { op_nop, op_load_constant, op_c_to_register, op_register_to_c };
		op_fcn_0300 = new op_fcn[4] { op_clr_s, op_test_s_eq_0, op_test_p_ne, op_set_p };
		op_fcn_0200 = new op_fcn[16]
		{
			op_clear_reg, op_clear_s, op_display_toggle, op_display_off, op_mx, op_mx, op_mx, op_mx, op_stack_to_a, op_down_rotate,
			op_y_to_a, op_c_to_stack, op_decimal, op_unknown, op_f_to_a, op_f_exch_a
		};
		act_a = new byte[14];
		act_b = new byte[14];
		act_c = new byte[14];
		act_y = new byte[14];
		act_z = new byte[14];
		act_t = new byte[14];
		act_m = new byte[14];
		act_n = new byte[14];
		act_stack = new ushort[2];
		act_ram = new byte[448];
		act_reset();
	}

	private void op_unknown()
	{
	}

	private void op_nop()
	{
	}

	private void op_binary()
	{
		act_base = 16;
	}

	private void op_decimal()
	{
		act_base = 10;
	}

	private void op_crc_test_motor_on()
	{
	}

	private void op_crc_write_prot()
	{
	}

	private void op_crc_motor_on()
	{
	}

	private void op_crc_motor_off()
	{
	}

	private void op_crc_test_card_in()
	{
	}

	private void op_crc_test_prot()
	{
	}

	private void op_display_reset_twf()
	{
	}

	private void op_pik_home()
	{
	}

	private void op_pik_cr()
	{
	}

	private void op_pik_c4()
	{
	}

	private void op_pik_d4()
	{
	}

	private void op_pik_e4()
	{
	}

	private void op_pik_print()
	{
	}

	private void op_pik_keys()
	{
	}

	private void op_crc_clear_f0()
	{
		if ((crc_flags & 1) != 0)
		{
			act_s |= 8;
			crc_flags &= 254;
		}
	}

	private void op_crc_clear_f1()
	{
		if ((crc_flags & 2) != 0)
		{
			act_s |= 8;
			crc_flags &= 253;
		}
	}

	private void op_crc_set_f0()
	{
		crc_flags |= 1;
	}

	private void op_crc_set_f1()
	{
		crc_flags |= 2;
	}

	private void op_crc_set_f2()
	{
		crc_flags |= 4;
	}

	private void op_crc_set_f3()
	{
		crc_flags |= 8;
	}

	private void op_crc_set_f4()
	{
		crc_flags |= 16;
	}

	private void op_crc_test_f1()
	{
		if (prgmmode)
		{
			act_s |= 8;
		}
		crc_flags &= 253;
	}

	private void op_crc_test_f2()
	{
		crc_flags &= 127;
		if ((act_s & 8) != 0)
		{
			crc_flags |= 128;
		}
		act_s &= 65527;
		if ((crc_flags & 4) != 0)
		{
			act_s |= 8;
		}
		crc_flags &= 251;
		if ((crc_flags & 0x80) != 0)
		{
			crc_flags |= 4;
		}
	}

	private void op_crc_test_f3()
	{
		crc_flags &= 127;
		if ((act_s & 8) != 0)
		{
			crc_flags |= 128;
		}
		act_s &= 65527;
		if ((crc_flags & 8) != 0)
		{
			act_s |= 8;
		}
		crc_flags &= 247;
		if ((crc_flags & 0x80) != 0)
		{
			crc_flags |= 8;
		}
	}

	private void op_crc_test_f4()
	{
		crc_flags &= 127;
		if ((act_s & 8) != 0)
		{
			crc_flags |= 128;
		}
		act_s &= 65527;
		if ((crc_flags & 0x10) != 0)
		{
			act_s |= 8;
		}
		crc_flags &= 239;
		if ((crc_flags & 0x80) != 0)
		{
			crc_flags |= 16;
		}
	}

	private void op_rom_addr_to_buffer()
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

	private void op_goto()
	{
		if ((act_flags & F.PREV_CARRY) == 0)
		{
			act_pc = (ushort)((act_pc & 0xFF00) | (opcode >> 2));
			handle_del_rom();
		}
	}

	private void op_jsb()
	{
		act_stack[act_sp] = act_pc;
		act_sp = (byte)((act_sp + 1) & 1);
		act_pc = (ushort)((act_pc & 0xFF00) | (opcode >> 2));
		handle_del_rom();
	}

	private void op_return()
	{
		act_sp = (byte)((act_sp - 1) & 1);
		act_pc = act_stack[act_sp];
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

	private void setfield()
	{
		switch ((byte)(((byte)opcode >> 2) & 7))
		{
		case 0:
			first = act_p;
			last = act_p;
			break;
		case 1:
			first = 0;
			last = act_p;
			break;
		case 2:
			first = 2;
			last = 2;
			break;
		case 3:
			first = 0;
			last = 2;
			break;
		case 4:
			first = 13;
			last = 13;
			break;
		case 5:
			first = 3;
			last = 12;
			break;
		case 6:
			first = 0;
			last = 13;
			break;
		case 7:
			first = 3;
			last = 13;
			break;
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
		act_inst_state = ST.branch;
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
		act_inst_state = ST.branch;
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

	private void op_dec_p()
	{
		if (act_p != 0)
		{
			act_p--;
		}
		else
		{
			act_p = 13;
		}
	}

	private void op_inc_p()
	{
		act_p++;
		if (act_p >= 14)
		{
			act_p = 0;
			act_flags |= F.PCARRY;
		}
	}

	private void op_load_constant()
	{
		act_c[act_p] = (byte)(opcode >> 6);
		op_dec_p();
	}

	private void op_sel_rom()
	{
		act_pc = (ushort)(((opcode & 0x3C0) << 2) | (byte)act_pc);
	}

	private void op_del_sel_rom()
	{
		act_del_rom = (byte)(opcode >> 6);
		act_flags |= F.DEL_ROM;
	}

	private void op_mx()
	{
		byte[] array = (((opcode & 0x80) != 0) ? act_n : act_m);
		for (byte b = 0; b < 14; b++)
		{
			byte b2 = act_c[b];
			act_c[b] = array[b];
			if ((opcode & 0x40) == 0)
			{
				array[b] = b2;
			}
		}
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

	private void op_arith()
	{
		switch (opcode >> 5)
		{
		case 0:
			dest = act_a;
			reg_zero();
			break;
		case 1:
			dest = act_b;
			reg_zero();
			break;
		case 2:
			src = act_b;
			dest = act_a;
			reg_exch();
			break;
		case 3:
			src = act_a;
			dest = act_b;
			reg_copy();
			break;
		case 4:
			src = act_c;
			dest = act_a;
			reg_exch();
			break;
		case 5:
			src = act_c;
			dest = act_a;
			reg_copy();
			break;
		case 6:
			src = act_b;
			dest = act_c;
			reg_copy();
			break;
		case 7:
			src = act_c;
			dest = act_b;
			reg_exch();
			break;
		case 8:
			dest = act_c;
			reg_zero();
			break;
		case 9:
			dest = act_a;
			src = act_b;
			reg_add();
			break;
		case 10:
			dest = act_a;
			src = act_c;
			reg_add();
			break;
		case 11:
			dest = act_c;
			src = act_c;
			reg_add();
			break;
		case 12:
			dest = act_c;
			src = act_a;
			reg_add();
			break;
		case 13:
			dest = act_a;
			reg_inc();
			break;
		case 14:
			dest = act_a;
			reg_shift_left();
			break;
		case 15:
			dest = act_c;
			reg_inc();
			break;
		case 16:
			dest = act_a;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 17:
			dest = act_c;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 18:
			act_flags |= F.CARRY;
			dest = act_a;
			src = act_a;
			src2 = null;
			reg_sub();
			break;
		case 19:
			act_flags |= F.CARRY;
			dest = act_c;
			src = act_c;
			src2 = null;
			reg_sub();
			break;
		case 20:
			dest = act_c;
			src = null;
			src2 = act_c;
			reg_sub();
			break;
		case 21:
			act_flags |= F.CARRY;
			dest = act_c;
			src = null;
			src2 = act_c;
			reg_sub();
			break;
		case 22:
			act_inst_state = ST.branch;
			src = act_b;
			dest = null;
			reg_test_nonequal();
			break;
		case 23:
			act_inst_state = ST.branch;
			src = act_c;
			dest = null;
			reg_test_nonequal();
			break;
		case 24:
			act_inst_state = ST.branch;
			dest = null;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 25:
			act_inst_state = ST.branch;
			dest = null;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 26:
			act_inst_state = ST.branch;
			src = act_a;
			dest = null;
			reg_test_equal();
			break;
		case 27:
			act_inst_state = ST.branch;
			src = act_c;
			dest = null;
			reg_test_equal();
			break;
		case 28:
			dest = act_a;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 29:
			dest = act_a;
			reg_shift_right();
			break;
		case 30:
			dest = act_b;
			reg_shift_right();
			break;
		case 31:
			dest = act_c;
			reg_shift_right();
			break;
		}
	}

	private void op_clear_s()
	{
		act_s &= 32806;
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

	private void op_bank_switch()
	{
		act_flags ^= F.BANK;
	}

	private void op_rom_selftest()
	{
		act_inst_state = ST.selftest;
		act_pc &= 64512;
	}

	private void rom_selftest_done()
	{
		act_inst_state = ST.norm;
		op_return();
	}

	private void op_c_to_addr()
	{
		act_ram_addr = (byte)((act_c[1] << 4) + act_c[0]);
	}

	private void register_to_c(byte addr)
	{
		if (addr < 64)
		{
			for (byte b = 0; b < 7; b++)
			{
				act_c[b * 2] = (byte)(act_ram[addr * 14 / 2 + b] & 0xF);
				act_c[b * 2 + 1] = (byte)(act_ram[addr * 14 / 2 + b] >> 4);
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
				act_ram[addr * 14 / 2 + b] = (byte)((act_c[b * 2] & 0xF) | (act_c[b * 2 + 1] << 4));
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

	private void op_clear_reg()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_a[b] = (act_b[b] = (act_c[b] = (act_y[b] = (act_z[b] = (act_t[b] = 0)))));
		}
	}

	private void op_set_p()
	{
		act_p = p_set_map[opcode >> 6];
	}

	private void op_test_p_eq()
	{
		act_inst_state = ST.branch;
		if (act_p == p_test_map[opcode >> 6])
		{
			act_flags &= (F)(-3);
		}
		else
		{
			act_flags |= F.CARRY;
		}
	}

	private void op_test_p_ne()
	{
		act_inst_state = ST.branch;
		byte b = p_test_map[opcode >> 6];
		if ((act_flags & F.PCARRY) != 0 && act_p == 1 && b == 0)
		{
			act_flags |= F.CARRY;
		}
		else if (act_p != b)
		{
			act_flags &= (F)(-3);
		}
		else
		{
			act_flags |= F.CARRY;
		}
	}

	private void op_keys_to_rom_addr()
	{
		act_pc &= 65280;
		handle_del_rom();
		act_pc += act_key_buf;
		rom_addr = act_key_buf;
	}

	private void op_keys_to_a()
	{
		act_a[2] = (byte)(act_key_buf >> 4);
		act_a[1] = (byte)(act_key_buf & 0xF);
	}

	private void op_display_off()
	{
		act_flags &= (F)(-33);
	}

	private void op_display_toggle()
	{
		act_flags ^= F.DISPLAY_ON;
	}

	private void act_execute_cycle()
	{
		ushort num = act_pc;
		if ((act_flags & F.BANK) != 0)
		{
			if (num < 1024)
			{
				act_flags &= (F)(-129);
			}
			else
			{
				num |= 0x1000;
			}
		}
		opcode = Getopcode(num);
		if ((act_flags & F.CARRY) != 0)
		{
			act_flags |= F.PREV_CARRY;
		}
		else
		{
			act_flags &= (F)(-5);
		}
		act_flags &= (F)(-3);
		act_pc++;
		switch (act_inst_state)
		{
		case ST.norm:
			switch ((byte)opcode & 3)
			{
			case 0:
				switch ((byte)opcode & 0xC)
				{
				case 0:
					op_fcn_0000[opcode >> 4]();
					break;
				case 4:
					op_fcn_0100[((byte)opcode >> 4) & 3]();
					break;
				case 8:
					if (((opcode >> 4) & 3) != 0)
					{
						op_fcn_02xx[((byte)opcode >> 4) & 3]();
					}
					else
					{
						op_fcn_0200[opcode >> 6]();
					}
					break;
				case 12:
					op_fcn_0300[((byte)opcode >> 4) & 3]();
					break;
				}
				break;
			case 1:
				op_jsb();
				break;
			case 2:
				setfield();
				op_arith();
				break;
			case 3:
				op_goto();
				break;
			}
			break;
		case ST.branch:
			act_inst_state = ST.norm;
			if ((act_flags & F.PREV_CARRY) == 0)
			{
				act_pc = (ushort)((act_pc & 0xFC00) | opcode);
			}
			break;
		case ST.selftest:
			if (opcode == 1060)
			{
				op_bank_switch();
			}
			if ((act_pc & 0x3FF) == 0)
			{
				rom_selftest_done();
			}
			break;
		}
		if ((opcode & 0x3D0) != 464)
		{
			act_flags &= (F)(-65);
		}
	}

	private bool act_execute_instruction()
	{
		do
		{
			act_execute_cycle();
		}
		while (act_inst_state != ST.norm);
		return true;
	}

	private void act_press_key(byte keycode)
	{
		act_key_buf = keycode;
		act_s |= 32768;
	}

	private void act_release_key()
	{
		act_s &= 32768;
	}

	private void act_clear_memory()
	{
		for (ushort num = 0; num < 448; num++)
		{
			act_ram[num] = 0;
		}
	}

	private void act_clear_reg()
	{
		for (byte b = 0; b < 2; b++)
		{
			act_stack[b] = 0;
		}
		for (byte b = 0; b < 14; b++)
		{
			act_m[b] = (act_n[b] = 0);
		}
	}

	private void act_reset()
	{
		act_flags = (F)0;
		crc_flags = 0;
		act_del_rom = 0;
		act_base = 10;
		act_inst_state = ST.norm;
		act_sp = 0;
		act_key_buf = 0;
		act_pc = 0;
		act_p = 0;
		act_s = 0;
		opcode = 0;
		op_clear_reg();
		act_clear_reg();
		act_clear_memory();
	}

	public HPSpice()
	{
		InitializeComponent();
		Text = "HP-31E";
		labelHPType.Text = "HP-31E Emulator";
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
		ImageTable = new string[1] { "spice.bmp" };
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
		ACTSpice();
	}

	private void DefaultRAM()
	{
		for (int i = 0; i < 448; i++)
		{
			act_ram[i] = 0;
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
			int num = 11;
			for (int i = 0; i < num; i++)
			{
				byte b = act_a[13 - i];
				byte b2 = act_b[13 - i];
				if (i == 1)
				{
					b2 &= 0xFB;
				}
				char c;
				if (i != 0)
				{
					c = (((b2 & 4) == 0) ? digittab[b] : (((b & 8) != 0) ? '-' : ' '));
				}
				else
				{
					c = (((act_b[12] & 4) == 0) ? ' ' : '-');
					b2 = 0;
				}
				if (c != ' ' || (b2 & 7) != 1)
				{
					text += c;
				}
				if ((b2 & 1) == 1)
				{
					c = (SegmentFont ? ';' : '.');
					text += c;
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
			act_execute_instruction();
			if (!prgmmode)
			{
				act_s |= 8;
			}

			if (buttonpressed)
			{
				act_s |= 32768;
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
			act_del_rom,
			0);
	private void timer1_Tick(object sender, EventArgs e)
	{
		if (!running)
		{
			return;
		}
		for (int i = 0; i < 200; i++)
		{
			act_execute_instruction();
			if (buttonpressed)
			{
				act_s |= 32768;
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
		if (num2 >= 56 && num2 < 76)
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
		if (num2 >= 81 && num2 < 449 && num >= 4 && num < 214)
		{
			int num3 = (num2 - 104 + 23) / 46;
			int num4 = ((num3 > 2) ? ((num - 25 + 27) / 54) : ((num - 25 + 21) / 42));
			byte code = HP3xKeytable[num3 * 5 + num4];
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

	private void HP25_KeyPress(object sender, KeyPressEventArgs e)
	{
		char c = char.ToLower(e.KeyChar);
		for (int i = 0; i < 35; i++)
		{
			if (c == HP3xKeyChartable[i])
			{
				press_key(HP3xKeytable[i]);
				break;
			}
		}
		switch (c)
		{
		case 'm':
			prgmmode = !prgmmode;
			break;
		case ',':
			press_key(HP3xKeytable[32]);
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
		openFileDialog.Filter = "hp25 files (*.hp25)|*.hp25|all files (*.*)|*.*";
		openFileDialog.FilterIndex = 2;
		openFileDialog.RestoreDirectory = true;
		if (openFileDialog.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		try
		{
			if (ReadProgram(openFileDialog.FileName))
			{
				int num = -17;
				crc_flags &= (byte)num;
				return;
			}
			throw new Exception("No program file");
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "HP25", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void buttonSave_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		saveFileDialog.Filter = "hp25 files (*.hp25)|*.hp25|all files (*.*)|*.*";
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
				MessageBox.Show(ex.Message, "HP25", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HPSpice));
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
		this.textBoxDisplay.Font = new System.Drawing.Font("HP Classic LED Set", 13f, System.Drawing.FontStyle.Bold);
		this.textBoxDisplay.ForeColor = System.Drawing.Color.Red;
		this.textBoxDisplay.Location = new System.Drawing.Point(10, 15);
		this.textBoxDisplay.Name = "textBoxDisplay";
		this.textBoxDisplay.ReadOnly = true;
		this.textBoxDisplay.Size = new System.Drawing.Size(180, 18);
		this.textBoxDisplay.TabIndex = 25;
		this.textBoxDisplay.TabStop = false;
		this.textBoxDisplay.Click += new System.EventHandler(textBoxDisplay_Click);
		this.textBoxDisplay.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
		this.labelHPType.AutoSize = true;
		this.labelHPType.Location = new System.Drawing.Point(270, 29);
		this.labelHPType.Name = "labelHPType";
		this.labelHPType.Size = new System.Drawing.Size(80, 13);
		this.labelHPType.TabIndex = 26;
		this.labelHPType.Text = "HP-3x Emulator";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(250, 53);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(111, 13);
		this.label2.TabIndex = 27;
		this.label2.Text = "(c) PANAMATIK 2016";
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(270, 77);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(66, 13);
		this.label3.TabIndex = 28;
		this.label3.Text = "Version 1.01";
		this.buttonLoad.Location = new System.Drawing.Point(260, 218);
		this.buttonLoad.Name = "buttonLoad";
		this.buttonLoad.Size = new System.Drawing.Size(98, 23);
		this.buttonLoad.TabIndex = 34;
		this.buttonLoad.Text = "Load Program";
		this.buttonLoad.UseVisualStyleBackColor = true;
		this.buttonLoad.Click += new System.EventHandler(buttonLoad_Click);
		this.buttonSave.Location = new System.Drawing.Point(260, 248);
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
		this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
		this.textBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(textBox1_KeyUp);
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(0, 0);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(220, 404);
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseDown);
		this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseUp);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(220, 407);
		base.Controls.Add(this.buttonSave);
		base.Controls.Add(this.buttonLoad);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.labelHPType);
		base.Controls.Add(this.textBoxDisplay);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.textBox1);
		this.MaximumSize = new System.Drawing.Size(420, 445);
		this.MinimumSize = new System.Drawing.Size(236, 445);
		base.Name = "HPSpice";
		this.Text = "HP-3x";
		base.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
