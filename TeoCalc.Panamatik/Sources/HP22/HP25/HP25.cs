using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Panamatik.Calc.HP22;

public class HP25 : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int EXPSIZE = 3;

	private const int BUTTONS = 30;

	private const int RAMSIZE = 448;

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

	private bool transparent;

	private bool mouseDown;

	private Point lastLocation;

	private Size OriginalSize;

	private int FirstCol = 19;

	private int FirstRow = 112;

	private int LastCol = 174;

	private int LastRow = 374;

	private int RowSize;

	private int ColSize;

	private int FirstCol2;

	private int ColSize2;

	private int SliderY;

	private int SliderLeft;

	private int SliderRight;

	private string[] HP25Mnemonics = new string[84]
	{
		"GTO", "FIX", "SCI", "ENG", "STO", "RCL", "->H.MS", "INT", "SQRT", "Y^X",
		"SIN", "COS", "TAN", "LN", "LOG", "->R", "->H", "FRAC", "x^2", "ABS",
		"SIN-1", "COS-1", "TAN-1", "e^x", "10^x", "->P", "0", "1", "2", "3",
		"4", "5", "6", "7", "8", "9", "x<y?", "x>=y?", "x!=y?", "x=y?",
		"LASTX", "PAUSE", "", "CL", "CLREG", "CLSTK", "xmean", "s", "E-", "",
		"", "", "x<0?", "x>=0?", "x!=0?", "x=0?", "PI", "NOP", "", "DEG",
		"RAD", "GRD", "%", "1/x", "gE+", "", "", "", "-", "+",
		"*", "/", ".", "R/S", "ENTER", "CHS", "EEX", "CLX", "x<>y", "ROLL",
		"E+", "", "", ""
	};

	private string[] HP29Mnemonics = new string[94]
	{
		"R/S", "ENTER", "CHS", "EEX", "CLX", "CLREG", "CLE", "GSB(i)", "GTO(i)", "RCL(i)",
		"STO(i)", "STO-(i)", "STO+(i)", "STO*(i)", "STO/(i)", "RCLE+", "0", "1", "2", "3",
		"4", "5", "6", "7", "8", "9", ".", "-", "+", "*",
		"/", "CL", "->H.MS", "INT", "SQRT", "Y^X", "SIN", "COS", "TAN", "LN",
		"LOG", "->R", "LastX", "x<=y?", "x>y?", "x!=y?", "x=y?", "", "->H", "FRAC",
		"x^2", "ABS", "SIN-1", "COS-1", "TAN-1", "e^x", "10^x", "->P", "PI", "x<0?",
		"x>0?", "x!=0?", "x=0?", "", "FIX", "SCI", "ENG", "X<>Y", "ROLL", "",
		"", "E+", "DEG", "xmean", "s", "PAUSE", "", "E-", "RAD", "%",
		"1/x", "DSZ", "ISZ", "RTN", "GRD", "GSB", "GTO", "RCL", "STO", "",
		"", "", "", "LBL"
	};

	private string[] HP2xMnemonicsAll = new string[256]
	{
		"GTO 00", "GTO 01", "GTO 02", "GTO 03", "GTO 04", "GTO 05", "GTO 06", "GTO 07", "GTO 08", "GTO 09",
		"STO 0", "RCL 0", "STO-0", "STO+0", "STO*0", "STO/0", "GTO 10", "GTO 11", "GTO 12", "GTO 13",
		"GTO 14", "GTO 15", "GTO 16", "GTO 17", "GTO 18", "GTO 19", "STO 1", "RCL 1", "STO-1", "STO+1",
		"STO*1", "STO/1", "GTO 20", "GTO 21", "GTO 22", "GTO 23", "GTO 24", "GTO 25", "GTO 26", "GTO 27",
		"GTO 28", "GTO 29", "STO 2", "RCL 2", "STO-2", "STO+2", "STO*2", "STO/2", "GTO 30", "GTO 31",
		"GTO 32", "GTO 33", "GTO 34", "GTO 35", "GTO 36", "GTO 37", "GTO 38", "GTO 39", "STO 3", "RCL 3",
		"STO-3", "STO+3", "STO*3", "STO/3", "GTO 40", "GTO 41", "GTO 42", "GTO 43", "GTO 44", "GTO 45",
		"GTO 46", "GTO 47", "GTO 48", "GTO 49", "STO 4", "RCL 4", "STO-4", "STO+4", "STO*4", "STO/4",
		"FIX 0", "FIX 1", "FIX 2", "FIX 3", "FIX 4", "FIX 5", "FIX 6", "FIX 7", "FIX 8", "FIX 9",
		"STO 5", "RCL 5", "STO-5", "STO+5", "STO*5", "STO/5", "SCI 0", "SCI 1", "SCI 2", "SCI 3",
		"SCI 4", "SCI 5", "SCI 6", "SCI 7", "SCI 8", "SCI 9", "STO 6", "RCL 6", "STO-6", "STO+6",
		"STO*6", "STO/6", "ENG 0", "ENG 1", "ENG 2", "ENG 3", "ENG 4", "ENG 5", "ENG 6", "ENG 7",
		"ENG 8", "ENG 9", "STO 7", "RCL 7", "STO-7", "STO+7", "STO*7", "STO/7", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"", "", "", "", "", "", "", "", "", "",
		"->H.MS", "INT", "SQRT", "Y^X", "SIN", "COS", "TAN", "LN", "LOG", "->R",
		"", "", "", "", "", "", "->H", "FRAC", "x^2", "ABS",
		"SIN-1", "COS-1", "TAN-1", "e^x", "10^x", "->P", "", "", "", "",
		"", "", "0", "1", "2", "3", "4", "5", "6", "7",
		"8", "9", "", "", "", "", "", "", "x<y?", "x>=y?",
		"x!=y?", "x=y?", "LASTX", "PAUSE", "", "", "CLREG", "CLSTK", "xmean", "s",
		"E-", "", "", "", "x<0?", "x>=0?", "x!=0?", "x=0?", "PI", "NOP",
		"", "DEG", "RAD", "GRD", "%", "1/x", "gE+", "", "", "",
		"-", "+", "*", "/", ".", "R/S", "ENTER", "CHS", "EEX", "CLX",
		"x<>y", "ROLL", "E+", "", "", ""
	};

	private char[] digittab = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'r', 'F', 'o', 'P', 'E', ' '
	};

	private char[] HP2xKeyChartable = new char[35]
	{
		'n', 'i', 't', 'p', 'v', 'y', 'd', 's', 'r', 'f',
		'\r', '\r', 'h', '%', '\b', '-', '7', '8', '9', '\0',
		'+', '4', '5', '6', '\0', '*', '1', '2', '3', '\0',
		'/', '0', '.', '#', '\0'
	};

	private byte[] HP2xKeytable = new byte[35]
	{
		179, 178, 177, 176, 180, 67, 66, 65, 64, 68,
		211, 211, 209, 208, 212, 99, 98, 97, 96, 0,
		163, 162, 161, 160, 0, 115, 114, 113, 112, 0,
		147, 146, 145, 144, 0
	};

	public ushort[] opcodeint = new ushort[2048]
	{
		442, 442, 968, 264, 282, 624, 688, 392, 282, 140,
		72, 776, 200, 153, 886, 17, 282, 58, 433, 904,
		92, 124, 241, 586, 842, 28, 186, 147, 361, 78,
		142, 746, 35, 654, 618, 142, 461, 551, 746, 45,
		622, 490, 618, 187, 494, 528, 508, 362, 207, 282,
		528, 262, 614, 266, 528, 712, 39, 0, 0, 0,
		508, 430, 315, 26, 166, 342, 295, 434, 958, 430,
		84, 73, 400, 110, 528, 228, 63, 400, 590, 303,
		255, 977, 76, 236, 88, 68, 828, 578, 968, 43,
		6, 272, 582, 776, 528, 284, 242, 500, 883, 156,
		102, 712, 132, 282, 508, 361, 18, 122, 26, 434,
		954, 434, 434, 90, 528, 124, 454, 454, 38, 66,
		418, 418, 66, 528, 334, 241, 842, 182, 361, 78,
		572, 834, 194, 590, 803, 14, 78, 332, 348, 174,
		977, 397, 1017, 272, 572, 418, 875, 454, 464, 620,
		145, 400, 400, 154, 186, 614, 142, 776, 854, 202,
		30, 158, 973, 228, 162, 404, 143, 886, 143, 834,
		172, 651, 413, 571, 508, 226, 674, 226, 400, 36,
		140, 703, 950, 430, 731, 14, 904, 508, 400, 590,
		755, 6, 854, 197, 124, 253, 95, 361, 78, 543,
		982, 535, 700, 400, 494, 706, 203, 508, 622, 866,
		161, 400, 470, 835, 404, 151, 982, 607, 578, 492,
		214, 14, 404, 152, 776, 572, 418, 90, 433, 142,
		174, 461, 973, 404, 232, 494, 153, 172, 230, 43,
		132, 43, 690, 268, 178, 136, 972, 188, 400, 748,
		248, 980, 246, 1008, 988, 254, 200, 828, 2, 16,
		508, 58, 426, 426, 490, 490, 782, 269, 154, 150,
		758, 273, 154, 246, 782, 281, 986, 430, 730, 281,
		75, 618, 618, 14, 914, 850, 291, 314, 850, 461,
		528, 822, 295, 690, 90, 538, 58, 299, 388, 380,
		528, 52, 975, 398, 652, 677, 636, 357, 508, 58,
		110, 294, 243, 934, 494, 418, 150, 186, 58, 528,
		571, 751, 875, 727, 72, 260, 52, 327, 850, 457,
		508, 838, 411, 270, 528, 508, 758, 476, 558, 677,
		230, 150, 913, 211, 314, 610, 355, 164, 328, 464,
		954, 359, 418, 418, 639, 276, 746, 505, 690, 433,
		479, 418, 528, 690, 17, 209, 528, 418, 418, 418,
		423, 276, 509, 505, 189, 860, 495, 52, 153, 752,
		328, 959, 860, 392, 181, 264, 154, 624, 56, 154,
		328, 528, 520, 528, 52, 397, 56, 959, 436, 871,
		480, 171, 528, 276, 748, 505, 516, 317, 479, 434,
		622, 834, 334, 454, 615, 418, 418, 418, 447, 276,
		935, 505, 415, 500, 795, 58, 86, 562, 699, 658,
		26, 528, 288, 288, 288, 288, 288, 276, 999, 520,
		712, 154, 959, 436, 847, 572, 6, 181, 228, 141,
		154, 624, 154, 20, 394, 752, 959, 540, 461, 660,
		463, 494, 954, 528, 671, 52, 383, 859, 244, 939,
		276, 939, 52, 223, 500, 867, 532, 602, 262, 614,
		266, 186, 528, 482, 542, 911, 318, 474, 400, 620,
		484, 186, 18, 238, 299, 32, 52, 413, 154, 920,
		664, 664, 792, 664, 154, 54, 72, 52, 549, 264,
		520, 154, 21, 52, 963, 652, 31, 644, 396, 524,
		588, 508, 758, 597, 882, 608, 750, 638, 494, 26,
		918, 750, 658, 934, 114, 700, 400, 594, 99, 82,
		114, 286, 135, 954, 482, 82, 122, 272, 338, 392,
		146, 474, 954, 626, 163, 776, 392, 314, 474, 594,
		127, 178, 594, 338, 607, 90, 474, 235, 626, 464,
		250, 857, 954, 250, 259, 314, 610, 255, 882, 568,
		164, 674, 282, 464, 622, 172, 583, 116, 297, 18,
		604, 593, 626, 660, 922, 116, 211, 668, 602, 264,
		886, 604, 436, 915, 882, 602, 282, 528, 274, 668,
		602, 264, 154, 122, 842, 602, 430, 590, 478, 854,
		635, 846, 631, 146, 178, 370, 370, 402, 754, 632,
		388, 274, 250, 264, 59, 846, 617, 363, 186, 578,
		854, 645, 282, 26, 327, 116, 297, 58, 246, 282,
		146, 116, 913, 272, 338, 594, 282, 87, 580, 116,
		297, 270, 87, 356, 565, 498, 90, 114, 474, 178,
		272, 306, 400, 135, 750, 587, 634, 250, 54, 789,
		154, 570, 714, 685, 570, 154, 250, 746, 690, 698,
		134, 400, 474, 364, 691, 508, 834, 706, 470, 154,
		146, 116, 357, 18, 278, 327, 430, 400, 751, 282,
		508, 152, 216, 24, 152, 344, 536, 344, 24, 600,
		152, 600, 600, 280, 508, 528, 282, 164, 754, 634,
		280, 506, 274, 1018, 464, 804, 768, 868, 781, 292,
		792, 100, 801, 932, 808, 528, 13, 7, 154, 500,
		467, 0, 0, 0, 316, 408, 600, 216, 88, 280,
		472, 88, 536, 24, 344, 408, 508, 528, 892, 216,
		88, 24, 88, 472, 600, 536, 24, 280, 216, 316,
		528, 188, 216, 216, 24, 536, 344, 216, 88, 472,
		444, 528, 956, 216, 216, 216, 24, 536, 280, 892,
		528, 636, 216, 216, 216, 216, 252, 528, 572, 216,
		216, 188, 528, 26, 150, 250, 180, 789, 250, 219,
		494, 954, 746, 855, 850, 820, 654, 746, 844, 278,
		26, 366, 315, 262, 882, 840, 634, 266, 404, 841,
		274, 186, 528, 954, 622, 303, 270, 399, 154, 462,
		154, 474, 874, 833, 379, 154, 454, 454, 454, 154,
		379, 494, 538, 375, 314, 630, 323, 754, 872, 90,
		538, 686, 286, 250, 508, 180, 857, 250, 451, 498,
		538, 447, 314, 474, 1022, 250, 400, 684, 875, 700,
		472, 146, 250, 519, 122, 626, 583, 934, 154, 478,
		154, 594, 507, 90, 418, 412, 908, 626, 116, 297,
		116, 211, 934, 594, 579, 18, 314, 418, 507, 934,
		418, 519, 90, 154, 264, 398, 116, 685, 636, 516,
		116, 357, 524, 278, 191, 181, 52, 963, 0, 264,
		989, 328, 52, 43, 644, 524, 396, 588, 508, 136,
		746, 988, 186, 58, 118, 18, 430, 803, 882, 996,
		508, 986, 90, 418, 90, 146, 262, 835, 986, 434,
		755, 26, 150, 180, 303, 482, 538, 831, 314, 474,
		400, 748, 976, 26, 154, 508, 180, 87, 26, 418,
		116, 17, 116, 209, 180, 39, 508, 180, 587, 900,
		500, 507, 284, 8, 282, 712, 712, 712, 989, 40,
		104, 168, 232, 296, 699, 282, 392, 282, 360, 424,
		488, 552, 616, 528, 154, 282, 508, 482, 482, 1018,
		482, 494, 528, 52, 155, 52, 399, 0, 0, 0,
		0, 0, 0, 0, 0, 28, 27, 45, 696, 52,
		963, 392, 508, 738, 41, 392, 284, 38, 1, 116,
		189, 37, 680, 52, 39, 913, 874, 48, 482, 508,
		482, 127, 969, 892, 1005, 746, 336, 252, 1005, 746,
		287, 372, 51, 28, 64, 45, 760, 103, 392, 316,
		738, 77, 392, 284, 75, 1, 116, 317, 37, 744,
		159, 913, 874, 84, 482, 316, 482, 275, 985, 892,
		1005, 746, 393, 252, 1005, 746, 367, 372, 367, 28,
		100, 45, 824, 103, 392, 444, 738, 107, 392, 808,
		159, 913, 874, 114, 482, 444, 482, 419, 969, 985,
		892, 1005, 746, 603, 436, 423, 28, 127, 45, 888,
		103, 392, 772, 892, 738, 135, 392, 872, 159, 913,
		874, 142, 482, 892, 482, 531, 969, 985, 252, 1005,
		746, 664, 436, 507, 28, 155, 45, 952, 103, 392,
		252, 738, 162, 392, 936, 159, 913, 874, 169, 482,
		252, 482, 639, 969, 985, 892, 1005, 746, 695, 436,
		583, 743, 731, 239, 87, 755, 276, 191, 383, 276,
		218, 491, 276, 192, 603, 708, 456, 969, 400, 738,
		249, 400, 738, 249, 508, 772, 372, 139, 680, 724,
		209, 500, 331, 568, 154, 282, 482, 116, 429, 488,
		500, 187, 456, 985, 969, 892, 738, 249, 508, 500,
		55, 0, 266, 636, 738, 241, 610, 738, 240, 610,
		738, 239, 490, 482, 482, 528, 316, 738, 248, 528,
		508, 975, 392, 436, 915, 266, 738, 255, 490, 528,
		116, 435, 116, 431, 116, 191, 116, 319, 180, 15,
		180, 23, 1005, 760, 742, 249, 973, 1, 33, 1000,
		952, 154, 888, 742, 249, 25, 33, 742, 330, 1016,
		275, 1005, 220, 292, 824, 163, 973, 1, 824, 17,
		1000, 973, 1, 33, 916, 338, 758, 332, 690, 264,
		888, 154, 760, 17, 1016, 742, 249, 494, 494, 25,
		26, 434, 954, 916, 343, 9, 33, 328, 25, 788,
		204, 52, 153, 680, 52, 963, 888, 154, 824, 275,
		900, 127, 758, 345, 264, 952, 207, 1, 267, 952,
		311, 1005, 696, 742, 249, 26, 418, 25, 264, 952,
		154, 888, 742, 249, 25, 41, 282, 482, 9, 436,
		343, 1001, 696, 220, 374, 616, 888, 511, 154, 282,
		482, 9, 616, 888, 154, 824, 9, 1000, 961, 154,
		824, 961, 25, 1000, 632, 961, 699, 1001, 696, 220,
		400, 616, 952, 615, 154, 282, 482, 1, 616, 952,
		154, 824, 1, 1000, 632, 154, 824, 17, 1016, 9,
		762, 330, 1016, 961, 154, 824, 961, 25, 690, 1000,
		632, 961, 690, 616, 12, 508, 26, 434, 418, 418,
		954, 1016, 17, 632, 154, 25, 746, 464, 1, 746,
		469, 26, 418, 632, 25, 690, 264, 1016, 274, 41,
		282, 482, 9, 744, 28, 526, 436, 327, 26, 418,
		1016, 25, 815, 632, 154, 1016, 9, 1, 744, 632,
		25, 494, 494, 494, 746, 483, 4, 26, 418, 632,
		1, 632, 17, 760, 154, 25, 815, 0, 0, 0,
		742, 249, 528, 760, 622, 622, 26, 434, 954, 528,
		452, 392, 136, 508, 204, 528, 116, 435, 116, 431,
		116, 191, 116, 319, 244, 707, 372, 975, 52, 155,
		632, 690, 264, 760, 762, 593, 33, 552, 26, 418,
		760, 1, 264, 632, 154, 568, 17, 760, 17, 328,
		25, 264, 26, 418, 568, 9, 552, 328, 9, 264,
		1016, 154, 760, 17, 568, 9, 328, 25, 552, 26,
		418, 9, 760, 17, 744, 568, 17, 26, 269, 568,
		26, 277, 59, 430, 430, 430, 430, 430, 430, 430,
		758, 593, 746, 592, 334, 327, 528, 282, 616, 552,
		760, 494, 494, 49, 744, 52, 963, 1005, 760, 762,
		631, 220, 611, 952, 419, 41, 1, 952, 154, 25,
		783, 1005, 760, 762, 633, 220, 625, 888, 475, 41,
		1, 888, 154, 25, 659, 952, 491, 888, 154, 696,
		25, 539, 1009, 696, 690, 264, 41, 33, 952, 17,
		49, 916, 655, 788, 653, 936, 359, 872, 359, 808,
		359, 1009, 696, 264, 41, 33, 888, 535, 1009, 760,
		758, 719, 220, 672, 824, 659, 41, 1, 824, 17,
		1000, 696, 690, 264, 41, 33, 26, 418, 9, 264,
		41, 154, 328, 916, 692, 154, 25, 1016, 535, 1009,
		760, 758, 719, 220, 703, 824, 783, 41, 1, 824,
		17, 1000, 696, 264, 41, 33, 282, 482, 9, 264,
		41, 154, 707, 696, 154, 824, 535, 276, 734, 836,
		12, 116, 759, 276, 865, 4, 844, 863, 516, 632,
		154, 504, 25, 359, 282, 200, 468, 748, 732, 753,
		488, 959, 552, 616, 744, 116, 963, 796, 751, 540,
		751, 392, 959, 0, 0, 0, 0, 900, 392, 508,
		204, 528, 116, 435, 154, 116, 431, 116, 191, 116,
		319, 372, 975, 244, 707, 696, 154, 282, 216, 408,
		494, 494, 508, 29, 888, 21, 760, 622, 622, 21,
		1000, 282, 26, 216, 408, 494, 494, 186, 344, 508,
		29, 1016, 21, 712, 888, 712, 1016, 827, 504, 154,
		632, 13, 264, 37, 45, 26, 418, 13, 1000, 632,
		154, 696, 13, 264, 37, 45, 430, 430, 760, 29,
		1016, 21, 1000, 632, 154, 504, 13, 1016, 13, 282,
		488, 824, 21, 827, 632, 154, 696, 13, 264, 37,
		45, 26, 418, 13, 430, 430, 760, 29, 319, 516,
		632, 186, 21, 504, 29, 568, 9, 264, 788, 897,
		26, 418, 504, 9, 328, 154, 29, 690, 282, 508,
		344, 622, 508, 264, 154, 180, 21, 827, 392, 772,
		391, 632, 154, 440, 21, 504, 29, 376, 9, 328,
		29, 916, 993, 895, 0, 0, 0, 186, 264, 632,
		284, 920, 9, 615, 1, 1017, 616, 328, 186, 21,
		568, 284, 931, 9, 659, 1, 1017, 552, 648, 328,
		21, 376, 284, 942, 9, 703, 1, 1017, 360, 648,
		440, 284, 951, 9, 739, 1, 1017, 424, 26, 418,
		504, 284, 961, 9, 779, 1, 1017, 488, 52, 39,
		520, 154, 712, 276, 976, 21, 622, 622, 52, 963,
		516, 13, 584, 712, 622, 622, 29, 827, 276, 894,
		584, 827, 186, 632, 851, 456, 21, 392, 632, 154,
		376, 21, 504, 29, 1000, 440, 154, 568, 21, 504,
		29, 1016, 13, 328, 29, 456, 916, 1019, 1, 282,
		392, 154, 827, 1017, 712, 991, 52, 155
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

	public void ACThp25()
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
	}

	public HP25()
	{
		InitializeComponent();
		CalculateKeyboard();
		Text = "HP-22";
		labelHPType.Text = "HP-22 Emulator";
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
		OriginalSize = pictureBox1.Size;
		Size size;
		try
		{
			ReadKeyboardFile("hp22.kml");
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
		ACThp25();
	}

	private void CalculateKeyboard()
	{
		RowSize = (LastRow - FirstRow) / 6;
		ColSize = (LastCol - FirstCol) / 4;
		ColSize2 = (LastCol - FirstCol) / 3;
		FirstCol2 = FirstCol;
		SliderY = FirstRow - RowSize;
		SliderLeft = FirstCol + ColSize * 2 / 3;
		SliderRight = LastCol - ColSize * 2 / 3;
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
		act_clear_memory();
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
			int num = 12;
			for (int i = 0; i < num; i++)
			{
				byte b = act_a[13 - i];
				byte b2 = act_b[13 - i];
				char c = digittab[b];
				if ((b2 & 2) != 0)
				{
					c = ((b != 9) ? ' ' : '-');
				}
				text += c;
				if ((b2 & 1) != 0)
				{
					c = '.';
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
			act_s |= 32;
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
			act_s |= 32;
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
		int num = e.X;
		int num2 = e.Y;
		if (num2 >= SliderY - RowSize / 2 && num2 < SliderY + RowSize / 2)
		{
			if (num < (SliderLeft + SliderRight) / 2)
			{
				if (num < SliderLeft)
				{
					if (transparent)
					{
						Application.Exit();
						return;
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
			else if (num < SliderRight)
			{
				prgmmode = true;
			}
			else
			{
				prgmmode = false;
			}
		}
		else if (num2 >= FirstRow - RowSize / 2 && num2 < FirstRow + 6 * RowSize + RowSize / 2 && num >= FirstCol - ColSize / 2 && num < FirstCol + 4 * ColSize + ColSize / 2)
		{
			int num3 = (num2 - FirstRow + RowSize / 2) / RowSize;
			int num4 = ((num3 > 2) ? ((num - FirstCol2 + ColSize2 / 2) / ColSize2) : ((num - FirstCol + ColSize / 2) / ColSize));
			byte code = HP2xKeytable[num3 * 5 + num4];
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

	private void HP25_KeyPress(object sender, KeyPressEventArgs e)
	{
		char c = char.ToLower(e.KeyChar);
		for (int i = 0; i < 35; i++)
		{
			if (c == HP2xKeyChartable[i])
			{
				press_key(HP2xKeytable[i]);
				break;
			}
		}
		switch (c)
		{
		case 'm':
			prgmmode = !prgmmode;
			break;
		case ',':
			press_key(HP2xKeytable[32]);
			break;
		}
		e.Handled = true;
	}

	private void textBoxDisplay_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		ShowDisplay();
	}

	private int GetOpcode25(string[] s)
	{
		int num = -1;
		for (int i = 0; i < HP2xMnemonicsAll.Length; i++)
		{
			if (string.Equals(s[0], HP2xMnemonicsAll[i], StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		for (int j = 0; j < HP25Mnemonics.Length; j++)
		{
			if (!string.Equals(s[0], HP25Mnemonics[j], StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (j < 6)
			{
				int num2;
				if (j == 0)
				{
					num2 = Convert.ToInt32(s[1]);
					if (num2 <= 49)
					{
						num = num2 / 10 * 16 + num2 % 10;
						break;
					}
					throw new Exception();
				}
				if (j < 4)
				{
					num2 = Convert.ToInt32(s[1]);
					if (num2 <= 9)
					{
						num = (j + 4) * 16 + num2;
						break;
					}
					throw new Exception();
				}
				if (j == 4)
				{
					for (int k = 0; k < 4; k++)
					{
						if (s[1] == HP25Mnemonics[k + 36 + 32])
						{
							num2 = Convert.ToInt32(s[2]);
							if (num2 <= 7)
							{
								return num2 * 16 + 12 + k;
							}
							throw new Exception();
						}
					}
				}
				num2 = Convert.ToInt32(s[1]);
				if (num2 <= 7)
				{
					num = num2 * 16 + 10;
					if (j == 5)
					{
						num++;
					}
					break;
				}
				throw new Exception();
			}
			if (j < 36)
			{
				j -= 6;
				num = j / 10 * 16 + 160 + j % 10;
				break;
			}
			if (j == 43)
			{
				if (s[1] == "REG")
				{
					j = 44;
				}
				if (s[1] == "STK")
				{
					j = 45;
				}
			}
			j -= 36;
			num = j + 208;
			break;
		}
		return num;
	}

	private int GetOpcode29(string[] s)
	{
		int result = -1;
		int num = 0;
		for (int i = 0; i < HP2xMnemonicsAll.Length; i++)
		{
			if (string.Equals(s[0], HP2xMnemonicsAll[i], StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		for (int j = 0; j < HP29Mnemonics.Length; j++)
		{
			if (!string.Equals(s[0], HP29Mnemonics[j], StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (j < 64)
			{
				if (j == 31)
				{
					if (s[1] == "X")
					{
						j = 4;
					}
					if (s[1] == "REG")
					{
						j = 5;
					}
					if (s[1] == "E")
					{
						j = 6;
					}
				}
				return j;
			}
			if (j < 67)
			{
				num = Convert.ToInt32(s[1]);
				if (num <= 9)
				{
					result = (j - 64) * 16 + 64 + num;
					break;
				}
				throw new Exception();
			}
			if (j < 73)
			{
				result = 74 + (j - 67);
			}
			else if (j < 79)
			{
				result = 90 + (j - 67 - 6);
			}
			else if (j < 85)
			{
				result = 106 + (j - 67 - 12);
			}
			else
			{
				if (j < 85)
				{
					break;
				}
				if (j == 87 && s[1] == "E+")
				{
					result = 15;
					break;
				}
				try
				{
					num = ((s[1][0] == '.') ? (Convert.ToInt32(s[1].Substring(1)) + 10) : ((s[1][0] < 'A' || s[1][0] > 'F') ? Convert.ToInt32(s[1]) : (s[1][0] - 65 + 10)));
					if (num >= 0 && num <= 15)
					{
						result = (j - 85) * 16 + 112 + num;
						break;
					}
					throw new Exception();
				}
				catch
				{
					if (s[1] == "(i)")
					{
						if (j < 89)
						{
							result = j - 85 + 7;
						}
					}
					else
					{
						if (j != 88)
						{
							break;
						}
						int num2 = 0;
						if (s[1] == "+")
						{
							num2 = 1;
						}
						if (s[1] == "*")
						{
							num2 = 2;
						}
						if (s[1] == "/")
						{
							num2 = 3;
						}
						try
						{
							num = Convert.ToInt32(s[2]);
							result = num2 * 16 + 176 + num;
						}
						catch
						{
							if (s[2] == "(i)")
							{
								result = 11 + num2;
								break;
							}
							num = ((s[2][0] != '.') ? Convert.ToInt32(s[2]) : (Convert.ToInt32(s[2].Substring(1)) + 10));
							if (num >= 0 && num <= 15)
							{
								result = num2 * 16 + 176 + num;
								break;
							}
							throw new Exception();
						}
						break;
					}
				}
			}
			break;
		}
		return result;
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
		bool flag2 = false;
		for (int i = 0; i < 14; i++)
		{
			buf[i] = 0;
		}
		int num = 0;
		int num2 = 0;
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
				flag2 = true;
				num2 = num - 1;
				continue;
			}
			if (s[i] == 'E' || s[i] == 'e')
			{
				num3 = Convert.ToInt32(s.Substring(i + 1));
				break;
			}
			if (s[i] < '0' || s[i] > '9')
			{
				continue;
			}
			if (s[i] != '0')
			{
				flag = false;
			}
			if (flag)
			{
				if (flag2)
				{
					num2--;
				}
			}
			else if (num < 10)
			{
				buf[12 - num] = (byte)(s[i] - 48);
				num++;
			}
		}
		if (!flag)
		{
			if (!flag2)
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
		else
		{
			buf[13] = 0;
		}
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
				act_s |= 512;
				press_key(HP2xKeytable[10]);
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HP25));
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
		this.textBoxDisplay.Font = new System.Drawing.Font("Microsoft Sans Serif", 13f, System.Drawing.FontStyle.Bold);
		this.textBoxDisplay.ForeColor = System.Drawing.Color.Red;
		this.textBoxDisplay.Location = new System.Drawing.Point(10, 18);
		this.textBoxDisplay.Name = "textBoxDisplay";
		this.textBoxDisplay.ReadOnly = true;
		this.textBoxDisplay.Size = new System.Drawing.Size(180, 20);
		this.textBoxDisplay.TabIndex = 25;
		this.textBoxDisplay.TabStop = false;
		this.textBoxDisplay.Click += new System.EventHandler(textBoxDisplay_Click);
		this.textBoxDisplay.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
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
		this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
		this.textBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(textBox1_KeyUp);
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(0, 0);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(200, 396);
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseDown);
		this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseMove);
		this.pictureBox1.MouseUp += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseUp);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(200, 397);
		base.Controls.Add(this.buttonSave);
		base.Controls.Add(this.buttonLoad);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.labelHPType);
		base.Controls.Add(this.textBoxDisplay);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.textBox1);
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		this.MaximumSize = new System.Drawing.Size(400, 435);
		this.MinimumSize = new System.Drawing.Size(216, 435);
		base.Name = "HP25";
		this.Text = "HP-2x";
		base.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP25_KeyPress);
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
