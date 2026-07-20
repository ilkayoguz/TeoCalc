using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Act;

namespace TeoCalc.Core.Engine.Hp19;

/// <summary>HP-19C CPU (ACT ISA variant via <see cref="ActCpuBase"/>; Panamatik ACThp19C).</summary>
public sealed class Hp19Cpu : ActCpuBase
{
  private static readonly char[] Digits =
  [
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
    '.', '-', '+', '*', ' ', 'F',
  ];

  private static readonly char[] AlphaChars =
  [
    'N', 'Y', '=', '0', 'L', 'M', '?', '1', 'G', '1',
    '>', '2', 'O', 'H', '?', '3', 'P', '?', 'X', '4',
    'R', 'F', 'Z', '5', 'S', '?', 'x', '6', 'T', '?',
    '#', '7', '%', '?', '?', '8', '$', 'x', '<', '9',
    'A', '$', '$', '.', 'B', '$', '/', '-', 'C', '$',
    '?', '+', 'D', '$', ' ', '*', 'E', 'e', ' ', ' ',
    'I', 'i', 'x', '#',
  ];

  /// <summary>Panamatik <c>DefaultRAM</c> continuous-memory seed at register address 0x2E (offset 322).</summary>
  private static readonly byte[] DefaultRamSeed = [0, 2, 64, 40, 52, 73, 3];

  /// <summary>Panamatik <c>act_switch</c> cold-start value (ON / run position).</summary>
  private byte _powerSwitch = 4;

  private bool _buttonPressed;

  private readonly char[] _printBuffer = new char[32];

  private int _printCharCount;

  private int _motorRunning;

  private readonly List<string> _printLines = [];

