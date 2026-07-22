namespace TeoCalc.Formats;

/// <summary>Strip captions (A–E) plus whether each column has a matching <c>LBL</c> subroutine.</summary>
public sealed class CardStripPresentation
{
  public string[] Captions { get; init; } = ["", "", "", "", ""];

  public bool[] Enabled { get; init; } = [true, true, true, true, true];
}

/// <summary>
/// HP-65 card strip captions (columns A–E): user-facing subroutine labels, not DATA registers.
/// </summary>
public static class ClassicCardStripLabels
{
  private static readonly string[] StripLetters = ["A", "B", "C", "D", "E"];

  /// <summary>
  /// Built-in HP-65 faceplate strip legends when no mag-card is inserted
  /// (same Unicode as <c>CalcFaceplateLayout.CardSlotLabels</c> / no-card chrome).
  /// A=<c>1/x</c>, B=<c>√x</c>, C=<c>y^x</c>, D=<c>R↓</c>, E=<c>x↔y</c>.
  /// </summary>
  public static readonly string[] DefaultNoCardCaptions =
    ["1/x", "\u221ax", "y^x", "R\u2193", "x\u2194y"];

  private static readonly HashSet<string> SubroutineStopMnemonics =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "LBL",
      "END",
      "PTR",
    };

  private static readonly HashSet<string> SubroutineSkipMnemonics =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "R/S",
      "RTN",
      "NOP",
    };

  public static string[] InferFromSteps(IReadOnlyList<string> steps)
  {
    string[] labels = StripLetters.Select(_ => string.Empty).ToArray();
    for (int i = 0; i < steps.Count - 1; i++)
    {
      if (!string.Equals(steps[i].Trim(), "LBL", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string letter = steps[i + 1].Trim();
      int column = Array.FindIndex(
        StripLetters,
        candidate => string.Equals(candidate, letter, StringComparison.OrdinalIgnoreCase));
      if (column < 0)
      {
        continue;
      }

      labels[column] = SummarizeSubroutine(steps, i + 2);
    }

    return labels;
  }

  public static string[] InferFromClassicSnapshot(
    ClassicCardSnapshot snapshot,
    Func<byte, string> mnemonicForCode)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    List<string> steps = [];
    int lastNonZero = 0;
    for (int i = 0; i < snapshot.ProgramCodes.Count; i++)
    {
      if (snapshot.ProgramCodes[i] != 0)
      {
        lastNonZero = i + 1;
      }
    }

    for (int i = 1; i < lastNonZero; i++)
    {
      byte code = snapshot.ProgramCodes[i];
      steps.Add(mnemonicForCode(code));
    }

    return InferFromSteps(steps);
  }

  public static CardStripPresentation Resolve(IReadOnlyList<string>? captions, IReadOnlyList<string> steps)
  {
    string[] normalized = TeoCardProgramFormat.NormalizeStripLabels(captions);
    bool[] subroutinePresent = SubroutinePresenceFromSteps(steps);
    bool[] enabled = new bool[StripLetters.Length];
    for (int column = 0; column < StripLetters.Length; column++)
    {
      enabled[column] = string.IsNullOrWhiteSpace(normalized[column]) || subroutinePresent[column];
    }

    return new CardStripPresentation
    {
      Captions = normalized,
      Enabled = enabled,
    };
  }

  public static CardStripPresentation ResolveFromClassicSnapshot(
    ClassicCardSnapshot snapshot,
    Func<byte, string> mnemonicForCode,
    IReadOnlyList<string>? captions = null)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    List<string> steps = [];
    int lastNonZero = 0;
    for (int i = 0; i < snapshot.ProgramCodes.Count; i++)
    {
      if (snapshot.ProgramCodes[i] != 0)
      {
        lastNonZero = i + 1;
      }
    }

    for (int i = 1; i < lastNonZero; i++)
    {
      steps.Add(mnemonicForCode(snapshot.ProgramCodes[i]));
    }

    if (captions is { Count: > 0 } && HasAnyLabel(captions))
    {
      return Resolve(captions, steps);
    }

    return Resolve(InferFromSteps(steps), steps);
  }

  public static bool[] SubroutinePresenceFromSteps(IReadOnlyList<string> steps)
  {
    bool[] present = StripLetters.Select(_ => false).ToArray();
    for (int i = 0; i < steps.Count - 1; i++)
    {
      if (!string.Equals(steps[i].Trim(), "LBL", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string letter = steps[i + 1].Trim();
      int column = Array.FindIndex(
        StripLetters,
        candidate => string.Equals(candidate, letter, StringComparison.OrdinalIgnoreCase));
      if (column >= 0)
      {
        present[column] = true;
      }
    }

    return present;
  }

  public static bool HasAnyLabel(IReadOnlyList<string>? labels)
  {
    if (labels is null)
    {
      return false;
    }

    foreach (string label in labels)
    {
      if (!string.IsNullOrWhiteSpace(label))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Map target letter <c>A</c>…<c>E</c> to strip column 0…4.</summary>
  public static bool TryGetStripColumn(string? letter, out int column)
  {
    column = -1;
    if (string.IsNullOrWhiteSpace(letter))
    {
      return false;
    }

    column = Array.FindIndex(
      StripLetters,
      candidate => string.Equals(candidate, letter.Trim(), StringComparison.OrdinalIgnoreCase));
    return column >= 0;
  }

  /// <summary>Faceplate label keys: strip columns A–E and numeric label digits 0–9.</summary>
  public static bool IsFaceplateLabelKey(string? key)
  {
    if (TryGetStripColumn(key, out _))
    {
      return true;
    }

    if (string.IsNullOrWhiteSpace(key))
    {
      return false;
    }

    string t = key.Trim();
    return t.Length == 1 && t[0] is >= '0' and <= '9';
  }

  /// <summary>
  /// Caption for strip column <paramref name="letter"/>.
  /// When <paramref name="captions"/> is null (no card), uses <see cref="DefaultNoCardCaptions"/>.
  /// When captions are provided (card loaded), empty columns stay empty.
  /// Empty when not A–E.
  /// </summary>
  public static string CaptionForLetter(IReadOnlyList<string>? captions, string? letter)
  {
    if (!TryGetStripColumn(letter, out int column))
    {
      return string.Empty;
    }

    IReadOnlyList<string> source = captions ?? DefaultNoCardCaptions;
    if (column >= source.Count)
    {
      return string.Empty;
    }

    string caption = source[column] ?? string.Empty;
    return caption.Trim();
  }

  /// <summary>True when Studio should paint no-card strip chrome (not mag-card chip).</summary>
  public static bool UsesNoCardStripChrome(IReadOnlyList<string>? captions) => captions is null;

  /// <summary>
  /// Remove Classic fall-through stubs (<c>LBL</c> A–E + only <c>NOP</c>/<c>RTN</c>) from a
  /// mnemonic step list. Used when exporting RAM so authoring files stay sparse.
  /// </summary>
  public static void RemoveEmptyStripLabelStubs(List<string> steps)
  {
    ArgumentNullException.ThrowIfNull(steps);
    for (int i = 0; i < steps.Count - 1; )
    {
      if (!string.Equals(steps[i].Trim(), "LBL", StringComparison.OrdinalIgnoreCase)
          || !TryGetStripColumn(steps[i + 1], out _))
      {
        i++;
        continue;
      }

      int bodyStart = i + 2;
      int bodyEnd = bodyStart;
      while (bodyEnd < steps.Count)
      {
        string m = steps[bodyEnd].Trim();
        if (string.Equals(m, "LBL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "END", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "PTR", StringComparison.OrdinalIgnoreCase))
        {
          break;
        }

        bodyEnd++;
      }

      bool emptyStub = true;
      for (int b = bodyStart; b < bodyEnd; b++)
      {
        string m = steps[b].Trim();
        if (m.Length == 0
            || string.Equals(m, "NOP", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "RTN", StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        emptyStub = false;
        break;
      }

      if (emptyStub)
      {
        steps.RemoveRange(i, bodyEnd - i);
        continue;
      }

      i = bodyEnd;
    }
  }

  private static string SummarizeSubroutine(IReadOnlyList<string> steps, int startIndex)
  {
    List<string> parts = [];
    for (int i = startIndex; i < steps.Count; i++)
    {
      string mnemonic = steps[i].Trim();
      if (mnemonic.Length == 0)
      {
        continue;
      }

      if (SubroutineStopMnemonics.Contains(mnemonic))
      {
        break;
      }

      if (SubroutineSkipMnemonics.Contains(mnemonic))
      {
        continue;
      }

      if (IsOperator(mnemonic) || IsDigit(mnemonic))
      {
        parts.Add(mnemonic);
        continue;
      }

      parts.Add(mnemonic);
    }

    if (parts.Count == 0)
    {
      return string.Empty;
    }

    return FormatCollectedParts(parts);
  }

  private static string FormatCollectedParts(List<string> parts)
  {
    // Digit run then operator → "+123" (add literal), not "1+2+3".
    if (parts.Count >= 2
        && IsOperator(parts[^1])
        && parts.Take(parts.Count - 1).All(IsDigit))
    {
      return parts[^1] + string.Concat(parts.Take(parts.Count - 1));
    }

    return string.Concat(parts);
  }

  private static bool IsDigit(string mnemonic) =>
    mnemonic.Length == 1 && mnemonic[0] is >= '0' and <= '9';

  private static bool IsOperator(string mnemonic) =>
    mnemonic is "+" or "-" or "×" or "÷" or "*" or "/";
}
