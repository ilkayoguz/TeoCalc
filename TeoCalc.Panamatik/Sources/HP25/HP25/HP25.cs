using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Panamatik.Calc.HP25;

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
		't', 'b', 'o', 'f', 'g', 'y', 'd', 's', 'r', '#',
		'\r', '\r', 'n', 'x', '\b', '-', '7', '8', '9', '\0',
		'+', '4', '5', '6', '\0', '*', '1', '2', '3', '\0',
		'/', '0', '.', ' ', '\0'
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
		635, 130, 674, 130, 279, 555, 365, 203, 419, 953,
		456, 886, 24, 452, 6, 854, 72, 985, 508, 282,
		482, 226, 194, 392, 901, 124, 710, 13, 219, 468,
		62, 953, 218, 572, 482, 875, 474, 464, 620, 34,
		400, 134, 400, 186, 614, 142, 776, 854, 6, 30,
		158, 392, 90, 901, 90, 84, 1004, 90, 220, 954,
		116, 471, 828, 66, 90, 572, 454, 636, 454, 124,
		934, 934, 456, 154, 174, 776, 746, 80, 266, 654,
		158, 897, 453, 158, 365, 158, 793, 392, 724, 1001,
		219, 78, 14, 904, 398, 508, 866, 235, 622, 400,
		468, 96, 470, 387, 90, 124, 468, 1, 690, 178,
		540, 998, 207, 124, 2, 418, 418, 98, 454, 454,
		528, 404, 40, 968, 430, 968, 163, 0, 282, 624,
		616, 680, 744, 808, 872, 936, 1000, 528, 476, 141,
		72, 388, 540, 31, 215, 220, 472, 596, 995, 660,
		995, 268, 68, 244, 627, 982, 935, 0, 624, 688,
		494, 490, 498, 378, 264, 282, 392, 200, 776, 58,
		26, 968, 901, 328, 174, 10, 180, 527, 508, 262,
		614, 266, 528, 708, 362, 715, 282, 528, 92, 58,
		136, 282, 506, 506, 758, 192, 223, 456, 776, 828,
		758, 186, 746, 209, 622, 490, 618, 735, 494, 528,
		604, 470, 26, 212, 847, 513, 244, 319, 610, 492,
		121, 388, 270, 171, 90, 26, 434, 954, 434, 434,
		94, 904, 828, 578, 623, 78, 46, 528, 532, 254,
		282, 392, 156, 245, 712, 132, 282, 272, 634, 968,
		26, 968, 274, 250, 516, 528, 276, 357, 90, 828,
		154, 792, 828, 130, 322, 1005, 212, 332, 474, 572,
		280, 572, 322, 430, 474, 474, 307, 494, 494, 494,
		90, 162, 649, 1001, 212, 392, 474, 474, 942, 426,
		543, 282, 634, 636, 528, 272, 282, 392, 456, 634,
		140, 508, 920, 664, 664, 792, 664, 72, 154, 282,
		58, 224, 972, 188, 400, 748, 314, 980, 312, 528,
		283, 795, 494, 663, 494, 267, 141, 152, 280, 677,
		636, 728, 929, 852, 405, 174, 462, 1018, 559, 186,
		468, 351, 746, 351, 282, 828, 344, 154, 334, 419,
		186, 528, 494, 631, 639, 916, 256, 572, 984, 763,
		604, 993, 186, 500, 523, 4, 494, 494, 494, 494,
		671, 494, 655, 588, 652, 747, 596, 277, 141, 88,
		216, 677, 929, 20, 444, 90, 162, 1001, 212, 392,
		474, 474, 929, 142, 270, 1018, 1006, 398, 244, 347,
		32, 871, 528, 494, 463, 72, 755, 141, 88, 280,
		72, 580, 743, 836, 494, 494, 494, 439, 447, 494,
		399, 494, 851, 494, 528, 212, 429, 250, 26, 90,
		282, 528, 607, 483, 224, 224, 141, 88, 344, 72,
		644, 677, 908, 933, 572, 792, 572, 660, 452, 604,
		398, 610, 610, 571, 141, 152, 216, 677, 72, 900,
		636, 664, 933, 311, 855, 32, 0, 883, 494, 494,
		494, 494, 494, 494, 260, 591, 596, 480, 668, 471,
		212, 995, 204, 220, 954, 988, 482, 971, 72, 136,
		225, 1008, 212, 482, 212, 167, 988, 494, 200, 272,
		828, 270, 90, 844, 16, 0, 154, 474, 474, 474,
		154, 528, 263, 263, 263, 263, 263, 387, 383, 379,
		116, 159, 580, 644, 507, 580, 644, 399, 115, 131,
		482, 482, 482, 776, 674, 610, 610, 956, 88, 892,
		152, 216, 154, 355, 152, 280, 123, 481, 490, 199,
		490, 490, 490, 195, 490, 485, 482, 482, 482, 482,
		482, 482, 490, 482, 154, 363, 200, 174, 286, 272,
		638, 764, 588, 652, 908, 433, 900, 462, 10, 776,
		430, 272, 842, 592, 916, 594, 88, 216, 636, 154,
		454, 359, 258, 144, 998, 88, 892, 465, 158, 10,
		636, 454, 272, 328, 244, 135, 482, 482, 482, 482,
		528, 437, 686, 174, 462, 10, 636, 262, 426, 426,
		144, 668, 631, 596, 628, 88, 344, 475, 88, 280,
		984, 528, 490, 490, 490, 528, 0, 0, 437, 154,
		462, 10, 355, 908, 618, 555, 900, 430, 430, 563,
		618, 791, 456, 965, 586, 842, 659, 186, 667, 769,
		78, 924, 672, 110, 238, 746, 669, 266, 622, 622,
		731, 206, 142, 746, 677, 654, 618, 142, 46, 52,
		453, 178, 72, 332, 340, 188, 508, 226, 674, 226,
		400, 36, 188, 703, 622, 743, 759, 622, 631, 590,
		982, 590, 982, 639, 272, 6, 582, 776, 528, 456,
		334, 836, 965, 842, 727, 769, 78, 572, 834, 741,
		590, 855, 14, 78, 679, 982, 835, 950, 430, 863,
		264, 174, 264, 10, 508, 400, 590, 895, 6, 854,
		744, 124, 977, 571, 769, 78, 843, 228, 756, 400,
		590, 943, 979, 508, 430, 955, 26, 166, 342, 1019,
		434, 958, 430, 860, 766, 400, 110, 528, 291, 291,
		291, 291, 291, 291, 291, 291, 291, 291, 679, 535,
		99, 99, 99, 99, 575, 575, 575, 575, 575, 490,
		490, 651, 372, 131, 490, 352, 32, 490, 490, 308,
		195, 886, 805, 26, 602, 380, 154, 464, 474, 812,
		807, 134, 282, 250, 220, 373, 136, 116, 225, 200,
		276, 167, 250, 947, 553, 32, 328, 186, 282, 828,
		536, 636, 472, 828, 776, 598, 775, 76, 632, 528,
		612, 918, 90, 474, 30, 474, 474, 328, 150, 886,
		852, 76, 264, 911, 212, 1014, 250, 1001, 233, 90,
		502, 667, 56, 130, 464, 130, 400, 752, 751, 776,
		918, 775, 624, 154, 186, 438, 691, 528, 58, 70,
		986, 444, 314, 400, 44, 882, 528, 776, 328, 758,
		1021, 630, 1015, 1018, 400, 971, 596, 278, 477, 260,
		747, 392, 156, 905, 712, 907, 92, 909, 477, 76,
		528, 110, 1006, 624, 56, 462, 10, 144, 328, 942,
		74, 142, 264, 911, 328, 886, 929, 502, 264, 747,
		700, 234, 266, 579, 474, 474, 371, 456, 752, 907,
		464, 464, 431, 1018, 400, 755, 0, 515, 596, 279,
		268, 212, 924, 1001, 233, 56, 748, 943, 266, 180,
		219, 272, 482, 407, 328, 278, 264, 76, 911, 308,
		909, 90, 441, 441, 436, 789, 903, 308, 909, 445,
		58, 70, 90, 474, 453, 436, 789, 282, 216, 408,
		508, 372, 881, 392, 132, 72, 52, 789, 392, 172,
		1002, 553, 92, 167, 980, 967, 136, 233, 1001, 56,
		748, 893, 286, 200, 266, 174, 426, 144, 776, 328,
		502, 264, 272, 528, 479, 543, 499, 811, 32, 535,
		119, 32, 32, 835, 859, 883, 683, 943, 943, 943,
		580, 791, 580, 635, 671, 907, 943, 434, 434, 995,
		367, 183, 907, 712, 140, 907, 580, 779, 580, 627,
		891, 663, 943, 943, 951, 831, 599, 227, 551, 26,
		418, 815, 462, 138, 456, 508, 776, 18, 588, 144,
		1009, 504, 186, 873, 577, 248, 516, 508, 877, 577,
		440, 154, 845, 392, 248, 186, 282, 482, 845, 26,
		418, 877, 456, 873, 577, 274, 500, 325, 899, 986,
		430, 343, 26, 764, 528, 648, 552, 622, 622, 873,
		623, 26, 874, 115, 154, 110, 400, 228, 108, 590,
		411, 134, 206, 14, 158, 604, 155, 436, 789, 596,
		155, 282, 623, 552, 690, 520, 849, 899, 520, 552,
		873, 899, 0, 0, 0, 0, 0, 76, 907, 552,
		487, 186, 552, 690, 392, 456, 644, 695, 52, 793,
		164, 224, 528, 1009, 504, 154, 248, 516, 877, 899,
		648, 845, 886, 199, 604, 226, 244, 1001, 907, 72,
		32, 1009, 372, 851, 186, 552, 652, 873, 580, 440,
		372, 901, 504, 372, 309, 456, 648, 873, 376, 372,
		901, 648, 154, 186, 668, 193, 690, 352, 648, 154,
		845, 754, 160, 604, 162, 907, 520, 552, 516, 877,
		899, 8, 282, 392, 123, 690, 372, 171, 520, 712,
		154, 899, 352, 352, 584, 899, 1009, 568, 392, 132,
		224, 186, 246, 270, 828, 344, 922, 842, 85, 116,
		159, 282, 624, 40, 104, 168, 232, 296, 360, 424,
		488, 907, 264, 146, 264, 907, 156, 255, 712, 528,
		835, 383, 839, 843, 459, 455, 463, 303, 295, 675,
		244, 807, 891, 311, 399, 415, 43, 387, 559, 999,
		595, 591, 599, 1015, 1011, 583, 52, 795, 462, 138,
		456, 552, 58, 508, 72, 776, 144, 0, 96, 604,
		472, 528, 508, 58, 426, 426, 490, 490, 782, 307,
		154, 150, 758, 311, 154, 246, 782, 952, 986, 430,
		730, 952, 227, 264, 178, 264, 508, 594, 439, 26,
		418, 430, 651, 516, 416, 516, 416, 186, 456, 903,
		480, 282, 316, 472, 536, 344, 216, 600, 536, 88,
		408, 216, 344, 508, 528, 580, 288, 0, 0, 186,
		456, 877, 907, 186, 456, 508, 516, 881, 907, 594,
		643, 836, 528, 452, 900, 253, 580, 116, 333, 508,
		924, 791, 690, 146, 500, 63, 392, 154, 648, 877,
		105, 796, 390, 690, 520, 712, 456, 877, 863, 321,
		668, 471, 712, 392, 463, 644, 657, 452, 900, 253,
		852, 413, 154, 881, 325, 378, 877, 116, 333, 480,
		26, 578, 430, 528, 520, 712, 154, 528, 644, 754,
		428, 772, 392, 456, 648, 854, 435, 758, 471, 508,
		881, 105, 392, 873, 657, 873, 520, 169, 586, 586,
		842, 395, 105, 571, 154, 312, 901, 508, 282, 482,
		668, 459, 690, 154, 248, 901, 308, 839, 224, 873,
		274, 863, 325, 378, 378, 392, 308, 903, 186, 416,
		558, 416, 186, 456, 690, 169, 105, 752, 172, 295,
		282, 272, 634, 24, 728, 116, 207, 668, 471, 26,
		418, 758, 382, 392, 881, 456, 877, 105, 515, 520,
		154, 708, 388, 516, 26, 918, 931, 954, 626, 931,
		508, 498, 122, 1005, 578, 23, 70, 306, 495, 188,
		701, 252, 225, 892, 221, 865, 444, 221, 513, 316,
		221, 401, 221, 961, 221, 461, 154, 570, 714, 547,
		570, 90, 400, 474, 364, 548, 154, 754, 556, 694,
		494, 316, 761, 724, 730, 412, 471, 461, 885, 619,
		480, 954, 250, 239, 314, 626, 235, 154, 478, 154,
		703, 154, 122, 182, 362, 643, 490, 954, 494, 287,
		679, 457, 881, 457, 261, 961, 316, 217, 401, 444,
		217, 513, 892, 217, 865, 252, 217, 217, 217, 764,
		6, 700, 250, 154, 408, 595, 892, 216, 88, 24,
		88, 472, 600, 536, 24, 344, 344, 740, 650, 859,
		186, 282, 508, 152, 216, 24, 152, 344, 531, 82,
		434, 1022, 454, 27, 188, 216, 216, 24, 536, 344,
		24, 612, 651, 600, 216, 859, 1005, 418, 122, 626,
		563, 934, 154, 478, 154, 594, 571, 90, 418, 789,
		288, 540, 672, 430, 842, 760, 542, 623, 318, 474,
		622, 635, 954, 262, 142, 754, 686, 90, 538, 698,
		954, 250, 282, 630, 540, 696, 280, 502, 751, 408,
		364, 695, 1018, 1018, 528, 314, 610, 759, 954, 464,
		620, 702, 494, 18, 508, 58, 834, 719, 474, 622,
		858, 712, 282, 110, 314, 850, 704, 150, 186, 58,
		508, 528, 956, 519, 456, 307, 636, 398, 562, 903,
		658, 58, 86, 26, 172, 702, 886, 749, 532, 294,
		198, 598, 490, 230, 150, 480, 408, 600, 216, 88,
		280, 472, 88, 431, 438, 667, 934, 594, 1003, 18,
		314, 528, 90, 185, 305, 185, 305, 476, 776, 154,
		924, 788, 754, 781, 772, 274, 877, 945, 873, 157,
		325, 305, 877, 604, 493, 26, 418, 118, 150, 622,
		966, 746, 795, 934, 494, 127, 954, 986, 945, 250,
		379, 26, 418, 352, 520, 712, 154, 619, 945, 154,
		738, 819, 666, 186, 206, 787, 416, 474, 990, 250,
		243, 498, 538, 239, 314, 416, 416, 482, 922, 263,
		346, 474, 400, 998, 748, 834, 258, 207, 154, 456,
		154, 528, 352, 186, 58, 250, 124, 895, 0, 966,
		594, 351, 498, 70, 390, 90, 122, 902, 355, 305,
		954, 134, 90, 454, 945, 434, 434, 151, 282, 46,
		958, 957, 610, 305, 154, 124, 253, 764, 217, 252,
		217, 380, 536, 444, 217, 993, 217, 321, 474, 217,
		218, 785, 321, 378, 468, 907, 796, 912, 378, 154,
		178, 154, 690, 165, 321, 378, 154, 852, 920, 154,
		877, 372, 253, 873, 660, 810, 288, 400, 318, 883,
		116, 159, 186, 321, 378, 378, 378, 257, 321, 444,
		221, 993, 252, 225, 380, 536, 764, 221, 124, 221,
		221, 90, 1018, 700, 344, 851, 618, 618, 14, 914,
		850, 959, 416, 822, 963, 690, 90, 538, 416, 966,
		966, 626, 791, 390, 518, 230, 218, 594, 799, 134,
		305, 722, 768, 474, 134, 945, 966, 626, 242, 819,
		352, 352, 302, 895, 610, 498, 748, 923, 142, 14,
		866, 999, 954, 1018, 238, 270, 508, 287, 392, 456,
		528, 482, 542, 959, 318, 478, 400, 748, 1008, 207,
		764, 536, 408, 344, 152, 280, 600, 528
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
		Text = "HP-25";
		labelHPType.Text = "HP-25 Emulator";
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
			ReadKeyboardFile("hp25.kml");
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
		else if (transparent && num2 > FirstRow + 6 * RowSize + RowSize / 2)
		{
			if (num < (SliderLeft + SliderRight) / 2)
			{
				LoadProgram();
			}
			else
			{
				SaveProgram();
			}
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
		return act_ram[63 + j / 7 * 7 + 6 - j % 7];
	}

	private void SetProgramCode(int j, byte code)
	{
		act_ram[63 + j / 7 * 7 + 6 - j % 7] = code;
	}

	private void WriteProgram(string FileName)
	{
		int num = 0;
		int num2 = 0;
		FileStream fileStream = File.Open(FileName, FileMode.Create);
		Encoding encoding = new ASCIIEncoding();
		num = 49;
		num2 = 8;
		fileStream.Write(encoding.GetBytes("HP25\r\n"), 0, 6);
		int num3 = 0;
		for (int i = 0; i < num; i++)
		{
			if (GetProgramCode(i) != 0)
			{
				num3 = i + 1;
			}
		}
		if (num3 > 0)
		{
			fileStream.Write(encoding.GetBytes("PROGRAM\r\n"), 0, encoding.GetByteCount("PROGRAM\r\n"));
			for (int i = 0; i < num3; i++)
			{
				int programCode = GetProgramCode(i);
				string s = HP2xMnemonicsAll[programCode] + "\r\n";
				fileStream.Write(encoding.GetBytes(s), 0, encoding.GetByteCount(s));
			}
			fileStream.Write(encoding.GetBytes("END\r\n"), 0, encoding.GetByteCount("END\r\n"));
			fileStream.Write(encoding.GetBytes("\r\n"), 0, 2);
		}
		fileStream.Write(encoding.GetBytes("DATA\r\n"), 0, 6);
		for (int j = 0; j < num2; j++)
		{
			double registerValue = GetRegisterValue(j);
			string s = Convert.ToString(registerValue, CultureInfo.InvariantCulture) + "\r\n";
			fileStream.Write(encoding.GetBytes(s), 0, encoding.GetByteCount(s));
		}
		fileStream.Write(encoding.GetBytes("END\r\n"), 0, 5);
		fileStream.Close();
	}

	private bool ReadProgram(string FileName)
	{
		bool result = false;
		int num = 0;
		int num2 = 0;
		string[] array = File.ReadAllLines(FileName);
		num = 49;
		num2 = 8;
		byte[] array2 = new byte[num];
		byte[] array3 = new byte[14];
		int num3 = -1;
		int num4 = -1;
		int num5 = 0;
		char[] separator = new char[1] { ' ' };
		int i = 0;
		try
		{
			for (i = 0; i < array.Length; i++)
			{
				string[] array4 = array[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if (array[i] == "" || array[i][0] == ';' || array4[0] == ";")
				{
					continue;
				}
				if (array4[0] == "PROGRAM")
				{
					num3 = 0;
					result = true;
				}
				else if (array4[0] == "DATA")
				{
					num4 = 0;
					result = true;
				}
				else if (array4[0] == "END")
				{
					if (num3 >= 0)
					{
						for (int j = 0; j < num; j++)
						{
							SetProgramCode(j, array2[j]);
						}
					}
					num3 = -1;
					num4 = -1;
				}
				else if (num3 >= 0)
				{
					num5 = GetOpcode25(array4);
					if (num5 < 0)
					{
						throw new Exception("Mnemonic not found");
					}
					if (num3 >= num)
					{
						throw new Exception("Program too large");
					}
					array2[num3++] = (byte)num5;
				}
				else if (num4 >= 0)
				{
					if (num4 >= num2)
					{
						throw new Exception("Data too large");
					}
					SetRegisterValue(array3, array4[0]);
					for (int j = 0; j < 7; j++)
					{
						act_ram[num4 * 7 + j] = (byte)((array3[j * 2 + 1] << 4) | array3[j * 2]);
					}
					num4++;
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			string message = $"Syntax error at line {i + 1} {array[i]} {ex.Message} ";
			throw new Exception(message);
		}
	}

	private void LoadProgram()
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

	private void SaveProgram()
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

	private void buttonLoad_Click(object sender, EventArgs e)
	{
		LoadProgram();
	}

	private void buttonSave_Click(object sender, EventArgs e)
	{
		SaveProgram();
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
		base.ClientSize = new System.Drawing.Size(384, 397);
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