  public Hp19Cpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers)
    : base(rom, handlers)
  {
  }

  /// <summary>Emitted printer lines (newest last), matching Panamatik printer strip.</summary>
  public IReadOnlyList<string> PrintLines => _printLines;

  public void ClearPrintLines() =>
    _printLines.Clear();

  public void AppendTestPrint(string line)
  {
    ArgumentNullException.ThrowIfNull(line);
    _printLines.Add(line);
  }

  /// <summary>Queue C-register characters into the print buffer and flush as one line (tests / diagnostics).</summary>
  public void PrintAndFlushCRegister(bool alpha)
  {
    AppendCRegisterToPrintBuffer(alpha);
    FlushPrintBuffer();
  }

  /// <summary>
  /// Panamatik timer motor countdown; call once per instruction slot before <see cref="CpuBase.Step"/>.
  /// When the motor finishes, flushes the print character buffer as one line.
  /// </summary>
  public void TickPrinterMotor()
  {
    if (_motorRunning != 0 && --_motorRunning == 0)
    {
      FlushPrintBuffer();
    }
  }

  public override void Reset()
  {
    base.Reset();
    _powerSwitch = 4;
    _buttonPressed = false;
    SuppressNextStatusPulse = false;
    _printCharCount = 0;
    _motorRunning = 0;
    _printLines.Clear();
    SeedDefaultRam();
  }

  /// <summary>HP-19C has no bank-OR on fetch; status bit 0 remaps high ROM.</summary>
  protected override int ResolveOpcodeFetchAddress()
  {
    int addr = State.ProgramCounter;
    if ((State.Status & 1) != 0 && addr >= 3072)
    {
      addr += 1024;
    }

    return addr;
  }

  protected override ushort TransformFetchedOpcode(ushort opcode) =>
    (ushort)(opcode ^ 3040);

  /// <summary>After XOR 3040, low bits are rotated vs Woodstock (<c>bits ^ 2</c>).</summary>
  protected override string ResolveNormAlias(ushort opcode)
  {
    int remapped = (opcode & ~3) | (((byte)opcode & 3) ^ 2);
    return ResolveStandardNormAlias((ushort)remapped);
  }

  protected override void ApplyBranchTarget(ushort opcode) =>
    State.ProgramCounter = (ushort)((State.ProgramCounter & 0xFC00) | (opcode ^ 2));

  /// <summary>
  /// HP-19C flag layout differs: INCP=0x80 (Woodstock Bank), BANK=0x10 (Woodstock Key).
  /// Panamatik clears INCP one cycle after op_inc_p; otherwise clears PCARRY.
  /// </summary>
  protected override void AfterCycle(ushort opcode)
  {
    _ = opcode;
    if ((State.Flags & ActCpuFlags.Bank) != 0)
    {
      // Clear INCP (mapped onto ActCpuFlags.Bank).
      State.Flags &= ~ActCpuFlags.Bank;
    }
    else
    {
      State.Flags &= ~ActCpuFlags.PCarry;
    }
  }

  protected override void ToggleBank() =>
    State.Flags ^= ActCpuFlags.Key;

  protected override void OnIncP() =>
    // HP-19C F.INCP == 0x80 == ActCpuFlags.Bank bit position.
    State.Flags |= ActCpuFlags.Bank;

  /// <summary>Panamatik HP-19C key→C uses C[2]/C[1] (Woodstock uses C[0]).</summary>
  protected override void LoadKeyBufferIntoC()
  {
    State.Registers.C[2] = (byte)(State.KeyBuffer & 0xF);
    State.Registers.C[1] = (byte)(State.KeyBuffer >> 4);
  }

  /// <summary>Panamatik HP-19C <c>op_keys_to_a</c> reads <c>act_switch</c>, not the key buffer.</summary>
  protected override void LoadKeysToA()
  {
    State.Registers.A[2] = (byte)(_powerSwitch >> 4);
    State.Registers.A[1] = (byte)(_powerSwitch & 0xF);
  }

  /// <summary>Panamatik <c>op_pik_keys</c>: S3 reflects a pending key edge; suppress next timer S3 pulse.</summary>
  protected override void OnPikKeys()
  {
    if (_buttonPressed)
    {
      State.Status |= 8;
      _buttonPressed = false;
    }
    else
    {
      State.Status &= 65527;
    }

    SuppressNextStatusPulse = true;
  }

  /// <summary>Panamatik <c>op_pik_home</c> with motor idle: set S3 and suppress next timer S3 pulse.</summary>
  protected override void OnPikHome()
  {
    if (_motorRunning == 0 || _motorRunning > 900)
    {
      State.Status |= 8;
    }
    else
    {
      State.Status &= 65527;
    }

    SuppressNextStatusPulse = true;
  }

  /// <summary>Panamatik <c>op_pik_cr</c>: start print motor; S3 when buffer has room.</summary>
  protected override void OnPikCr()
  {
    if (_motorRunning == 0)
    {
      _motorRunning = 1000;
    }

    if (_printCharCount < 21)
    {
      State.Status |= 8;
    }
    else
    {
      State.Status &= 65527;
    }

    SuppressNextStatusPulse = true;
  }

  protected override void OnPikPrint(bool alpha)
  {
    AppendCRegisterToPrintBuffer(alpha);
    SuppressNextStatusPulse = true;
  }

  /// <summary>Latch a key-press edge for <c>op_pik_keys</c> (Panamatik <c>buttonpressed</c>).</summary>
  public void NotifyButtonPressed() =>
    _buttonPressed = true;

  private void AppendCRegisterToPrintBuffer(bool alpha)
  {
    if (alpha)
    {
      for (int i = 0; i < 9; i++)
      {
        int wordIndex = i * 6 / 4;
        int low = State.Registers.C[wordIndex] & 0xF;
        int high = State.Registers.C[wordIndex + 1] & 0xF;
        int code = (i & 1) != 0
          ? (low >> 2) | (high << 2)
          : (low | (high << 4)) & 0x3F;
        if (code >= 63 || _printCharCount >= _printBuffer.Length)
        {
          break;
        }

        _printBuffer[_printCharCount++] = AlphaChars[code];
      }

      return;
    }

    for (int i = 0; i < 13; i++)
    {
      int digit = State.Registers.C[i];
      if (digit >= 15 || _printCharCount >= _printBuffer.Length)
      {
        break;
      }

      _printBuffer[_printCharCount++] = Digits[digit];
    }
  }

  private void FlushPrintBuffer()
  {
    char[] chars = new char[_printCharCount];
    int write = 0;
    while (_printCharCount > 0)
    {
      _printCharCount--;
      chars[write++] = _printBuffer[_printCharCount];
    }

    _printCharCount = 0;
    _printLines.Add(new string(chars));
  }

  private void SeedDefaultRam()
  {
    for (int i = 0; i < DefaultRamSeed.Length; i++)
    {
      State.Ram[322 + i] = DefaultRamSeed[i];
    }
  }
}
