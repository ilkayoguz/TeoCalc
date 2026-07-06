using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP32;

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
		'n', 'i', 't', 'p', 'v', 'y', 'd', 's', 'r', 'f',
		'\r', '\r', 'h', '%', '\b', '-', '7', '8', '9', '\0',
		'+', '4', '5', '6', '\0', '*', '1', '2', '3', '\0',
		'/', '0', '.', '#', '\0'
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
		288, 51, 59, 71, 487, 495, 507, 587, 595, 607,
		971, 759, 58, 86, 264, 328, 270, 50, 274, 264,
		572, 400, 426, 490, 172, 21, 250, 762, 61, 264,
		90, 762, 61, 814, 104, 530, 850, 39, 207, 666,
		178, 814, 51, 264, 154, 474, 154, 264, 90, 590,
		90, 814, 61, 430, 1018, 146, 178, 400, 620, 51,
		282, 154, 328, 250, 314, 618, 618, 618, 154, 264,
		328, 754, 75, 430, 1018, 154, 508, 838, 99, 250,
		154, 508, 186, 366, 391, 502, 391, 206, 494, 482,
		210, 90, 886, 96, 282, 18, 528, 206, 363, 834,
		79, 622, 454, 399, 264, 90, 814, 109, 135, 154,
		264, 794, 116, 264, 154, 143, 264, 154, 264, 90,
		143, 58, 86, 264, 328, 270, 50, 274, 264, 398,
		562, 535, 658, 26, 264, 700, 464, 954, 563, 314,
		610, 559, 172, 136, 328, 275, 58, 86, 264, 328,
		270, 50, 274, 758, 183, 264, 558, 562, 643, 658,
		264, 154, 90, 826, 169, 264, 474, 622, 264, 508,
		282, 695, 482, 538, 691, 314, 474, 400, 620, 173,
		154, 328, 319, 828, 308, 19, 58, 246, 154, 850,
		183, 50, 730, 231, 218, 90, 378, 378, 410, 250,
		286, 186, 378, 662, 366, 398, 828, 866, 210, 986,
		1018, 142, 282, 90, 700, 344, 1018, 907, 482, 922,
		875, 346, 474, 740, 229, 400, 998, 258, 879, 154,
		323, 26, 282, 323, 58, 246, 154, 282, 264, 282,
		508, 88, 250, 154, 264, 154, 607, 282, 690, 508,
		88, 59, 282, 999, 408, 600, 216, 88, 280, 472,
		88, 536, 24, 344, 344, 614, 508, 528, 227, 375,
		311, 363, 440, 146, 508, 230, 424, 632, 956, 454,
		454, 454, 134, 142, 616, 295, 250, 264, 632, 182,
		118, 982, 982, 982, 252, 230, 238, 616, 440, 250,
		504, 424, 568, 488, 150, 1018, 1018, 1018, 242, 264,
		250, 528, 504, 552, 440, 488, 218, 146, 178, 424,
		632, 150, 316, 454, 454, 454, 150, 616, 134, 934,
		934, 934, 528, 504, 252, 250, 264, 632, 400, 1018,
		236, 338, 242, 154, 264, 528, 568, 316, 319, 440,
		956, 319, 58, 246, 154, 396, 679, 396, 524, 50,
		154, 186, 366, 571, 850, 401, 154, 469, 508, 154,
		286, 186, 555, 90, 122, 264, 328, 954, 858, 383,
		328, 154, 528, 494, 487, 508, 418, 90, 52, 655,
		420, 1002, 400, 498, 494, 539, 90, 763, 52, 1009,
		679, 516, 451, 508, 218, 610, 154, 858, 411, 180,
		27, 834, 416, 622, 474, 623, 264, 529, 451, 58,
		246, 154, 388, 726, 1006, 524, 50, 850, 183, 726,
		183, 142, 174, 750, 403, 334, 735, 686, 516, 30,
		158, 26, 508, 518, 610, 482, 122, 264, 328, 783,
		954, 626, 779, 328, 314, 594, 759, 498, 90, 474,
		400, 684, 446, 154, 122, 454, 454, 454, 154, 828,
		472, 654, 718, 532, 764, 954, 250, 180, 433, 250,
		180, 109, 758, 511, 620, 475, 58, 828, 98, 314,
		954, 250, 244, 677, 532, 501, 90, 538, 90, 314,
		90, 636, 180, 109, 758, 511, 954, 987, 954, 180,
		11, 850, 508, 622, 286, 540, 518, 690, 52, 305,
		412, 544, 116, 121, 52, 505, 328, 754, 548, 590,
		250, 63, 954, 721, 90, 764, 116, 887, 314, 610,
		107, 258, 494, 464, 528, 58, 246, 154, 516, 850,
		552, 524, 30, 90, 218, 366, 231, 218, 850, 573,
		700, 400, 676, 651, 494, 199, 433, 250, 519, 244,
		677, 764, 267, 90, 430, 986, 163, 502, 538, 263,
		314, 474, 622, 327, 956, 738, 591, 610, 866, 598,
		482, 508, 551, 154, 470, 154, 738, 578, 282, 508,
		614, 186, 380, 88, 540, 607, 686, 90, 186, 284,
		617, 116, 225, 52, 989, 116, 65, 90, 52, 323,
		282, 164, 254, 630, 280, 502, 868, 953, 292, 966,
		100, 978, 932, 988, 420, 996, 828, 216, 764, 528,
		482, 538, 515, 314, 420, 652, 474, 622, 400, 250,
		219, 250, 284, 657, 721, 154, 90, 700, 408, 956,
		758, 714, 464, 738, 670, 610, 122, 264, 328, 699,
		494, 954, 626, 595, 1018, 1018, 1018, 418, 90, 186,
		540, 609, 52, 949, 391, 986, 626, 695, 314, 434,
		328, 607, 264, 328, 122, 250, 378, 378, 410, 90,
		1018, 890, 706, 328, 154, 528, 430, 755, 666, 274,
		264, 494, 52, 643, 90, 286, 186, 540, 722, 690,
		116, 469, 284, 726, 116, 225, 52, 1009, 423, 58,
		246, 154, 116, 225, 648, 456, 850, 1017, 975, 750,
		183, 622, 478, 854, 739, 878, 755, 146, 178, 370,
		370, 402, 754, 755, 452, 648, 18, 154, 268, 116,
		653, 520, 476, 765, 690, 308, 11, 816, 32, 32,
		32, 32, 32, 32, 32, 508, 882, 183, 874, 183,
		186, 122, 478, 838, 790, 430, 782, 794, 490, 528,
		590, 55, 52, 735, 282, 482, 1018, 498, 250, 706,
		803, 966, 494, 26, 898, 167, 474, 314, 159, 914,
		195, 934, 442, 494, 314, 187, 70, 610, 127, 626,
		127, 474, 110, 286, 294, 346, 158, 528, 844, 255,
		836, 649, 282, 508, 88, 473, 40, 120, 186, 456,
		473, 104, 456, 186, 13, 184, 581, 168, 648, 248,
		154, 473, 232, 648, 154, 186, 13, 312, 581, 296,
		456, 648, 13, 376, 581, 360, 641, 308, 23, 649,
		248, 154, 886, 877, 636, 308, 19, 25, 489, 609,
		649, 120, 154, 25, 308, 3, 860, 889, 690, 1,
		886, 893, 282, 776, 508, 746, 902, 622, 490, 618,
		543, 494, 528, 362, 559, 282, 575, 262, 614, 266,
		400, 400, 528, 860, 918, 154, 690, 154, 5, 491,
		186, 596, 925, 456, 712, 154, 712, 528, 456, 680,
		661, 186, 528, 282, 624, 56, 528, 508, 152, 216,
		24, 152, 344, 536, 344, 24, 600, 152, 600, 600,
		280, 250, 528, 216, 88, 24, 88, 472, 600, 536,
		24, 280, 216, 152, 316, 528, 252, 216, 216, 24,
		536, 344, 216, 88, 408, 536, 444, 528, 764, 216,
		216, 216, 24, 536, 216, 344, 892, 528, 124, 216,
		216, 216, 216, 88, 252, 528, 380, 216, 216, 216,
		188, 528, 90, 250, 180, 87, 116, 121, 758, 183,
		328, 882, 183, 26, 58, 180, 391, 150, 874, 183,
		180, 923, 0, 347, 79, 35, 219, 828, 939, 580,
		392, 95, 200, 72, 8, 282, 372, 741, 688, 280,
		154, 968, 584, 244, 489, 392, 588, 285, 989, 297,
		436, 865, 456, 14, 804, 671, 868, 319, 16, 76,
		623, 444, 103, 52, 485, 887, 244, 243, 500, 495,
		500, 627, 239, 151, 327, 247, 52, 745, 154, 456,
		680, 154, 3, 316, 103, 52, 937, 11, 335, 559,
		707, 684, 591, 572, 103, 716, 140, 204, 268, 396,
		460, 524, 652, 844, 528, 180, 871, 648, 622, 622,
		52, 485, 11, 282, 23, 380, 19, 430, 388, 647,
		483, 827, 831, 684, 104, 636, 103, 690, 520, 52,
		49, 11, 764, 103, 956, 103, 359, 179, 187, 676,
		125, 712, 23, 430, 430, 660, 91, 516, 391, 668,
		979, 471, 164, 42, 660, 42, 428, 42, 248, 244,
		609, 120, 3, 164, 920, 292, 920, 671, 515, 435,
		443, 75, 520, 712, 154, 3, 572, 19, 68, 4,
		968, 95, 520, 347, 375, 430, 479, 684, 103, 124,
		103, 100, 920, 932, 920, 660, 920, 644, 107, 430,
		164, 794, 292, 244, 100, 35, 932, 154, 404, 91,
		660, 194, 430, 272, 430, 776, 426, 759, 154, 624,
		56, 154, 420, 708, 676, 225, 36, 230, 863, 0,
		430, 430, 430, 430, 703, 684, 158, 380, 103, 484,
		231, 228, 39, 52, 585, 244, 489, 868, 152, 752,
		282, 624, 456, 3, 690, 52, 49, 887, 282, 624,
		272, 634, 10, 740, 694, 426, 400, 959, 12, 76,
		627, 456, 250, 724, 763, 776, 218, 268, 186, 20,
		279, 874, 273, 572, 88, 24, 782, 279, 14, 904,
		302, 782, 270, 107, 622, 174, 107, 14, 904, 430,
		334, 95, 107, 260, 14, 904, 218, 274, 508, 400,
		590, 119, 276, 293, 746, 293, 464, 26, 166, 350,
		6, 158, 895, 244, 251, 516, 692, 327, 223, 231,
		400, 400, 400, 308, 103, 316, 215, 444, 215, 218,
		186, 18, 979, 16, 355, 699, 375, 260, 433, 216,
		472, 536, 344, 280, 88, 88, 472, 536, 280, 276,
		341, 52, 485, 308, 11, 52, 585, 335, 154, 430,
		430, 120, 343, 116, 385, 335, 183, 1019, 775, 433,
		88, 536, 52, 485, 601, 52, 57, 335, 186, 282,
		508, 528, 527, 499, 471, 508, 215, 8, 741, 688,
		392, 456, 308, 95, 741, 744, 808, 872, 936, 1000,
		491, 282, 40, 104, 168, 232, 296, 360, 491, 494,
		400, 939, 244, 407, 175, 571, 736, 736, 692, 755,
		282, 494, 508, 216, 152, 528, 452, 260, 756, 299,
		691, 687, 683, 260, 433, 494, 152, 344, 280, 319,
		644, 388, 628, 243, 116, 385, 282, 264, 328, 244,
		677, 250, 52, 605, 335, 282, 572, 88, 624, 688,
		282, 624, 528, 628, 227, 578, 590, 799, 979, 434,
		776, 622, 400, 272, 418, 783, 787, 0, 627, 635,
		631, 260, 433, 622, 280, 344, 216, 344, 600, 152,
		216, 472, 319, 754, 491, 284, 395, 494, 746, 490,
		618, 746, 315, 490, 1022, 190, 272, 582, 174, 776,
		284, 753, 84, 641, 746, 505, 776, 654, 618, 142,
		500, 701, 436, 1007, 436, 39, 32, 32, 32, 32,
		32, 32, 32, 32, 32, 26, 73, 250, 456, 17,
		497, 29, 308, 11, 564, 619, 186, 497, 13, 73,
		33, 67, 150, 78, 528, 26, 244, 677, 456, 17,
		180, 145, 67, 244, 29, 67, 26, 508, 418, 528,
		388, 836, 564, 215, 223, 251, 935, 707, 186, 13,
		67, 316, 308, 103, 596, 573, 712, 528, 444, 227,
		659, 119, 307, 352, 780, 908, 491, 780, 299, 772,
		900, 491, 180, 133, 67, 758, 3, 520, 25, 67,
		456, 186, 58, 246, 116, 225, 456, 17, 756, 457,
		116, 227, 827, 83, 443, 186, 372, 601, 690, 1,
		282, 508, 88, 536, 29, 67, 564, 3, 275, 287,
		295, 154, 58, 136, 972, 980, 630, 200, 308, 95,
		282, 508, 600, 494, 528, 238, 426, 590, 572, 270,
		216, 910, 543, 334, 1006, 910, 559, 619, 756, 83,
		151, 611, 603, 615, 884, 335, 756, 611, 736, 736,
		334, 206, 508, 372, 787, 16, 183, 644, 179, 352,
		648, 146, 690, 146, 1, 154, 648, 154, 622, 622,
		29, 67, 233, 73, 378, 502, 270, 959, 474, 474,
		124, 130, 316, 920, 664, 664, 792, 664, 154, 58,
		865, 491, 596, 711, 712, 282, 624, 154, 959, 178,
		250, 528, 692, 327, 859, 571, 851, 352, 756, 231,
		756, 135, 200, 776, 136, 972, 980, 731, 988, 734,
		200, 528, 766, 279, 954, 494, 750, 761, 915, 233,
		696, 959, 758, 24, 690, 308, 3, 18, 842, 738,
		991, 434, 590, 846, 757, 272, 590, 282, 146, 178,
		272, 426, 95, 586, 722, 774, 508, 280, 700, 400,
		594, 31, 482, 164, 715, 464, 164, 715, 464, 164,
		715, 464, 482, 482, 43, 956, 408, 7, 732, 804,
		250, 218, 270, 272, 148, 844, 150, 155, 393, 630,
		462, 462, 636, 454, 482, 207, 470, 464, 172, 810,
		482, 207, 251, 610, 492, 825, 196, 270, 255, 212,
		828, 434, 400, 950, 390, 182, 174, 400, 582, 758,
		838, 429, 156, 841, 701, 392, 308, 99, 142, 956,
		934, 934, 828, 130, 462, 124, 934, 142, 174, 776,
		746, 860, 266, 654, 429, 244, 489, 172, 0, 283,
		596, 869, 712, 588, 708, 282, 190, 272, 528, 776,
		122, 14, 336, 94, 150, 398, 508, 866, 538, 622,
		400, 148, 883, 470, 463, 724, 902, 393, 508, 88,
		186, 582, 14, 132, 196, 283, 148, 915, 758, 918,
		956, 150, 182, 262, 758, 915, 456, 710, 898, 508,
		308, 103, 405, 507, 196, 724, 915, 147, 732, 748,
		148, 935, 758, 931, 690, 272, 590, 0, 295, 956,
		934, 934, 934, 138, 682, 170, 343, 956, 454, 454,
		454, 528, 828, 186, 624, 56, 922, 858, 969, 610,
		727, 688, 270, 508, 482, 392, 456, 528, 348, 958,
		807, 282, 392, 892, 308, 19, 828, 624, 752, 610,
		823, 528, 816, 8, 272, 506, 392, 282, 392, 264,
		264, 584, 584, 584, 520, 968, 968, 90, 250, 634,
		890, 967, 508, 482, 392, 456, 828, 984, 817, 721,
		572, 88, 216, 817, 216, 721, 332, 180, 1021, 785,
		841, 785, 692, 713, 785, 692, 959, 610, 456, 520,
		758, 40, 754, 9, 452, 644, 274, 52, 585, 886,
		14, 26, 116, 225, 116, 373, 52, 505, 52, 1009,
		52, 757, 456, 274, 52, 493, 712, 116, 373, 154,
		186, 227, 660, 38, 644, 528, 652, 528, 154, 452,
		754, 45, 260, 274, 712, 758, 251, 26, 843, 137,
		199, 58, 186, 86, 754, 66, 260, 860, 66, 668,
		66, 652, 452, 268, 508, 142, 174, 366, 291, 711,
		846, 88, 218, 730, 49, 508, 610, 890, 88, 404,
		51, 641, 26, 590, 154, 843, 404, 253, 52, 949,
		137, 90, 218, 508, 286, 494, 750, 106, 498, 400,
		428, 97, 250, 843, 264, 282, 498, 1018, 495, 154,
		264, 482, 178, 264, 986, 986, 594, 467, 18, 314,
		154, 122, 922, 447, 264, 498, 264, 90, 474, 400,
		428, 123, 250, 52, 677, 90, 264, 270, 188, 250,
		1021, 250, 587, 314, 610, 583, 954, 258, 758, 207,
		464, 567, 282, 264, 641, 378, 1018, 528, 508, 282,
		472, 536, 344, 216, 600, 536, 88, 408, 216, 216,
		600, 472, 344, 508, 528, 412, 93, 116, 225, 116,
		225, 52, 1009, 116, 65, 52, 989, 116, 121, 52,
		505, 18, 52, 757, 116, 121, 52, 969, 18, 142,
		174, 366, 363, 375, 464, 622, 172, 206, 286, 52,
		305, 668, 221, 617, 690, 146, 274, 52, 69, 476,
		226, 617, 52, 69, 788, 241, 617, 430, 430, 52,
		605, 916, 241, 282, 508, 600, 622, 52, 493, 284,
		244, 690, 852, 251, 244, 489, 520, 712, 154, 308,
		11, 308, 15, 282, 634, 274, 164, 272, 804, 290,
		868, 298, 292, 304, 100, 308, 828, 472, 188, 528,
		444, 408, 408, 536, 408, 344, 152, 280, 600, 88,
		88, 408, 508, 528, 408, 740, 269, 123, 252, 121,
		828, 344, 124, 536, 316, 528, 764, 121, 828, 600,
		444, 528, 124, 121, 892, 528, 380, 121, 252, 528,
		836, 388, 520, 154, 26, 58, 150, 754, 328, 452,
		412, 326, 668, 327, 260, 274, 250, 788, 404, 924,
		337, 154, 186, 1018, 922, 282, 508, 280, 344, 250,
		622, 874, 349, 622, 375, 494, 954, 250, 264, 328,
		378, 378, 378, 1018, 250, 874, 377, 538, 415, 314,
		474, 622, 415, 282, 250, 328, 378, 796, 373, 954,
		1018, 250, 538, 523, 314, 250, 328, 250, 796, 413,
		878, 412, 474, 631, 660, 395, 644, 276, 393, 260,
		475, 268, 475, 652, 412, 389, 476, 402, 460, 475,
		452, 475, 564, 641, 379, 90, 538, 564, 137, 651,
		494, 874, 418, 538, 607, 314, 622, 52, 305, 788,
		432, 328, 378, 622, 52, 593, 564, 617, 52, 505,
		264, 154, 186, 494, 751, 90, 474, 763, 400, 420,
		496, 494, 739, 90, 282, 250, 564, 1021, 250, 791,
		498, 538, 787, 314, 400, 1022, 474, 428, 447, 264,
		954, 954, 282, 508, 88, 264, 828, 408, 408, 907,
		934, 934, 594, 867, 18, 264, 154, 570, 314, 264,
		122, 178, 610, 875, 154, 470, 154, 758, 547, 626,
		622, 18, 954, 907, 328, 852, 563, 668, 563, 52,
		949, 692, 207, 456, 264, 328, 270, 692, 75, 0,
		32, 32, 32, 32, 32, 32, 32, 32, 32, 32,
		32, 96, 96, 96, 96, 96, 244, 491, 37, 712,
		49, 21, 668, 539, 520, 712, 154, 65, 476, 543,
		690, 520, 712, 154, 267, 274, 264, 154, 328, 602,
		852, 556, 660, 558, 654, 90, 726, 583, 264, 52,
		653, 412, 578, 45, 154, 186, 264, 218, 21, 52,
		1009, 41, 852, 505, 52, 949, 284, 581, 690, 308,
		11, 282, 508, 614, 380, 88, 186, 122, 412, 581,
		207, 250, 357, 279, 326, 998, 870, 596, 528, 726,
		612, 508, 206, 494, 494, 874, 645, 400, 748, 614,
		218, 528, 622, 391, 282, 214, 540, 633, 464, 620,
		625, 553, 467, 464, 553, 400, 400, 553, 186, 218,
		527, 26, 545, 464, 612, 639, 464, 545, 474, 346,
		250, 52, 275, 714, 612, 419, 998, 390, 166, 998,
		358, 358, 550, 540, 596, 26, 174, 410, 270, 528,
		49, 850, 902, 53, 850, 902, 244, 649, 58, 86,
		52, 989, 45, 57, 49, 37, 244, 661, 29, 41,
		276, 984, 65, 244, 609, 260, 53, 659, 816, 57,
		33, 65, 244, 609, 26, 58, 756, 939, 844, 680,
		186, 18, 754, 707, 836, 282, 700, 600, 88, 152,
		536, 1, 850, 741, 456, 274, 392, 436, 337, 820,
		497, 61, 884, 85, 180, 145, 49, 37, 884, 261,
		21, 852, 739, 884, 85, 52, 1009, 308, 3, 436,
		337, 820, 65, 372, 437, 622, 344, 5, 911, 884,
		1013, 500, 785, 282, 508, 152, 236, 757, 250, 218,
		374, 374, 154, 436, 865, 32, 32, 32, 32, 32,
		32, 32, 32, 32, 32, 32, 32, 96, 96, 96,
		96, 96, 116, 407, 180, 147, 581, 549, 45, 456,
		581, 1, 49, 21, 41, 1009, 65, 308, 11, 477,
		404, 799, 186, 508, 578, 838, 813, 282, 614, 266,
		391, 186, 1, 18, 45, 456, 274, 581, 553, 49,
		33, 65, 387, 477, 404, 799, 186, 573, 52, 1009,
		41, 52, 1009, 49, 33, 456, 274, 5, 65, 391,
		477, 276, 850, 412, 852, 581, 154, 127, 404, 799,
		274, 180, 133, 45, 284, 872, 53, 33, 53, 9,
		468, 870, 457, 186, 456, 178, 154, 127, 61, 45,
		52, 949, 1009, 468, 879, 457, 127, 53, 33, 391,
		282, 508, 622, 344, 19, 762, 897, 746, 898, 142,
		828, 344, 654, 782, 898, 388, 456, 528, 244, 663,
		636, 308, 19, 154, 690, 0, 3, 186, 690, 13,
		47, 436, 163, 388, 516, 619, 244, 651, 260, 644,
		529, 581, 549, 882, 902, 758, 902, 844, 460, 248,
		690, 186, 120, 573, 376, 186, 529, 13, 1009, 61,
		852, 950, 836, 248, 565, 312, 186, 683, 468, 958,
		452, 120, 565, 184, 186, 683, 276, 986, 660, 662,
		404, 1000, 532, 1000, 57, 977, 248, 690, 5, 49,
		21, 61, 57, 120, 17, 1009, 57, 37, 529, 29,
		412, 799, 308, 3, 49, 53, 21, 850, 902, 758,
		902, 456, 436, 233, 41, 57, 33, 867, 49, 977,
		120, 690, 5, 57, 21, 45, 45, 57, 248, 835,
		726, 902, 404, 691, 529, 186, 456, 15, 49, 11,
		0, 954, 52, 507, 32, 282, 264, 372, 439, 116,
		375, 32, 116, 311, 282, 690, 494, 19, 29, 13,
		344, 600, 152, 536, 536, 344, 472, 152, 280, 280,
		216, 536, 9, 21, 494, 17, 280, 536, 408, 600,
		344, 600, 600, 216, 24, 408, 600, 152, 37, 29,
		9, 13, 152, 408, 152, 280, 216, 216, 88, 152,
		88, 408, 472, 600, 9, 49, 152, 600, 536, 152,
		88, 216, 344, 344, 472, 536, 24, 536, 37, 29,
		9, 13, 344, 472, 344, 536, 536, 344, 280, 536,
		24, 280, 344, 536, 9, 116, 225, 41, 884, 241,
		17, 216, 600, 600, 600, 24, 216, 280, 216, 536,
		344, 24, 280, 1, 29, 37, 884, 261, 636, 280,
		280, 280, 9, 116, 361, 3, 41, 13, 216, 600,
		600, 24, 88, 600, 280, 88, 472, 24, 88, 88,
		9, 282, 494, 17, 216, 24, 472, 536, 600, 600,
		216, 216, 24, 216, 280, 37, 41, 9, 282, 622,
		17, 472, 280, 152, 216, 536, 24, 600, 152, 280,
		24, 152, 472, 9, 49, 88, 344, 88, 344, 24,
		536, 600, 472, 152, 280, 344, 88, 37, 13, 280,
		536, 216, 536, 344, 600, 88, 152, 536, 24, 536,
		9, 41, 9, 13, 344, 152, 600, 216, 216, 24,
		216, 152, 280, 600, 152, 408, 37, 884, 241, 17,
		88, 344, 88, 408, 472, 600, 88, 88, 408, 408,
		216, 344, 9, 41, 9, 13, 88, 600, 536, 408,
		88, 344, 216, 536, 88, 216, 408, 280, 37, 41,
		9, 282, 828, 280, 654, 17, 884, 103, 32, 32,
		32, 32, 32, 32, 32, 32, 32, 32, 32, 96,
		96, 96, 96, 96, 180, 147, 282, 820, 19, 146,
		690, 146, 528, 216, 600, 536, 24, 408, 280, 472,
		600, 280, 9, 73, 88, 764, 408, 88, 344, 216,
		24, 152, 37, 53, 9, 282, 690, 828, 536, 654,
		77, 216, 536, 24, 344, 152, 11, 186, 282, 508,
		690, 622, 528, 282, 622, 264, 282, 508, 216, 600,
		536, 600, 280, 152, 152, 536, 24, 216, 536, 344,
		528, 754, 343, 308, 15, 874, 360, 762, 353, 436,
		161, 570, 890, 341, 516, 634, 266, 540, 358, 274,
		308, 11, 680, 844, 237, 600, 1, 754, 405, 456,
		237, 88, 1, 850, 412, 456, 237, 344, 1, 690,
		392, 261, 33, 45, 49, 21, 436, 369, 820, 65,
		456, 5, 61, 53, 65, 49, 21, 261, 33, 85,
		61, 57, 49, 9, 45, 925, 507, 436, 161, 456,
		690, 1, 392, 631, 836, 456, 116, 385, 73, 690,
		152, 17, 41, 45, 77, 536, 5, 820, 49, 600,
		280, 37, 49, 9, 77, 88, 216, 494, 5, 282,
		690, 77, 408, 88, 37, 49, 9, 45, 49, 21,
		436, 369, 61, 45, 53, 65, 136, 45, 820, 497,
		52, 949, 85, 45, 53, 261, 33, 456, 17, 49,
		9, 57, 21, 45, 52, 1009, 57, 33, 45, 116,
		397, 77, 152, 17, 18, 41, 61, 925, 803, 45,
		57, 726, 499, 842, 494, 967, 572, 418, 975, 136,
		55, 200, 53, 90, 52, 321, 860, 507, 690, 308,
		3, 816, 0, 829, 0, 0, 0, 0, 0, 0,
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
		Text = "HP-32";
		labelHPType.Text = "HP-32 Emulator";
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
