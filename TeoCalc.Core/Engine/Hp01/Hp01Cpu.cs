using System.Diagnostics;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;

namespace TeoCalc.Core.Engine.Hp01;

/// <summary>
/// Native T-01 CPU (ACThp01 ISA). Distinct ISA from <see cref="Act.ActCpuBase"/>.
/// </summary>
public sealed class Hp01Cpu : CpuBase
{
  private static readonly byte[] PSetMap01 =
  [
    255, 11, 8, 0, 5, 255, 9, 1, 4, 2,
    255, 3, 7, 6, 10, 255,
  ];

  private static readonly byte[] PTestMap01 =
  [
    1, 11, 8, 0, 5, 255, 9, 255, 4, 2,
    255, 3, 7, 6, 10, 255,
  ];

  private readonly Dictionary<int, string> _dispatchTable;
  private readonly Stopwatch _stopwatch = new();
  private readonly IHp01ToneSink _tones;

  private byte[]? _src;
  private byte[]? _dest;
  private byte[]? _src2;
  private byte _first;
  private byte _last;
  private int _swStartTime;
  private byte _second;
  private byte _blinkCnt;
  private int _displayCnt;
  private byte _pendingKeycode;
  private bool _refreshDisplay;

  public Hp01Cpu(IMicrocodeRom rom, MicrocodeHandlerCatalog handlers, IHp01ToneSink? tones = null)
    : base(rom, handlers)
  {
    _dispatchTable = Hp01DispatchTable.Build();
    State = new Hp01CpuState();
    _tones = tones ?? NullHp01ToneSink.Instance;
  }

  public Hp01CpuState State { get; }

  /// <summary>When false, instruction batches should stop (reference <c>running</c> flag).</summary>
  public bool Running { get; private set; } = true;

  /// <summary>First sleep after reset injects wall-clock time (Panamatik <c>ShowTime</c>).</summary>
  public bool ShowTimeOnSleep { get; private set; } = true;

  public bool NeedsDisplayRefresh => _refreshDisplay;

  public override void Reset()
  {
    State.Reset();
    StepCount = 0;
    Running = true;
    ShowTimeOnSleep = true;
    _src = null;
    _dest = null;
    _src2 = null;
    _first = 0;
    _last = 0;
    _swStartTime = 0;
    _second = 0;
    _blinkCnt = 0;
    _displayCnt = 0;
    _pendingKeycode = 0;
    _refreshDisplay = false;
    _stopwatch.Reset();
  }

  public override void PressKey(byte keyCode)
  {
    State.Flags &= ~Hp01CpuFlags.DisplayOn;
    _refreshDisplay = true;
    _pendingKeycode = keyCode;
    State.KeyBuffer = keyCode;
    State.ExtraFlags &= ~Hp01ExtraFlags.Sleep;
    Running = true;
  }

  public override MicrocodeHandlerEntry Step()
  {
    int address = State.ProgramCounter;
    if (address < 0 || address >= Rom.WordCount)
    {
      throw new InvalidOperationException($"ROM address out of range: {address:X4}");
    }

    ushort opcode = Rom.ReadWord(address);
    if (opcode == 400 && address + 1 < Rom.WordCount && Rom.ReadWord(address + 1) == 268)
    {
      opcode = 0;
    }

    if ((State.Flags & Hp01CpuFlags.Carry) != 0)
    {
      State.Flags &= ~Hp01CpuFlags.Carry;
      State.Flags |= Hp01CpuFlags.PrevCarry;
    }
    else
    {
      State.Flags &= ~Hp01CpuFlags.PrevCarry;
    }

    State.ProgramCounter++;
    if ((byte)State.ProgramCounter == 0)
    {
      State.Rom = (byte)(State.ProgramCounter >> 8);
    }

    State.Opcode = opcode;
    MicrocodeHandlerEntry handler = Handlers.ResolveByDispatchIndex(opcode, _dispatchTable);
    Execute(handler.PanamatikAlias, opcode);
    State.LastOpcode = opcode;
    State.LastHandlerId = handler.HandlerId;
    StepCount++;

    if ((State.ExtraFlags & Hp01ExtraFlags.Sleep) != 0)
    {
      HandleSleepAfterInstruction();
    }

    return handler;
  }

