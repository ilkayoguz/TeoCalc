using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine.Classic;

/// <summary>HP Classic user program storage in act_ram[ProgramRamBase+].</summary>
public sealed class ClassicProgramMemory
{
  private readonly ClassicCpuState _state;
  private readonly ProgramVocabulary? _vocabulary;

  public ClassicProgramMemory(ClassicCpuState state, ProgramVocabulary? vocabulary = null)
  {
    _state = state;
    _vocabulary = vocabulary;
  }

  public int MemLength { get; private set; } = 102;

  public byte EndState { get; private set; }

  public int Busy { get; private set; }

  public int Base => _state.ProgramRamBase;

  public byte ReadCode(int index)
  {
    return _state.Ram[Base + index];
  }

  public void WriteCode(int index, byte code)
  {
    _state.Ram[Base + index] = code;
  }

  public string FormatCode(byte code)
  {
    if (code == 0)
    {
      return string.Empty;
    }

    if (_vocabulary is null)
    {
      return $"#{code}";
    }

    try
    {
      return _vocabulary.ResolveCode(code).Mnemonic;
    }
    catch (KeyNotFoundException)
    {
      return $"#{code}";
    }
  }

  public string FormatStep(int index)
  {
    return FormatCode(ReadCode(index));
  }

  public void Initialize()
  {
    MemLength = 102;
    WriteCode(0, ClassicProgramCodes.Start);
    WriteCode(1, ClassicProgramCodes.Pointer);
    for (int index = 2; index < MemLength; index++)
    {
      WriteCode(index, 0);
    }

    Cleanup(7);
  }

  public int PointerPosition()
  {
    int marker = MemLength == 103 ? ClassicProgramCodes.Mark : ClassicProgramCodes.Pointer;
    int index = 1;
    while (index < MemLength && ReadCode(index) != marker)
    {
      index++;
    }

    return index == MemLength ? 1 : index;
  }

  /// <summary>
  /// Last RAM index that still holds a non-zero program byte (or the pointer marker).
  /// Trailing zeros are empty capacity — seeking into them would shift zeros into the program.
  /// </summary>
  public int LastContentIndex()
  {
    int last = MemLength - 1;
    while (last > 1 && ReadCode(last) == 0)
    {
      last--;
    }

    return Math.Max(1, last);
  }

  public int LabelPosition(int startIndex, int labelDigit)
  {
    int index = startIndex;
    while (index < MemLength && (ReadCode(index - 1) != ClassicProgramCodes.Label || ReadCode(index) != labelDigit))
    {
      index++;
    }

    if (index == MemLength)
    {
      index = 1;
      while (index < MemLength && (ReadCode(index - 1) != ClassicProgramCodes.Label || ReadCode(index) != labelDigit))
      {
        index++;
      }

      if (index == MemLength)
      {
        return 0;
      }
    }

    return index;
  }

  public void InsertAt(int index, int code)
  {
    while (index < MemLength)
    {
      int previous = ReadCode(index);
      WriteCode(index, (byte)code);
      code = previous;
      index++;
    }
  }

  public void DeleteAt(int index)
  {
    for (index++; index < MemLength; index++)
    {
      WriteCode(index - 1, ReadCode(index));
    }

    WriteCode(index - 1, 0);
  }

  public void InsertFromBuffer()
  {
    int pointer = PointerPosition();
    if (pointer < MemLength - 1)
    {
      InsertAt(pointer, _state.Buffer);
      Cleanup(10);
    }
    else
    {
      Busy = 5;
    }
  }

  public void DeleteBeforePointer()
  {
    int pointer = PointerPosition();
    if (pointer > 1)
    {
      DeleteAt(pointer - 1);
    }

    Cleanup(10);
  }

  public void MarkAndSearch()
  {
    int pointer = PointerPosition();
    if (MemLength == 103)
    {
      DeleteAt(pointer);
    }

    MemLength = 103;
    pointer = LabelPosition(pointer, _state.Buffer);
    if (pointer == 0)
    {
      MemLength = 102;
    }
    else
    {
      InsertAt(pointer + 1, ClassicProgramCodes.Mark);
    }

    Cleanup(12);
  }

  public void SearchForLabel()
  {
    int pointer = PointerPosition();
    int savedCode = ReadCode(pointer);
    DeleteAt(pointer);
    pointer = LabelPosition(pointer, _state.Buffer);
    if (pointer == 0 && MemLength == 103)
    {
      MemLength = 102;
    }
    else
    {
      InsertAt(pointer + 1, savedCode);
    }

    Cleanup(12);
  }

  public void AdvancePointer()
  {
    int pointer = PointerPosition();
    int currentCode = ReadCode(pointer);
    if (pointer < MemLength - 1)
    {
      WriteCode(pointer, ReadCode(pointer + 1));
      WriteCode(pointer + 1, (byte)currentCode);
    }
    else
    {
      InsertAt(1, currentCode);
    }

    Cleanup(7);
  }

  /// <summary>Move the Classic PTR marker to <paramref name="targetIndex"/> (program start / SST point).</summary>
  public void SeekPointer(int targetIndex)
  {
    int last = LastContentIndex();
    if (targetIndex < 1 || targetIndex > last)
    {
      targetIndex = Math.Clamp(targetIndex, 1, last);
    }

    int pointer = PointerPosition();
    if (pointer == targetIndex)
    {
      return;
    }

    byte marker = ReadCode(pointer);
    if (targetIndex > pointer)
    {
      for (int i = pointer; i < targetIndex; i++)
      {
        WriteCode(i, ReadCode(i + 1));
      }
    }
    else
    {
      for (int i = pointer; i > targetIndex; i--)
      {
        WriteCode(i, ReadCode(i - 1));
      }
    }

    WriteCode(targetIndex, marker);
    Cleanup(7);
  }

  public void ApplyMemoryFullToDisplay()
  {
    _state.Registers.A[0] = (byte)(EndState & 1);
    if (EndState >= 2)
    {
      _state.F |= 4;
    }
  }

  public void BufferToBranchOffset()
  {
    _state.BranchOffset = _state.Buffer;
  }

  public void Cleanup(int busyCode)
  {
    int pointer = PointerPosition();
    _state.Buffer = ReadCode(pointer - 1);
    EndState = 0;
    if (ReadCode(MemLength - 1) != 0)
    {
      EndState = 1;
    }

    if (pointer == MemLength - 1)
    {
      EndState = 2;
    }

    Busy = busyCode;
  }
}
