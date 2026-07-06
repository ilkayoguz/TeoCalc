using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Panamatik.Calc.HP65;

public class HPClassic : Form
{
	private delegate void op_fcn();

	private const int SSIZE = 16;

	private const int STACK_SIZE = 2;

	private const int WSIZE = 14;

	private const int EXPSIZE = 3;

	private const int BUTTONS = 35;

	private const int RAMSIZE = 448;

	private F act_flags;

	private byte[] src;

	private byte[] dest;

	private byte[] src2;

	private byte first;

	private byte last;

	private byte act_key_buf;

	private byte act_ram_addr;

	private byte act_del_rom;

	private byte act_del_grp;

	private byte act_rom;

	private byte act_grp;

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

	private bool mouseDown;

	private bool transparent;

	private Size OriginalSize;

	private Point lastLocation;

	private int FirstCol = 25;

	private int FirstRow = 127;

	private int LastCol = 192;

	private int LastRow = 436;

	private int RowSize;

	private int ColSize;

	private int FirstCol2;

	private int ColSize2;

	private int SliderY;

	private int SliderLeft;

	private int SliderRight;

	private string[] HPClassicMnemonics = new string[64]
	{
		"NOP", "STO 4", "3", "2", "1", "STO 6", "*", "x!=y?", "g", "RUP",
		"RCL", "STO", "f-1", "RDOWN", "f", "RCL 8", "RCL 7", "X<>Y", "6", "5",
		"4", "RCL 6", "+", "RCL 4", "E", "x=y?", "D", "C", "B", "RCL 5",
		"A", "RCL 3", "RCL 2", "RCL 1", "R/S", ".", "0", "STO 7", "/", "f-1",
		"2", "x>y?", "RTN", "LBL", "GTO", "STO 5", "DSP", "STO 3", "STO 2", "STO 1",
		"9", "8", "7", "STO 8", "-", "x<=y?", "CLX", "", "EEX", "CHS",
		"LSTX", "PTR", "ENTER", "START"
	};

	private char[] HPClassicKeyChartable = new char[40]
	{
		'a', 'b', 'c', 'd', 'e', 'p', 'o', 'l', 'q', 't',
		'f', 'h', 's', 'r', 'g', '\r', '\r', 'n', 'x', '\b',
		'-', '7', '8', '9', '\0', '+', '4', '5', '6', '\0',
		'*', '1', '2', '3', '\0', '/', '0', '.', ' ', '\0'
	};

	private byte[] HPClassicKeytable = new byte[40]
	{
		30, 28, 27, 26, 24, 46, 44, 43, 42, 40,
		14, 12, 11, 10, 8, 62, 62, 59, 58, 56,
		54, 52, 51, 50, 0, 22, 20, 19, 18, 0,
		6, 4, 3, 2, 0, 38, 36, 35, 34, 0
	};