  /// <summary>
  /// Peripheral service once per gateway batch (blink / wakeup / clock / stopwatch).
  /// </summary>
  public void ServicePeripherals()
  {
    if ((State.ExtraFlags & Hp01ExtraFlags.Sleep) != 0 && _refreshDisplay)
    {
      _refreshDisplay = false;
    }

    if ((State.ExtraFlags & Hp01ExtraFlags.Blink) != 0 && ++_blinkCnt >= 20)
    {
      _blinkCnt = 0;
      State.Flags ^= Hp01CpuFlags.DisplayOn;
      _refreshDisplay = true;
    }

    if ((State.ExtraFlags & Hp01ExtraFlags.Wakeup) != 0)
    {
      State.ExtraFlags &= ~Hp01ExtraFlags.Wakeup;
      State.ExtraFlags &= ~Hp01ExtraFlags.Sleep;
      State.KeyBuffer = 63;
      Running = true;
    }

    if (_displayCnt != 0 && --_displayCnt == 0 && (State.ExtraFlags & Hp01ExtraFlags.DisplayOnHold) == 0)
    {
      State.Flags &= ~Hp01CpuFlags.DisplayOn;
      _refreshDisplay = true;
    }

    if (Hp01Clinc())
    {
      _refreshDisplay = true;
    }
  }

  /// <summary>Test hook: run a dispatch alias without advancing ROM.</summary>
  internal void InvokeOpcodeAliasForTests(string alias) =>
    Execute(alias, opcode: 0);

  /// <summary>Consume pending key display-hold side effects after a sleep stop (Panamatik timer loop).</summary>
  public void ApplyPendingKeyDisplayHold()
  {
    if (_pendingKeycode == 0)
    {
      return;
    }

    if ((State.ExtraFlags & Hp01ExtraFlags.DisplayOnHold) == 0)
    {
      if (_pendingKeycode == 52)
      {
        _displayCnt = 500;
      }
      else
      {
        _displayCnt = 200;
      }

      if (State.C[11] == 2 || (State.ExtraFlags & Hp01ExtraFlags.Scwp) != 0)
      {
        _displayCnt = 0;
      }
    }

    _pendingKeycode = 0;
    _refreshDisplay = true;
  }

  public void ClearDisplayRefresh() => _refreshDisplay = false;

  private void HandleSleepAfterInstruction()
  {
    if (!ShowTimeOnSleep)
    {
      Running = false;
      ApplyPendingKeyDisplayHold();
      return;
    }

    ShowTimeOnSleep = false;
    SetTimeDateFromPc();
    PressKey(51);
  }

  private void SetTimeDateFromPc()
  {
    DateTime now = DateTime.Now;
    string hhmmss = now.ToString("HHmmss");
    for (int i = 0; i < 6; i++)
    {
      State.Cl[5 - i] = (byte)(hhmmss[i] & 0xF);
    }

    int oa = (int)now.ToOADate() - 2;
    int place = 100000;
    for (int j = 0; j < 6; j++)
    {
      byte digit = (byte)(oa / place);
      State.Cl[11 - j] = digit;
      oa -= digit * place;
      place /= 10;
    }
  }

