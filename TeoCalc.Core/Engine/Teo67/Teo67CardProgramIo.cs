using TeoCalc.Core.Engine.Act;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core.Engine.Teo67;

/// <summary>
/// Apply / capture mag-card program + data against ACT program RAM (base 112, 224 steps)
/// and the 26 data registers used by T-67 card packing.
/// </summary>
public static class Teo67CardProgramIo
{
  public const int ProgramCapacity = 224;

  public const int RegisterCount = 26;

  public const int ProgramRamBase = 112;

  /// <summary>Display-format / flag nibbles stored after program+data on card files.</summary>
  public const int ModeRamSciEng = 436;

  public const int ModeRamDigits = 437;

  public const int ModeRamFlagsHi = 435;

  public const int ModeRamFlagsLo = 434;

  private static readonly string[] Mnemonics =
  [
    "R/S", "1/x", "x^2", "SQRT", "%", "E+", "Y^X", "LN", "e^x", "R->P",
    "SIN", "COS", "TAN", "P->R", "RTN", "", "0", "1", "2", "3",
    "4", "5", "6", "7", "8", "9", ".", "ENTER", "CHS", "EEX",
    "/", "", "PAUSE", "N!", "xmean", "s", "%CH", "E-", "ABS", "LOG",
    "10^x", "INT", "SIN-1", "COS-1", "TAN-1", "FRAC", "RND", "", "X<>Y", "RDOWN",
    "CLX", "ENG", "FIX", "-x-", "SCI", "+", "-", "*", "D->R", "R->D",
    "H->H.MS", "H.MS->H", "STO(i)", "RCL(i)", "H.MS+", "SPACE", "STK", "LastX", "W/DATA", "MERGE",
    "X<>I", "RUP", "PI", "DEG", "RAD", "GRD", "P<>S", "CLREG", "REG", "",
    "x!=y?", "x=y?", "x>y?", "x!=0?", "x=0?", "x>0?", "x<0?", "x<=y?", "F0?", "F1?",
    "F2?", "F3?", "ISZ", "ISZ(i)", "DSZ", "DSZ(i)", "DSP 0", "DSP 1", "DSP 2", "DSP 3",
    "DSP 4", "DSP 5", "DSP 6", "DSP 7", "DSP 8", "DSP 9", "CF 0", "CF 1", "CF 2", "CF 3",
    "", "DSP(i)", "RCL 0", "RCL 1", "RCL 2", "RCL 3", "RCL 4", "RCL 5", "RCL 6", "RCL 7",
    "RCL 8", "RCL 9", "RCL A", "RCL B", "RCL C", "RCL D", "RCL E", "RCL I", "STO/0", "STO/1",
    "STO/2", "STO/3", "STO/4", "STO/5", "STO/6", "STO/7", "STO/8", "STO/9", "SF 0", "SF 1",
    "SF 2", "SF 3", "", "STO/(i)", "STO 0", "STO 1", "STO 2", "STO 3", "STO 4", "STO 5",
    "STO 6", "STO 7", "STO 8", "STO 9", "STO A", "STO B", "STO C", "STO D", "STO E", "STO I",
    "STO-0", "STO-1", "STO-2", "STO-3", "STO-4", "STO-5", "STO-6", "STO-7", "STO-8", "STO-9",
    "GSB a", "GSB b", "GSB c", "GSB d", "GSB e", "STO-(i)", "GSB 0", "GSB 1", "GSB 2", "GSB 3",
    "GSB 4", "GSB 5", "GSB 6", "GSB 7", "GSB 8", "GSB 9", "GSB A", "GSB B", "GSB C", "GSB D",
    "GSB E", "GSB(i)", "STO+0", "STO+1", "STO+2", "STO+3", "STO+4", "STO+5", "STO+6", "STO+7",
    "STO+8", "STO+9", "GTO a", "GTO b", "GTO c", "GTO d", "GTO e", "STO+(i)", "GTO 0", "GTO 1",
    "GTO 2", "GTO 3", "GTO 4", "GTO 5", "GTO 6", "GTO 7", "GTO 8", "GTO 9", "GTO A", "GTO B",
    "GTO C", "GTO D", "GTO E", "GTO(i)", "STO*0", "STO*1", "STO*2", "STO*3", "STO*4", "STO*5",
    "STO*6", "STO*7", "STO*8", "STO*9", "LBL a", "LBL b", "LBL c", "LBL d", "LBL e", "STO*(i)",
    "LBL 0", "LBL 1", "LBL 2", "LBL 3", "LBL 4", "LBL 5", "LBL 6", "LBL 7", "LBL 8", "LBL 9",
    "LBL A", "LBL B", "LBL C", "LBL D", "LBL E", "LBL(i)",
  ];

  public static void Export(
    ActCpuState state,
    out byte[] programCodes,
    out double[] registers)
  {
    ArgumentNullException.ThrowIfNull(state);
    programCodes = new byte[ProgramCapacity];
    for (int i = 0; i < 32; i++)
    {
      for (int j = 0; j < 7; j++)
      {
        programCodes[i * 7 + j] = state.Ram[ProgramRamBase + ProgramCapacity - 7 - i * 7 + j];
      }
    }

    registers = new double[RegisterCount];
    for (int i = 0; i < RegisterCount; i++)
    {
      int registerIndex = i >= 16 ? i + 32 : i;
      registers[i] = ClassicDataRegisterCodec.GetRegisterValue(state.Ram, registerIndex);
    }
  }