	public ushort[] opcodeint = new ushort[3072]
	{
		773, 187, 0, 0, 667, 179, 973, 866, 866, 815,
		903, 0, 866, 419, 85, 107, 111, 354, 515, 729,
		1011, 115, 487, 123, 760, 855, 1002, 1002, 1002, 1002,
		1002, 1002, 1002, 1002, 212, 863, 903, 175, 532, 571,
		575, 0, 1002, 1002, 1002, 1002, 1002, 1002, 1002, 1002,
		212, 839, 903, 171, 866, 866, 499, 903, 866, 763,
		21, 198, 168, 740, 224, 724, 275, 296, 206, 624,
		99, 866, 299, 129, 866, 1015, 125, 27, 332, 418,
		539, 354, 418, 431, 354, 140, 465, 991, 46, 270,
		60, 812, 359, 942, 624, 942, 48, 866, 51, 117,
		528, 866, 931, 189, 866, 535, 65, 553, 212, 847,
		994, 31, 903, 866, 407, 193, 260, 634, 355, 276,
		495, 553, 992, 272, 577, 866, 287, 133, 354, 831,
		729, 657, 655, 61, 212, 403, 656, 400, 198, 168,
		140, 48, 644, 144, 928, 384, 740, 736, 724, 591,
		736, 48, 268, 418, 315, 553, 212, 871, 994, 219,
		903, 213, 855, 398, 784, 784, 780, 418, 487, 152,
		168, 575, 262, 874, 60, 812, 691, 942, 398, 746,
		692, 407, 198, 168, 780, 398, 760, 942, 752, 48,
		866, 647, 149, 692, 272, 0, 0, 890, 803, 551,
		656, 703, 487, 577, 866, 455, 197, 729, 661, 553,
		12, 469, 752, 272, 272, 553, 12, 469, 740, 224,
		724, 895, 296, 760, 855, 528, 741, 942, 575, 0,
		0, 0, 866, 947, 5, 487, 866, 235, 181, 0,
		0, 64, 3, 132, 398, 912, 0, 332, 354, 71,
		729, 254, 656, 866, 391, 93, 396, 98, 219, 16,
		528, 1018, 567, 692, 144, 614, 303, 319, 422, 467,
		692, 272, 0, 757, 955, 422, 103, 582, 362, 106,
		79, 126, 723, 838, 39, 254, 752, 558, 625, 740,
		96, 724, 579, 692, 144, 0, 51, 506, 676, 340,
		931, 490, 167, 875, 388, 942, 400, 28, 490, 237,
		656, 0, 218, 844, 362, 2, 207, 780, 610, 499,
		262, 362, 249, 276, 475, 686, 478, 614, 735, 254,
		733, 610, 319, 262, 874, 942, 121, 994, 1002, 317,
		460, 418, 3, 656, 895, 228, 288, 76, 168, 740,
		480, 212, 427, 724, 839, 168, 692, 400, 506, 227,
		170, 229, 724, 979, 418, 571, 354, 418, 571, 152,
		692, 16, 372, 271, 400, 942, 398, 614, 411, 742,
		934, 272, 757, 212, 975, 528, 1018, 791, 424, 68,
		260, 324, 195, 198, 453, 0, 0, 400, 692, 16,
		532, 359, 780, 20, 171, 60, 812, 599, 36, 660,
		647, 587, 168, 6, 19, 272, 16, 340, 883, 895,
		784, 784, 0, 663, 558, 206, 780, 354, 354, 624,
		38, 760, 398, 198, 482, 93, 966, 319, 327, 100,
		400, 204, 98, 675, 339, 780, 416, 552, 168, 270,
		48, 168, 795, 400, 692, 811, 757, 928, 212, 823,
		528, 272, 271, 198, 569, 98, 907, 88, 198, 397,
		596, 587, 612, 644, 40, 587, 532, 803, 783, 416,
		52, 579, 354, 354, 98, 1011, 216, 851, 532, 135,
		768, 40, 168, 270, 740, 736, 724, 959, 736, 272,
		418, 1019, 354, 418, 831, 354, 98, 551, 482, 482,
		168, 859, 651, 963, 656, 656, 656, 963, 803, 656,
		927, 907, 927, 927, 927, 567, 927, 963, 963, 843,
		656, 656, 656, 963, 807, 963, 935, 656, 935, 935,
		935, 963, 935, 963, 963, 963, 263, 743, 656, 963,
		799, 144, 927, 656, 927, 927, 927, 963, 927, 963,
		963, 963, 656, 656, 656, 963, 811, 656, 627, 0,
		767, 1015, 16, 0, 159, 168, 575, 198, 692, 168,
		296, 639, 947, 658, 441, 579, 84, 663, 468, 355,
		938, 718, 76, 402, 447, 52, 48, 0, 0, 654,
		558, 494, 12, 98, 423, 60, 274, 367, 168, 718,
		468, 607, 938, 250, 938, 447, 656, 366, 558, 236,
		987, 754, 726, 414, 468, 463, 42, 580, 480, 160,
		144, 532, 335, 52, 516, 48, 477, 144, 442, 883,
		506, 442, 871, 506, 442, 939, 198, 202, 358, 362,
		586, 52, 731, 168, 919, 144, 477, 204, 656, 52,
		516, 644, 575, 254, 414, 480, 580, 507, 198, 168,
		206, 224, 477, 883, 198, 168, 643, 740, 224, 724,
		683, 296, 206, 780, 370, 510, 510, 558, 206, 148,
		351, 355, 596, 591, 416, 643, 144, 102, 739, 132,
		84, 147, 787, 102, 159, 452, 84, 19, 168, 718,
		445, 1002, 1002, 1002, 477, 422, 419, 528, 208, 0,
		416, 507, 168, 424, 296, 942, 160, 477, 507, 426,
		939, 378, 102, 895, 206, 940, 575, 16, 168, 808,
		808, 808, 859, 528, 863, 528, 206, 557, 84, 559,
		224, 559, 16, 270, 68, 443, 64, 3, 84, 967,
		148, 287, 28, 34, 441, 422, 387, 159, 362, 362,
		398, 137, 144, 243, 0, 0, 784, 676, 780, 206,
		88, 344, 472, 24, 472, 600, 408, 216, 152, 472,
		780, 48, 784, 276, 99, 692, 144, 866, 3, 362,
		829, 661, 41, 657, 891, 362, 825, 657, 355, 521,
		692, 272, 784, 1018, 519, 427, 398, 84, 179, 969,
		468, 387, 911, 0, 398, 784, 969, 911, 644, 780,
		661, 404, 699, 420, 424, 296, 942, 260, 435, 669,
		866, 863, 362, 825, 661, 37, 659, 965, 657, 760,
		549, 692, 144, 0, 866, 151, 692, 144, 521, 855,
		490, 829, 676, 663, 669, 866, 119, 891, 0, 206,
		482, 468, 35, 784, 254, 388, 692, 144, 484, 1018,
		183, 435, 521, 692, 272, 784, 404, 747, 823, 362,
		28, 106, 595, 210, 938, 394, 543, 144, 780, 398,
		442, 491, 206, 48, 254, 656, 731, 1018, 19, 431,
		167, 144, 490, 490, 398, 1013, 172, 487, 503, 692,
		144, 1018, 559, 669, 866, 347, 490, 825, 657, 41,
		661, 355, 784, 784, 942, 168, 394, 168, 12, 100,
		644, 48, 276, 287, 292, 644, 424, 780, 549, 355,
		521, 692, 16, 545, 665, 866, 995, 404, 855, 692,
		144, 661, 424, 296, 657, 607, 459, 521, 16, 16,
		657, 692, 144, 144, 644, 398, 780, 206, 354, 48,
		319, 272, 272, 866, 371, 855, 656, 660, 875, 48,
		398, 46, 780, 676, 68, 506, 951, 426, 951, 206,
		12, 344, 780, 458, 407, 468, 471, 942, 227, 398,
		558, 354, 624, 558, 46, 48, 866, 579, 490, 829,
		657, 41, 661, 759, 527, 0, 656, 656, 656, 623,
		527, 527, 439, 527, 443, 447, 451, 527, 455, 672,
		144, 527, 656, 656, 656, 0, 527, 707, 527, 527,
		527, 527, 527, 0, 527, 940, 975, 241, 527, 527,
		656, 16, 527, 48, 463, 527, 467, 471, 475, 0,
		479, 895, 692, 16, 656, 656, 656, 0, 527, 527,
		102, 375, 527, 527, 527, 37, 527, 812, 299, 29,
		596, 779, 256, 510, 510, 52, 644, 1017, 876, 127,
		890, 599, 69, 0, 482, 532, 895, 168, 52, 516,
		575, 740, 736, 724, 355, 736, 48, 204, 418, 611,
		384, 349, 384, 757, 911, 716, 98, 703, 652, 98,
		963, 656, 639, 28, 28, 28, 28, 28, 198, 28,
		28, 28, 28, 556, 791, 761, 580, 768, 212, 67,
		911, 684, 747, 101, 931, 757, 212, 63, 907, 0,
		256, 52, 516, 510, 198, 168, 349, 144, 620, 323,
		212, 643, 895, 890, 247, 53, 692, 272, 160, 768,
		349, 768, 349, 272, 532, 875, 446, 547, 382, 446,
		883, 691, 640, 349, 928, 157, 640, 349, 883, 640,
		349, 963, 212, 731, 384, 349, 928, 620, 511, 165,
		748, 255, 221, 198, 168, 718, 414, 48, 640, 283,
		623, 422, 579, 358, 422, 715, 198, 992, 579, 208,
		332, 418, 195, 198, 266, 266, 482, 482, 890, 855,
		168, 575, 596, 675, 214, 168, 144, 761, 212, 575,
		128, 349, 416, 575, 757, 907, 64, 3, 736, 422,
		999, 652, 418, 987, 198, 168, 151, 1004, 811, 1,
		716, 98, 703, 214, 532, 267, 256, 52, 516, 707,
		0, 767, 1002, 1002, 1002, 147, 535, 912, 961, 846,
		760, 622, 859, 783, 750, 994, 46, 784, 1002, 1002,
		1002, 11, 619, 1018, 611, 335, 961, 846, 760, 622,
		783, 859, 1018, 323, 68, 583, 1002, 283, 543, 1018,
		411, 331, 953, 254, 621, 760, 195, 400, 630, 247,
		783, 1002, 1002, 75, 615, 791, 953, 942, 254, 621,
		760, 1022, 859, 783, 1018, 707, 324, 715, 692, 144,
		422, 891, 272, 272, 1018, 367, 16, 794, 844, 803,
		1018, 95, 580, 324, 139, 198, 168, 443, 725, 400,
		191, 1018, 811, 719, 890, 851, 424, 296, 942, 388,
		132, 31, 1018, 439, 335, 729, 64, 3, 559, 400,
		46, 890, 843, 224, 724, 471, 296, 784, 890, 491,
		908, 803, 890, 911, 799, 396, 418, 275, 725, 1018,
		299, 16, 1019, 204, 170, 424, 67, 943, 623, 460,
		418, 503, 725, 1018, 259, 46, 189, 266, 266, 98,
		475, 16, 400, 254, 424, 46, 1018, 1018, 506, 506,
		74, 655, 942, 934, 422, 671, 942, 550, 74, 763,
		654, 1002, 14, 763, 675, 0, 1018, 131, 388, 580,
		403, 198, 168, 46, 558, 624, 558, 752, 780, 48,
		784, 52, 206, 324, 831, 528, 16, 725, 400, 972,
		528, 867, 452, 1018, 159, 139, 528, 272, 623, 1019,
		733, 787, 222, 272, 272, 272, 890, 379, 59, 276,
		855, 48, 168, 212, 295, 528, 0, 140, 558, 344,
		558, 538, 311, 212, 343, 198, 168, 903, 100, 164,
		168, 46, 558, 382, 590, 624, 142, 424, 752, 942,
		296, 260, 48, 621, 908, 855, 574, 975, 814, 193,
		760, 942, 193, 760, 942, 596, 51, 942, 340, 107,
		126, 71, 420, 222, 665, 752, 661, 609, 181, 760,
		400, 942, 665, 468, 875, 750, 994, 294, 934, 362,
		658, 442, 135, 722, 490, 151, 718, 654, 752, 558,
		283, 558, 268, 891, 752, 942, 418, 215, 174, 398,
		138, 815, 0, 400, 340, 119, 254, 958, 79, 658,
		894, 255, 510, 818, 466, 814, 302, 850, 259, 942,
		760, 590, 958, 814, 274, 752, 1022, 1022, 175, 206,
		42, 726, 713, 866, 760, 268, 657, 396, 621, 524,
		621, 140, 536, 652, 621, 569, 621, 817, 270, 621,
		142, 813, 817, 686, 468, 471, 596, 471, 942, 254,
		617, 817, 686, 400, 817, 686, 686, 597, 686, 941,
		817, 652, 625, 569, 524, 629, 140, 536, 396, 625,
		268, 625, 625, 814, 590, 844, 344, 1011, 396, 536,
		408, 344, 152, 280, 600, 84, 875, 48, 750, 994,
		656, 912, 270, 662, 558, 647, 510, 782, 643, 910,
		912, 912, 330, 912, 482, 846, 675, 974, 270, 28,
		594, 44, 679, 215, 482, 790, 715, 918, 278, 28,
		44, 719, 215, 28, 918, 879, 656, 378, 378, 746,
		862, 638, 795, 912, 518, 811, 254, 814, 782, 912,
		206, 716, 472, 536, 344, 216, 600, 536, 88, 408,
		216, 344, 780, 48, 400, 906, 891, 354, 510, 44,
		751, 938, 746, 98, 923, 718, 590, 554, 202, 780,
		699, 912, 658, 658, 382, 947, 466, 786, 562, 142,
		894, 955, 958, 760, 942, 30, 11, 270, 946, 752,
		658, 382, 656, 830, 1022, 598, 274, 75, 424, 665,
		398, 404, 267, 750, 838, 3, 718, 382, 3, 510,
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
		350, 687, 190, 806, 750, 812, 791, 102, 731, 84,
		3, 692, 16, 879, 562, 934, 784, 420, 408, 600,
		216, 88, 280, 472, 88, 923, 998, 403, 910, 354,
		787, 718, 60, 876, 791, 490, 766, 780, 46, 610,
		859, 270, 362, 622, 831, 206, 298, 910, 638, 799,
		934, 398, 46, 780, 491, 588, 216, 88, 24, 88,
		472, 600, 536, 24, 344, 344, 216, 887, 942, 302,
		390, 698, 379, 506, 718, 490, 971, 415, 206, 780,
		152, 216, 24, 152, 344, 519, 332, 507, 0, 198,
		168, 564, 528, 0, 98, 559, 396, 98, 551, 83,
		446, 647, 1015, 608, 724, 563, 544, 7, 564, 16,
		123, 864, 724, 563, 800, 7, 608, 563, 98, 179,
		396, 98, 63, 83, 544, 563, 98, 447, 396, 98,
		95, 83, 608, 724, 7, 544, 563, 0, 740, 460,
		64, 3, 98, 147, 396, 98, 115, 83, 2, 259,
		658, 1002, 48, 552, 394, 168, 714, 714, 12, 102,
		299, 202, 558, 206, 366, 510, 510, 510, 558, 352,
		48, 28, 874, 539, 750, 402, 966, 375, 1022, 726,
		1002, 298, 48, 206, 510, 486, 590, 398, 490, 144,
		0, 34, 890, 634, 615, 746, 874, 298, 839, 864,
		724, 7, 800, 563, 261, 970, 729, 634, 979, 844,
		28, 662, 874, 967, 745, 423, 646, 426, 995, 987,
		0, 0, 172, 335, 347, 864, 563, 800, 198, 168,
		564, 144, 261, 729, 890, 618, 783, 38, 662, 398,
		835, 28, 172, 415, 766, 1022, 1022, 318, 599, 746,
		626, 1015, 748, 763, 942, 750, 994, 174, 403, 942,
		590, 510, 780, 879, 210, 370, 218, 564, 912, 707,
		1002, 780, 339, 51, 168, 394, 168, 48, 684, 383,
		270, 716, 667, 1018, 662, 797, 34, 60, 812, 795,
		938, 442, 831, 170, 378, 938, 42, 414, 340, 871,
		52, 324, 564, 144, 0, 52, 859, 310, 974, 883,
		846, 270, 974, 899, 1022, 814, 241, 716, 241, 142,
		50, 654, 814, 918, 699, 942, 554, 490, 401, 236,
		491, 627, 745, 554, 710, 490, 890, 515, 662, 554,
		431, 144, 168, 859, 718, 934, 723, 564, 400, 596,
		115, 267, 0, 68, 452, 644, 388, 398, 446, 71,
		260, 808, 296, 942, 614, 227, 210, 482, 225, 168,
		931, 0, 272, 296, 206, 624, 780, 760, 942, 46,
		100, 644, 767, 552, 928, 669, 928, 669, 750, 212,
		231, 352, 768, 673, 673, 673, 673, 555, 896, 675,
		661, 673, 673, 352, 480, 740, 352, 724, 511, 963,
		718, 414, 564, 272, 780, 954, 354, 418, 315, 76,
		216, 323, 76, 88, 198, 168, 923, 378, 400, 739,
		676, 398, 564, 656, 577, 814, 998, 314, 890, 910,
		746, 857, 857, 746, 709, 851, 403, 352, 736, 96,
		564, 528, 851, 571, 577, 861, 46, 818, 814, 270,
		869, 709, 206, 216, 408, 15, 506, 106, 335, 206,
		337, 96, 724, 991, 416, 247, 532, 23, 267, 96,
		724, 543, 416, 740, 96, 724, 939, 559, 398, 550,
		202, 12, 344, 842, 634, 899, 1019, 424, 296, 766,
		760, 446, 643, 894, 942, 404, 491, 254, 489, 564,
		144, 640, 60, 812, 675, 48, 362, 270, 638, 3,
		691, 614, 695, 206, 398, 780, 46, 48, 890, 890,
		890, 347, 378, 849, 400, 564, 400, 398, 100, 424,
		296, 942, 398, 780, 564, 400, 102, 103, 763, 752,
		206, 624, 292, 760, 564, 400, 564, 272, 46, 818,
		654, 652, 910, 28, 300, 875, 48, 0, 654, 1002,
		899, 750, 396, 48, 564, 272, 564, 4, 480, 740,
		352, 724, 543, 416, 750, 1002, 618, 967, 768, 673,
		415, 1002, 1002, 247, 217, 288, 736, 352, 564, 912,
		0, 874, 298, 198, 486, 209, 780, 379, 127, 640,
		135, 718, 477, 127, 127, 0, 255, 127, 0, 127,
		127, 564, 528, 0, 127, 0, 127, 127, 127, 1019,
		127, 128, 928, 740, 736, 724, 139, 736, 48, 12,
		152, 771, 127, 127, 206, 506, 586, 74, 1015, 487,
		127, 874, 981, 618, 207, 934, 806, 631, 579, 703,
		896, 87, 283, 460, 64, 3, 0, 0, 244, 531,
		98, 771, 396, 98, 771, 204, 98, 459, 479, 746,
		626, 1015, 12, 866, 74, 999, 1015, 694, 694, 694,
		598, 490, 28, 874, 758, 802, 470, 618, 351, 158,
		406, 144, 0, 0, 482, 870, 419, 144, 742, 210,
		618, 7, 806, 423, 12, 88, 771, 319, 0, 564,
		16, 746, 626, 1015, 511, 710, 28, 172, 503, 806,
		958, 780, 486, 981, 390, 870, 518, 435, 1002, 74,
		1015, 535, 564, 144, 98, 667, 396, 98, 667, 204,
		98, 159, 479, 0, 460, 679, 482, 774, 627, 918,
		490, 294, 810, 390, 28, 443, 937, 398, 969, 98,
		243, 396, 98, 243, 479, 98, 751, 396, 98, 751,
		204, 418, 47, 12, 24, 771, 0, 925, 296, 296,
		296, 558, 198, 168, 571, 936, 288, 416, 52, 140,
		152, 152, 88, 736, 928, 173, 121, 33, 81, 169,
		173, 113, 57, 201, 169, 173, 109, 33, 77, 169,
		173, 105, 53, 169, 173, 97, 69, 169, 37, 937,
		959, 168, 206, 168, 780, 46, 558, 48, 942, 624,
		942, 752, 994, 955, 767, 678, 678, 678, 48, 394,
		206, 310, 25, 144, 564, 591, 267, 279, 903, 907,
		911, 291, 967, 303, 651, 319, 975, 487, 159, 355,
		367, 375, 387, 399, 891, 895, 899, 331, 495, 503,
		515, 523, 675, 543, 551, 559, 571, 579, 591, 603,
		615, 623, 915, 631, 643, 216, 907, 659, 667, 531,
		783, 691, 311, 703, 715, 727, 879, 883, 887, 1007,
		759, 775, 683, 0, 1019, 791, 867, 0, 799, 260,
		913, 915, 260, 649, 911, 260, 485, 899, 260, 485,
		891, 260, 649, 152, 911, 260, 649, 879, 260, 973,
		891, 983, 564, 144, 260, 649, 883, 216, 911, 260,
		973, 883, 260, 973, 887, 260, 649, 887, 552, 352,
		398, 814, 324, 120, 12, 610, 451, 356, 750, 878,
		262, 262, 206, 268, 292, 64, 3, 216, 903, 408,
		911, 260, 973, 899, 88, 895, 260, 649, 152, 903,
		0, 88, 903, 88, 907, 260, 973, 895, 88, 911,
		260, 973, 903, 260, 973, 907, 260, 973, 911, 536,
		899, 536, 903, 260, 485, 887, 536, 911, 216, 895,
		260, 649, 152, 899, 88, 899, 280, 899, 260, 485,
		895, 260, 485, 903, 260, 485, 907, 260, 485, 911,
		890, 942, 296, 942, 144, 344, 911, 168, 347, 260,
		649, 152, 907, 280, 907, 280, 911, 340, 847, 378,
		762, 740, 352, 724, 847, 382, 766, 942, 558, 52,
		564, 144, 260, 649, 915, 486, 486, 486, 486, 486,
		486, 486, 486, 486, 276, 807, 292, 942, 262, 262,
		262, 460, 24, 24, 942, 268, 48, 472, 911, 216,
		899, 424, 1018, 618, 739, 750, 741, 260, 485, 883,
		280, 903
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
			op_fcn0[32 + (i << 6)] = op_set_f;
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
			op_fcn0[936] = op_clear_regs;
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
			act_pc = (ushort)(((act_grp << 11) | (act_rom << 8) | (opcode >> 2)) + over);
			over = 0;
			act_rom = act_del_rom;
			act_grp = act_del_grp;
		}
	}

	private void op_jsb()
	{
		act_stack[0] = act_pc;
		act_rom = act_del_rom;
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | (opcode >> 2));
		if ((act_f & 0x80) != 0)
		{
			buffer = act_pc & 0x3F;
			act_f &= 127;
		}
	}

	private void op_return()
	{
		act_pc = act_stack[0];
	}

	private void op_clear_s()
	{
		act_s = 0;
	}

	private void op_del_sel_rom()
	{
		act_del_rom = (byte)(opcode >> 7);
	}

	private void op_del_sel_grp()
	{
		act_del_grp = (byte)((opcode >> 7) & 1);
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
		act_grp = act_del_grp;
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
		for (byte b = 0; b < 7; b++)
		{
			byte b2 = act_ram[act_ram_addr * 14 / 2 + b];
			act_c[b * 2] = (byte)(b2 & 0xF);
			act_c[b * 2 + 1] = (byte)(b2 >> 4);
		}
	}

	private int pointerpos()
	{
		int num = ((memlen == 103) ? 57 : 61);
		int i;
		for (i = 1; i < memlen && act_ram[112 + i] != num; i++)
		{
		}
		if (i == memlen)
		{
			return 1;
		}
		return i;
	}

	private int label_pos(int n, int n2)
	{
		while (n < memlen && (act_ram[112 + n - 1] != 43 || act_ram[112 + n] != n2))
		{
			n++;
		}
		if (n == memlen)
		{
			n = 1;
			while (n < memlen && (act_ram[112 + n - 1] != 43 || act_ram[112 + n] != n2))
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
		buffer = act_ram[112 + num - 1];
		endstate = 0;
		if (act_ram[112 + memlen - 1] != 0)
		{
			endstate = 1;
		}
		if (num == memlen - 1)
		{
			endstate = 2;
		}
		busy = n;
	}

	private void insert_at(int n, int n2)
	{
		while (n < memlen)
		{
			int num = act_ram[112 + n];
			act_ram[112 + n] = (byte)n2;
			n2 = num;
			n++;
		}
	}

	private void delete_at(int n)
	{
		for (n++; n < memlen; n++)
		{
			act_ram[112 + n - 1] = act_ram[112 + n];
		}
		act_ram[112 + n - 1] = 0;
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
		int n = act_ram[112 + num];
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
		int num2 = act_ram[112 + num];
		if (num < memlen - 1)
		{
			act_ram[112 + num] = act_ram[112 + num + 1];
			act_ram[112 + num + 1] = (byte)num2;
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
		act_ram[112] = 63;
		act_ram[113] = 61;
		for (int i = 2; i < memlen; i++)
		{
			act_ram[112 + i] = 0;
		}
		cleanup(7);
	}

	private void op_sel_rom()
	{
		act_rom = (byte)(opcode >> 7);
		act_grp = act_del_grp;
		act_del_rom = act_rom;
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | (byte)act_pc);
	}

	private void op_keys_to_rom_addr()
	{
		act_pc = (ushort)((act_grp << 11) | (act_rom << 8) | act_key_buf);
		if ((act_f & 0x80) != 0)
		{
			buffer = act_pc & 0x3F;
			act_f &= 127;
		}
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

	private void op_set_s()
	{
		act_s |= (ushort)(1 << (opcode >> 6));
		if (opcode >> 6 == 0)
		{
			act_grp = act_del_grp;
		}
	}

	private void op_set_f()
	{
		int num = opcode >> 7;
		if ((opcode & 0x40) == 0)
		{
			act_f |= (byte)(1 << num);
			return;
		}
		if ((act_f & (1 << num)) != 0)
		{
			act_s |= 2048;
			act_f &= (byte)(~(1 << num));
		}
		if (num == 5)
		{
			act_s |= 2048;
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

	private void op_clr_s()
	{
		ushort num = (ushort)(1 << (opcode >> 6));
		act_s &= (ushort)(~num);
	}

	private void op_a_to_rom_addr()
	{
		act_pc &= 65280;
		handle_del_rom();
		act_pc += (ushort)((act_a[2] << 4) + act_a[1]);
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
			for (byte b = 0; b < 7; b++)
			{
				byte b2 = act_ram[addr * 14 / 2 + b];
				act_c[b * 2] = (byte)(b2 & 0xF);
				act_c[b * 2 + 1] = (byte)(b2 >> 4);
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
				act_ram[addr * 14 / 2 + b] = (byte)(act_c[b * 2] | (act_c[b * 2 + 1] << 4));
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

	private void op_clear_regs()
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
		ushort pc = (ushort)((act_grp << 11) | (act_rom << 8) | (byte)act_pc);
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
		act_del_grp = 0;
		act_rom = 0;
		act_grp = 0;
		act_base = 10;
		act_sp = 0;
		act_key_buf = 0;
		act_pc = 0;
		act_p = 0;
		act_s = 0;
		op_clear_regs();
		act_clear_memory();
	}

	public HPClassic()
	{
		InitializeComponent();
		CalculateKeyboard();
		Text = "HP-65";
		labelHPType.Text = "HP-65 Emulator";
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
		OriginalSize = pictureBox1.Size;
		Size size;
		try
		{
			ReadKeyboardFile("hp65.kml");
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
		ACTClassic();
		op_memoryinitialize();
	}

	private void CalculateKeyboard()
	{
		RowSize = (LastRow - FirstRow) / 7;
		ColSize = (LastCol - FirstCol) / 4;
		ColSize2 = (LastCol - FirstCol) * 4 / 11;
		FirstCol2 = LastCol - 3 * ColSize2;
		SliderY = FirstRow - RowSize * 3 / 2;
		SliderLeft = FirstCol + ColSize / 2;
		SliderRight = LastCol - ColSize / 2;
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

	private void Reset()
	{
		act_reset();
		op_memoryinitialize();
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
				if (endstate == 2 && prgmmode && i == 0)
				{
					text += '-';
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
			act_grp);

	public void TraceKey7Scenario(string outputPath)
	{
		TraceDisplayScenarios(outputPath);
	}

	public void TraceDisplayScenarios(string outputPath)
	{
		StringBuilder trace = new StringBuilder();
		int eventIndex = 0;
		trace.AppendLine("event\tsequence\tphase\ttick\tkey\tvisible_display\tdisplay_on\ta13_0\tb13_0\tpc\tflags\tstatus\tp\tkey_buffer\tendstate\tprgmmode\ttimermode\tbuttonpressed");

		TraceScenario("digit-1", new byte[] { 4 }, settleTicks: 40);
		TraceScenario("digits-1234567890", new byte[] { 4, 3, 2, 20, 19, 18, 52, 51, 50, 36 }, settleTicks: 40);
		TraceScenario("digits-1234567890-enter", new byte[] { 4, 3, 2, 20, 19, 18, 52, 51, 50, 36, 62 }, settleTicks: 40);
		TraceScenario("one-enter-zero-divide", new byte[] { 4, 62, 36, 38 }, settleTicks: 120);

		string directory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(outputPath, trace.ToString());

		void TraceScenario(string sequence, byte[] keys, int settleTicks)
		{
			Reset();
			running = true;
			prgmmode = false;
				buttonpressed = false;
			ShowDisplay();
			Log(sequence, "reset", 0, "-");
			RunTicks(sequence, "warmup", "-", 60);
			for (int i = 0; i < keys.Length; i++)
			{
				byte key = keys[i];
				string keyName = KeyName(key);
				press_key(key);
				buttonpressed = true;
				Log(sequence, "keydown", 0, keyName);
				RunTicks(sequence, "keyheld", keyName, 30);
				buttonpressed = false;
				Log(sequence, "keyup", 0, keyName);
				RunTicks(sequence, "keyup-ticks", keyName, 5);
				RunTicks(sequence, "post-key", keyName, 25);
			}
			RunTicks(sequence, "settle", "-", settleTicks);
		}

		void RunTicks(string sequence, string phase, string key, int ticks)
		{
			for (int tick = 1; tick <= ticks; tick++)
			{
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
				Log(sequence, phase, tick, key);
			}
		}

		void Log(string sequence, string phase, int tick, string key)
		{
			trace.Append(eventIndex++);
			trace.Append('\t');
			trace.Append(sequence);
			trace.Append('\t');
			trace.Append(phase);
			trace.Append('\t');
			trace.Append(tick);
			trace.Append('\t');
			trace.Append(key);
			trace.Append('\t');
			trace.Append(Escape(textBoxDisplay.Text.Replace(';', '.')));
			trace.Append('\t');
			trace.Append(((act_flags & F.DISPLAY_ON) != 0) ? "1" : "0");
			trace.Append('\t');
			trace.Append(RegisterText(act_a));
			trace.Append('\t');
			trace.Append(RegisterText(act_b));
			trace.Append('\t');
			trace.Append(act_pc.ToString("X4"));
			trace.Append('\t');
			trace.Append(((byte)act_flags).ToString("X2"));
			trace.Append('\t');
			trace.Append(act_s.ToString("X3"));
			trace.Append('\t');
			trace.Append(act_p.ToString("X1"));
			trace.Append('\t');
			trace.Append(act_key_buf.ToString("X2"));
			trace.Append('\t');
			trace.Append(endstate);
			trace.Append('\t');
			trace.Append(prgmmode ? "1" : "0");
			trace.Append('\t');
			trace.Append(timermode ? "1" : "0");
			trace.Append('\t');
			trace.Append(buttonpressed ? "1" : "0");
			trace.AppendLine();
		}

		string RegisterText(byte[] register)
		{
			StringBuilder builder = new StringBuilder(14);
			for (int index = 13; index >= 0; index--)
			{
				builder.Append(register[index].ToString("X1"));
			}
			return builder.ToString();
		}

		string Escape(string text)
		{
			return text.Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
		}

		string KeyName(byte key)
		{
			switch (key)
			{
			case 2:
				return "3";
			case 3:
				return "2";
			case 4:
				return "1";
			case 18:
				return "6";
			case 19:
				return "5";
			case 20:
				return "4";
			case 36:
				return "0";
			case 38:
				return "/";
			case 50:
				return "9";
			case 51:
				return "8";
			case 52:
				return "7";
			case 62:
				return "ENTER";
			default:
				return key.ToString("X2");
			}
		}
	}

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
		act_flags &= (F)(-33);
		ShowDisplay();
		act_press_key(code);
		Run();
	}

	private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
	{
		int num = e.X;
		int num2 = e.Y;
		if (num >= pictureBox1.Width / 10 && num < pictureBox1.Width * 9 / 10 && num2 >= SliderY - RowSize / 2 && num2 < SliderY + RowSize / 4)
		{
			if (num < (SliderLeft + SliderRight) / 2)
			{
				if (num < SliderLeft)
				{
					if (transparent)
					{
						Application.Exit();
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
			else
			{
						if (num < SliderRight)
				{
					prgmmode = true;
				}
				else
				{
					prgmmode = false;
				}
			}
		}
		else if (num2 >= SliderY + RowSize / 4 && num2 < FirstRow - RowSize / 2)
		{
			if (prgmmode)
			{
				buttonSave_Click(sender, e);
			}
			else
			{
				buttonLoad_Click(sender, e);
			}
		}
		else if (num2 >= FirstRow - RowSize / 2 && num2 < FirstRow + 7 * RowSize + RowSize / 2 && num >= FirstCol - ColSize / 2 && num < FirstCol + 4 * ColSize + ColSize / 2)
		{
			int num3 = (num2 - FirstRow + RowSize / 2) / RowSize;
			int num4 = ((num3 > 3) ? ((num - FirstCol2 + ColSize2 / 2) / ColSize2) : ((num - FirstCol + ColSize / 2) / ColSize));
			byte code = HPClassicKeytable[num3 * 5 + num4];
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
		}
		e.Handled = true;
	}

	private void textBoxDisplay_Click(object sender, EventArgs e)
	{
		textBox1.Focus();
		ShowDisplay();
	}

	private int GetProgramCode(string[] s)
	{
		int result = -1;
		for (int i = 0; i < HPClassicMnemonics.Length; i++)
		{
			if (string.Equals(s[0], HPClassicMnemonics[i], StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
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
		return act_ram[112 + j];
	}

	private void SetProgramCode(int j, byte code)
	{
		act_ram[112 + j] = code;
	}

	private void WriteProgram(string FileName)
	{
		int num = 0;
		int num2 = 0;
		FileStream fileStream = File.Open(FileName, FileMode.Create);
		Encoding encoding = new ASCIIEncoding();
		num = 103;
		num2 = 10;
		fileStream.Write(encoding.GetBytes("HP65\r\n\r\n"), 0, 8);
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
			fileStream.Write(encoding.GetBytes("PROGRAM\r\n"), 0, 9);
			for (int i = 1; i < num3; i++)
			{
				int programCode = GetProgramCode(i);
				string s = HPClassicMnemonics[programCode] + "\r\n";
				fileStream.Write(encoding.GetBytes(s), 0, encoding.GetByteCount(s));
			}
			fileStream.Write(encoding.GetBytes("END\r\n"), 0, 5);
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
		num = 100;
		num2 = 10;
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
					array2[num3++] = 63;
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
					num5 = GetProgramCode(array4);
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
		this.textBoxDisplay.BackColor = System.Drawing.Color.FromArgb(20, 0, 0);
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
		this.label2.Text = "(c) PANAMATIK 2017";
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(253, 77);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(66, 13);
		this.label3.TabIndex = 28;
		this.label3.Text = "Version 1.05";
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
		this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(pictureBox1_MouseMove);
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
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		this.MaximumSize = new System.Drawing.Size(400, 500);
		this.MinimumSize = new System.Drawing.Size(236, 500);
		base.Name = "HPClassic";
		this.Text = "HP-";
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
