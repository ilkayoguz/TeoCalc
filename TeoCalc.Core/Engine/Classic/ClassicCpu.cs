using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>Classic-family CPU stepper (HP-65 first).</summary>
public sealed class ClassicCpu
{
  private readonly ClassicMicrocodeRom _rom;
  private readonly MicrocodeHandlerCatalog _handlers;
  private readonly Dictionary<int, string> _dispatchTable;

  public ClassicCpu(ClassicMicrocodeRom rom, MicrocodeHandlerCatalog handlers, ProgramVocabulary? vocabulary = null)
  {
    _rom = rom;
    _handlers = handlers;
    _dispatchTable = ClassicDispatchTable.Build();
    State = new ClassicCpuState();
    Program = new ClassicProgramMemory(State, vocabulary);
  }

  public ClassicCpuState State { get; }

  public ClassicProgramMemory Program { get; }

  public int StepCount { get; private set; }

  public void Reset()
  {
    State.Reset();
    Program.Initialize();
    StepCount = 0;
  }

  public void PressKey(byte keyCode)
  {
    ClassicProgramInput.PressKey(State, keyCode);
  }

  public MicrocodeHandlerEntry Step()
  {
    int address = State.FetchAddress;
    if (address < 0 || address >= _rom.WordCount)
    {
      throw new InvalidOperationException($"ROM address out of range: {address:X4}");
    }

    ushort opcode = _rom.ReadWord(address);
    State.PrepareOpcodeFlags();
    State.ProgramCounter++;
    MicrocodeHandlerEntry handler = _handlers.ResolveByDispatchIndex(opcode, _dispatchTable);
    Execute(handler.HandlerId, opcode);
    State.LastOpcode = opcode;
    State.LastHandlerId = handler.HandlerId;
    StepCount++;
    return handler;
  }

  private void Execute(string handlerId, ushort opcode)
  {
    switch (handlerId)
    {
      case "ClassicCpu.Nop":
        break;
      case "ClassicCpu.SubroutineJump":
        SubroutineJump(opcode);
        break;
      case "ClassicCpu.Branch":
        Branch(opcode);
        break;
      case "ClassicCpu.Return":
        Return();
        break;
      case "ClassicCpu.DelayedSelectRom":
        DelayedSelectRom(opcode);
        break;
      case "ClassicCpu.DelayedSelectGroup":
        DelayedSelectGroup(opcode);
        break;
      case "ClassicCpu.SetP":
        SetP(opcode);
        break;
      case "ClassicCpu.TestP":
        TestP(opcode);
        break;
      case "ClassicCpu.DecrementP":
        DecrementP();
        break;
      case "ClassicCpu.IncrementP":
        IncrementP();
        break;
      case "ClassicCpu.SetStatus":
        SetStatus(opcode);
        break;
      case "ClassicCpu.TestStatusZero":
        TestStatusZero(opcode);
        break;
      case "ClassicCpu.ClearStatus":
        ClearStatus(opcode);
        break;
      case "ClassicCpu.ClearS":
        ClearStatusRegister();
        break;
      case "ClassicCpu.SetFlag":
        SetFlag(opcode);
        break;
      case "ClassicCpu.LoadConstant":
        LoadConstant(opcode);
        break;
      case "ClassicCpu.SelectRom":
        SelectRom(opcode);
        break;
      case "ClassicCpu.Arithmetic":
        ClassicCpuArithmetic.Execute(opcode, State);
        break;
      case "ClassicCpu.ClearRegisters":
        State.Registers.ClearAll();
        break;
      case "ClassicCpu.MemoryInitialize":
        Program.Initialize();
        break;
      case "ClassicCpu.MemoryInsert":
        Program.InsertFromBuffer();
        break;
      case "ClassicCpu.MemoryDelete":
        Program.DeleteBeforePointer();
        break;
      case "ClassicCpu.MarkAndSearch":
        Program.MarkAndSearch();
        break;
      case "ClassicCpu.SearchForLabel":
        Program.SearchForLabel();
        break;
      case "ClassicCpu.PointerAdvance":
        Program.AdvancePointer();
        break;
      case "ClassicCpu.MemoryFull":
        Program.ApplyMemoryFullToDisplay();
        break;
      case "ClassicCpu.BufferToRomAddress":
        Program.BufferToBranchOffset();
        break;
      case "ClassicCpu.KeysToRomAddress":
        KeysToRomAddress();
        break;
      case "ClassicCpu.CToStack":
        ClassicCpuStack.CToStack(State.Registers);
        break;
      case "ClassicCpu.StackToA":
        ClassicCpuStack.StackToA(State.Registers);
        break;
      case "ClassicCpu.DownRotate":
        ClassicCpuStack.DownRotate(State.Registers);
        break;
      case "ClassicCpu.ExchangeCAndM":
        ClassicCpuStack.ExchangeCAndM(State.Registers);
        State.Grp = State.DelGrp;
        break;
      case "ClassicCpu.MToC":
        ClassicCpuStack.MToC(State.Registers);
        break;
      case "ClassicCpu.DataToC":
        ClassicCpuDataRam.DataToC(State);
        break;
      case "ClassicCpu.CToAddress":
        ClassicCpuDataRam.CToAddress(State);
        break;
      case "ClassicCpu.CToData":
        ClassicCpuDataRam.CToData(State);
        break;
      case "ClassicCpu.DisplayToggle":
        State.Flags ^= ClassicCpuFlags.DisplayOn;
        break;
      case "ClassicCpu.DisplayOff":
        State.Flags &= ~ClassicCpuFlags.DisplayOn;
        break;
    }
  }

