using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Panamatik.Calc.HP01;

public class HP01 : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int WSIZE01 = 12;

	private const int EXPSIZE = 3;

	private const int NROFIMAGES = 2;

	public byte[] act_a;

	public byte[] act_b;

	public byte[] act_c;

	public byte[] act_y;

	public byte[] act_z;

	public byte[] act_t;

	public byte[] act_m;

	public byte act_sp;

	public byte act_p;

	public byte act_f;

	public ushort act_pc;

	public ushort opcode;

	public ushort act_s;

	public byte[] act_dsp;

	public byte[] act_cl;

	public byte[] act_sw;

	public byte[] act_al;

	private int SWStartTime;

	private F act_flags;

	public F01 hp01_flags;

	private byte act_rom;

	private byte TickCnt;

	private byte[] src;

	private byte[] dest;

	private byte[] src2;

	private byte first;

	private byte last;

	private byte act_key_buf;

	private byte act_ram_addr;

	private byte rom_addr;

	private ushort[] act_stack;

	private op_fcn[] op_fcn00;

	private byte[] p_set_map01 = new byte[16]
	{
		255, 11, 8, 0, 5, 255, 9, 1, 4, 2,
		255, 3, 7, 6, 10, 255
	};

	private byte[] p_test_map01 = new byte[16]
	{
		1, 11, 8, 0, 5, 255, 9, 255, 4, 2,
		255, 3, 7, 6, 10, 255
	};

	private bool ShowTime = true;

	private bool running = true;

	private bool RefreshDisplay = true;

	private byte BlinkCnt;

	private int DisplayCnt;

	private byte second;

	private byte keycode;

	private Stopwatch SW = new Stopwatch();

	private string[] ImageTable;

	private int ImageNr;

	private char[] digittab = new char[16]
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'.', '-', ':', 'o', '-', ' '
	};

	private byte[] HP01Keytable = new byte[28]
	{
		62, 15, 14, 12, 11, 9, 60, 54, 7, 6,
		4, 3, 1, 57, 55, 31, 30, 28, 27, 25,
		59, 52, 39, 38, 36, 35, 33, 51
	};

	private char[] HP01KeyChartable = new char[28]
	{
		'R', '0', '1', '2', '3', '4', 'S', '.', '5', '6',
		'7', '8', '9', 'a', ':', '+', '-', '*', '/', '=',
		'P', 'D', ';', 'A', 'd', 'M', 'C', 'T'
	};

	private byte[] columntab = new byte[7] { 0, 2, 4, 3, 6, 5, 6 };

	public ushort[] opcodeint = new ushort[2304]
	{
		411, 602, 0, 602, 602, 0, 602, 602, 0, 602,
		0, 602, 602, 0, 602, 540, 336, 436, 3, 436,
		455, 436, 575, 372, 651, 500, 3, 586, 586, 0,
		586, 500, 31, 540, 399, 71, 540, 183, 79, 204,
		540, 276, 67, 372, 91, 602, 183, 260, 315, 116,
		643, 87, 436, 307, 602, 460, 163, 372, 639, 95,
		436, 99, 491, 324, 318, 638, 638, 638, 283, 483,
		638, 295, 483, 356, 564, 19, 988, 292, 476, 596,
		367, 724, 367, 788, 367, 860, 20, 359, 347, 796,
		379, 20, 379, 367, 602, 379, 400, 860, 144, 532,
		455, 471, 860, 956, 700, 16, 28, 316, 444, 284,
		1020, 124, 188, 732, 420, 46, 92, 46, 80, 964,
		116, 3, 318, 638, 638, 638, 307, 436, 891, 76,
		88, 382, 571, 606, 555, 380, 274, 1011, 606, 571,
		197, 1011, 980, 587, 1011, 24, 450, 603, 130, 583,
		588, 546, 208, 450, 631, 130, 607, 38, 852, 647,
		655, 916, 939, 268, 450, 791, 130, 791, 946, 140,
		946, 910, 910, 910, 910, 910, 908, 274, 146, 731,
		62, 94, 268, 753, 716, 753, 868, 1011, 344, 208,
		386, 67, 604, 988, 38, 116, 551, 946, 140, 450,
		707, 946, 574, 916, 695, 929, 908, 274, 558, 216,
		152, 88, 216, 140, 262, 402, 771, 50, 390, 771,
		262, 905, 908, 905, 308, 43, 130, 67, 272, 2,
		771, 336, 116, 931, 908, 706, 991, 422, 967, 46,
		1011, 106, 934, 418, 1007, 967, 74, 272, 706, 987,
		946, 262, 558, 782, 964, 336, 558, 782, 204, 292,
		676, 318, 638, 327, 574, 408, 394, 75, 554, 618,
		204, 344, 362, 159, 294, 298, 442, 103, 586, 111,
		902, 586, 99, 716, 277, 908, 135, 272, 618, 426,
		127, 914, 664, 716, 255, 426, 171, 260, 294, 298,
		844, 277, 266, 844, 26, 215, 202, 728, 219, 984,
		266, 946, 946, 946, 946, 396, 914, 664, 844, 418,
		475, 984, 208, 208, 255, 678, 344, 726, 774, 446,
		307, 336, 918, 586, 276, 303, 75, 638, 603, 302,
		268, 690, 422, 399, 690, 260, 942, 942, 423, 690,
		908, 418, 447, 984, 383, 690, 780, 690, 422, 375,
		690, 942, 942, 942, 140, 914, 792, 268, 914, 276,
		471, 664, 475, 792, 565, 764, 318, 638, 638, 638,
		511, 508, 523, 638, 523, 252, 988, 852, 539, 636,
		340, 551, 555, 476, 356, 180, 3, 76, 344, 76,
		414, 595, 728, 336, 984, 336, 638, 627, 380, 270,
		286, 335, 638, 675, 641, 683, 60, 188, 942, 942,
		38, 780, 274, 336, 638, 859, 558, 268, 84, 795,
		780, 306, 942, 942, 942, 942, 396, 914, 792, 844,
		914, 984, 716, 660, 767, 664, 771, 984, 76, 418,
		787, 984, 204, 479, 294, 678, 786, 780, 88, 152,
		686, 750, 851, 718, 430, 703, 718, 703, 644, 835,
		638, 991, 302, 929, 942, 396, 914, 728, 844, 914,
		728, 716, 418, 923, 984, 771, 664, 771, 148, 943,
		336, 140, 678, 690, 780, 966, 934, 914, 966, 934,
		914, 710, 336, 638, 638, 638, 35, 335, 0, 0,
		0, 0, 588, 536, 588, 666, 554, 292, 201, 80,
		516, 550, 774, 46, 236, 71, 76, 984, 423, 44,
		167, 76, 984, 442, 99, 319, 588, 664, 644, 588,
		858, 26, 263, 122, 890, 858, 154, 215, 660, 159,
		215, 644, 263, 76, 984, 442, 111, 964, 116, 3,
		52, 311, 52, 319, 122, 208, 946, 154, 211, 272,
		660, 247, 251, 664, 984, 44, 251, 554, 588, 764,
		193, 620, 291, 115, 660, 303, 263, 44, 423, 442,
		99, 588, 152, 538, 263, 94, 351, 126, 126, 836,
		588, 344, 538, 375, 467, 588, 152, 762, 782, 407,
		908, 902, 984, 602, 395, 268, 535, 158, 263, 588,
		344, 538, 263, 286, 76, 344, 286, 900, 698, 762,
		634, 442, 491, 511, 902, 916, 511, 908, 984, 588,
		216, 698, 140, 852, 543, 792, 547, 728, 24, 24,
		984, 44, 555, 554, 588, 764, 193, 620, 639, 826,
		615, 268, 946, 946, 946, 208, 208, 946, 272, 272,
		555, 826, 567, 852, 683, 44, 567, 442, 99, 782,
		268, 535, 236, 567, 782, 268, 543, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 204, 558,
		318, 446, 27, 336, 606, 39, 336, 638, 350, 446,
		487, 26, 71, 38, 42, 280, 344, 472, 216, 24,
		280, 536, 394, 115, 135, 998, 74, 394, 111, 390,
		143, 604, 270, 50, 558, 262, 902, 437, 396, 405,
		524, 461, 844, 461, 461, 6, 255, 140, 437, 418,
		255, 686, 558, 396, 408, 396, 526, 303, 216, 88,
		311, 562, 686, 558, 396, 344, 600, 396, 526, 303,
		216, 152, 311, 216, 24, 718, 908, 686, 558, 216,
		24, 344, 408, 686, 140, 461, 461, 908, 437, 910,
		140, 274, 286, 270, 916, 455, 932, 52, 1011, 686,
		216, 408, 344, 152, 344, 686, 336, 686, 558, 88,
		718, 336, 66, 758, 459, 726, 272, 950, 336, 280,
		154, 535, 394, 535, 614, 716, 546, 286, 270, 298,
		604, 878, 46, 746, 524, 838, 1006, 618, 743, 761,
		272, 272, 761, 558, 204, 216, 746, 442, 611, 627,
		270, 942, 270, 558, 826, 639, 618, 618, 787, 76,
		280, 766, 446, 671, 683, 606, 606, 703, 558, 826,
		891, 1006, 891, 558, 826, 811, 204, 290, 462, 1006,
		716, 909, 863, 208, 108, 559, 761, 579, 306, 1010,
		178, 178, 498, 336, 1006, 142, 639, 867, 50, 867,
		780, 274, 14, 887, 274, 716, 558, 216, 208, 386,
		803, 50, 933, 909, 894, 830, 455, 558, 507, 274,
		460, 290, 462, 434, 731, 558, 344, 208, 386, 455,
		34, 208, 558, 578, 462, 208, 336, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 318, 446, 19, 336, 606, 31,
		336, 638, 350, 371, 844, 1010, 716, 2, 71, 844,
		66, 558, 780, 216, 622, 140, 402, 123, 558, 780,
		88, 524, 88, 135, 558, 88, 216, 462, 558, 782,
		908, 333, 524, 341, 341, 341, 430, 183, 195, 622,
		268, 562, 686, 558, 844, 216, 24, 408, 686, 780,
		341, 341, 686, 558, 844, 280, 152, 600, 686, 750,
		942, 942, 998, 998, 998, 268, 562, 50, 326, 204,
		280, 266, 525, 916, 367, 244, 3, 244, 407, 910,
		355, 718, 98, 351, 208, 336, 908, 146, 387, 336,
		562, 204, 344, 262, 266, 588, 208, 106, 942, 446,
		439, 172, 411, 910, 874, 262, 42, 497, 505, 208,
		208, 466, 497, 505, 334, 874, 525, 336, 244, 763,
		338, 1010, 146, 507, 336, 908, 434, 559, 38, 42,
		336, 942, 106, 418, 571, 551, 782, 574, 716, 658,
		718, 562, 446, 607, 615, 910, 74, 262, 336, 0,
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
		318, 350, 51, 446, 503, 977, 148, 43, 164, 479,
		132, 479, 76, 152, 414, 503, 84, 83, 100, 479,
		68, 479, 44, 283, 442, 3, 260, 127, 618, 351,
		292, 318, 350, 503, 446, 503, 977, 929, 241, 844,
		921, 908, 276, 287, 686, 558, 472, 686, 953, 556,
		195, 910, 422, 223, 718, 270, 80, 516, 964, 479,
		558, 204, 280, 362, 294, 275, 902, 618, 426, 267,
		336, 46, 74, 74, 961, 953, 953, 953, 140, 910,
		6, 335, 921, 780, 562, 969, 227, 618, 407, 318,
		350, 503, 446, 503, 980, 391, 503, 977, 524, 66,
		479, 618, 427, 977, 92, 227, 76, 472, 414, 487,
		980, 459, 18, 463, 254, 985, 764, 980, 507, 116,
		3, 76, 152, 382, 523, 988, 588, 554, 52, 311,
		446, 443, 380, 606, 270, 443, 318, 350, 571, 446,
		571, 503, 977, 158, 591, 94, 607, 318, 606, 611,
		126, 945, 318, 350, 627, 227, 62, 94, 227, 540,
		260, 743, 540, 276, 743, 30, 515, 318, 606, 687,
		503, 318, 350, 446, 707, 503, 977, 937, 318, 350,
		735, 94, 227, 62, 227, 905, 468, 503, 84, 767,
		775, 980, 783, 977, 847, 977, 190, 799, 847, 62,
		780, 88, 152, 882, 276, 867, 358, 422, 843, 38,
		882, 913, 76, 280, 270, 227, 134, 879, 843, 614,
		390, 895, 843, 582, 454, 843, 436, 847, 436, 667,
		244, 439, 308, 43, 308, 371, 244, 3, 244, 463,
		244, 407, 308, 527, 52, 519, 116, 567, 0, 0,
		0, 0, 0, 0, 0, 0, 540, 276, 87, 404,
		39, 484, 580, 500, 55, 929, 318, 638, 638, 638,
		71, 126, 83, 638, 83, 94, 284, 156, 372, 227,
		540, 276, 259, 404, 135, 484, 580, 708, 31, 929,
		76, 280, 414, 171, 274, 604, 116, 3, 14, 187,
		30, 159, 558, 780, 274, 134, 155, 326, 956, 228,
		430, 235, 700, 239, 572, 444, 62, 94, 94, 91,
		126, 126, 126, 243, 212, 295, 956, 228, 243, 892,
		196, 243, 540, 276, 403, 404, 343, 484, 708, 772,
		31, 929, 76, 344, 382, 446, 159, 937, 945, 270,
		60, 268, 270, 274, 188, 0, 558, 204, 280, 344,
		270, 268, 932, 60, 188, 562, 262, 953, 91, 540,
		276, 535, 404, 487, 484, 708, 31, 929, 845, 468,
		159, 665, 910, 910, 852, 531, 1020, 535, 316, 828,
		942, 942, 76, 280, 270, 80, 836, 372, 231, 540,
		276, 651, 404, 603, 564, 675, 929, 845, 468, 159,
		665, 910, 910, 270, 268, 60, 274, 124, 76, 216,
		286, 91, 558, 782, 878, 908, 152, 280, 686, 396,
		707, 66, 742, 703, 710, 272, 974, 300, 707, 908,
		434, 751, 336, 670, 734, 843, 798, 574, 910, 204,
		546, 686, 524, 818, 831, 268, 216, 408, 810, 831,
		614, 588, 408, 750, 942, 238, 336, 452, 318, 350,
		446, 875, 484, 336, 318, 606, 871, 867, 212, 915,
		380, 606, 270, 163, 558, 444, 700, 163, 52, 519,
		308, 43, 372, 243, 244, 3, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 540, 276, 23, 372, 547, 484, 55, 540,
		276, 51, 372, 115, 452, 764, 969, 92, 969, 92,
		988, 468, 127, 404, 107, 532, 115, 135, 92, 220,
		204, 732, 867, 404, 139, 92, 878, 220, 566, 938,
		938, 204, 668, 644, 292, 76, 318, 350, 231, 446,
		203, 263, 606, 606, 223, 216, 267, 152, 267, 638,
		243, 215, 350, 255, 263, 350, 3295, 908, 878, 660,
		303, 676, 918, 272, 620, 283, 175, 204, 610, 499,
		614, 367, 418, 343, 638, 407, 615, 638, 638, 638,
		638, 407, 611, 614, 435, 638, 387, 407, 418, 399,
		491, 638, 463, 878, 404, 427, 92, 878, 436, 159,
		638, 455, 614, 611, 407, 638, 471, 644, 491, 638,
		483, 491, 614, 615, 260, 615, 418, 511, 315, 76,
		610, 527, 407, 610, 539, 407, 108, 555, 716, 515,
		212, 615, 76, 152, 766, 446, 615, 614, 614, 615,
		580, 708, 772, 615, 644, 885, 686, 764, 977, 988,
		270, 764, 977, 988, 270, 686, 564, 87, 420, 985,
		660, 707, 276, 707, 1001, 76, 280, 270, 788, 803,
		724, 743, 596, 735, 879, 436, 347, 596, 795, 686,
		60, 268, 786, 718, 1006, 1006, 274, 188, 436, 651,
		436, 607, 724, 835, 596, 827, 436, 139, 436, 491,
		596, 851, 436, 43, 468, 871, 92, 220, 388, 80,
		964, 116, 3, 204, 668, 468, 911, 588, 732, 204,
		420, 548, 610, 935, 336, 610, 947, 963, 516, 610,
		963, 336, 388, 336, 52, 519, 308, 3, 244, 3,
		308, 527, 436, 667, 0, 0, 0, 0, 988, 860,
		52, 367, 596, 3, 724, 3, 788, 3, 380, 606,
		270, 941, 676, 292, 558, 782, 981, 878, 220, 204,
		532, 263, 404, 207, 134, 155, 216, 204, 732, 878,
		949, 92, 388, 516, 436, 159, 617, 490, 478, 175,
		62, 882, 574, 957, 236, 183, 302, 874, 407, 617,
		458, 478, 227, 62, 716, 678, 558, 965, 108, 239,
		74, 910, 407, 617, 404, 279, 254, 602, 90, 394,
		299, 270, 262, 6, 315, 270, 870, 394, 347, 974,
		586, 814, 347, 319, 122, 554, 382, 446, 383, 718,
		74, 910, 407, 518, 399, 254, 686, 798, 750, 973,
		134, 423, 46, 558, 588, 344, 618, 394, 479, 558,
		634, 394, 535, 539, 390, 539, 519, 570, 614, 844,
		280, 618, 394, 539, 586, 394, 467, 266, 262, 604,
		539, 46, 660, 579, 76, 344, 286, 276, 607, 446,
		603, 595, 276, 607, 30, 603, 126, 607, 94, 500,
		667, 908, 190, 635, 158, 643, 62, 651, 62, 126,
		894, 940, 615, 558, 686, 336, 989, 484, 727, 468,
		719, 703, 92, 484, 772, 500, 55, 452, 92, 76,
		152, 414, 759, 76, 472, 414, 687, 468, 775, 484,
		92, 76, 216, 382, 446, 887, 668, 418, 703, 92,
		941, 270, 764, 46, 780, 60, 942, 942, 274, 997,
		878, 988, 270, 981, 644, 260, 772, 580, 87, 92,
		76, 216, 382, 446, 699, 668, 610, 927, 935, 610,
		699, 220, 811, 308, 3, 244, 3, 244, 463, 308,
		343, 308, 527, 500, 887, 52, 519, 308, 371, 0,
		0, 0, 0, 0
	};

	private IContainer components;

	private PictureBox pictureBox1;

	private global::System.Windows.Forms.Timer timer1;

	private TextBox textBoxDisplay;

	private Label labelHP01;

	private Label label2;

	private Label label3;

	private Label label4;

	private PictureBox pictureBoxDash;

	private PictureBox pictureBox2;

	private Label labelModel;

	private TextBox textBox1;

	private void op_unknown()
	{
	}

	private void op_nop()
	{
	}

	public void ACThp01()
	{
		op_fcn00 = new op_fcn[256];
		init_ops01();
		act_a = new byte[14];
		act_b = new byte[14];
		act_c = new byte[14];
		act_y = new byte[14];
		act_z = new byte[14];
		act_t = new byte[14];
		act_m = new byte[14];
		act_dsp = new byte[12];
		act_cl = new byte[12];
		act_sw = new byte[12];
		act_al = new byte[12];
		act_flags = (F)0;
		act_rom = 0;
		act_sp = 0;
		act_key_buf = 0;
		act_pc = 0;
		act_p = 0;
		act_s = 0;
		opcode = 0;
		act_stack = new ushort[2];
		op_clear_reg();
	}

	private void init_ops01()
	{
		for (int i = 0; i < 256; i++)
		{
			op_fcn00[i] = op_unknown;
		}
		for (int i = 0; i < 16; i++)
		{
			op_fcn00[(4 | (i << 6)) >> 2] = op_set_s;
			op_fcn00[(0xC | (i << 6)) >> 2] = op_set_p;
			op_fcn00[(0x14 | (i << 6)) >> 2] = op_test_s_eq_0;
			op_fcn00[(0x18 | (i << 6)) >> 2] = op_load_constant;
			op_fcn00[(0x20 | (i << 6)) >> 2] = op_sel_rom;
			op_fcn00[(0x24 | (i << 6)) >> 2] = op_clr_s;
			op_fcn00[(0x2C | (i << 6)) >> 2] = op_test_p_ne;
			op_fcn00[(0x34 | (i << 6)) >> 2] = op_del_sel_rom;
		}
		op_fcn00[0] = op_nop;
		op_fcn00[4] = op_clr_s17;
		op_fcn00[20] = op_clr_s815;
		op_fcn00[36] = op_gokeys;
		op_fcn00[52] = op_inc_p;
		op_fcn00[68] = op_dec_p;
		op_fcn00[84] = op_return;
		op_fcn00[100] = op_sleep;
		op_fcn00[7] = op_clear_reg;
		op_fcn00[23] = op_cdex;
		op_fcn00[39] = op_mtoc;
		op_fcn00[55] = op_dtoc;
		op_fcn00[71] = op_ctom;
		op_fcn00[119] = op_display_on;
		op_fcn00[135] = op_display_off;
		op_fcn00[151] = op_blink;
		op_fcn00[167] = op_ftoap;
		op_fcn00[183] = op_aptof;
		op_fcn00[199] = op_enscwp;
		op_fcn00[215] = op_dsscwp;
		op_fcn00[247] = op_dsptoa;
		op_fcn00[15] = op_cltoa;
		op_fcn00[31] = op_atoclrs;
		op_fcn00[47] = op_atocl;
		op_fcn00[63] = op_cltodsp;
		op_fcn00[79] = op_atoal;
		op_fcn00[95] = op_swtoa;
		op_fcn00[111] = op_atosw;
		op_fcn00[127] = op_swtodsp;
		op_fcn00[143] = op_swdec;
		op_fcn00[159] = op_altodsp;
		op_fcn00[175] = op_swinc;
		op_fcn00[191] = op_atodsp;
		op_fcn00[207] = op_altoa;
		op_fcn00[223] = op_swstrt;
		op_fcn00[239] = op_swstop;
		op_fcn00[255] = op_altog;
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
			if (b3 >= 10)
			{
				b3 -= 10;
				act_flags |= F.CARRY;
			}
			else
			{
				act_flags &= (F)(-3);
			}
			dest[b] = b3;
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
				b4 += 10;
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
			last = 10;
			break;
		case 2:
			first = 0;
			last = 2;
			break;
		case 3:
			first = 0;
			last = 11;
			break;
		case 4:
			first = 0;
			last = act_p;
			break;
		case 5:
			first = 3;
			last = 11;
			break;
		case 6:
			first = 2;
			last = 2;
			break;
		case 7:
			first = 11;
			last = 11;
			break;
		}
	}

	private void op_arith()
	{
		op_setfield();
		switch ((byte)(opcode >> 5))
		{
		case 0:
			src = act_c;
			dest = null;
			reg_test_nonequal();
			break;
		case 1:
			dest = act_c;
			reg_zero();
			break;
		case 2:
			dest = act_c;
			reg_inc();
			break;
		case 3:
			act_flags |= F.CARRY;
			dest = act_c;
			src = act_c;
			src2 = null;
			reg_sub();
			break;
		case 4:
			src = act_c;
			dest = null;
			reg_test_equal();
			break;
		case 5:
			dest = act_c;
			src = act_c;
			reg_add();
			break;
		case 6:
			dest = act_c;
			src = null;
			src2 = act_c;
			reg_sub();
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
			src = act_c;
			reg_exch();
			break;
		case 9:
			dest = act_a;
			src = act_c;
			reg_copy();
			break;
		case 10:
			dest = act_a;
			src = act_c;
			reg_add();
			break;
		case 11:
			dest = act_a;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 12:
			dest = null;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 13:
			src = act_a;
			dest = null;
			reg_test_equal();
			break;
		case 14:
			dest = act_c;
			src = act_a;
			reg_add();
			break;
		case 15:
			dest = act_c;
			src = act_a;
			src2 = act_c;
			reg_sub();
			break;
		case 16:
			dest = null;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 17:
			dest = act_a;
			reg_zero();
			break;
		case 18:
			dest = act_a;
			reg_inc();
			break;
		case 19:
			act_flags |= F.CARRY;
			dest = act_a;
			src = act_a;
			src2 = null;
			reg_sub();
			break;
		case 20:
			dest = act_b;
			src = act_a;
			reg_copy();
			break;
		case 21:
			dest = act_a;
			src = act_b;
			reg_exch();
			break;
		case 22:
			dest = act_a;
			src = act_b;
			reg_add();
			break;
		case 23:
			dest = act_a;
			src = act_a;
			src2 = act_b;
			reg_sub();
			break;
		case 24:
			dest = act_b;
			reg_zero();
			break;
		case 25:
			src = act_b;
			dest = null;
			reg_test_nonequal();
			break;
		case 26:
			dest = act_c;
			src = act_b;
			reg_copy();
			break;
		case 27:
			dest = act_b;
			src = act_c;
			reg_exch();
			break;
		case 28:
			dest = act_a;
			reg_shift_right();
			break;
		case 29:
			dest = act_a;
			reg_shift_left();
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

	private void op_clear_reg()
	{
		for (byte b = 0; b < 14; b++)
		{
			act_a[b] = (act_b[b] = (act_c[b] = (act_y[b] = (act_z[b] = (act_t[b] = (act_m[b] = 0))))));
		}
	}

	private void op_goto()
	{
		if ((act_flags & F.PREV_CARRY) == 0)
		{
			act_pc = (ushort)((act_rom << 8) | (opcode >> 2));
		}
	}

	private void op_jsb()
	{
		act_stack[act_sp] = act_pc;
		act_sp = (byte)((act_sp + 1) & 1);
		act_pc = (ushort)((act_rom << 8) | (opcode >> 2));
	}

	private void op_return()
	{
		act_sp = (byte)((act_sp - 1) & 1);
		act_pc = act_stack[act_sp];
		act_rom = (byte)(act_pc >> 8);
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

	private void op_clr_s()
	{
		ushort num = (ushort)(1 << (opcode >> 6));
		act_s &= (ushort)(~num);
	}

	private void op_set_s()
	{
		act_s |= (ushort)(1 << (opcode >> 6));
	}

	private void op_del_sel_rom()
	{
		act_rom = (byte)(opcode >> 6);
	}

	private void op_sel_rom()
	{
		act_rom = (byte)(opcode >> 6);
		act_pc = (ushort)((act_rom << 8) | (act_pc & 0xFF));
	}

	private void op_set_p()
	{
		act_p = p_set_map01[opcode >> 6];
	}

	private void op_test_p_ne()
	{
		if (act_p == p_test_map01[opcode >> 6])
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
		if (act_p != 0)
		{
			act_p--;
		}
		else
		{
			act_p = 11;
		}
	}

	private void op_inc_p()
	{
		if (++act_p >= 12)
		{
			act_p = 0;
		}
	}

	private void op_load_constant()
	{
		if (act_p < 12)
		{
			act_a[act_p] = (byte)(opcode >> 6);
		}
		op_dec_p();
	}

	private void op_gokeys()
	{
		act_pc = (ushort)((act_rom << 8) | act_key_buf);
	}

	private void op_clr_s17()
	{
		act_s &= 65280;
	}

	private void op_clr_s815()
	{
		act_s &= 255;
	}

	private void op_display_on()
	{
		act_flags |= F.DISPLAY_ON;
	}

	private void op_display_off()
	{
		act_flags &= (F)(-33);
		hp01_flags &= (F01)(-3);
	}

	private void op_sleep()
	{
		hp01_flags |= F01.SLEEP;
	}

	private void op_blink()
	{
		hp01_flags |= F01.BLINK;
	}

	private void op_enscwp()
	{
		hp01_flags |= F01.SCWP;
	}

	private void op_dsscwp()
	{
		hp01_flags &= (F01)(-5);
	}

	private void op_swinc()
	{
		hp01_flags &= (F01)(-9);
	}

	private void op_swdec()
	{
		hp01_flags |= F01.SWDEC;
	}

	private int GetSWTime()
	{
		int num = 0;
		int num2 = 1;
		for (int i = 0; i < 8; i++)
		{
			num += act_sw[i] * num2;
			num2 = ((i != 3 && i != 5) ? (num2 * 10) : (num2 * 6));
		}
		return num;
	}

	private void op_swstrt()
	{
		SW.Restart();
		SWStartTime = GetSWTime();
		hp01_flags |= F01.SWSTARTED;
	}

	private void op_swstop()
	{
		SW.Stop();
		hp01_flags &= (F01)(-17);
	}

	private void op_altog()
	{
		hp01_flags ^= F01.ALARMACTIV;
	}

	private void op_cdex()
	{
		first = 0;
		last = 11;
		dest = act_c;
		src = act_y;
		reg_exch();
	}

	private void op_mtoc()
	{
		first = 0;
		last = 11;
		dest = act_c;
		src = act_m;
		reg_copy();
	}

	private void op_dtoc()
	{
		first = 0;
		last = 11;
		dest = act_c;
		src = act_y;
		reg_copy();
	}

	private void op_ctom()
	{
		first = 0;
		last = 11;
		dest = act_m;
		src = act_c;
		reg_copy();
	}

	private void op_ftoap()
	{
		act_a[act_p] = act_f;
	}

	private void op_aptof()
	{
		act_f = act_a[act_p];
	}

	private void op_dsptoa()
	{
		first = 0;
		last = 11;
		dest = act_a;
		src = act_dsp;
		reg_copy();
	}

	private void op_cltoa()
	{
		first = 0;
		last = 11;
		dest = act_a;
		src = act_cl;
		reg_copy();
	}

	private void op_atoclrs()
	{
		op_atocl();
		TickCnt = 0;
	}

	private void op_atocl()
	{
		first = 0;
		last = 11;
		dest = act_cl;
		src = act_a;
		reg_copy();
	}

	private void op_cltodsp()
	{
		act_dsp[4] = act_cl[0];
		act_dsp[5] = act_cl[1];
		act_dsp[7] = act_cl[2];
		act_dsp[8] = act_cl[3];
	}

	private void op_atoal()
	{
		first = 0;
		last = 11;
		dest = act_al;
		src = act_a;
		reg_copy();
		hp01_flags ^= F01.ALARMACTIV;
	}

	private void op_swtoa()
	{
		first = 0;
		last = 11;
		dest = act_a;
		src = act_sw;
		reg_copy();
	}

	private void op_atosw()
	{
		first = 0;
		last = 11;
		dest = act_sw;
		src = act_a;
		reg_copy();
		dest = null;
		reg_test_nonequal();
		if ((act_flags & F.CARRY) == 0)
		{
			SW.Reset();
		}
	}

	private void op_swtodsp()
	{
		int num = (((act_sw[6] | act_sw[7]) != 0) ? 2 : 0);
		int num2 = 3;
		int num3 = 0;
		while (num3 < 6)
		{
			act_dsp[num2] = act_sw[num3 + num];
			if ((num3 & 1) != 0)
			{
				num2++;
			}
			num3++;
			num2++;
		}
	}

	private void op_altodsp()
	{
		if ((hp01_flags & F01.ALARMACTIV) != 0)
		{
			if (act_dsp[3] == 10)
			{
				act_dsp[3] = 14;
			}
			else
			{
				act_dsp[3] = 11;
			}
		}
		act_dsp[4] = act_al[0];
		act_dsp[5] = act_al[1];
		act_dsp[7] = act_al[2];
		act_dsp[8] = act_al[3];
	}

	private void op_atodsp()
	{
		first = 0;
		last = 11;
		dest = act_dsp;
		src = act_a;
		reg_copy();
	}

	private void op_altoa()
	{
		first = 0;
		last = 11;
		dest = act_a;
		src = act_al;
		reg_copy();
	}

	private void act_press_key(byte keycode)
	{
		act_key_buf = keycode;
		hp01_flags &= (F01)(-2);
	}

	private void hp01_inc(byte[] dest)
	{
		byte b = 1;
		for (byte b2 = first; b2 <= last; b2++)
		{
			byte b3 = (byte)(dest[b2] + b);
			b = 0;
			if (b3 >= 10)
			{
				b3 = 0;
				b = 1;
			}
			dest[b2] = b3;
		}
	}

	private void hp01_dec(byte[] dest)
	{
		byte b = 1;
		for (byte b2 = first; b2 <= last; b2++)
		{
			sbyte b3 = (sbyte)(dest[b2] - b);
			b = 0;
			if (b3 < 0)
			{
				b3 += 10;
				b = 1;
			}
			dest[b2] = (byte)b3;
		}
	}

	private void hp01_incSW()
	{
		byte b = 1;
		for (byte b2 = 0; b2 < 8; b2++)
		{
			byte b3 = (byte)(act_sw[b2] + b);
			b = 0;
			if (((b2 == 3 || b2 == 5) && b3 >= 6) || b3 >= 10)
			{
				b3 = 0;
				b = 1;
			}
			act_sw[b2] = b3;
		}
	}

	private void hp01_decSW()
	{
		byte b = 1;
		for (byte b2 = 0; b2 < 8; b2++)
		{
			sbyte b3 = (sbyte)(act_sw[b2] - b);
			b = 0;
			if (b3 < 0)
			{
				b3 = (sbyte)((b2 != 3 && b2 != 5) ? 9 : 5);
				b = 1;
			}
			act_sw[b2] = (byte)b3;
		}
		if (b != 0)
		{
			hp01_flags &= (F01)(-9);
			hp01_incSW();
			SystemSounds.Beep.Play();
		}
	}

	private void hp01_inccl()
	{
		byte b = 1;
		for (byte b2 = 0; b2 < 6; b2++)
		{
			byte b3 = (byte)(act_cl[b2] + b);
			b = 0;
			if (((b2 == 1 || b2 == 3) && b3 >= 6) || b3 >= 10)
			{
				if (b2 == 3)
				{
					hp01_flags |= F01.WAKEUP;
				}
				b3 = 0;
				b = 1;
			}
			act_cl[b2] = b3;
		}
		if (act_cl[5] == 2 && act_cl[4] == 4)
		{
			act_cl[5] = (act_cl[4] = 0);
			first = 6;
			last = 11;
			hp01_inc(act_cl);
		}
	}

	private bool hp01_checkalarm()
	{
		for (byte b = 0; b < 6; b++)
		{
			if (act_cl[b] != act_al[b])
			{
				return false;
			}
		}
		return true;
	}

	private void Alarm()
	{
		hp01_flags |= F01.BLINK;
		try
		{
			new SoundPlayer("notify.wav")?.Play();
		}
		catch
		{
			SystemSounds.Beep.Play();
		}
	}

	private bool hp01_clinc()
	{
		bool result = false;
		if ((++TickCnt & 0x1F) == 0)
		{
			string text = DateTime.Now.ToString("hhmmss");
			byte b = Convert.ToByte(text.Substring(5, 1));
			if (b != second)
			{
				second = b;
				hp01_inccl();
				if ((hp01_flags & F01.SCWP) != 0)
				{
					hp01_flags |= F01.WAKEUP;
					result = true;
				}
				if (act_c[11] == 3)
				{
					op_cltodsp();
					result = true;
				}
				if ((hp01_flags & F01.ALARMACTIV) != 0 && hp01_checkalarm())
				{
					hp01_flags &= (F01)(-33);
					Alarm();
				}
			}
		}
		if ((hp01_flags & F01.SWSTARTED) != 0)
		{
			long num = SW.ElapsedMilliseconds / 10;
			num = (((hp01_flags & F01.SWDEC) != 0) ? (SWStartTime - num) : (num + SWStartTime));
			if (num < 0)
			{
				hp01_flags &= (F01)(-9);
				SW.Restart();
				SWStartTime = 0;
				num = 0L;
				Alarm();
			}
			for (int i = 0; i < 8; i++)
			{
				byte b2 = (byte)((i != 3 && i != 5) ? 10 : 6);
				act_sw[i] = (byte)(num % b2);
				num /= b2;
			}
			if (act_c[11] == 2)
			{
				op_swtodsp();
				result = true;
			}
		}
		return result;
	}

	private bool hp01_execute_instruction()
	{
		opcode = Getopcode(act_pc);
		if (opcode == 400 && Getopcode((ushort)(act_pc + 1)) == 268)
		{
			opcode = 0;
		}
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
		if ((byte)act_pc == 0)
		{
			act_rom = (byte)(act_pc >> 8);
		}
		switch (opcode & 3)
		{
		case 0:
			op_fcn00[opcode >> 2]();
			break;
		case 1:
			op_jsb();
			break;
		case 2:
			op_arith();
			break;
		case 3:
			op_goto();
			break;
		}
		return true;
	}

	public HP01()
	{
		InitializeComponent();
		textBoxDisplay.Font = new Font(textBoxDisplay.Font.Name, 14f);
		textBoxDisplay.Font = new Font(textBoxDisplay.Font, FontStyle.Bold);
		textBoxDisplay.BackColor = Color.FromArgb(255, 25, 25, 20);
		textBoxDisplay.Focus();
		ImageTable = new string[2] { "hp01.bmp", "hp01a.bmp" };
		ImageNr = 0;
		pictureBox1.Visible = false;
		pictureBox2.Visible = true;
		ACThp01();
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
			int num = 9;
			text = "   ";
			for (int i = 0; i < num; i++)
			{
				char c = digittab[act_dsp[11 - i]];
				text += c;
			}
			pictureBoxDash.Visible = act_dsp[3] == 14;
		}
		else
		{
			pictureBoxDash.Visible = false;
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

	private bool SingleStep()
	{
		hp01_execute_instruction();
		if ((hp01_flags & F01.SLEEP) != 0)
		{
			if (!ShowTime)
			{
				Stop();
				return false;
			}
			ShowTime = false;
			SetTimeDateFromPC();
			press_key(51);
		}
		return true;
	}

	private void SetTimeDateFromPC()
	{
		string text = DateTime.Now.ToString("HHmmss");
		for (int i = 0; i < 6; i++)
		{
			act_cl[5 - i] = (byte)((byte)text[i] & 0xF);
		}
		int num = (int)DateTime.Now.ToOADate() - 2;
		int num2 = 100000;
		for (int j = 0; j < 6; j++)
		{
			byte b = (byte)(num / num2);
			act_cl[11 - j] = b;
			num -= b * num2;
			num2 /= 10;
		}
	}

	
	public void HeadlessPowerOn()
	{
		running = true;
		ShowDisplay();
	}

	public void HeadlessPowerOff()
	{
		Stop();
		keycode = 0;
		ShowDisplay();
	}

	public void HeadlessPressKey(byte code)
	{
		press_key(code);
	}

	public void HeadlessReleaseKey()
	{
	}

	public void HeadlessSetProgramMode(bool programMode)
	{
	}

	public bool HeadlessProgramMode => false;

	public void HeadlessRunTimerBatch()
	{
		timer1_Tick(this, EventArgs.Empty);
		if (RefreshDisplay)
		{
			RefreshDisplay = false;
			ShowDisplay();
		}
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
			0,
			0);
	private void timer1_Tick(object sender, EventArgs e)
	{
		if ((hp01_flags & F01.SLEEP) != 0 && RefreshDisplay)
		{
			RefreshDisplay = false;
			ShowDisplay();
		}
		if ((hp01_flags & F01.BLINK) != 0 && ++BlinkCnt >= 20)
		{
			BlinkCnt = 0;
			act_flags ^= F.DISPLAY_ON;
			RefreshDisplay = true;
		}
		if ((hp01_flags & F01.WAKEUP) != 0)
		{
			hp01_flags &= (F01)(-65);
			hp01_flags &= (F01)(-2);
			act_key_buf = 63;
			running = true;
		}
		if (DisplayCnt != 0 && --DisplayCnt == 0 && (hp01_flags & F01.DISPLAYON) == 0)
		{
			act_flags &= (F)(-33);
			RefreshDisplay = true;
		}
		if (hp01_clinc())
		{
			RefreshDisplay = true;
		}
		if (!running)
		{
			return;
		}
		for (int i = 0; i < 100; i++)
		{
			if (SingleStep())
			{
				continue;
			}
			if (keycode != 0)
			{
				if ((hp01_flags & F01.DISPLAYON) == 0)
				{
					if (keycode == 52)
					{
						DisplayCnt = 500;
					}
					else
					{
						DisplayCnt = 200;
					}
					if (act_c[11] == 2 || (hp01_flags & F01.SCWP) != 0)
					{
						DisplayCnt = 0;
					}
				}
				keycode = 0;
			}
			RefreshDisplay = true;
			break;
		}
	}

	private void press_key(byte code)
	{
		act_flags &= (F)(-33);
		ShowDisplay();
		keycode = code;
		act_press_key(code);
		Run();
	}

	private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
	{
		if (e.X >= 28 && e.X < 175 && e.Y >= 68 && e.Y < 148)
		{
			int num = (e.Y - 68) / 22;
			int num2 = (e.X - 28) / 22;
			byte code = HP01Keytable[num * 7 + num2];
			press_key(code);
		}
	}

	private void HP01_KeyPress(object sender, KeyPressEventArgs e)
	{
		for (int i = 0; i < 28; i++)
		{
			if (e.KeyChar == HP01KeyChartable[i])
			{
				press_key(HP01Keytable[i]);
				break;
			}
		}
		e.Handled = true;
	}

	private void textBoxDisplay_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		hp01_flags ^= F01.DISPLAYON;
		if ((hp01_flags & F01.DISPLAYON) != 0)
		{
			act_flags |= F.DISPLAY_ON;
		}
		else
		{
			act_flags &= (F)(-33);
		}
		DisplayCnt = 0;
		ShowDisplay();
	}

	private void textBoxDisplay_KeyDown(object sender, KeyEventArgs e)
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HP01));
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.timer1 = new System.Windows.Forms.Timer(this.components);
		this.textBoxDisplay = new System.Windows.Forms.TextBox();
		this.labelHP01 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.label4 = new System.Windows.Forms.Label();
		this.pictureBoxDash = new System.Windows.Forms.PictureBox();
		this.pictureBox2 = new System.Windows.Forms.PictureBox();
		this.labelModel = new System.Windows.Forms.Label();
		this.textBox1 = new System.Windows.Forms.TextBox();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBoxDash).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).BeginInit();
		base.SuspendLayout();
		this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
		this.pictureBox1.Location = new System.Drawing.Point(0, 0);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(200, 173);
		this.pictureBox1.TabIndex = 0;
		this.pictureBox1.TabStop = false;
		this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseDown);
		this.timer1.Enabled = true;
		this.timer1.Interval = 10;
		this.timer1.Tick += new System.EventHandler(timer1_Tick);
		this.textBoxDisplay.BackColor = System.Drawing.Color.FromArgb(64, 0, 0);
		this.textBoxDisplay.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.textBoxDisplay.Cursor = System.Windows.Forms.Cursors.IBeam;
		this.textBoxDisplay.Font = new System.Drawing.Font("Lucida Console", 13f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.textBoxDisplay.ForeColor = System.Drawing.Color.Red;
		this.textBoxDisplay.Location = new System.Drawing.Point(12, 27);
		this.textBoxDisplay.Name = "textBoxDisplay";
		this.textBoxDisplay.ReadOnly = true;
		this.textBoxDisplay.Size = new System.Drawing.Size(174, 18);
		this.textBoxDisplay.TabIndex = 25;
		this.textBoxDisplay.TabStop = false;
		this.textBoxDisplay.Click += new System.EventHandler(textBoxDisplay_Click);
		this.labelHP01.AutoSize = true;
		this.labelHP01.Location = new System.Drawing.Point(249, 29);
		this.labelHP01.Name = "labelHP01";
		this.labelHP01.Size = new System.Drawing.Size(81, 13);
		this.labelHP01.TabIndex = 26;
		this.labelHP01.Text = "HP-01 Emulator";
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
		this.label3.Text = "Version 1.00";
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(236, 94);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(120, 13);
		this.label4.TabIndex = 29;
		this.label4.Text = "Microcode  US4158285";
		this.pictureBoxDash.Image = (System.Drawing.Image)resources.GetObject("pictureBoxDash.Image");
		this.pictureBoxDash.Location = new System.Drawing.Point(147, 38);
		this.pictureBoxDash.Name = "pictureBoxDash";
		this.pictureBoxDash.Size = new System.Drawing.Size(6, 5);
		this.pictureBoxDash.TabIndex = 30;
		this.pictureBoxDash.TabStop = false;
		this.pictureBoxDash.Visible = false;
		this.pictureBox2.Image = (System.Drawing.Image)resources.GetObject("pictureBox2.Image");
		this.pictureBox2.Location = new System.Drawing.Point(0, 0);
		this.pictureBox2.Name = "pictureBox2";
		this.pictureBox2.Size = new System.Drawing.Size(200, 173);
		this.pictureBox2.TabIndex = 31;
		this.pictureBox2.TabStop = false;
		this.pictureBox2.Visible = false;
		this.pictureBox2.MouseDown += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseDown);
		this.labelModel.AutoSize = true;
		this.labelModel.Location = new System.Drawing.Point(254, 130);
		this.labelModel.Name = "labelModel";
		this.labelModel.Size = new System.Drawing.Size(76, 13);
		this.labelModel.TabIndex = 32;
		this.labelModel.Text = "Stainless Steel";
		this.textBox1.Location = new System.Drawing.Point(0, 0);
		this.textBox1.Name = "textBox1";
		this.textBox1.Size = new System.Drawing.Size(200, 20);
		this.textBox1.TabIndex = 33;
		this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP01_KeyPress);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(201, 174);
		base.Controls.Add(this.labelModel);
		base.Controls.Add(this.pictureBoxDash);
		base.Controls.Add(this.label4);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.labelHP01);
		base.Controls.Add(this.textBoxDisplay);
		base.Controls.Add(this.pictureBox1);
		base.Controls.Add(this.pictureBox2);
		base.Controls.Add(this.textBox1);
		this.MaximumSize = new System.Drawing.Size(400, 212);
		this.MinimumSize = new System.Drawing.Size(217, 212);
		base.Name = "HP01";
		this.Text = "HP-01";
		base.KeyPress += new System.Windows.Forms.KeyPressEventHandler(HP01_KeyPress);
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBoxDash).EndInit();
		((System.ComponentModel.ISupportInitialize)this.pictureBox2).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