  private void Execute(string alias, ushort opcode)
  {
    switch (alias)
    {
      case "op_nop":
      case "op_unknown":
        break;
      case "op_jsb":
        OpJsb(opcode);
        break;
      case "op_goto":
        OpGoto(opcode);
        break;
      case "op_arith":
        OpArith(opcode);
        break;
      case "op_return":
        OpReturn();
        break;
      case "op_set_s":
        State.Status |= (ushort)(1 << (opcode >> 6));
        break;
      case "op_clr_s":
        State.Status &= (ushort)~(1 << (opcode >> 6));
        break;
      case "op_test_s_eq_0":
        if ((State.Status & (1 << (opcode >> 6))) != 0)
        {
          State.Flags |= Hp01CpuFlags.Carry;
        }
        else
        {
          State.Flags &= ~Hp01CpuFlags.Carry;
        }

        break;
      case "op_set_p":
        State.P = PSetMap01[opcode >> 6];
        break;
      case "op_test_p_ne":
        if (State.P == PTestMap01[opcode >> 6])
        {
          State.Flags |= Hp01CpuFlags.Carry;
        }
        else
        {
          State.Flags &= ~Hp01CpuFlags.Carry;
        }

        break;
      case "op_load_constant":
        OpLoadConstant(opcode);
        break;
      case "op_sel_rom":
        State.Rom = (byte)(opcode >> 6);
        State.ProgramCounter = (ushort)((State.Rom << 8) | (State.ProgramCounter & 0xFF));
        break;
      case "op_del_sel_rom":
        State.Rom = (byte)(opcode >> 6);
        break;
      case "op_inc_p":
        OpIncP();
        break;
      case "op_dec_p":
        OpDecP();
        break;
      case "op_gokeys":
        State.ProgramCounter = (ushort)((State.Rom << 8) | State.KeyBuffer);
        break;
      case "op_clr_s17":
        State.Status &= 65280;
        break;
      case "op_clr_s815":
        State.Status &= 255;
        break;
      case "op_sleep":
        State.ExtraFlags |= Hp01ExtraFlags.Sleep;
        break;
      case "op_clear_reg":
        OpClearReg();
        break;
      case "op_display_on":
        State.Flags |= Hp01CpuFlags.DisplayOn;
        break;
      case "op_display_off":
        State.Flags &= ~Hp01CpuFlags.DisplayOn;
        State.ExtraFlags &= ~Hp01ExtraFlags.Blink;
        break;
      case "op_blink":
        State.ExtraFlags |= Hp01ExtraFlags.Blink;
        break;
      case "op_enscwp":
        State.ExtraFlags |= Hp01ExtraFlags.Scwp;
        break;
      case "op_dsscwp":
        State.ExtraFlags &= ~Hp01ExtraFlags.Scwp;
        break;
      case "op_swinc":
        State.ExtraFlags &= ~Hp01ExtraFlags.SwDec;
        break;
      case "op_swdec":
        State.ExtraFlags |= Hp01ExtraFlags.SwDec;
        break;
      case "op_swstrt":
        _stopwatch.Restart();
        _swStartTime = GetSwTime();
        State.ExtraFlags |= Hp01ExtraFlags.SwStarted;
        break;
      case "op_swstop":
        _stopwatch.Stop();
        State.ExtraFlags &= ~Hp01ExtraFlags.SwStarted;
        break;
      case "op_altog":
        State.ExtraFlags ^= Hp01ExtraFlags.AlarmActive;
        break;
      case "op_cdex":
        FieldCopy(0, 11, State.C, State.Y, exch: true);
        break;
      case "op_mtoc":
        FieldCopy(0, 11, State.C, State.M, exch: false);
        break;
      case "op_dtoc":
        FieldCopy(0, 11, State.C, State.Y, exch: false);
        break;
      case "op_ctom":
        FieldCopy(0, 11, State.M, State.C, exch: false);
        break;
      case "op_ftoap":
        if (State.P < Hp01CpuState.WordSize)
        {
          State.A[State.P] = State.F;
        }

        break;
      case "op_aptof":
        if (State.P < Hp01CpuState.WordSize)
        {
          State.F = State.A[State.P];
        }

        break;
      case "op_dsptoa":
        FieldCopy(0, 11, State.A, State.Dsp, exch: false);
        break;
      case "op_cltoa":
        FieldCopy(0, 11, State.A, State.Cl, exch: false);
        break;
      case "op_atoclrs":
        FieldCopy(0, 11, State.Cl, State.A, exch: false);
        State.TickCnt = 0;
        break;
      case "op_atocl":
        FieldCopy(0, 11, State.Cl, State.A, exch: false);
        break;
      case "op_cltodsp":
        OpClToDsp();
        break;
      case "op_atoal":
        FieldCopy(0, 11, State.Al, State.A, exch: false);
        State.ExtraFlags ^= Hp01ExtraFlags.AlarmActive;
        break;
      case "op_swtoa":
        FieldCopy(0, 11, State.A, State.Sw, exch: false);
        break;
      case "op_atosw":
        OpAToSw();
        break;
      case "op_swtodsp":
        OpSwToDsp();
        break;
      case "op_altodsp":
        OpAlToDsp();
        break;
      case "op_atodsp":
        FieldCopy(0, 11, State.Dsp, State.A, exch: false);
        break;
      case "op_altoa":
        FieldCopy(0, 11, State.A, State.Al, exch: false);
        break;
    }
  }

  private void OpJsb(ushort opcode)
  {
    State.Stack[State.Sp] = State.ProgramCounter;
    State.Sp = (byte)((State.Sp + 1) & 1);
    State.ProgramCounter = (ushort)((State.Rom << 8) | (opcode >> 2));
  }

