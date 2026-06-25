using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

/// <summary>Panamatik Classic faceplate grid: 8 rows x 5 columns (40 keys).</summary>
public static class CalcFaceplateLayout
{
  public const int Rows = 8;

  public const int Columns = 5;

  public static int ToIndex(int row, int column) => row * Columns + column;

  public static string LabelForKey(ProgramKeyEntry key, ProgramVocabulary? vocabulary)
  {
    if (key.Char == "\0")
    {
      return string.Empty;
    }

    if (key.Char == "\r")
    {
      return "R/S";
    }

    if (key.Char == "\b")
    {
      return "BST";
    }

    if (key.Char.Length == 1 && char.IsLetterOrDigit(key.Char[0]))
    {
      return key.Char.ToUpperInvariant();
    }

    if (vocabulary is not null && key.KeyCode > 0)
    {
      try
      {
        return vocabulary.ResolveCode(key.KeyCode).Mnemonic;
      }
      catch (KeyNotFoundException)
      {
      }
    }

    return key.Char;
  }
}
