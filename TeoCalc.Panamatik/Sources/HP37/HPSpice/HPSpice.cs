using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Panamatik.Calc.HP37;

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

	public ushort[] opcodeint = new ushort[3072]
	{
		200, 430, 430, 968, 572, 145, 482, 624, 688, 282,
		624, 380, 856, 688, 552, 8, 72, 76, 805, 392,
		282, 624, 456, 156, 179, 692, 931, 28, 16, 72,
		4, 71, 0, 0, 0, 816, 482, 624, 688, 528,
		26, 904, 182, 264, 328, 508, 274, 400, 590, 191,
		356, 63, 874, 77, 622, 235, 14, 327, 228, 61,
		400, 498, 219, 328, 274, 572, 746, 69, 646, 956,
		186, 454, 454, 454, 580, 380, 371, 14, 954, 494,
		315, 58, 102, 90, 314, 862, 95, 886, 63, 850,
		1017, 178, 272, 582, 528, 6, 858, 89, 400, 90,
		327, 388, 468, 180, 462, 462, 456, 973, 636, 418,
		459, 454, 464, 439, 578, 484, 180, 612, 122, 404,
		122, 434, 122, 400, 400, 582, 90, 336, 508, 958,
		178, 776, 862, 174, 26, 154, 90, 687, 456, 250,
		282, 604, 145, 956, 408, 146, 178, 508, 722, 153,
		280, 700, 400, 626, 611, 482, 164, 525, 464, 164,
		525, 464, 164, 525, 464, 482, 482, 623, 758, 180,
		690, 12, 392, 723, 834, 135, 590, 470, 699, 161,
		148, 25, 553, 14, 780, 776, 200, 136, 972, 980,
		188, 988, 191, 200, 492, 256, 456, 284, 27, 692,
		507, 508, 758, 254, 776, 746, 212, 622, 490, 618,
		855, 494, 528, 362, 871, 700, 1019, 262, 614, 266,
		140, 700, 528, 188, 282, 140, 272, 634, 58, 464,
		490, 108, 229, 920, 664, 664, 792, 664, 154, 474,
		474, 474, 735, 272, 468, 255, 20, 249, 712, 588,
		452, 30, 598, 76, 282, 528, 564, 3, 408, 600,
		216, 88, 280, 472, 88, 536, 24, 344, 344, 614,
		508, 528, 58, 86, 264, 328, 270, 50, 274, 508,
		264, 572, 400, 426, 490, 172, 282, 250, 762, 322,
		264, 90, 762, 322, 814, 390, 530, 850, 300, 227,
		666, 178, 814, 312, 264, 154, 474, 154, 264, 90,
		590, 90, 814, 322, 430, 1018, 146, 178, 400, 620,
		312, 26, 154, 328, 250, 314, 618, 618, 618, 154,
		264, 328, 754, 336, 430, 1018, 154, 508, 838, 360,
		250, 154, 508, 186, 366, 411, 502, 411, 206, 494,
		482, 210, 90, 886, 357, 282, 18, 528, 206, 383,
		834, 340, 622, 454, 419, 58, 86, 264, 328, 270,
		50, 274, 264, 398, 562, 487, 658, 26, 264, 700,
		464, 954, 515, 314, 610, 511, 172, 380, 328, 295,
		264, 90, 814, 395, 155, 154, 264, 794, 402, 264,
		154, 163, 264, 154, 264, 90, 163, 58, 86, 264,
		328, 270, 50, 274, 758, 223, 264, 558, 562, 663,
		658, 264, 154, 90, 826, 430, 264, 474, 622, 264,
		508, 282, 715, 482, 538, 711, 314, 474, 400, 620,
		434, 154, 328, 339, 58, 246, 154, 282, 264, 282,
		508, 88, 250, 154, 264, 154, 627, 282, 690, 508,
		88, 75, 282, 815, 26, 508, 904, 228, 498, 400,
		590, 847, 186, 334, 975, 174, 464, 612, 254, 430,
		883, 927, 400, 228, 498, 590, 907, 58, 102, 310,
		963, 18, 434, 494, 954, 400, 6, 150, 528, 174,
		911, 250, 264, 568, 182, 118, 982, 982, 982, 252,
		230, 238, 552, 376, 250, 440, 360, 504, 424, 150,
		1018, 1018, 1018, 242, 264, 250, 528, 376, 146, 508,
		230, 360, 568, 956, 454, 454, 454, 134, 142, 552,
		187, 440, 488, 376, 424, 218, 146, 178, 360, 568,
		150, 316, 454, 454, 454, 150, 552, 134, 934, 934,
		934, 528, 396, 524, 50, 154, 186, 366, 387, 850,
		614, 154, 265, 508, 154, 286, 186, 351, 90, 122,
		264, 328, 954, 858, 588, 328, 154, 528, 494, 283,
		508, 418, 90, 116, 675, 420, 611, 400, 498, 494,
		335, 90, 587, 58, 246, 154, 388, 207, 116, 825,
		503, 90, 250, 915, 516, 247, 508, 218, 610, 154,
		858, 623, 855, 834, 628, 622, 474, 447, 264, 325,
		247, 58, 246, 154, 388, 726, 1007, 524, 50, 850,
		223, 726, 223, 142, 174, 750, 616, 334, 559, 686,
		516, 30, 158, 26, 508, 518, 610, 482, 122, 264,
		328, 607, 954, 626, 603, 328, 314, 594, 583, 498,
		90, 474, 400, 684, 658, 154, 122, 454, 454, 454,
		154, 828, 472, 654, 718, 739, 764, 954, 250, 244,
		441, 250, 965, 758, 718, 620, 687, 58, 828, 98,
		314, 954, 250, 244, 377, 532, 712, 90, 538, 90,
		314, 90, 636, 965, 758, 718, 954, 807, 850, 745,
		622, 286, 540, 725, 690, 116, 325, 412, 758, 116,
		981, 116, 457, 328, 754, 785, 590, 250, 891, 954,
		244, 761, 90, 764, 711, 954, 839, 58, 246, 154,
		396, 503, 314, 610, 963, 258, 494, 464, 528, 764,
		216, 216, 216, 24, 536, 216, 344, 892, 528, 216,
		88, 24, 88, 472, 600, 536, 24, 280, 216, 152,
		316, 528, 58, 246, 154, 516, 850, 789, 524, 30,
		90, 218, 366, 155, 218, 850, 809, 700, 400, 676,
		917, 494, 123, 441, 250, 559, 377, 764, 187, 90,
		430, 986, 87, 502, 538, 183, 314, 474, 622, 247,
		956, 738, 827, 610, 866, 834, 482, 508, 591, 154,
		470, 154, 738, 814, 282, 508, 614, 186, 380, 88,
		540, 843, 686, 90, 186, 92, 853, 180, 117, 116,
		805, 180, 61, 90, 116, 343, 380, 216, 216, 216,
		188, 528, 508, 152, 216, 24, 152, 344, 536, 344,
		24, 600, 152, 600, 600, 280, 250, 528, 282, 164,
		258, 630, 280, 502, 868, 769, 292, 995, 100, 759,
		932, 898, 420, 856, 828, 216, 764, 528, 124, 216,
		216, 216, 216, 88, 252, 528, 482, 538, 555, 314,
		420, 918, 474, 622, 400, 250, 143, 250, 92, 923,
		761, 154, 90, 700, 408, 956, 758, 980, 464, 738,
		936, 610, 122, 264, 328, 739, 494, 954, 626, 635,
		1018, 1018, 1018, 418, 90, 186, 540, 845, 116, 765,
		311, 986, 626, 735, 314, 434, 328, 647, 264, 328,
		122, 250, 378, 378, 410, 90, 1018, 890, 972, 328,
		154, 528, 430, 795, 666, 274, 264, 494, 116, 663,
		90, 286, 186, 540, 988, 690, 180, 265, 92, 992,
		180, 117, 116, 825, 343, 252, 216, 216, 24, 536,
		344, 216, 88, 408, 536, 444, 528, 116, 981, 758,
		223, 328, 882, 223, 26, 58, 311, 498, 400, 954,
		52, 367, 0, 78, 455, 1007, 83, 383, 644, 269,
		345, 180, 365, 184, 717, 165, 248, 717, 73, 257,
		296, 131, 116, 983, 265, 345, 180, 365, 312, 717,
		165, 248, 717, 73, 257, 168, 690, 500, 261, 52,
		805, 752, 72, 52, 75, 180, 61, 345, 660, 47,
		690, 758, 52, 116, 613, 235, 282, 624, 56, 58,
		246, 154, 732, 838, 337, 345, 717, 73, 116, 87,
		652, 204, 780, 844, 908, 708, 220, 75, 716, 282,
		624, 56, 660, 81, 690, 58, 246, 154, 180, 119,
		120, 622, 622, 754, 838, 874, 838, 380, 735, 265,
		345, 180, 365, 312, 717, 184, 116, 73, 165, 758,
		93, 441, 232, 131, 73, 116, 787, 265, 184, 601,
		629, 754, 130, 758, 130, 644, 312, 690, 601, 629,
		758, 130, 882, 93, 180, 201, 668, 137, 154, 690,
		154, 337, 345, 58, 246, 154, 180, 201, 441, 264,
		282, 624, 328, 135, 154, 345, 116, 437, 337, 248,
		223, 73, 257, 337, 184, 154, 312, 116, 65, 73,
		660, 171, 264, 690, 264, 758, 93, 116, 625, 264,
		345, 758, 146, 116, 447, 816, 956, 52, 899, 124,
		735, 508, 186, 874, 206, 470, 400, 854, 203, 262,
		142, 264, 270, 622, 146, 116, 327, 622, 767, 783,
		282, 264, 154, 528, 58, 246, 154, 436, 895, 282,
		344, 626, 502, 828, 216, 654, 116, 73, 754, 93,
		282, 88, 40, 282, 624, 56, 758, 93, 116, 753,
		337, 372, 617, 690, 178, 337, 372, 657, 726, 93,
		441, 180, 489, 436, 623, 265, 372, 593, 56, 882,
		93, 717, 758, 229, 593, 754, 215, 717, 653, 613,
		488, 440, 861, 690, 853, 360, 440, 690, 154, 504,
		685, 758, 93, 737, 758, 671, 754, 285, 900, 593,
		769, 360, 440, 726, 93, 741, 882, 299, 758, 299,
		869, 104, 195, 772, 657, 726, 93, 737, 392, 136,
		20, 309, 712, 593, 360, 605, 376, 754, 568, 850,
		319, 146, 399, 729, 917, 408, 709, 56, 709, 717,
		653, 857, 488, 613, 857, 788, 334, 424, 504, 729,
		690, 488, 882, 355, 917, 56, 116, 753, 56, 690,
		861, 917, 216, 709, 825, 440, 796, 379, 709, 796,
		380, 882, 360, 392, 456, 869, 104, 360, 593, 68,
		901, 120, 601, 116, 981, 726, 489, 753, 947, 690,
		186, 456, 687, 741, 882, 383, 869, 360, 617, 424,
		248, 690, 861, 440, 741, 392, 376, 477, 376, 850,
		403, 120, 477, 456, 850, 403, 120, 1019, 917, 56,
		690, 500, 479, 424, 916, 422, 312, 732, 425, 186,
		58, 86, 528, 360, 916, 412, 184, 732, 415, 154,
		248, 116, 67, 180, 119, 116, 439, 116, 615, 248,
		731, 690, 116, 447, 376, 264, 328, 270, 116, 787,
		308, 855, 360, 617, 424, 657, 440, 725, 882, 93,
		488, 376, 637, 917, 152, 709, 717, 360, 729, 504,
		861, 761, 376, 178, 376, 116, 75, 360, 76, 917,
		152, 186, 56, 116, 605, 693, 440, 180, 479, 282,
		624, 508, 528, 593, 690, 178, 456, 725, 769, 653,
		737, 456, 729, 120, 709, 360, 593, 690, 178, 116,
		765, 693, 440, 909, 637, 68, 227, 882, 563, 874,
		563, 186, 122, 478, 838, 561, 430, 782, 527, 490,
		219, 282, 482, 1018, 498, 250, 706, 536, 966, 494,
		26, 898, 123, 474, 314, 115, 914, 151, 934, 442,
		494, 314, 143, 70, 610, 83, 626, 83, 474, 110,
		286, 294, 346, 158, 219, 590, 27, 188, 52, 899,
		52, 67, 721, 136, 713, 721, 737, 180, 489, 721,
		372, 617, 729, 116, 457, 689, 721, 789, 721, 821,
		789, 758, 592, 116, 625, 335, 713, 690, 178, 721,
		701, 789, 813, 721, 372, 657, 729, 813, 721, 789,
		758, 663, 721, 713, 789, 813, 721, 737, 821, 729,
		726, 674, 693, 701, 721, 372, 657, 282, 624, 56,
		705, 729, 813, 689, 796, 636, 721, 713, 729, 813,
		882, 722, 282, 508, 88, 88, 142, 174, 366, 871,
		186, 122, 524, 801, 116, 625, 690, 178, 729, 813,
		746, 661, 490, 874, 667, 540, 568, 924, 667, 116,
		765, 821, 690, 494, 494, 104, 308, 143, 713, 56,
		690, 705, 282, 508, 152, 116, 613, 431, 737, 116,
		787, 248, 116, 447, 372, 595, 180, 119, 116, 983,
		440, 252, 250, 264, 568, 400, 1018, 236, 701, 242,
		154, 264, 528, 504, 316, 747, 376, 956, 747, 116,
		87, 146, 690, 146, 116, 827, 270, 516, 572, 88,
		558, 551, 547, 846, 722, 182, 822, 646, 547, 50,
		188, 850, 183, 218, 90, 378, 378, 410, 250, 286,
		186, 378, 662, 366, 398, 828, 866, 755, 986, 1018,
		142, 282, 90, 700, 344, 1018, 500, 27, 0, 0,
		0, 0, 482, 922, 3, 346, 474, 400, 998, 748,
		769, 258, 90, 116, 323, 888, 213, 257, 712, 760,
		213, 52, 67, 649, 557, 161, 225, 565, 241, 585,
		360, 649, 557, 601, 225, 565, 241, 585, 257, 712,
		376, 79, 760, 573, 557, 824, 186, 696, 577, 565,
		264, 690, 264, 116, 87, 58, 246, 154, 696, 264,
		328, 270, 758, 182, 116, 627, 985, 392, 20, 837,
		712, 456, 528, 621, 557, 456, 154, 696, 577, 888,
		541, 161, 529, 180, 61, 760, 395, 161, 557, 456,
		154, 696, 577, 760, 541, 621, 529, 180, 61, 888,
		116, 445, 565, 205, 565, 241, 225, 360, 161, 557,
		601, 529, 585, 557, 621, 565, 241, 985, 712, 376,
		79, 58, 246, 154, 116, 827, 1016, 58, 246, 154,
		760, 229, 261, 79, 565, 116, 459, 264, 328, 270,
		193, 180, 119, 116, 983, 186, 116, 439, 850, 182,
		308, 855, 888, 573, 557, 952, 179, 760, 154, 888,
		577, 557, 1016, 179, 696, 58, 246, 154, 116, 807,
		972, 204, 980, 936, 360, 758, 185, 882, 185, 308,
		749, 886, 185, 184, 993, 168, 248, 993, 232, 282,
		261, 712, 120, 622, 622, 154, 184, 577, 993, 212,
		975, 154, 282, 624, 56, 154, 854, 975, 282, 424,
		154, 248, 146, 456, 977, 392, 440, 154, 248, 977,
		424, 520, 977, 712, 440, 154, 184, 977, 168, 282,
		624, 56, 477, 40, 376, 653, 360, 980, 1010, 758,
		1010, 136, 754, 958, 456, 79, 116, 65, 52, 807,
		116, 835, 0, 0, 0, 0, 0, 69, 16, 27,
		997, 116, 605, 39, 997, 116, 437, 392, 456, 68,
		72, 4, 155, 430, 852, 185, 788, 218, 724, 129,
		660, 27, 493, 52, 419, 885, 154, 56, 154, 812,
		154, 690, 116, 65, 76, 72, 52, 805, 752, 456,
		612, 470, 52, 75, 282, 483, 430, 430, 430, 430,
		270, 905, 724, 130, 462, 852, 320, 924, 83, 846,
		173, 727, 867, 927, 67, 852, 479, 796, 281, 400,
		400, 400, 780, 644, 52, 743, 852, 348, 628, 67,
		418, 92, 11, 308, 144, 584, 795, 660, 473, 724,
		473, 788, 473, 430, 430, 430, 831, 852, 475, 796,
		329, 299, 860, 46, 52, 39, 852, 422, 628, 335,
		419, 751, 775, 852, 200, 788, 355, 712, 4, 52,
		111, 508, 908, 652, 716, 780, 528, 885, 981, 154,
		795, 300, 34, 116, 605, 147, 716, 900, 307, 493,
		52, 675, 675, 435, 315, 811, 852, 324, 76, 493,
		772, 307, 876, 133, 116, 437, 147, 430, 430, 430,
		359, 852, 286, 796, 328, 295, 724, 138, 493, 836,
		307, 572, 578, 846, 82, 981, 116, 437, 795, 981,
		116, 605, 795, 968, 487, 852, 87, 520, 712, 154,
		795, 852, 237, 468, 141, 690, 52, 67, 500, 675,
		852, 321, 493, 708, 307, 430, 430, 430, 63, 852,
		477, 796, 333, 291, 628, 743, 885, 752, 795, 270,
		622, 1006, 1006, 272, 398, 624, 456, 776, 528, 272,
		852, 159, 493, 52, 407, 282, 40, 104, 168, 232,
		296, 456, 487, 20, 248, 712, 56, 154, 282, 508,
		88, 152, 494, 528, 544, 544, 323, 87, 308, 325,
		460, 648, 456, 850, 288, 211, 520, 116, 605, 251,
		622, 622, 648, 969, 251, 76, 308, 841, 251, 886,
		268, 188, 52, 899, 500, 55, 150, 874, 283, 159,
		750, 283, 622, 478, 854, 292, 878, 308, 146, 178,
		370, 370, 402, 754, 308, 452, 648, 18, 154, 76,
		180, 477, 520, 476, 318, 690, 52, 67, 144, 180,
		941, 251, 76, 244, 57, 251, 690, 520, 116, 65,
		251, 520, 969, 251, 116, 753, 251, 58, 246, 648,
		154, 622, 622, 116, 613, 251, 648, 154, 690, 116,
		65, 154, 343, 272, 8, 494, 392, 508, 282, 392,
		264, 264, 584, 584, 584, 584, 250, 90, 968, 968,
		590, 858, 418, 282, 482, 828, 624, 752, 482, 491,
		624, 154, 56, 154, 922, 858, 418, 482, 507, 508,
		482, 688, 332, 52, 141, 340, 471, 482, 308, 725,
		340, 471, 482, 661, 340, 471, 636, 152, 492, 409,
		250, 536, 492, 413, 52, 955, 508, 270, 863, 816,
		648, 154, 622, 622, 690, 58, 246, 154, 116, 825,
		758, 283, 456, 264, 328, 270, 116, 785, 251, 186,
		760, 901, 456, 965, 824, 909, 648, 888, 901, 648,
		154, 965, 952, 909, 648, 456, 969, 1016, 909, 26,
		434, 954, 696, 901, 392, 456, 540, 120, 764, 392,
		115, 636, 115, 500, 87, 500, 343, 436, 7, 58,
		86, 860, 488, 154, 690, 154, 116, 73, 52, 805,
		620, 495, 516, 752, 528, 186, 116, 439, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 167,
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
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0
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
		Text = "HP-37E";
		labelHPType.Text = "HP-37E Emulator";
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