  private void OpGoto(ushort opcode)
  {
    if ((State.Flags & Hp01CpuFlags.PrevCarry) == 0)
    {
      State.ProgramCounter = (ushort)((State.Rom << 8) | (opcode >> 2));
    }
  }

  private void OpReturn()
  {
    State.Sp = (byte)((State.Sp - 1) & 1);
    State.ProgramCounter = State.Stack[State.Sp];
    State.Rom = (byte)(State.ProgramCounter >> 8);
  }

  private void OpLoadConstant(ushort opcode)
  {
    if (State.P < 12)
    {
      State.A[State.P] = (byte)(opcode >> 6);
    }

    OpDecP();
  }

  private void OpIncP()
  {
    if (++State.P >= 12)
    {
      State.P = 0;
    }
  }

  private void OpDecP()
  {
    if (State.P != 0)
    {
      State.P--;
    }
    else
    {
      State.P = 11;
    }
  }

  private void OpClearReg()
  {
    Array.Clear(State.A);
    Array.Clear(State.B);
    Array.Clear(State.C);
    Array.Clear(State.Y);
    Array.Clear(State.Z);
    Array.Clear(State.T);
    Array.Clear(State.M);
  }

  private void FieldCopy(byte first, byte last, byte[] dest, byte[] src, bool exch)
  {
    _first = first;
    _last = last;
    _dest = dest;
    _src = src;
    if (exch)
    {
      RegExch();
    }
    else
    {
      RegCopy();
    }
  }

  private void OpClToDsp()
  {
    State.Dsp[4] = State.Cl[0];
    State.Dsp[5] = State.Cl[1];
    State.Dsp[7] = State.Cl[2];
    State.Dsp[8] = State.Cl[3];
  }

  private void OpAToSw()
  {
    FieldCopy(0, 11, State.Sw, State.A, exch: false);
    _dest = null;
    _src = State.A;
    RegTestNonequal();
    if ((State.Flags & Hp01CpuFlags.Carry) == 0)
    {
      _stopwatch.Reset();
    }
  }

  private void OpSwToDsp()
  {
    int offset = ((State.Sw[6] | State.Sw[7]) != 0) ? 2 : 0;
    int dsp = 3;
    int i = 0;
    while (i < 6)
    {
      State.Dsp[dsp] = State.Sw[i + offset];
      if ((i & 1) != 0)
      {
        dsp++;
      }

      i++;
      dsp++;
    }
  }

  private void OpAlToDsp()
  {
    if ((State.ExtraFlags & Hp01ExtraFlags.AlarmActive) != 0)
    {
      State.Dsp[3] = State.Dsp[3] == 10 ? (byte)14 : (byte)11;
    }

    State.Dsp[4] = State.Al[0];
    State.Dsp[5] = State.Al[1];
    State.Dsp[7] = State.Al[2];
    State.Dsp[8] = State.Al[3];
  }

  private int GetSwTime()
  {
    int value = 0;
    int place = 1;
    for (int i = 0; i < 8; i++)
    {
      value += State.Sw[i] * place;
      place = (i is 3 or 5) ? place * 6 : place * 10;
    }

    return value;
  }

  private bool Hp01Clinc()
  {
    bool refresh = false;
    if ((++State.TickCnt & 0x1F) == 0)
    {
      string text = DateTime.Now.ToString("hhmmss");
      byte digit = Convert.ToByte(text.Substring(5, 1));
      if (digit != _second)
      {
        _second = digit;
        Hp01IncCl();
        if ((State.ExtraFlags & Hp01ExtraFlags.Scwp) != 0)
        {
          State.ExtraFlags |= Hp01ExtraFlags.Wakeup;
          refresh = true;
        }

        if (State.C[11] == 3)
        {
          OpClToDsp();
          refresh = true;
        }

        if ((State.ExtraFlags & Hp01ExtraFlags.AlarmActive) != 0 && Hp01CheckAlarm())
        {
          State.ExtraFlags &= ~Hp01ExtraFlags.AlarmActive;
          State.ExtraFlags |= Hp01ExtraFlags.Blink;
          _tones.Alarm();
        }
      }
    }

    if ((State.ExtraFlags & Hp01ExtraFlags.SwStarted) != 0)
    {
      long ticks = _stopwatch.ElapsedMilliseconds / 10;
      ticks = (State.ExtraFlags & Hp01ExtraFlags.SwDec) != 0
        ? _swStartTime - ticks
        : ticks + _swStartTime;
      if (ticks < 0)
      {
        State.ExtraFlags &= ~Hp01ExtraFlags.SwDec;
        _stopwatch.Restart();
        _swStartTime = 0;
        ticks = 0;
        State.ExtraFlags |= Hp01ExtraFlags.Blink;
        _tones.Beep();
      }

      for (int i = 0; i < 8; i++)
      {
        byte radix = (byte)((i is 3 or 5) ? 6 : 10);
        State.Sw[i] = (byte)(ticks % radix);
        ticks /= radix;
      }

      if (State.C[11] == 2)
      {
        OpSwToDsp();
        refresh = true;
      }
    }

    return refresh;
  }

