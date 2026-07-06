using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP33;

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

	private string[] HP2xMnemonicsAll = new string[256]
	{
		"GTO 00", "GTO 01", "GTO 02", "GTO 03", "GTO 04", "GTO 05", "GTO 06", "GTO 07", "GTO 08", "GTO 09",
		"STO 0", "RCL 0", "STO - 0", "STO + 0", "STO * 0", "STO : 0", "GTO 10", "GTO 11", "GTO 12", "GTO 13",
		"GTO 14", "GTO 15", "GTO 16", "GTO 17", "GTO 18", "GTO 19", "STO 1", "RCL 1", "STO - 1", "STO + 1",
		"STO * 1", "STO : 1", "GTO 20", "GTO 21", "GTO 22", "GTO 23", "GTO 24", "GTO 25", "GTO 26", "GTO 27",
		"GTO 28", "GTO 29", "STO 2", "RCL 2", "STO - 2", "STO + 2", "STO * 2", "STO : 2", "GTO 30", "GTO 31",
		"GTO 32", "GTO 33", "GTO 34", "GTO 35", "GTO 36", "GTO 37", "GTO 38", "GTO 39", "STO 3", "RCL 3",
		"STO - 3", "STO + 3", "STO * 3", "STO : 3", "GTO 40", "GTO 41", "GTO 42", "GTO 43", "GTO 44", "GTO 45",
		"GTO 46", "GTO 47", "GTO 48", "GTO 49", "STO 4", "RCL 4", "STO - 4", "STO + 4", "STO * 4", "STO : 4",
		"FIX 0", "FIX 1", "FIX 2", "FIX 3", "FIX 4", "FIX 5", "FIX 6", "FIX 7", "FIX 8", "FIX 9",
		"STO 5", "RCL 5", "STO - 5", "STO + 5", "STO * 5", "STO : 5", "SCI 0", "SCI 1", "SCI 2", "SCI 3",
		"SCI 4", "SCI 5", "SCI 6", "SCI 7", "SCI 8", "SCI 9", "STO 6", "RCL 6", "STO - 6", "STO + 6",
		"STO * 6", "STO : 6", "ENG 0", "ENG 1", "ENG 2", "ENG 3", "ENG 4", "ENG 5", "ENG 6", "ENG 7",
		"ENG 8", "ENG 9", "STO 7", "RCL 7", "STO - 7", "STO + 7", "STO * 7", "STO : 7", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"->H.MS", "INT", "SQRT", "Y^X", "SIN", "COS", "TAN", "LN", "LOG", "->R",
		"", "", "", "", "", "", "->H", "FRAC", "x^2", "ABS",
		"SIN-1", "COS-1", "TAN-1", "e^x", "10^x", "->P", "", "", "", "",
		"", "", "0", "1", "2", "3", "4", "5", "6", "7",
		"8", "9", "", "", "", "", "", "", "x<y?", "x>=y?",
		"x!=y?", "x=y?", "LASTX", "PAUSE", "", "", "CL REG", "CL STK", "/x", "s",
		"E-", "", "", "", "x<0?", "x>=0?", "x!=0?", "x=0?", "PI", "NOP",
		"", "DEG", "RAD", "GRD", "%", "1/x", "gE+", "", "", "",
		"-", "+", "*", ":", ".", "R/S", "ENTER", "CHS", "EEX", "CLX",
		"x<>y", "ROLL", "E+", "", "", ""
	};

	private char[] digittab = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'r', 'F', 'o', 'P', 'E', ' '
	};

	private char[] HP3xKeyChartable = new char[35]
	{
		't', 'b', 'o', 'f', 'g', 'y', 'd', 's', 'r', '#',
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

	public ushort[] opcodeint = new ushort[4096]
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
		344, 508, 528, 939, 836, 388, 644, 520, 27, 186,
		392, 154, 26, 58, 150, 754, 18, 452, 660, 17,
		260, 274, 250, 788, 89, 924, 27, 154, 186, 1018,
		922, 116, 137, 250, 622, 874, 37, 622, 151, 494,
		954, 250, 264, 328, 378, 378, 378, 1018, 250, 874,
		62, 116, 429, 282, 250, 328, 378, 474, 796, 58,
		954, 1018, 250, 538, 287, 314, 250, 328, 250, 796,
		98, 878, 97, 474, 395, 668, 80, 652, 276, 78,
		260, 239, 268, 239, 644, 412, 74, 476, 87, 460,
		239, 452, 239, 244, 953, 155, 90, 538, 244, 909,
		415, 494, 874, 105, 538, 371, 314, 52, 753, 18,
		788, 118, 250, 328, 250, 52, 449, 264, 244, 953,
		52, 313, 622, 264, 328, 494, 619, 474, 627, 282,
		26, 418, 52, 805, 599, 508, 328, 660, 283, 860,
		128, 250, 456, 372, 317, 712, 456, 90, 52, 185,
		284, 149, 690, 436, 3, 400, 420, 131, 494, 607,
		282, 250, 116, 13, 250, 116, 389, 428, 158, 250,
		264, 954, 954, 282, 508, 88, 264, 700, 408, 747,
		934, 934, 594, 707, 18, 264, 154, 570, 314, 264,
		122, 178, 610, 715, 154, 470, 154, 758, 200, 626,
		18, 954, 747, 494, 626, 799, 274, 264, 154, 602,
		328, 660, 211, 654, 90, 730, 270, 52, 449, 412,
		128, 90, 154, 756, 981, 218, 104, 154, 52, 329,
		372, 197, 250, 90, 860, 258, 456, 154, 52, 425,
		154, 264, 398, 712, 328, 154, 372, 309, 520, 712,
		456, 146, 120, 154, 264, 52, 313, 52, 193, 0,
		337, 175, 328, 154, 558, 154, 120, 154, 52, 449,
		274, 337, 308, 515, 282, 404, 279, 634, 266, 708,
		274, 436, 3, 860, 125, 282, 167, 404, 289, 90,
		52, 565, 51, 860, 125, 250, 90, 456, 52, 185,
		317, 712, 456, 308, 587, 0, 0, 0, 0, 58,
		238, 508, 218, 366, 271, 218, 244, 933, 418, 251,
		464, 235, 52, 349, 58, 52, 855, 218, 622, 291,
		218, 235, 400, 748, 324, 218, 259, 52, 193, 476,
		338, 690, 52, 27, 250, 270, 624, 250, 528, 637,
		180, 955, 637, 26, 150, 250, 244, 953, 250, 180,
		919, 637, 250, 441, 87, 326, 998, 870, 361, 528,
		726, 377, 508, 206, 494, 494, 874, 412, 400, 748,
		379, 218, 528, 622, 475, 282, 214, 540, 398, 464,
		620, 390, 973, 551, 464, 973, 400, 400, 973, 186,
		218, 619, 26, 965, 464, 612, 404, 464, 965, 474,
		346, 250, 52, 349, 52, 819, 714, 377, 503, 52,
		363, 692, 819, 816, 282, 624, 204, 212, 432, 616,
		680, 744, 808, 872, 936, 1000, 568, 170, 820, 959,
		204, 220, 737, 58, 328, 186, 200, 136, 972, 980,
		444, 270, 380, 408, 622, 779, 200, 436, 107, 276,
		910, 412, 473, 476, 470, 916, 748, 130, 134, 454,
		998, 130, 500, 111, 532, 461, 851, 532, 480, 660,
		964, 476, 910, 823, 660, 726, 852, 746, 644, 500,
		907, 866, 492, 956, 738, 601, 444, 998, 998, 436,
		895, 998, 390, 166, 998, 358, 358, 550, 540, 361,
		26, 174, 410, 270, 528, 0, 75, 923, 200, 72,
		282, 572, 482, 624, 688, 610, 31, 828, 280, 174,
		968, 8, 392, 282, 52, 25, 264, 732, 537, 140,
		76, 72, 780, 233, 225, 148, 702, 76, 716, 204,
		220, 699, 564, 1001, 200, 136, 972, 980, 552, 76,
		204, 220, 563, 732, 565, 716, 107, 732, 698, 980,
		768, 175, 500, 43, 148, 574, 92, 584, 568, 636,
		776, 482, 287, 124, 482, 354, 295, 552, 528, 568,
		482, 636, 24, 287, 148, 601, 84, 538, 132, 780,
		969, 878, 628, 249, 339, 249, 140, 903, 969, 750,
		628, 776, 572, 614, 568, 636, 610, 419, 124, 610,
		552, 969, 467, 68, 969, 750, 626, 204, 212, 628,
		249, 969, 90, 174, 572, 474, 464, 172, 631, 444,
		272, 582, 878, 658, 250, 58, 200, 136, 204, 220,
		552, 972, 980, 647, 200, 276, 737, 154, 969, 90,
		609, 803, 609, 564, 129, 523, 628, 679, 601, 266,
		336, 336, 622, 619, 154, 528, 238, 249, 969, 750,
		699, 601, 90, 572, 474, 464, 620, 680, 266, 750,
		693, 622, 954, 954, 400, 400, 695, 66, 400, 66,
		250, 752, 225, 708, 969, 467, 969, 750, 716, 788,
		709, 980, 602, 90, 609, 136, 90, 225, 820, 195,
		568, 764, 866, 723, 956, 738, 602, 170, 820, 959,
		468, 795, 174, 124, 454, 454, 454, 568, 134, 138,
		552, 780, 115, 264, 580, 107, 828, 628, 781, 107,
		124, 927, 380, 927, 572, 386, 500, 111, 568, 124,
		998, 998, 998, 528, 884, 33, 552, 780, 339, 0,
		0, 0, 200, 568, 572, 852, 775, 41, 262, 272,
		844, 16, 268, 396, 460, 524, 652, 844, 908, 528,
		664, 728, 572, 284, 795, 482, 404, 795, 482, 41,
		204, 220, 672, 436, 807, 41, 151, 41, 388, 260,
		262, 836, 552, 436, 155, 41, 452, 516, 155, 0,
		135, 143, 339, 303, 284, 619, 412, 604, 984, 41,
		388, 24, 159, 344, 24, 111, 75, 723, 859, 661,
		494, 494, 494, 494, 364, 788, 907, 284, 849, 412,
		829, 920, 231, 41, 644, 155, 284, 811, 412, 858,
		856, 231, 984, 728, 111, 494, 900, 494, 494, 823,
		831, 661, 283, 856, 984, 83, 856, 999, 284, 878,
		404, 420, 856, 627, 407, 419, 427, 276, 894, 412,
		891, 468, 891, 532, 417, 920, 856, 111, 412, 436,
		41, 155, 276, 907, 412, 907, 476, 907, 600, 984,
		111, 856, 792, 83, 664, 83, 523, 867, 927, 971,
		284, 923, 404, 923, 984, 664, 111, 536, 856, 83,
		856, 615, 494, 375, 383, 661, 287, 276, 941, 412,
		941, 468, 941, 532, 944, 664, 728, 528, 41, 452,
		998, 528, 276, 961, 404, 961, 476, 961, 540, 961,
		660, 961, 41, 516, 155, 664, 664, 83, 468, 970,
		852, 746, 452, 907, 742, 746, 1007, 494, 836, 494,
		494, 494, 855, 661, 279, 494, 372, 799, 284, 989,
		792, 728, 83, 41, 452, 24, 664, 388, 572, 134,
		454, 134, 159, 284, 1005, 412, 926, 536, 551, 41,
		24, 728, 516, 903, 284, 1016, 404, 1016, 920, 615,
		536, 920, 83, 130, 344, 436, 955, 555, 83, 71,
		79, 39, 55, 892, 549, 617, 83, 892, 549, 849,
		83, 892, 549, 669, 83, 537, 83, 897, 636, 219,
		649, 216, 152, 223, 923, 387, 610, 610, 610, 223,
		150, 272, 444, 600, 266, 174, 572, 664, 664, 828,
		770, 63, 572, 770, 61, 344, 572, 770, 87, 188,
		849, 124, 329, 182, 282, 444, 408, 250, 528, 898,
		144, 572, 770, 106, 536, 572, 770, 246, 664, 892,
		861, 472, 88, 636, 333, 166, 942, 426, 764, 144,
		454, 454, 454, 134, 528, 898, 838, 93, 905, 669,
		223, 188, 669, 215, 397, 861, 223, 892, 262, 614,
		636, 130, 764, 528, 898, 216, 572, 770, 145, 834,
		128, 124, 472, 88, 472, 454, 170, 124, 144, 0,
		943, 955, 610, 610, 610, 223, 545, 572, 578, 834,
		248, 455, 764, 551, 188, 88, 280, 400, 528, 779,
		795, 545, 391, 472, 572, 322, 462, 905, 124, 144,
		545, 723, 88, 88, 559, 775, 791, 188, 873, 739,
		892, 262, 614, 124, 528, 88, 152, 559, 391, 935,
		735, 91, 719, 649, 216, 280, 223, 649, 216, 216,
		223, 649, 885, 223, 795, 927, 843, 967, 611, 545,
		707, 545, 152, 88, 223, 545, 152, 152, 223, 0,
		779, 835, 739, 95, 723, 707, 849, 223, 545, 739,
		88, 216, 559, 152, 216, 559, 152, 280, 559, 152,
		344, 559, 764, 911, 188, 88, 344, 559, 397, 873,
		223, 545, 927, 472, 216, 223, 472, 280, 223, 649,
		216, 88, 223, 816, 462, 479, 905, 455, 328, 250,
		788, 356, 776, 568, 12, 908, 746, 265, 618, 4,
		746, 265, 900, 218, 186, 20, 292, 874, 282, 572,
		88, 24, 782, 291, 14, 904, 302, 782, 288, 155,
		14, 904, 430, 334, 143, 155, 622, 174, 155, 4,
		14, 904, 218, 274, 508, 400, 590, 167, 20, 305,
		746, 305, 464, 26, 166, 350, 6, 158, 754, 322,
		28, 339, 494, 746, 321, 618, 746, 342, 490, 1022,
		190, 272, 582, 174, 776, 28, 346, 916, 397, 746,
		336, 776, 654, 618, 142, 969, 403, 494, 400, 263,
		218, 186, 18, 303, 18, 842, 390, 387, 434, 590,
		846, 350, 272, 590, 282, 146, 178, 272, 426, 527,
		586, 722, 367, 508, 280, 700, 400, 594, 451, 482,
		164, 384, 464, 164, 384, 464, 164, 384, 464, 482,
		482, 463, 178, 250, 528, 956, 408, 427, 766, 291,
		954, 494, 878, 392, 395, 238, 426, 590, 572, 270,
		216, 910, 591, 334, 1006, 910, 607, 334, 206, 508,
		635, 578, 590, 647, 303, 434, 776, 622, 400, 272,
		418, 631, 635, 174, 380, 536, 24, 472, 572, 272,
		490, 723, 6, 747, 776, 902, 703, 326, 838, 444,
		272, 618, 1006, 1006, 624, 56, 154, 776, 528, 282,
		624, 272, 634, 10, 740, 461, 426, 400, 803, 474,
		474, 124, 130, 316, 920, 664, 664, 792, 664, 154,
		58, 76, 140, 200, 136, 972, 980, 477, 204, 212,
		485, 988, 480, 980, 490, 204, 212, 480, 136, 528,
		636, 436, 927, 877, 436, 11, 956, 454, 454, 454,
		528, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		796, 519, 377, 276, 572, 150, 39, 337, 630, 462,
		462, 636, 454, 482, 91, 470, 464, 172, 525, 482,
		91, 135, 610, 492, 540, 452, 270, 139, 468, 543,
		434, 400, 950, 390, 182, 174, 400, 582, 758, 553,
		437, 264, 197, 284, 559, 628, 969, 436, 111, 568,
		316, 258, 476, 570, 482, 284, 570, 482, 552, 528,
		142, 956, 934, 934, 828, 130, 462, 124, 934, 142,
		174, 776, 746, 588, 266, 654, 437, 52, 25, 620,
		553, 140, 436, 3, 596, 600, 328, 712, 588, 772,
		282, 190, 272, 528, 328, 250, 568, 316, 738, 617,
		610, 452, 738, 617, 260, 218, 272, 270, 528, 776,
		122, 14, 336, 94, 150, 398, 508, 866, 637, 622,
		400, 276, 629, 470, 471, 150, 78, 528, 788, 651,
		337, 508, 88, 186, 582, 14, 260, 452, 167, 377,
		276, 668, 758, 677, 150, 182, 956, 262, 758, 668,
		700, 472, 786, 668, 218, 543, 637, 264, 191, 218,
		14, 276, 676, 590, 528, 353, 527, 788, 683, 452,
		31, 377, 452, 637, 197, 191, 796, 709, 377, 776,
		276, 701, 758, 553, 690, 272, 641, 206, 631, 956,
		934, 934, 934, 138, 682, 170, 279, 328, 758, 559,
		776, 690, 0, 331, 8, 506, 264, 282, 264, 392,
		392, 584, 584, 584, 584, 250, 90, 968, 968, 602,
		884, 703, 184, 844, 186, 756, 969, 52, 381, 756,
		997, 184, 52, 381, 882, 492, 52, 837, 852, 594,
		836, 52, 25, 989, 756, 945, 899, 186, 596, 764,
		328, 712, 154, 712, 528, 0, 184, 758, 492, 154,
		376, 154, 184, 109, 852, 594, 836, 117, 905, 248,
		23, 52, 595, 52, 607, 52, 611, 52, 179, 52,
		15, 52, 839, 52, 383, 52, 27, 52, 25, 186,
		981, 154, 752, 528, 836, 392, 248, 154, 456, 52,
		597, 93, 456, 186, 85, 312, 61, 93, 376, 648,
		61, 93, 648, 154, 186, 85, 440, 61, 93, 648,
		456, 85, 504, 61, 93, 913, 61, 93, 436, 911,
		260, 323, 452, 319, 260, 388, 913, 836, 61, 882,
		492, 758, 492, 312, 154, 184, 85, 125, 532, 882,
		997, 248, 186, 85, 969, 61, 117, 532, 892, 886,
		877, 284, 492, 404, 492, 104, 516, 997, 440, 355,
		660, 887, 997, 376, 387, 997, 248, 186, 376, 391,
		660, 899, 168, 644, 997, 504, 355, 232, 997, 913,
		61, 125, 276, 963, 412, 918, 468, 936, 248, 154,
		120, 109, 117, 905, 26, 687, 120, 186, 184, 758,
		492, 85, 117, 101, 117, 250, 328, 820, 849, 90,
		248, 154, 109, 767, 897, 328, 186, 997, 184, 85,
		248, 69, 933, 85, 125, 997, 376, 186, 945, 85,
		969, 77, 120, 109, 997, 184, 109, 997, 154, 436,
		3, 412, 734, 248, 762, 492, 885, 997, 184, 186,
		456, 85, 376, 69, 945, 85, 125, 997, 328, 186,
		248, 85, 969, 77, 328, 109, 755, 264, 392, 528,
		328, 891, 692, 991, 26, 508, 418, 184, 528, 981,
		248, 528, 981, 120, 528, 981, 184, 528, 981, 56,
		528, 270, 572, 88, 1003, 270, 624, 528, 0, 0,
		0, 181, 291, 315, 315, 0, 0, 828, 770, 273,
		18, 883, 331, 339, 347, 371, 379, 391, 415, 483,
		479, 471, 559, 455, 443, 543, 539, 547, 867, 787,
		715, 727, 679, 659, 403, 495, 503, 427, 571, 463,
		447, 519, 515, 523, 815, 435, 751, 755, 687, 667,
		266, 174, 272, 572, 664, 664, 572, 770, 70, 828,
		770, 277, 572, 344, 572, 770, 234, 838, 68, 140,
		436, 871, 898, 144, 828, 770, 78, 328, 692, 3,
		462, 776, 328, 144, 692, 671, 436, 315, 581, 690,
		52, 585, 436, 3, 581, 355, 52, 169, 363, 52,
		393, 363, 52, 373, 363, 52, 825, 363, 52, 119,
		52, 147, 516, 372, 407, 372, 359, 372, 371, 589,
		644, 388, 516, 180, 3, 116, 159, 516, 116, 335,
		644, 388, 836, 605, 244, 67, 644, 388, 605, 308,
		23, 605, 308, 3, 605, 180, 999, 52, 363, 520,
		712, 154, 528, 250, 568, 780, 754, 162, 626, 754,
		161, 900, 651, 772, 250, 528, 648, 570, 762, 176,
		695, 648, 570, 890, 176, 436, 249, 716, 436, 107,
		648, 836, 735, 648, 154, 690, 52, 609, 755, 690,
		886, 193, 852, 176, 695, 754, 176, 695, 780, 564,
		1001, 200, 136, 372, 767, 849, 244, 953, 378, 378,
		1018, 502, 270, 363, 596, 215, 712, 528, 849, 456,
		363, 434, 578, 879, 572, 322, 907, 426, 418, 903,
		568, 138, 552, 336, 968, 707, 898, 568, 170, 838,
		251, 444, 84, 247, 148, 248, 262, 436, 895, 140,
		764, 372, 927, 444, 84, 259, 148, 259, 262, 436,
		995, 436, 249, 33, 436, 899, 132, 150, 138, 462,
		444, 454, 454, 150, 528, 462, 328, 776, 144, 572,
		536, 572, 770, 273, 920, 572, 130, 1006, 624, 56,
		250, 71, 18, 163, 18, 167, 18, 434, 434, 568,
		146, 552, 695, 409, 14, 52, 753, 622, 150, 979,
		90, 52, 381, 507, 756, 299, 155, 551, 279, 263,
		271, 282, 291, 692, 707, 692, 515, 756, 159, 712,
		436, 911, 139, 559, 335, 287, 659, 8, 264, 328,
		979, 756, 155, 820, 589, 979, 584, 979, 147, 695,
		227, 395, 183, 1009, 274, 979, 409, 262, 142, 979,
		1009, 186, 508, 746, 365, 494, 528, 494, 750, 373,
		622, 470, 400, 854, 366, 528, 756, 315, 756, 307,
		690, 90, 52, 609, 52, 25, 724, 401, 752, 328,
		979, 90, 52, 177, 507, 756, 319, 756, 3, 343,
		355, 756, 323, 572, 436, 927, 816, 340, 486, 270,
		508, 482, 264, 328, 528, 475, 483, 376, 692, 989,
		248, 979, 282, 40, 104, 168, 232, 296, 360, 424,
		488, 436, 107, 858, 484, 282, 508, 482, 264, 328,
		828, 984, 985, 873, 605, 572, 88, 216, 985, 216,
		873, 605, 332, 52, 9, 597, 372, 653, 597, 564,
		981, 597, 593, 597, 282, 508, 152, 236, 464, 250,
		218, 374, 374, 154, 628, 959, 828, 186, 624, 56,
		922, 858, 486, 610, 879, 528, 282, 264, 892, 436,
		927, 0, 963, 523, 491, 495, 535, 211, 596, 499,
		712, 250, 436, 3, 828, 624, 752, 610, 991, 528,
		392, 456, 528, 43, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0
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
		Text = "HP-33E";
		labelHPType.Text = "HP-33E Emulator";
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
			if (!prgmmode)
			{
				act_s |= 8;
			}
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
			case 142:
			case 143:
			case 144:
			case 145:
			case 146:
			case 147:
			case 148:
			case 149:
				prgmmode = true;
				break;
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
			case 184:
			case 185:
			case 186:
			case 187:
			case 188:
			case 189:
			case 190:
			case 191:
			case 192:
			case 193:
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