  private void SubroutineJump(ushort opcode)
  {
    State.ReturnStack[0] = State.ProgramCounter;
    State.Rom = State.DelRom;
    State.ProgramCounter = (ushort)((State.Grp << 11) | (State.Rom << 8) | (opcode >> 2));
    if ((State.F & 0x80) != 0)
    {
      State.Buffer = State.ProgramCounter & 0x3F;
      State.F &= 127;
    }
  }

  private void Branch(ushort opcode)
  {
    if ((State.Flags & ClassicCpuFlags.PrevCarry) == 0)
    {
      State.ProgramCounter = (ushort)(((State.Grp << 11) | (State.Rom << 8) | (opcode >> 2)) + State.BranchOffset);
      State.BranchOffset = 0;
      State.Rom = State.DelRom;
      State.Grp = State.DelGrp;
    }
  }

  private void Return()
  {
    State.ProgramCounter = State.ReturnStack[0];
  }

  private void DelayedSelectRom(ushort opcode)
  {
    State.DelRom = (byte)(opcode >> 7);
  }

  private void DelayedSelectGroup(ushort opcode)
  {
    State.DelGrp = (byte)((opcode >> 7) & 1);
  }

  private void SetP(ushort opcode)
  {
    State.P = (byte)(opcode >> 6);
  }

  private void TestP(ushort opcode)
  {
    if (State.P == opcode >> 6)
    {
      State.Flags |= ClassicCpuFlags.Carry;
    }
    else
    {
      State.Flags &= ~ClassicCpuFlags.Carry;
    }
  }

  private void DecrementP()
  {
    State.P = (byte)((State.P - 1) & 0xF);
  }

  private void IncrementP()
  {
    State.P = (byte)((State.P + 1) & 0xF);
  }

  private void SetStatus(ushort opcode)
  {
    State.Status |= (ushort)(1 << (opcode >> 6));
    if (opcode >> 6 == 0)
    {
      State.Grp = State.DelGrp;
    }
  }

  private void TestStatusZero(ushort opcode)
  {
    if ((State.Status & (1 << (opcode >> 6))) != 0)
    {
      State.Flags |= ClassicCpuFlags.Carry;
    }
    else
    {
      State.Flags &= ~ClassicCpuFlags.Carry;
    }
  }

  private void ClearStatus(ushort opcode)
  {
    ushort mask = (ushort)(1 << (opcode >> 6));
    State.Status &= (ushort)~mask;
  }

  private void ClearStatusRegister()
  {
    State.Status = 0;
  }

  private void SetFlag(ushort opcode)
  {
    int bit = opcode >> 7;
    if ((opcode & 0x40) == 0)
    {
      State.F |= (byte)(1 << bit);
      return;
    }

    if ((State.F & (1 << bit)) != 0)
    {
      State.Status |= 2048;
      State.F &= (byte)~(1 << bit);
    }

    if (bit == 5)
    {
      State.Status |= 2048;
    }
  }

  private void LoadConstant(ushort opcode)
  {
    if (State.P < 14 && opcode >> 6 < 10)
    {
      State.Registers.C[State.P] = (byte)(opcode >> 6);
    }

    DecrementP();
  }

  private void SelectRom(ushort opcode)
  {
    State.Rom = (byte)(opcode >> 7);
    State.Grp = State.DelGrp;
    State.DelRom = State.Rom;
    State.ProgramCounter = (ushort)((State.Grp << 11) | (State.Rom << 8) | (byte)State.ProgramCounter);
  }

  private void KeysToRomAddress()
  {
    State.ProgramCounter = (ushort)((State.Grp << 11) | (State.Rom << 8) | State.KeyBuffer);
    if ((State.F & 0x80) != 0)
    {
      State.Buffer = State.ProgramCounter & 0x3F;
      State.F &= 127;
    }
  }
}