  private void Hp01IncCl()
  {
    byte carry = 1;
    for (byte i = 0; i < 6; i++)
    {
      byte n = (byte)(State.Cl[i] + carry);
      carry = 0;
      if (((i is 1 or 3) && n >= 6) || n >= 10)
      {
        if (i == 3)
        {
          State.ExtraFlags |= Hp01ExtraFlags.Wakeup;
        }

        n = 0;
        carry = 1;
      }

      State.Cl[i] = n;
    }

    if (State.Cl[5] == 2 && State.Cl[4] == 4)
    {
      State.Cl[5] = State.Cl[4] = 0;
      _first = 6;
      _last = 11;
      Hp01Inc(State.Cl);
    }
  }

  private bool Hp01CheckAlarm()
  {
    for (byte i = 0; i < 6; i++)
    {
      if (State.Cl[i] != State.Al[i])
      {
        return false;
      }
    }

    return true;
  }

  private void Hp01Inc(byte[] dest)
  {
    byte carry = 1;
    for (byte i = _first; i <= _last; i++)
    {
      byte n = (byte)(dest[i] + carry);
      carry = 0;
      if (n >= 10)
      {
        n = 0;
        carry = 1;
      }

      dest[i] = n;
    }
  }

  private void OpArith(ushort opcode)
  {
    OpSetField(opcode);
    switch ((byte)(opcode >> 5))
    {
      case 0:
        _src = State.C;
        _dest = null;
        RegTestNonequal();
        break;
      case 1:
        _dest = State.C;
        RegZero();
        break;
      case 2:
        _dest = State.C;
        RegInc();
        break;
      case 3:
        State.Flags |= Hp01CpuFlags.Carry;
        _dest = State.C;
        _src = State.C;
        _src2 = null;
        RegSub();
        break;
      case 4:
        _src = State.C;
        _dest = null;
        RegTestEqual();
        break;
      case 5:
        _dest = State.C;
        _src = State.C;
        RegAdd();
        break;
      case 6:
        _dest = State.C;
        _src = null;
        _src2 = State.C;
        RegSub();
        break;
      case 7:
        State.Flags |= Hp01CpuFlags.Carry;
        _dest = State.C;
        _src = null;
        _src2 = State.C;
        RegSub();
        break;
      case 8:
        _dest = State.A;
        _src = State.C;
        RegExch();
        break;
      case 9:
        _dest = State.A;
        _src = State.C;
        RegCopy();
        break;
      case 10:
        _dest = State.A;
        _src = State.C;
        RegAdd();
        break;
      case 11:
        _dest = State.A;
        _src = State.A;
        _src2 = State.C;
        RegSub();
        break;
      case 12:
        _dest = null;
        _src = State.A;
        _src2 = State.C;
        RegSub();
        break;
      case 13:
        _src = State.A;
        _dest = null;
        RegTestEqual();
        break;
      case 14:
        _dest = State.C;
        _src = State.A;
        RegAdd();
        break;
      case 15:
        _dest = State.C;
        _src = State.A;
        _src2 = State.C;
        RegSub();
        break;
      case 16:
        _dest = null;
        _src = State.A;
        _src2 = State.B;
        RegSub();
        break;
      case 17:
        _dest = State.A;
        RegZero();
        break;
      case 18:
        _dest = State.A;
        RegInc();
        break;
      case 19:
        State.Flags |= Hp01CpuFlags.Carry;
        _dest = State.A;
        _src = State.A;
        _src2 = null;
        RegSub();
        break;
      case 20:
        _dest = State.B;
        _src = State.A;
        RegCopy();
        break;
      case 21:
        _dest = State.A;
        _src = State.B;
        RegExch();
        break;
      case 22:
        _dest = State.A;
        _src = State.B;
        RegAdd();
        break;
      case 23:
        _dest = State.A;
        _src = State.A;
        _src2 = State.B;
        RegSub();
        break;
      case 24:
        _dest = State.B;
        RegZero();
        break;
      case 25:
        _src = State.B;
        _dest = null;
        RegTestNonequal();
        break;
      case 26:
        _dest = State.C;
        _src = State.B;
        RegCopy();
        break;
      case 27:
        _dest = State.B;
        _src = State.C;
        RegExch();
        break;
      case 28:
        _dest = State.A;
        RegShiftRight();
        break;
      case 29:
        _dest = State.A;
        RegShiftLeft();
        break;
      case 30:
        _dest = State.B;
        RegShiftRight();
        break;
      case 31:
        _dest = State.C;
        RegShiftRight();
        break;
    }
  }

