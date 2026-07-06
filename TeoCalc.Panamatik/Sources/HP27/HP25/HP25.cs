using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Panamatik.Calc.HP27;

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
		'y', 'x', '%', 'f', 'g', 'c', 'd', 's', 'r', 'p',
		'\r', '\r', 'n', 'e', '\b', '-', '7', '8', '9', '\0',
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

	public ushort[] opcodeint = new ushort[3072]
	{
		200, 132, 442, 442, 968, 624, 688, 392, 282, 679,
		485, 4, 468, 129, 596, 129, 716, 780, 1023, 660,
		87, 72, 836, 727, 724, 170, 520, 72, 776, 1000,
		528, 485, 72, 644, 708, 519, 485, 520, 712, 154,
		611, 68, 72, 836, 1019, 690, 178, 604, 429, 519,
		485, 105, 544, 485, 109, 308, 591, 142, 572, 746,
		62, 646, 142, 528, 127, 751, 603, 147, 485, 416,
		485, 468, 129, 604, 42, 758, 41, 508, 706, 107,
		72, 613, 6, 264, 328, 452, 519, 72, 4, 91,
		288, 250, 824, 637, 956, 763, 430, 430, 639, 97,
		372, 253, 671, 485, 712, 68, 675, 676, 85, 400,
		315, 430, 430, 430, 1019, 516, 97, 160, 485, 109,
		544, 852, 978, 916, 1021, 528, 332, 348, 382, 136,
		972, 252, 400, 364, 132, 980, 130, 1008, 988, 138,
		200, 272, 6, 16, 475, 43, 1023, 388, 97, 288,
		485, 584, 671, 124, 226, 152, 124, 226, 528, 430,
		430, 430, 447, 260, 97, 372, 257, 76, 72, 96,
		668, 174, 652, 196, 772, 519, 79, 215, 203, 739,
		72, 900, 517, 160, 485, 109, 544, 485, 72, 139,
		20, 870, 456, 866, 205, 266, 622, 622, 622, 746,
		215, 392, 494, 88, 392, 218, 752, 671, 283, 963,
		528, 415, 485, 282, 423, 188, 738, 220, 389, 389,
		764, 738, 224, 389, 956, 738, 228, 445, 124, 738,
		232, 453, 636, 738, 236, 430, 462, 250, 109, 352,
		776, 485, 758, 129, 476, 45, 596, 45, 124, 130,
		674, 130, 244, 763, 430, 916, 210, 468, 291, 724,
		844, 788, 210, 84, 268, 596, 275, 712, 76, 580,
		941, 30, 598, 10, 586, 572, 454, 464, 612, 316,
		418, 239, 83, 196, 362, 411, 282, 528, 2, 582,
		32, 596, 290, 572, 454, 636, 454, 124, 934, 934,
		776, 225, 572, 838, 421, 225, 1009, 139, 508, 238,
		573, 499, 32, 573, 475, 578, 400, 228, 288, 20,
		324, 164, 324, 982, 154, 186, 400, 614, 854, 463,
		154, 852, 80, 139, 850, 456, 643, 590, 590, 590,
		842, 505, 467, 706, 346, 299, 430, 400, 351, 776,
		746, 357, 622, 490, 618, 111, 494, 528, 508, 262,
		614, 266, 528, 456, 142, 842, 313, 444, 573, 174,
		225, 10, 846, 337, 174, 225, 52, 613, 1009, 328,
		72, 32, 508, 776, 250, 674, 400, 44, 385, 250,
		139, 750, 309, 484, 451, 982, 622, 400, 543, 400,
		228, 406, 750, 406, 622, 575, 282, 134, 166, 346,
		6, 328, 858, 334, 852, 451, 272, 582, 776, 178,
		528, 328, 398, 373, 220, 305, 886, 429, 282, 373,
		264, 328, 941, 182, 14, 904, 20, 453, 156, 363,
		78, 746, 391, 836, 934, 400, 494, 750, 308, 236,
		444, 4, 703, 124, 601, 475, 156, 495, 20, 495,
		400, 982, 979, 776, 508, 834, 343, 590, 470, 839,
		58, 26, 76, 700, 984, 920, 664, 664, 792, 664,
		200, 984, 748, 481, 154, 604, 380, 744, 808, 936,
		499, 30, 58, 434, 224, 494, 618, 746, 502, 490,
		954, 643, 328, 186, 475, 622, 982, 455, 124, 454,
		454, 528, 147, 171, 471, 115, 775, 775, 775, 480,
		480, 648, 765, 758, 676, 754, 711, 787, 143, 175,
		239, 255, 827, 823, 831, 591, 587, 765, 756, 143,
		817, 186, 1016, 308, 603, 756, 751, 260, 250, 461,
		599, 672, 372, 183, 690, 250, 461, 520, 712, 90,
		461, 520, 249, 260, 147, 713, 26, 150, 485, 877,
		163, 213, 599, 0, 288, 508, 713, 874, 470, 186,
		122, 478, 838, 589, 430, 782, 594, 490, 599, 590,
		275, 116, 859, 0, 282, 482, 1018, 498, 250, 706,
		603, 966, 494, 26, 898, 391, 474, 314, 383, 914,
		419, 934, 442, 494, 314, 411, 70, 610, 351, 626,
		351, 474, 110, 286, 346, 158, 599, 244, 503, 186,
		372, 53, 599, 122, 250, 378, 378, 410, 250, 286,
		186, 366, 531, 630, 366, 398, 828, 866, 650, 986,
		1018, 142, 282, 90, 700, 344, 1018, 627, 388, 436,
		185, 224, 482, 922, 603, 346, 474, 400, 998, 748,
		663, 258, 154, 250, 528, 0, 490, 146, 858, 714,
		90, 520, 250, 712, 250, 372, 689, 692, 11, 0,
		886, 693, 282, 882, 470, 528, 462, 860, 700, 426,
		52, 109, 144, 500, 935, 836, 426, 144, 452, 644,
		274, 372, 389, 18, 692, 123, 288, 644, 388, 765,
		852, 551, 756, 163, 282, 404, 90, 634, 266, 274,
		599, 372, 919, 494, 626, 887, 274, 264, 154, 602,
		328, 660, 745, 654, 90, 730, 724, 372, 281, 18,
		412, 545, 122, 572, 290, 1007, 474, 446, 1011, 434,
		954, 122, 494, 474, 158, 372, 689, 500, 797, 860,
		800, 250, 90, 1016, 154, 372, 397, 154, 264, 398,
		712, 328, 154, 500, 9, 520, 712, 1016, 142, 14,
		954, 264, 372, 89, 372, 181, 756, 823, 0, 607,
		250, 90, 328, 154, 558, 14, 954, 372, 281, 274,
		756, 751, 622, 535, 282, 214, 284, 982, 464, 620,
		823, 425, 235, 464, 425, 400, 400, 425, 186, 218,
		907, 0, 479, 363, 747, 731, 250, 952, 430, 636,
		759, 981, 490, 931, 142, 624, 142, 788, 997, 668,
		206, 941, 56, 407, 0, 0, 0, 0, 32, 756,
		73, 378, 378, 1018, 502, 270, 528, 494, 494, 32,
		218, 335, 998, 390, 166, 998, 358, 358, 550, 284,
		937, 26, 174, 410, 270, 528, 544, 250, 888, 52,
		449, 124, 759, 726, 904, 508, 206, 494, 494, 874,
		942, 400, 748, 812, 218, 528, 186, 282, 296, 360,
		424, 659, 475, 823, 941, 365, 407, 688, 407, 874,
		928, 20, 930, 282, 634, 892, 486, 622, 628, 355,
		628, 191, 488, 552, 616, 154, 407, 326, 998, 870,
		937, 528, 714, 904, 187, 927, 295, 981, 132, 931,
		250, 696, 188, 759, 250, 760, 764, 32, 636, 838,
		297, 52, 519, 430, 430, 282, 200, 744, 604, 932,
		808, 936, 671, 941, 1016, 407, 599, 555, 16, 407,
		8, 407, 26, 417, 464, 612, 988, 464, 417, 474,
		346, 250, 372, 709, 372, 755, 352, 308, 1015, 981,
		968, 392, 407, 84, 1006, 712, 528, 434, 82, 508,
		418, 98, 528, 72, 772, 52, 517, 140, 392, 266,
		528, 72, 776, 16, 52, 105, 754, 5, 580, 186,
		749, 821, 378, 381, 690, 500, 981, 264, 244, 365,
		378, 180, 213, 186, 821, 154, 381, 328, 749, 712,
		936, 945, 152, 216, 88, 408, 280, 88, 600, 154,
		1016, 604, 40, 690, 749, 821, 253, 821, 154, 381,
		872, 505, 216, 216, 24, 152, 472, 280, 280, 216,
		186, 888, 749, 501, 536, 279, 352, 352, 391, 3,
		571, 544, 544, 690, 152, 88, 152, 344, 344, 600,
		536, 933, 749, 501, 472, 536, 88, 280, 472, 472,
		600, 280, 933, 749, 611, 860, 176, 282, 811, 372,
		75, 1000, 568, 186, 821, 253, 552, 690, 264, 760,
		186, 961, 381, 328, 436, 893, 1016, 749, 632, 253,
		616, 888, 249, 160, 1000, 186, 376, 1003, 0, 264,
		282, 508, 88, 528, 216, 933, 749, 952, 749, 604,
		140, 821, 154, 249, 564, 387, 452, 544, 483, 224,
		0, 648, 749, 961, 381, 479, 264, 945, 216, 344,
		408, 344, 408, 216, 472, 536, 152, 690, 933, 749,
		264, 945, 216, 88, 600, 216, 536, 88, 344, 523,
		736, 979, 575, 564, 71, 404, 193, 90, 372, 265,
		736, 372, 55, 392, 508, 258, 528, 860, 176, 250,
		90, 1016, 372, 61, 500, 17, 712, 1016, 736, 352,
		757, 859, 827, 757, 931, 903, 757, 482, 482, 931,
		220, 192, 154, 58, 700, 984, 24, 728, 96, 392,
		508, 226, 278, 226, 828, 258, 224, 186, 328, 352,
		282, 508, 622, 528, 821, 494, 494, 528, 1000, 648,
		154, 249, 648, 154, 381, 244, 399, 776, 1000, 154,
		624, 56, 284, 349, 257, 212, 422, 369, 752, 1016,
		308, 865, 667, 58, 86, 581, 709, 183, 758, 469,
		389, 183, 90, 425, 328, 711, 154, 146, 437, 709,
		278, 436, 163, 186, 825, 257, 436, 185, 732, 297,
		690, 186, 328, 154, 663, 508, 58, 110, 294, 215,
		934, 494, 418, 150, 186, 528, 917, 389, 338, 594,
		270, 436, 243, 690, 497, 183, 26, 434, 954, 654,
		278, 508, 299, 482, 538, 295, 314, 474, 400, 236,
		342, 238, 270, 606, 299, 700, 620, 330, 26, 154,
		146, 859, 96, 540, 408, 53, 23, 58, 246, 562,
		411, 658, 558, 14, 18, 283, 828, 603, 314, 610,
		435, 164, 374, 464, 954, 439, 494, 954, 528, 497,
		181, 369, 752, 528, 508, 58, 426, 426, 490, 490,
		782, 389, 154, 150, 758, 393, 154, 246, 782, 472,
		986, 430, 730, 472, 555, 398, 636, 562, 603, 658,
		26, 439, 212, 411, 154, 412, 439, 73, 23, 952,
		186, 760, 886, 446, 824, 73, 32, 0, 0, 708,
		888, 643, 264, 218, 425, 328, 366, 850, 372, 528,
		452, 917, 243, 253, 23, 284, 444, 690, 917, 183,
		824, 73, 1009, 53, 825, 724, 459, 257, 608, 581,
		278, 500, 995, 154, 253, 795, 282, 498, 1018, 528,
		430, 400, 107, 96, 238, 919, 618, 618, 14, 914,
		850, 480, 314, 711, 822, 484, 690, 90, 538, 508,
		838, 500, 270, 528, 0, 0, 497, 181, 888, 528,
		628, 83, 434, 622, 834, 489, 454, 971, 0, 0,
		0, 0, 760, 622, 622, 528, 750, 547, 634, 250,
		54, 957, 154, 570, 714, 523, 570, 154, 250, 746,
		528, 698, 134, 400, 474, 364, 529, 508, 834, 466,
		470, 352, 314, 610, 107, 882, 620, 164, 512, 282,
		709, 949, 18, 476, 552, 626, 660, 707, 404, 711,
		352, 761, 508, 268, 460, 758, 717, 882, 636, 750,
		728, 494, 26, 918, 750, 436, 934, 114, 700, 400,
		594, 255, 82, 114, 286, 311, 749, 185, 520, 52,
		671, 954, 482, 82, 122, 272, 338, 392, 146, 474,
		954, 626, 339, 776, 392, 314, 474, 594, 303, 178,
		594, 338, 403, 90, 474, 443, 356, 609, 498, 400,
		90, 82, 474, 311, 626, 464, 250, 500, 445, 954,
		250, 111, 846, 645, 851, 850, 724, 154, 282, 528,
		668, 469, 154, 328, 154, 122, 842, 724, 430, 590,
		478, 854, 628, 846, 659, 146, 178, 370, 370, 402,
		754, 565, 260, 274, 215, 58, 118, 18, 846, 682,
		882, 694, 272, 434, 776, 90, 434, 954, 90, 146,
		270, 372, 281, 272, 372, 235, 430, 986, 434, 400,
		228, 725, 611, 464, 622, 172, 689, 528, 82, 954,
		452, 282, 311, 52, 109, 648, 388, 644, 264, 154,
		528, 90, 154, 328, 352, 250, 957, 250, 372, 281,
		179, 668, 469, 154, 328, 154, 854, 631, 352, 26,
		150, 143, 186, 578, 854, 312, 282, 26, 163, 761,
		508, 186, 430, 842, 661, 26, 418, 372, 497, 177,
		187, 183, 895, 372, 919, 282, 508, 152, 216, 24,
		152, 344, 536, 344, 24, 600, 152, 600, 600, 280,
		508, 528, 0, 0, 372, 181, 468, 859, 371, 0,
		403, 181, 407, 250, 445, 250, 709, 684, 780, 700,
		472, 146, 250, 103, 122, 626, 147, 934, 154, 478,
		154, 594, 91, 90, 418, 372, 743, 934, 594, 143,
		18, 314, 418, 91, 934, 418, 103, 154, 357, 268,
		250, 282, 154, 372, 799, 494, 954, 746, 995, 850,
		821, 654, 746, 846, 278, 26, 366, 323, 262, 882,
		841, 634, 266, 276, 842, 274, 524, 186, 528, 954,
		622, 311, 270, 754, 854, 90, 538, 686, 286, 528,
		0, 436, 959, 690, 96, 494, 538, 375, 314, 630,
		411, 327, 981, 32, 154, 462, 154, 474, 540, 862,
		874, 834, 379, 282, 164, 953, 634, 280, 506, 274,
		1018, 868, 899, 292, 912, 100, 923, 932, 932, 420,
		939, 464, 528, 892, 216, 88, 24, 88, 472, 600,
		536, 24, 280, 216, 316, 528, 188, 216, 216, 24,
		536, 344, 216, 88, 472, 444, 528, 956, 216, 216,
		216, 24, 536, 280, 892, 528, 636, 216, 216, 216,
		216, 252, 528, 572, 216, 216, 188, 528, 498, 538,
		707, 314, 474, 1022, 250, 400, 528, 316, 408, 600,
		216, 88, 280, 472, 88, 536, 24, 344, 408, 508,
		528, 58, 238, 508, 218, 366, 891, 218, 692, 889,
		418, 851, 464, 835, 372, 709, 58, 180, 487, 400,
		748, 991, 218, 859, 218, 622, 871, 218, 835, 154,
		454, 454, 454, 154, 379, 250, 456, 508, 738, 1011,
		610, 610, 971, 772, 900, 250, 528, 26, 268, 150,
		250, 357, 250, 516, 221, 532, 779, 528, 788, 182,
		952, 404, 189, 65, 852, 94, 872, 276, 12, 712,
		836, 175, 504, 151, 608, 1000, 520, 1013, 709, 264,
		412, 27, 568, 115, 32, 440, 805, 186, 852, 14,
		312, 781, 762, 280, 482, 709, 328, 245, 916, 0,
		936, 1013, 860, 205, 632, 83, 264, 797, 264, 911,
		0, 261, 312, 65, 712, 1013, 312, 65, 403, 288,
		288, 440, 528, 504, 1019, 0, 719, 817, 744, 888,
		136, 588, 758, 280, 690, 936, 969, 270, 264, 282,
		808, 1000, 760, 186, 824, 709, 952, 847, 852, 63,
		568, 528, 276, 109, 282, 872, 936, 154, 224, 312,
		186, 817, 993, 308, 865, 52, 423, 264, 888, 186,
		1016, 709, 328, 249, 387, 690, 352, 105, 260, 264,
		836, 1013, 328, 993, 328, 186, 709, 361, 989, 860,
		101, 648, 328, 154, 264, 328, 709, 632, 989, 844,
		261, 499, 1000, 520, 261, 709, 312, 65, 361, 154,
		245, 274, 264, 312, 186, 817, 245, 328, 154, 65,
		468, 165, 1005, 852, 100, 712, 836, 1013, 591, 274,
		1005, 888, 154, 65, 387, 372, 55, 1000, 772, 79,
		872, 261, 780, 388, 83, 260, 79, 709, 886, 171,
		817, 154, 387, 874, 280, 758, 280, 508, 610, 882,
		280, 528, 288, 504, 709, 264, 632, 900, 119, 249,
		808, 760, 186, 952, 709, 1016, 624, 56, 596, 227,
		882, 48, 758, 48, 580, 249, 936, 969, 998, 400,
		748, 230, 186, 1016, 558, 750, 384, 558, 494, 335,
		696, 781, 750, 255, 803, 154, 276, 117, 475, 180,
		215, 376, 186, 528, 729, 941, 817, 159, 398, 747,
		583, 99, 708, 772, 3, 791, 352, 635, 672, 99,
		99, 352, 99, 191, 144, 259, 619, 99, 116, 859,
		99, 947, 899, 3, 224, 139, 35, 708, 729, 690,
		941, 817, 154, 249, 758, 472, 781, 724, 415, 69,
		843, 696, 186, 817, 154, 69, 28, 336, 690, 264,
		632, 186, 18, 331, 952, 887, 352, 352, 696, 690,
		552, 952, 690, 853, 69, 616, 817, 316, 152, 49,
		568, 805, 154, 975, 1013, 69, 328, 937, 817, 249,
		28, 450, 744, 498, 508, 610, 762, 450, 568, 690,
		941, 488, 136, 568, 49, 760, 49, 264, 760, 186,
		817, 253, 328, 154, 69, 504, 253, 817, 249, 264,
		632, 186, 760, 49, 817, 249, 504, 253, 328, 758,
		402, 559, 760, 853, 49, 264, 186, 952, 49, 328,
		249, 952, 154, 69, 282, 828, 408, 842, 260, 760,
		49, 758, 442, 760, 253, 596, 70, 355, 696, 552,
		888, 279, 352, 154, 69, 796, 461, 952, 827, 817,
		253, 264, 568, 186, 632, 249, 253, 328, 69, 494,
		494, 494, 874, 877, 568, 927, 781, 186, 696, 528,
		817, 253, 760, 49, 604, 450, 817, 249, 224, 372,
		1011, 1017, 69, 933, 959, 758, 280, 528, 288, 888,
		49, 736, 696, 186, 824, 827, 952, 186, 824, 528,
		732, 464, 788, 316, 888, 733, 69, 831, 729, 690,
		941, 655, 817, 186, 632, 69, 355, 416, 416, 416,
		729, 941, 823, 264, 781, 372, 135, 69, 746, 484,
		253, 568, 4, 842, 304, 186, 663, 264, 952, 186,
		888, 528, 500, 797, 90, 154, 1016, 372, 61, 520,
		712, 328, 175, 0, 0, 372, 679, 122, 730, 607,
		578, 858, 655, 282, 412, 601, 865, 383, 180, 675,
		116, 373, 220, 540, 890, 552, 520, 1016, 712, 282,
		26, 58, 182, 754, 564, 260, 860, 565, 668, 565,
		652, 452, 268, 508, 746, 652, 860, 570, 412, 663,
		893, 282, 122, 250, 954, 418, 646, 315, 90, 154,
		909, 264, 154, 372, 93, 622, 180, 485, 250, 90,
		1016, 154, 372, 397, 18, 874, 663, 90, 591, 756,
		73, 154, 282, 436, 713, 250, 756, 73, 378, 1018,
		250, 668, 624, 897, 250, 154, 570, 154, 250, 282,
		909, 18, 476, 628, 897, 314, 274, 788, 642, 494,
		494, 372, 281, 18, 916, 642, 122, 986, 538, 909,
		372, 741, 852, 650, 520, 712, 154, 274, 52, 671,
		750, 529, 90, 404, 280, 860, 659, 372, 265, 18,
		865, 508, 264, 328, 286, 494, 750, 676, 498, 400,
		428, 667, 328, 383, 264, 282, 498, 1018, 727, 154,
		264, 482, 178, 264, 986, 986, 594, 699, 18, 314,
		154, 122, 922, 679, 264, 498, 264, 90, 474, 400,
		428, 693, 250, 372, 281, 791, 934, 594, 787, 18,
		270, 264, 188, 250, 913, 250, 835, 314, 610, 831,
		954, 258, 758, 605, 464, 815, 660, 732, 644, 528,
		652, 528, 954, 494, 878, 734, 528, 224, 282, 634,
		274, 164, 757, 804, 774, 868, 780, 292, 1020, 100,
		873, 828, 472, 188, 528, 444, 408, 408, 536, 408,
		344, 152, 280, 600, 88, 88, 408, 135, 408, 740,
		754, 11, 252, 9, 124, 536, 316, 528, 764, 9,
		828, 600, 444, 528, 508, 282, 472, 536, 344, 216,
		600, 536, 88, 408, 216, 216, 600, 472, 344, 508,
		528, 836, 388, 644, 520, 154, 886, 811, 282, 26,
		58, 150, 754, 821, 452, 660, 820, 260, 274, 250,
		788, 949, 924, 830, 154, 186, 1018, 922, 589, 250,
		622, 874, 839, 622, 287, 494, 954, 250, 264, 328,
		378, 378, 378, 1018, 250, 874, 864, 500, 221, 282,
		250, 328, 378, 474, 796, 860, 954, 1018, 250, 538,
		843, 314, 250, 328, 250, 796, 888, 878, 887, 474,
		483, 380, 9, 252, 528, 568, 372, 73, 628, 763,
		90, 538, 692, 865, 503, 494, 874, 895, 538, 459,
		314, 372, 917, 18, 788, 908, 250, 328, 250, 372,
		281, 18, 264, 73, 372, 89, 622, 264, 328, 494,
		623, 474, 631, 282, 508, 280, 344, 135, 400, 420,
		958, 494, 611, 282, 250, 692, 913, 250, 500, 709,
		428, 927, 250, 264, 954, 954, 282, 508, 88, 264,
		700, 408, 947, 739, 452, 375, 288, 73, 291, 0,
		282, 26, 418, 372, 741, 835, 508, 328, 660, 948,
		860, 955, 250, 1016, 500, 17, 712, 1016, 90, 372,
		61, 284, 976, 690, 52, 671, 668, 987, 652, 276,
		985, 260, 375, 268, 375, 644, 412, 981, 476, 946,
		460, 375, 934, 934, 594, 907, 18, 264, 154, 570,
		314, 264, 122, 178, 610, 915, 154, 470, 154, 758,
		1017, 626, 18, 954, 947, 180, 891, 0, 124, 9,
		892, 528
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
		Text = "HP-27";
		labelHPType.Text = "HP-27 Emulator";
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
			ReadKeyboardFile("hp27.kml");
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
		char c2 = c;
		if (c2 == ',')
		{
			press_key(HP2xKeytable[32]);
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
