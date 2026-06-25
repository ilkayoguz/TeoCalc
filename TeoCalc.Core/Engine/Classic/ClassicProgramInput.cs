using TeoCalc.Core.Catalog;

namespace TeoCalc.Core.Engine.Classic;

public static class ClassicProgramInput
{
  public static void PressKey(ClassicCpuState state, byte keyCode)
  {
    state.KeyBuffer = keyCode;
    state.Flags &= ~ClassicCpuFlags.DisplayOn;
  }

  public static bool TryResolveKeyCode(ProgramVocabulary vocabulary, char character, out byte keyCode)
  {
    foreach (ProgramKeyEntry entry in vocabulary.KeyChart)
    {
      if (entry.Char.Length == 1 && entry.Char[0] == character)
      {
        keyCode = (byte)entry.KeyCode;
        return true;
      }
    }

    keyCode = 0;
    return false;
  }

  public static bool TryResolveStepCode(ProgramVocabulary vocabulary, string mnemonic, out byte keyCode)
  {
    ProgramStepEntry? step = vocabulary.TryResolveMnemonic(mnemonic);
    if (step is null)
    {
      keyCode = 0;
      return false;
    }

    keyCode = (byte)step.Code;
    return true;
  }
}