  public static void Import(
    ActCpuState state,
    IReadOnlyList<byte> programCodes,
    IReadOnlyList<double> registers)
  {
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(programCodes);
    ArgumentNullException.ThrowIfNull(registers);

    byte[] codes = new byte[ProgramCapacity];
    int count = Math.Min(ProgramCapacity, programCodes.Count);
    for (int i = 0; i < count; i++)
    {
      codes[i] = programCodes[i];
    }

    for (int i = 0; i < 32; i++)
    {
      for (int j = 0; j < 7; j++)
      {
        state.Ram[ProgramRamBase + ProgramCapacity - 7 - i * 7 + j] = codes[i * 7 + j];
      }
    }

    int regCount = Math.Min(RegisterCount, registers.Count);
    for (int i = 0; i < regCount; i++)
    {
      int registerIndex = i >= 16 ? i + 32 : i;
      ClassicDataRegisterCodec.SetRegisterValue(state.Ram, registerIndex, registers[i]);
    }

    for (int i = regCount; i < RegisterCount; i++)
    {
      int registerIndex = i >= 16 ? i + 32 : i;
      ClassicDataRegisterCodec.SetRegisterValue(state.Ram, registerIndex, 0d);
    }
  }

  public static Teo67CardMode ExportMode(ActCpuState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    string angle = (state.Status & 1) != 0
      ? "RAD"
      : (state.Status & 0x4000) == 0 ? "DEG" : "GRD";
    byte sciEng = state.Ram[ModeRamSciEng];
    string display = sciEng == 0 ? "SCI" : sciEng == 64 ? "ENG" : "FIX";
    int digits = state.Ram[ModeRamDigits] & 0xF;
    return new Teo67CardMode(
      angle,
      display,
      digits,
      state.Ram[ModeRamFlagsHi],
      state.Ram[ModeRamFlagsLo]);
  }

  public static void ImportMode(ActCpuState state, Teo67CardMode mode)
  {
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(mode);

    state.Status &= 0xBFFE;
    if (string.Equals(mode.Angle, "RAD", StringComparison.OrdinalIgnoreCase))
    {
      state.Status |= 1;
    }
    else if (string.Equals(mode.Angle, "GRD", StringComparison.OrdinalIgnoreCase))
    {
      state.Status |= 0x4000;
    }

    byte sciEng = string.Equals(mode.Display, "SCI", StringComparison.OrdinalIgnoreCase)
      ? (byte)0
      : string.Equals(mode.Display, "ENG", StringComparison.OrdinalIgnoreCase)
        ? (byte)64
        : (byte)34;
    state.Ram[ModeRamSciEng] = sciEng;
    state.Ram[ModeRamDigits] = (byte)((state.Ram[ModeRamDigits] & 0xF0) | (mode.Digits & 0xF));
    state.Ram[ModeRamFlagsHi] = mode.FlagsHi;
    state.Ram[ModeRamFlagsLo] = mode.FlagsLo;
  }

  public static string FormatMnemonic(byte code)
  {
    if (code < Mnemonics.Length && !string.IsNullOrEmpty(Mnemonics[code]))
    {
      return Mnemonics[code];
    }

    return $"#{code}";
  }

  public static byte? ResolveMnemonic(string mnemonic)
  {
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return null;
    }

    if (mnemonic.StartsWith('#')
        && byte.TryParse(mnemonic.AsSpan(1), out byte numeric))
    {
      return numeric;
    }

    string[] parts = mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    byte? grouped = TryResolveGrouped(parts);
    if (grouped is not null)
    {
      return grouped;
    }

    for (int i = 0; i < Mnemonics.Length; i++)
    {
      if (string.Equals(Mnemonics[i], mnemonic, StringComparison.OrdinalIgnoreCase)
          || (parts.Length == 1
              && string.Equals(Mnemonics[i], parts[0], StringComparison.OrdinalIgnoreCase)))
      {
        if ((i == 92 || i == 94)
            && parts.Length > 1
            && parts[1] == "(i)")
        {
          return (byte)(i + 1);
        }

        return (byte)i;
      }
    }

    return null;
  }

  private static byte? TryResolveGrouped(string[] parts)
  {
    if (parts.Length < 2)
    {
      return null;
    }

    // Prefer exact table hits for multi-token mnemonics already listed (e.g. "DSP 0", "STO 1").
    string joined = string.Join(' ', parts);
    for (int i = 0; i < Mnemonics.Length; i++)
    {
      if (string.Equals(Mnemonics[i], joined, StringComparison.OrdinalIgnoreCase))
      {
        return (byte)i;
      }
    }

    return null;
  }
}

/// <summary>Optional MODE line payload for card files.</summary>
public sealed record Teo67CardMode(
  string Angle,
  string Display,
  int Digits,
  byte FlagsHi,
  byte FlagsLo);
