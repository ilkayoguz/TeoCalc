using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Panamatik.Calc.HP55;

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

	public ushort[] opcodeint = new ushort[3072]
	{
		381, 756, 319, 372, 495, 324, 780, 272, 482, 613,
		780, 344, 24, 940, 47, 942, 270, 780, 152, 216,
		716, 143, 369, 404, 23, 324, 644, 580, 780, 372,
		11, 369, 404, 27, 107, 490, 624, 942, 883, 296,
		324, 865, 507, 100, 164, 780, 46, 784, 302, 739,
		756, 71, 401, 369, 276, 531, 398, 5, 543, 396,
		168, 48, 372, 387, 237, 600, 755, 372, 419, 0,
		401, 468, 299, 296, 593, 543, 398, 206, 780, 354,
		490, 803, 0, 211, 609, 543, 0, 162, 162, 675,
		168, 519, 272, 756, 119, 601, 388, 609, 780, 35,
		656, 356, 260, 424, 793, 201, 195, 168, 396, 482,
		354, 351, 168, 874, 874, 685, 593, 5, 201, 296,
		269, 424, 296, 942, 580, 163, 398, 276, 431, 865,
		13, 907, 126, 1019, 173, 144, 340, 543, 660, 407,
		676, 424, 296, 302, 739, 237, 24, 755, 372, 571,
		52, 936, 206, 490, 780, 404, 635, 482, 716, 624,
		558, 752, 296, 558, 482, 639, 142, 48, 168, 305,
		373, 809, 5, 201, 519, 369, 276, 719, 254, 292,
		324, 644, 0, 558, 692, 272, 237, 88, 168, 372,
		79, 401, 468, 783, 296, 809, 686, 543, 756, 611,
		144, 0, 100, 784, 865, 660, 851, 596, 467, 593,
		5, 0, 296, 612, 269, 543, 196, 740, 516, 48,
		752, 942, 482, 147, 936, 995, 424, 68, 596, 931,
		179, 528, 942, 202, 12, 344, 170, 942, 442, 179,
		74, 975, 179, 398, 276, 819, 500, 567, 652, 88,
		460, 152, 372, 219, 500, 779, 68, 231, 218, 1017,
		0, 354, 755, 385, 532, 735, 142, 743, 372, 527,
		756, 375, 354, 119, 168, 126, 407, 168, 247, 212,
		383, 724, 687, 756, 827, 168, 446, 135, 957, 168,
		354, 195, 657, 495, 84, 59, 942, 168, 524, 186,
		827, 88, 831, 0, 354, 267, 657, 999, 142, 559,
		756, 71, 168, 381, 596, 211, 559, 845, 148, 155,
		500, 987, 354, 319, 657, 945, 961, 168, 218, 506,
		132, 227, 372, 495, 656, 354, 703, 657, 46, 845,
		52, 231, 468, 363, 558, 296, 580, 463, 272, 354,
		23, 548, 228, 740, 48, 0, 656, 382, 446, 855,
		942, 168, 12, 386, 674, 799, 930, 168, 942, 957,
		79, 206, 750, 780, 370, 510, 510, 558, 983, 401,
		369, 845, 424, 942, 296, 362, 362, 313, 547, 424,
		296, 48, 52, 217, 452, 692, 519, 756, 607, 276,
		591, 102, 927, 168, 394, 76, 270, 60, 300, 603,
		946, 780, 418, 639, 358, 168, 927, 724, 839, 699,
		12, 354, 354, 679, 528, 354, 691, 272, 354, 375,
		400, 942, 168, 938, 548, 879, 276, 591, 907, 401,
		961, 168, 692, 272, 354, 111, 945, 148, 971, 49,
		140, 152, 90, 967, 986, 49, 231, 532, 95, 212,
		675, 647, 24, 372, 435, 116, 867, 548, 400, 942,
		168, 274, 12, 386, 942, 168, 942, 401, 305, 533,
		404, 723, 565, 404, 575, 446, 591, 533, 942, 989,
		372, 79, 548, 400, 0, 222, 400, 49, 961, 596,
		347, 452, 231, 372, 419, 168, 404, 11, 218, 506,
		168, 961, 209, 1015, 422, 295, 126, 295, 596, 235,
		132, 398, 660, 243, 750, 838, 718, 382, 912, 756,
		611, 689, 961, 558, 227, 296, 942, 644, 780, 35,
		656, 401, 849, 424, 296, 942, 543, 692, 739, 401,
		373, 849, 424, 638, 311, 614, 95, 11, 206, 780,
		280, 490, 48, 119, 756, 319, 168, 52, 144, 0,
		942, 543, 340, 263, 1012, 39, 0, 912, 142, 254,
		77, 961, 260, 227, 0, 596, 311, 612, 323, 942,
		296, 942, 500, 779, 849, 961, 596, 267, 942, 660,
		515, 250, 398, 756, 427, 100, 164, 579, 689, 525,
		12, 855, 656, 116, 175, 0, 68, 575, 756, 71,
		270, 28, 44, 435, 460, 930, 168, 942, 689, 401,
		79, 0, 0, 756, 607, 68, 379, 942, 116, 159,
		254, 398, 227, 168, 942, 168, 803, 144, 276, 563,
		209, 543, 113, 543, 100, 132, 558, 206, 490, 780,
		152, 84, 611, 88, 624, 148, 807, 760, 84, 803,
		942, 424, 942, 760, 296, 142, 803, 113, 296, 378,
		442, 759, 711, 0, 196, 740, 548, 48, 0, 250,
		442, 739, 378, 122, 743, 378, 209, 206, 482, 69,
		405, 766, 569, 209, 424, 296, 942, 425, 116, 519,
		142, 752, 144, 84, 795, 142, 302, 424, 942, 752,
		296, 814, 655, 0, 400, 386, 525, 401, 373, 558,
		680, 750, 12, 386, 206, 874, 143, 142, 493, 424,
		644, 276, 503, 110, 663, 622, 951, 296, 543, 302,
		500, 547, 400, 814, 340, 547, 185, 69, 206, 780,
		88, 536, 276, 3, 113, 185, 485, 543, 1015, 942,
		780, 88, 490, 624, 168, 942, 168, 401, 168, 378,
		527, 168, 752, 543, 701, 401, 692, 255, 401, 853,
		808, 543, 965, 596, 671, 558, 614, 215, 780, 750,
		206, 994, 370, 152, 215, 487, 354, 482, 807, 168,
		869, 845, 197, 189, 811, 756, 119, 116, 811, 83,
		0, 202, 644, 558, 144, 378, 251, 897, 845, 423,
		168, 430, 779, 901, 189, 423, 0, 923, 750, 12,
		386, 270, 60, 812, 295, 942, 624, 942, 48, 0,
		404, 99, 401, 369, 692, 272, 756, 611, 372, 419,
		272, 84, 387, 612, 148, 695, 543, 0, 656, 378,
		735, 897, 353, 753, 760, 942, 752, 942, 543, 372,
		571, 378, 407, 281, 168, 468, 539, 296, 539, 401,
		369, 853, 430, 779, 750, 780, 994, 189, 543, 378,
		719, 467, 760, 144, 701, 197, 30, 567, 382, 701,
		296, 398, 361, 660, 627, 445, 446, 627, 197, 686,
		638, 623, 382, 353, 942, 398, 168, 396, 482, 354,
		155, 168, 1002, 1002, 177, 468, 687, 558, 296, 580,
		123, 244, 807, 196, 708, 516, 48, 378, 455, 281,
		55, 378, 231, 897, 889, 423, 756, 71, 0, 701,
		361, 445, 168, 780, 194, 168, 52, 324, 223, 168,
		753, 612, 660, 543, 424, 296, 766, 942, 543, 756,
		319, 276, 955, 16, 0, 116, 307, 148, 375, 383,
		756, 607, 168, 398, 760, 942, 752, 48, 853, 965,
		168, 218, 132, 168, 227, 0, 404, 695, 863, 420,
		164, 292, 100, 695, 701, 168, 942, 168, 12, 386,
		942, 270, 60, 748, 48, 617, 313, 424, 605, 296,
		197, 737, 398, 313, 741, 381, 269, 733, 601, 404,
		667, 389, 317, 373, 197, 721, 398, 737, 313, 317,
		197, 729, 398, 733, 313, 424, 601, 741, 269, 296,
		547, 756, 179, 468, 803, 378, 122, 803, 653, 729,
		317, 653, 539, 942, 655, 401, 292, 452, 369, 373,
		27, 276, 299, 292, 653, 741, 750, 994, 839, 756,
		71, 756, 119, 625, 388, 715, 500, 855, 296, 609,
		707, 159, 656, 424, 296, 48, 695, 641, 729, 452,
		839, 617, 653, 737, 260, 839, 272, 372, 495, 126,
		763, 422, 763, 48, 656, 750, 994, 601, 1017, 317,
		373, 197, 729, 398, 313, 741, 269, 725, 601, 317,
		27, 737, 317, 609, 302, 317, 142, 942, 269, 276,
		519, 222, 633, 261, 468, 543, 484, 475, 737, 144,
		197, 737, 398, 729, 313, 741, 269, 721, 601, 468,
		7, 317, 942, 475, 254, 656, 372, 419, 372, 571,
		500, 967, 116, 175, 424, 942, 296, 398, 780, 48,
		276, 911, 475, 228, 740, 516, 48, 401, 289, 296,
		558, 52, 244, 231, 354, 354, 354, 354, 354, 334,
		624, 0, 760, 48, 400, 468, 231, 484, 617, 653,
		721, 942, 424, 831, 373, 484, 644, 641, 725, 942,
		398, 313, 760, 942, 404, 855, 254, 605, 261, 752,
		660, 767, 468, 335, 276, 351, 617, 653, 733, 676,
		823, 389, 468, 999, 484, 83, 677, 401, 369, 373,
		452, 197, 741, 404, 407, 381, 317, 373, 197, 729,
		317, 197, 471, 424, 269, 296, 609, 543, 446, 763,
		366, 236, 19, 35, 468, 31, 28, 594, 746, 558,
		363, 210, 370, 218, 906, 843, 206, 398, 780, 298,
		394, 442, 107, 170, 378, 47, 938, 398, 803, 780,
		46, 100, 164, 884, 667, 686, 803, 533, 148, 495,
		122, 495, 472, 359, 825, 405, 484, 148, 295, 452,
		295, 533, 148, 247, 122, 247, 408, 359, 516, 692,
		144, 168, 405, 525, 369, 424, 317, 523, 0, 533,
		719, 386, 427, 292, 369, 680, 309, 628, 679, 170,
		204, 123, 533, 555, 0, 402, 558, 987, 280, 168,
		244, 231, 272, 76, 596, 939, 660, 343, 274, 12,
		287, 596, 787, 452, 612, 676, 942, 422, 67, 398,
		442, 455, 784, 42, 810, 844, 2, 767, 874, 28,
		172, 703, 815, 168, 405, 369, 430, 547, 424, 117,
		528, 500, 955, 140, 168, 48, 500, 779, 148, 571,
		442, 599, 168, 405, 525, 369, 424, 605, 523, 344,
		359, 254, 100, 164, 780, 46, 1018, 1018, 506, 506,
		74, 655, 942, 934, 422, 671, 942, 550, 74, 763,
		654, 1002, 14, 763, 675, 98, 815, 262, 475, 148,
		735, 442, 355, 168, 405, 525, 369, 424, 609, 523,
		784, 28, 98, 807, 262, 467, 558, 102, 803, 206,
		144, 2, 879, 970, 942, 891, 196, 708, 548, 48,
		168, 194, 168, 71, 84, 143, 784, 780, 859, 1002,
		28, 807, 810, 42, 596, 71, 442, 363, 170, 442,
		363, 825, 413, 523, 558, 580, 468, 963, 484, 296,
		818, 766, 206, 366, 190, 510, 494, 98, 3, 274,
		60, 991, 825, 405, 558, 235, 975, 814, 161, 424,
		161, 424, 596, 39, 942, 340, 75, 222, 665, 296,
		661, 609, 149, 424, 665, 276, 467, 750, 994, 294,
		934, 362, 658, 442, 103, 722, 490, 119, 718, 654,
		296, 558, 263, 558, 268, 891, 296, 942, 418, 183,
		174, 398, 138, 815, 398, 84, 151, 276, 475, 340,
		87, 254, 958, 55, 658, 894, 235, 510, 818, 466,
		814, 302, 850, 239, 424, 718, 946, 814, 274, 296,
		1022, 1022, 143, 206, 42, 726, 713, 354, 424, 942,
		268, 657, 396, 621, 524, 621, 140, 536, 652, 621,
		569, 621, 817, 270, 621, 142, 813, 817, 686, 665,
		596, 435, 254, 609, 817, 686, 661, 500, 567, 170,
		1012, 59, 116, 819, 817, 686, 686, 597, 686, 941,
		817, 652, 625, 569, 524, 629, 140, 536, 396, 625,
		268, 625, 625, 814, 590, 844, 344, 1007, 396, 536,
		408, 344, 152, 280, 600, 84, 875, 48, 750, 994,
		656, 912, 270, 662, 558, 647, 510, 782, 643, 910,
		912, 912, 330, 912, 482, 846, 675, 974, 270, 28,
		594, 44, 679, 183, 482, 790, 715, 918, 278, 28,
		44, 719, 183, 28, 918, 879, 400, 378, 378, 746,
		862, 638, 795, 912, 518, 811, 254, 814, 782, 912,
		206, 716, 472, 536, 344, 216, 600, 536, 88, 408,
		216, 344, 656, 48, 400, 906, 891, 354, 510, 44,
		751, 938, 746, 98, 923, 718, 590, 554, 202, 780,
		699, 912, 658, 658, 382, 947, 466, 786, 562, 142,
		894, 955, 946, 424, 30, 7, 270, 946, 296, 658,
		382, 574, 0, 830, 1022, 598, 274, 75, 424, 665,
		272, 985, 46, 665, 398, 267, 378, 756, 455, 510,
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
		784, 722, 894, 599, 766, 910, 48, 784, 718, 558,
		643, 910, 382, 639, 942, 278, 942, 439, 204, 458,
		350, 687, 190, 806, 750, 812, 791, 102, 731, 210,
		370, 218, 879, 0, 562, 934, 784, 676, 408, 600,
		216, 88, 280, 472, 88, 923, 998, 403, 910, 354,
		787, 718, 60, 876, 791, 490, 766, 780, 46, 610,
		859, 270, 362, 622, 831, 206, 298, 910, 638, 799,
		934, 398, 46, 780, 491, 588, 216, 88, 24, 88,
		472, 600, 536, 24, 344, 344, 216, 887, 942, 302,
		390, 698, 379, 506, 718, 490, 971, 415, 206, 780,
		152, 216, 24, 152, 344, 519, 332, 507, 144, 482,
		482, 482, 482, 0, 482, 267, 168, 99, 482, 482,
		482, 0, 482, 259, 244, 911, 482, 482, 482, 107,
		536, 259, 244, 399, 482, 482, 482, 271, 536, 263,
		0, 482, 7, 135, 271, 0, 536, 267, 324, 3,
		482, 482, 482, 0, 482, 263, 710, 561, 482, 482,
		482, 75, 536, 255, 482, 0, 482, 482, 0, 0,
		482, 506, 506, 506, 506, 76, 212, 699, 394, 168,
		446, 931, 382, 446, 67, 168, 202, 344, 74, 927,
		970, 330, 726, 266, 270, 394, 206, 374, 354, 0,
		890, 332, 24, 152, 766, 558, 716, 168, 194, 36,
		28, 748, 411, 552, 418, 479, 340, 447, 579, 168,
		596, 471, 76, 202, 208, 558, 459, 40, 20, 495,
		407, 194, 482, 228, 740, 512, 340, 935, 28, 748,
		515, 510, 515, 479, 978, 850, 903, 844, 1002, 195,
		731, 0, 222, 168, 52, 452, 596, 611, 548, 395,
		398, 680, 302, 580, 524, 28, 590, 108, 631, 98,
		719, 634, 547, 610, 903, 844, 28, 874, 675, 731,
		612, 603, 586, 144, 532, 483, 579, 612, 194, 780,
		214, 482, 482, 28, 172, 791, 202, 942, 596, 867,
		874, 865, 482, 1011, 819, 362, 743, 210, 370, 942,
		674, 1011, 60, 876, 779, 28, 198, 482, 596, 859,
		726, 874, 490, 1002, 506, 891, 170, 942, 558, 691,
		378, 442, 879, 814, 680, 524, 88, 600, 619, 168,
		144, 724, 951, 969, 400, 212, 707, 532, 999, 483,
		222, 168, 596, 991, 48, 558, 48, 969, 564, 656,
		596, 863, 218, 865, 0, 168, 212, 127, 398, 206,
		486, 268, 340, 63, 356, 978, 942, 168, 247, 850,
		49, 76, 942, 418, 95, 234, 218, 394, 319, 590,
		28, 695, 590, 28, 255, 340, 647, 356, 631, 850,
		887, 780, 210, 370, 268, 719, 0, 202, 108, 207,
		426, 223, 362, 60, 187, 490, 270, 28, 179, 760,
		930, 28, 930, 752, 1021, 969, 760, 108, 115, 487,
		394, 202, 76, 344, 762, 842, 315, 970, 586, 66,
		71, 319, 890, 680, 390, 262, 206, 374, 490, 724,
		355, 202, 362, 168, 222, 168, 16, 222, 420, 292,
		164, 100, 48, 222, 510, 510, 168, 212, 499, 680,
		486, 390, 268, 674, 459, 24, 24, 939, 262, 206,
		366, 218, 762, 890, 369, 724, 963, 267, 385, 596,
		515, 519, 558, 168, 780, 532, 567, 716, 36, 20,
		607, 98, 611, 780, 591, 418, 591, 486, 482, 354,
		535, 194, 168, 116, 595, 194, 40, 168, 596, 667,
		671, 780, 600, 168, 671, 373, 268, 210, 168, 503,
		558, 969, 724, 691, 564, 31, 760, 108, 103, 1021,
		76, 564, 67, 946, 402, 972, 28, 28, 486, 731,
		590, 510, 510, 590, 202, 490, 624, 202, 362, 362,
		362, 942, 266, 762, 270, 270, 942, 382, 382, 382,
		851, 998, 168, 942, 168, 48, 966, 823, 946, 204,
		472, 268, 870, 510, 510, 510, 143, 942, 210, 390,
		987, 168, 634, 931, 266, 718, 168, 486, 168, 714,
		969, 724, 175, 247, 1021, 267, 740, 168, 942, 168,
		206, 268, 946, 102, 859, 946, 708, 835, 0, 272,
		168, 332, 354, 244, 843, 490, 718, 46, 298, 910,
		638, 23, 915, 26, 819, 847, 874, 415, 280, 344,
		216, 344, 600, 152, 216, 472, 227, 874, 67, 88,
		24, 344, 344, 24, 344, 344, 536, 344, 216, 12,
		216, 231, 780, 874, 111, 88, 472, 280, 344, 216,
		152, 600, 152, 344, 152, 362, 362, 564, 967, 874,
		267, 152, 344, 280, 490, 231, 874, 983, 216, 24,
		280, 536, 227, 874, 239, 324, 231, 354, 418, 339,
		600, 339, 516, 88, 244, 523, 0, 0, 780, 126,
		511, 122, 511, 398, 302, 278, 626, 547, 1002, 74,
		559, 506, 691, 874, 295, 280, 280, 280, 536, 152,
		152, 88, 408, 88, 344, 231, 408, 116, 583, 0,
		0, 0, 0, 168, 524, 24, 467, 564, 323, 14,
		831, 780, 138, 803, 564, 16, 874, 379, 511, 206,
		482, 590, 510, 558, 2, 595, 658, 490, 750, 834,
		619, 270, 910, 611, 862, 647, 722, 1006, 490, 910,
		639, 818, 354, 579, 382, 579, 270, 298, 914, 214,
		974, 950, 564, 543, 594, 466, 402, 594, 690, 690,
		338, 276, 783, 48, 874, 171, 452, 519, 780, 418,
		331, 532, 311, 548, 339, 978, 594, 114, 783, 48,
		490, 490, 122, 55, 28, 44, 839, 142, 539, 362,
		819, 206, 134, 276, 923, 60, 60, 705, 28, 28,
		705, 398, 142, 780, 610, 915, 362, 270, 934, 539,
		750, 697, 60, 60, 697, 270, 462, 750, 394, 974,
		142, 638, 23, 915, 0, 216, 472, 536, 344, 280,
		88, 88, 472, 536, 280, 231, 231, 579, 231, 231,
		231, 31, 231, 39, 231, 55, 231, 231, 231, 87,
		231, 404, 7, 387, 243, 503, 511, 103, 231, 600,
		807, 119, 519, 971, 979, 135, 231, 536, 807, 151,
		547, 231, 987, 159, 231, 167, 231, 183, 231, 231,
		231, 199, 231, 472, 807, 215, 95, 127, 191, 667,
		231, 388, 750, 61, 563, 231, 408, 807, 231, 46,
		398, 206, 358, 524, 408, 24, 300, 275, 24, 566,
		150, 510, 598, 490, 638, 223, 634, 935, 874, 907,
		718, 718, 718, 718, 722, 722, 460, 66, 223, 332,
		66, 223, 388, 993, 548, 36, 28, 748, 403, 20,
		527, 532, 399, 208, 585, 302, 194, 624, 942, 760,
		942, 585, 942, 752, 942, 482, 447, 142, 272, 1013,
		679, 344, 807, 280, 807, 216, 807, 516, 740, 724,
		435, 399, 404, 387, 52, 23, 558, 234, 558, 404,
		495, 387, 780, 622, 607, 716, 48, 610, 599, 204,
		274, 274, 270, 270, 1002, 780, 262, 610, 599, 874,
		552, 643, 787, 752, 942, 76, 1010, 963, 268, 994,
		23, 332, 994, 66, 723, 103, 738, 396, 994, 151,
		460, 994, 66, 759, 215, 738, 524, 994, 883, 588,
		994, 893, 468, 847, 716, 52, 208, 624, 942, 404,
		671, 760, 780, 98, 259, 942, 387, 36, 20, 875,
		532, 903, 452, 7, 516, 903, 652, 28, 556, 887,
		7, 899, 278, 874, 223, 339, 630, 935, 227, 726,
		1002, 923, 339, 564, 67, 0, 993, 103, 152, 807,
		88, 807, 24, 807, 588, 610, 1019, 546, 130, 48,
		34, 1015
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
		Text = "HP-55";
		labelHPType.Text = "HP-55 Emulator";
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
		act_ram_size = 30;
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

			if (prgmmode)
			{
				act_s |= 8;
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
			if (prgmmode)
			{
				act_s |= 8;
			}
			if (timermode)
			{
				act_s |= 2048;
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
		if (num2 >= 78 && num2 < 98)
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
			if (num >= 102 && num < 214)
			{
						prgmmode = false;
				if (num >= 150 && num <= 170)
				{
					timermode = true;
				}
				else if (num < 160)
				{
					prgmmode = true;
				}
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
		int num = 0;
		int num2 = 0;
		FileStream fileStream = File.Open(FileName, FileMode.Create);
		Encoding encoding = new ASCIIEncoding();
		num = 49;
		num2 = 8;
		fileStream.Write(encoding.GetBytes("HP55\r\n"), 0, 6);
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
		return false;
	}

	private void buttonLoad_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "hp55 files (*.hp55)|*.hp55|all files (*.*)|*.*";
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
		saveFileDialog.Filter = "hp55 files (*.hp55)|*.hp55|all files (*.*)|*.*";
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