  private void OpSetField(ushort opcode)
  {
    switch ((byte)((opcode >> 2) & 7))
    {
      case 0:
        _first = State.P;
        _last = State.P;
        break;
      case 1:
        _first = 3;
        _last = 10;
        break;
      case 2:
        _first = 0;
        _last = 2;
        break;
      case 3:
        _first = 0;
        _last = 11;
        break;
      case 4:
        _first = 0;
        _last = State.P;
        break;
      case 5:
        _first = 3;
        _last = 11;
        break;
      case 6:
        _first = 2;
        _last = 2;
        break;
      case 7:
        _first = 11;
        _last = 11;
        break;
    }
  }

  private void RegTestEqual()
  {
    State.Flags |= Hp01CpuFlags.Carry;
    for (byte i = _first; i <= _last; i++)
    {
      byte other = _dest is null ? (byte)0 : _dest[i];
      if (_src![i] != other)
      {
        State.Flags &= ~Hp01CpuFlags.Carry;
        break;
      }
    }
  }

  private void RegTestNonequal()
  {
    State.Flags &= ~Hp01CpuFlags.Carry;
    for (byte i = _first; i <= _last; i++)
    {
      byte other = _dest is null ? (byte)0 : _dest[i];
      if (_src![i] != other)
      {
        State.Flags |= Hp01CpuFlags.Carry;
        break;
      }
    }
  }

  private void RegInc()
  {
    State.Flags |= Hp01CpuFlags.Carry;
    _src = null;
    RegAdd();
  }

  private void RegAdd()
  {
    for (byte i = _first; i <= _last; i++)
    {
      byte addend = _src is null ? (byte)0 : _src[i];
      byte sum = (byte)(_dest![i] + addend + (((State.Flags & Hp01CpuFlags.Carry) != 0) ? 1 : 0));
      if (sum >= 10)
      {
        sum -= 10;
        State.Flags |= Hp01CpuFlags.Carry;
      }
      else
      {
        State.Flags &= ~Hp01CpuFlags.Carry;
      }

      _dest[i] = sum;
    }
  }

  private void RegSub()
  {
    for (byte i = _first; i <= _last; i++)
    {
      byte left = _src is null ? (byte)0 : _src[i];
      byte right = _src2 is null ? (byte)0 : _src2[i];
      sbyte diff = (sbyte)(left - right - (((State.Flags & Hp01CpuFlags.Carry) != 0) ? 1 : 0));
      if (diff < 0)
      {
        diff += 10;
        State.Flags |= Hp01CpuFlags.Carry;
      }
      else
      {
        State.Flags &= ~Hp01CpuFlags.Carry;
      }

      if (_dest is not null)
      {
        _dest[i] = (byte)diff;
      }
    }
  }

  private void RegZero()
  {
    for (byte i = _first; i <= _last; i++)
    {
      _dest![i] = 0;
    }
  }

  private void RegCopy()
  {
    for (byte i = _first; i <= _last; i++)
    {
      _dest![i] = _src![i];
    }
  }

  private void RegExch()
  {
    for (byte i = _first; i <= _last; i++)
    {
      byte tmp = _dest![i];
      _dest[i] = _src![i];
      _src[i] = tmp;
    }
  }

  private void RegShiftRight()
  {
    for (byte i = _first; i <= _last; i++)
    {
      _dest![i] = (byte)((i != _last) ? _dest[i + 1] : 0);
    }
  }

  private void RegShiftLeft()
  {
    for (sbyte i = (sbyte)_last; i >= _first; i--)
    {
      _dest![i] = (byte)((i != _first) ? _dest[i - 1] : 0);
    }
  }
}
