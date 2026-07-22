using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// User-facing Studio listing: hide Classic runtime markers (START/PTR) and optionally
/// merge display rows (LBL+target, shift+key) without changing RAM encoding.
/// </summary>
public static class StudioListingView
{
  public enum MergeKind : byte
  {
    Single = 0,
    LabelPair = 1,
    ShiftPair = 2,
    /// <summary>Display-only merge of <c>STO|RCL</c> + <c>+|-|*|/</c> + digit (3 keystrokes).</summary>
    RegisterArith = 3,
    /// <summary><c>STO|RCL</c> + register digit on the next line (2 keystrokes).</summary>
    OpDigitPair = 4,
    /// <summary><c>GTO|GSB</c> + target on the next line (2 keystrokes).</summary>
    BranchPair = 5,
  }

  /// <summary>
  /// One painted Studio row (may span two or three program steps).
  /// <see cref="Index"/> is the RAM program address of the first step in the row
  /// (used for selection / pointer). The Studio <c>#</c> column shows a 1-based
  /// index among filtered user steps, advancing by <see cref="StepSpan"/> so a
  /// merged Classic pair (e.g. LBL+A) counts as two steps: next row is previous + 2.
  /// </summary>
  public readonly record struct Row(
    int Index,
    byte Code,
    string Mnemonic,
    byte? SecondCode,
    string? SecondMnemonic,
    MergeKind Kind,
    byte? ThirdCode = null,
    string? ThirdMnemonic = null)
  {
    public string DisplayMnemonic =>
      Kind switch
      {
        MergeKind.RegisterArith
          when !string.IsNullOrEmpty(SecondMnemonic) && !string.IsNullOrEmpty(ThirdMnemonic)
          => $"{Mnemonic.Trim()}{SecondMnemonic.Trim()}{ThirdMnemonic.Trim()}",
        MergeKind.Single when string.IsNullOrEmpty(SecondMnemonic) => Mnemonic,
        _ when !string.IsNullOrEmpty(SecondMnemonic) => $"{Mnemonic} {SecondMnemonic}",
        _ => Mnemonic,
      };

    /// <summary>How many underlying program steps this painted row covers.</summary>
    public int StepSpan => Kind switch
    {
      MergeKind.Single => InferSingleStepSpan(Mnemonic),
      MergeKind.RegisterArith => 3,
      _ => 2,
    };

    /// <summary>
    /// Fused card mnemonics like <c>STO 1</c> / <c>GTO A</c> occupy two keystrokes in one RAM step.
    /// </summary>
    private static int InferSingleStepSpan(string mnemonic)
    {
      string display = mnemonic.Trim();
      if (display.Length == 0)
      {
        return 1;
      }

      if (StudioFlowchartGraph.IsComparisonTestToken(display))
      {
        return 2;
      }

      if (StudioFlowchartGraph.TrySplitOpcodeTarget(display, out string op, out _))
      {
        return op is "STO" or "RCL" or "GTO" or "GSB" or "LBL" ? 2 : 1;
      }

      return 1;
    }

    public bool ContainsIndex(int stepIndex) =>
      stepIndex >= Index && stepIndex < Index + StepSpan;
  }

  /// <summary>
  /// 1-based display step number for the row: cumulative span of prior visible rows + 1
  /// (not dense 1..N row count). Merged pairs bump the following row by 2.
  /// </summary>
  public static int DisplayStepNumber(IReadOnlyList<Row> rows, int rowIndex)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (rowIndex < 0 || rowIndex >= rows.Count)
    {
      throw new ArgumentOutOfRangeException(nameof(rowIndex));
    }

    int n = 1;
    for (int i = 0; i < rowIndex; i++)
    {
      n += rows[i].StepSpan;
    }

    return n;
  }

  /// <summary>Largest <c>#</c> value shown (start index of the last visible row), or 0.</summary>
  public static int MaxDisplayStepNumber(IReadOnlyList<Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (rows.Count == 0)
    {
      return 0;
    }

    return DisplayStepNumber(rows, rows.Count - 1);
  }

  /// <summary>
  /// Keys keycaps + Legend column for a Studio row (display-only; RAM encoding unchanged).
  /// Fused vocab names (RDOWN, X&lt;&gt;Y, …) expand to prefix+CapFace keystrokes.
  /// </summary>
  public readonly record struct Paint(
    string KeysMnemonic,
    string Legend,
    StudioShiftLegend.ShiftKind LegendKind);

  /// <summary>
  /// Resolve Keys / Legend paint from a listing row for the active faceplate model.
  /// <paramref name="cardStripCaptions"/> supplies A–E strip captions from the loaded card
  /// (<see cref="CalcExplorerSession.CardStripLabels"/>) for <c>LBL</c> label rows.
  /// When null (no card), legends fall back to <see cref="ClassicCardStripLabels.DefaultNoCardCaptions"/>.
  /// </summary>
  public static Paint ResolvePaint(
    Row row,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions = null)
  {
    if (row.Kind == MergeKind.ShiftPair
        && !string.IsNullOrEmpty(row.SecondMnemonic))
    {
      string keys = row.DisplayMnemonic;
      if (StudioShiftLegend.TryResolve(
            modelId,
            row.Mnemonic,
            row.SecondMnemonic,
            out string legend,
            out StudioShiftLegend.ShiftKind kind))
      {
        return new Paint(keys, legend, kind);
      }

      return new Paint(keys, string.Empty, StudioShiftLegend.ShiftKind.None);
    }

    if (row.Kind == MergeKind.LabelPair)
    {
      return PaintLabelCaption(row.SecondMnemonic!, row.SecondMnemonic, cardStripCaptions);
    }

    if (row.Kind == MergeKind.RegisterArith
        && !string.IsNullOrEmpty(row.SecondMnemonic)
        && !string.IsNullOrEmpty(row.ThirdMnemonic))
    {
      // Keys: three keycaps; Legend: STO + 2 (same as PROCESS).
      string keys = $"{row.Mnemonic.Trim()} {row.SecondMnemonic.Trim()} {row.ThirdMnemonic.Trim()}";
      string legend = StudioProcessCaption.ResolvePart(row, modelId, cardStripCaptions);
      return new Paint(keys, legend, StudioShiftLegend.ShiftKind.None);
    }

    // Single step: expand fused g/f legends, else show vocab tokens as keycaps.
    if (StudioShiftLegend.TryExpandFusedMnemonic(
          modelId,
          row.Mnemonic,
          out string prefix,
          out string key,
          out string fusedLegend,
          out StudioShiftLegend.ShiftKind fusedKind))
    {
      return new Paint($"{prefix} {key}", fusedLegend, fusedKind);
    }

    // Same-line "g 4" that was not merge-paired (defensive).
    List<string> tokens = StudioMnemonicPaint.Tokenize(row.Mnemonic);
    if (tokens.Count >= 2
        && StudioShiftLegend.IsShiftPrefix(tokens[0])
        && StudioShiftLegend.TryResolve(
          modelId,
          tokens[0],
          tokens[1],
          out string inlineLegend,
          out StudioShiftLegend.ShiftKind inlineKind))
    {
      return new Paint(row.Mnemonic.Trim(), inlineLegend, inlineKind);
    }

    // Fused "LBL A" (or LBL + letter tokens) — strip caption, not CapAbove/CapSkirt.
    if (TryLabelTargetLetter(tokens, out string labelLetter))
    {
      return PaintLabelCaption(row.DisplayMnemonic, labelLetter, cardStripCaptions);
    }

    return new Paint(row.DisplayMnemonic, string.Empty, StudioShiftLegend.ShiftKind.None);
  }

  private static Paint PaintLabelCaption(
    string keysMnemonic,
    string? letter,
    IReadOnlyList<string>? cardStripCaptions)
  {
    if (!ClassicCardStripLabels.IsFaceplateLabelKey(letter))
    {
      return new Paint(keysMnemonic, string.Empty, StudioShiftLegend.ShiftKind.None);
    }

    string caption = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, letter);
    StudioShiftLegend.ShiftKind kind = ClassicCardStripLabels.UsesNoCardStripChrome(cardStripCaptions)
      ? StudioShiftLegend.ShiftKind.NoCardStrip
      : StudioShiftLegend.ShiftKind.CardStrip;
    return new Paint(letter!.Trim(), caption, kind);
  }

  /// <summary>
  /// True when tokens are <c>LBL</c> + A–E (merged display or single-line fused mnemonic).
  /// </summary>
  private static bool TryLabelTargetLetter(List<string> tokens, out string letter)
  {
    letter = string.Empty;
    if (tokens.Count < 2
        || !string.Equals(tokens[0], "LBL", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (!ClassicCardStripLabels.IsFaceplateLabelKey(tokens[1]))
    {
      return false;
    }

    letter = tokens[1].Trim();
    return letter.Length > 0;
  }

  public static bool IsRuntimeMarker(byte code) =>
    code is ClassicProgramCodes.Start or ClassicProgramCodes.Pointer;

  public static bool IsRuntimeMarkerMnemonic(string? mnemonic) =>
    !string.IsNullOrWhiteSpace(mnemonic)
    && (string.Equals(mnemonic.Trim(), "START", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mnemonic.Trim(), "PTR", StringComparison.OrdinalIgnoreCase));

  /// <summary>Next listing line that is not a Classic START/PTR runtime marker.</summary>
  private static bool TryGetNonMarker(
    IReadOnlyList<ClassicProgramLine> lines,
    int startIndex,
    out int index,
    out ClassicProgramLine line)
  {
    for (int i = startIndex; i < lines.Count; i++)
    {
      ClassicProgramLine candidate = lines[i];
      if (IsRuntimeMarker(candidate.Code) || IsRuntimeMarkerMnemonic(candidate.Mnemonic))
      {
        continue;
      }

      index = i;
      line = candidate;
      return true;
    }

    index = -1;
    line = default;
    return false;
  }

  /// <summary>
  /// Filter START/PTR and merge LBL+target / f|g|f-1|h+key into single display rows.
  /// Classic fall-through stubs (<c>LBL</c> + <c>NOP</c>/<c>RTN</c> only) stay in RAM
  /// for A–E key fidelity but are omitted from Studio listing / <c>#</c> / FC input.
  /// When <paramref name="cardAuthoringSteps"/> is set (card loaded), A–E routines that
  /// were not in the authoring file are also omitted (firmware may leave built-in bodies).
  /// </summary>
  public static IReadOnlyList<Row> Build(
    IReadOnlyList<ClassicProgramLine> lines,
    IReadOnlyList<string>? cardAuthoringSteps = null)
  {
    ArgumentNullException.ThrowIfNull(lines);
    List<Row> rows = [];
    for (int i = 0; i < lines.Count; i++)
    {
      ClassicProgramLine line = lines[i];
      if (IsRuntimeMarker(line.Code) || IsRuntimeMarkerMnemonic(line.Mnemonic))
      {
        continue;
      }

      // PTR/START may sit between a merge pair (LBL·A, f·9, …). Skip markers when
      // looking ahead so SST / Set start does not split or shrink the Studio listing.
      if (TryGetNonMarker(lines, i + 1, out int midIdx, out ClassicProgramLine mid)
          && TryGetNonMarker(lines, midIdx + 1, out int digIdx, out ClassicProgramLine dig)
          && TryMergeRegisterArith(line, mid, dig))
      {
        rows.Add(new Row(
          line.Index,
          line.Code,
          line.Mnemonic,
          mid.Code,
          mid.Mnemonic,
          MergeKind.RegisterArith,
          dig.Code,
          dig.Mnemonic));
        i = digIdx;
        continue;
      }

      if (TryGetNonMarker(lines, i + 1, out int nextIdx, out ClassicProgramLine next)
          && TryMerge(line, next, out MergeKind kind))
      {
        rows.Add(new Row(
          line.Index,
          line.Code,
          line.Mnemonic,
          next.Code,
          next.Mnemonic,
          kind));
        i = nextIdx;
        continue;
      }

      rows.Add(new Row(
        line.Index,
        line.Code,
        line.Mnemonic,
        SecondCode: null,
        SecondMnemonic: null,
        MergeKind.Single));
    }

    IReadOnlyList<Row> filtered = OmitEmptyStubRoutines(rows);
    filtered = OmitFirmwareBuiltinStripBodies(filtered);
    filtered = OmitDuplicateStripLabelRoutines(filtered);
    if (cardAuthoringSteps is not null)
    {
      filtered = OmitUnauthoredStripRoutines(filtered, cardAuthoringSteps);
    }
    else
    {
      filtered = EnsureNoCardStripCatalog(filtered);
    }

    return filtered;
  }

  /// <summary>
  /// Faceplate chrome is A–E filled on no-card; Classic RAM often omits <c>LBL E · X&lt;&gt;Y</c>.
  /// Synthesize any missing strip builtins so Studio/FC match the strip.
  /// </summary>
  public static IReadOnlyList<Row> EnsureNoCardStripCatalog(IReadOnlyList<Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (!StudioFlowchartGraph.IsNoCardFaceplateCatalogOnly(rows))
    {
      return rows;
    }

    HashSet<string> present = new(StringComparer.OrdinalIgnoreCase);
    foreach (Row row in rows)
    {
      if (StudioFlowchartGraph.TryGetLabelKey(row, out string key)
          && ClassicCardStripLabels.TryGetStripColumn(key, out _))
      {
        present.Add(key);
      }
    }

    string[] letters = ["A", "B", "C", "D", "E"];
    if (letters.All(present.Contains))
    {
      return rows;
    }

    List<Row> expanded = [.. rows];
    int nextIndex = rows.Count == 0 ? 0 : rows.Max(r => r.Index + r.StepSpan);
    foreach (string letter in letters)
    {
      if (present.Contains(letter))
      {
        continue;
      }

      AppendNoCardStripRoutine(expanded, ref nextIndex, letter);
    }

    return expanded;
  }

  private static void AppendNoCardStripRoutine(List<Row> rows, ref int nextIndex, string letter)
  {
    byte letterCode = letter switch
    {
      "A" => 30,
      "B" => 28,
      "C" => 27,
      "D" => 26,
      "E" => 24,
      _ => 0,
    };

    rows.Add(new Row(
      nextIndex,
      ClassicProgramCodes.Label,
      "LBL",
      letterCode,
      letter,
      MergeKind.LabelPair));
    nextIndex += 2;

    switch (letter)
    {
      case "A":
        rows.Add(new Row(nextIndex, 8, "g", 20, "4", MergeKind.ShiftPair));
        nextIndex += 2;
        break;
      case "B":
        rows.Add(new Row(nextIndex, 14, "f", 50, "9", MergeKind.ShiftPair));
        nextIndex += 2;
        break;
      case "C":
        rows.Add(new Row(nextIndex, 8, "g", 19, "5", MergeKind.ShiftPair));
        nextIndex += 2;
        break;
      case "D":
        rows.Add(new Row(nextIndex, 13, "RDOWN", null, null, MergeKind.Single));
        nextIndex += 1;
        break;
      case "E":
        rows.Add(new Row(nextIndex, 17, "X<>Y", null, null, MergeKind.Single));
        nextIndex += 1;
        break;
    }

    rows.Add(new Row(nextIndex, 42, "RTN", null, null, MergeKind.Single));
    nextIndex += 1;
  }

  /// <summary>
  /// Drop later A–E strip routines when Classic RAM repeats a label letter
  /// (firmware leftover after the real card body).
  /// </summary>
  public static IReadOnlyList<Row> OmitDuplicateStripLabelRoutines(IReadOnlyList<Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    return OmitLabelSpans(rows, span =>
    {
      if (!StudioFlowchartGraph.TryGetLabelKey(rows[span.First], out string key)
          || !ClassicCardStripLabels.TryGetStripColumn(key, out _))
      {
        return false;
      }

      for (int i = 0; i < span.First; i++)
      {
        if (StudioFlowchartGraph.TryGetLabelKey(rows[i], out string prior)
            && string.Equals(prior, key, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

      return false;
    });
  }

  /// <summary>
  /// Remove firmware no-card strip defaults left in Classic RAM (e.g. <c>LBL E · X&lt;&gt;Y · RTN</c>).
  /// </summary>
  public static IReadOnlyList<Row> OmitFirmwareBuiltinStripBodies(IReadOnlyList<Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    return OmitLabelSpans(
      rows,
      span => StudioFlowchartGraph.ShouldOmitFirmwareBuiltinStripRoutine(rows, span.First, span.Last));
  }

  /// <summary>
  /// Remove Classic injected fall-through stubs from a listing (same rule as FC).
  /// </summary>
  public static IReadOnlyList<Row> OmitEmptyStubRoutines(IReadOnlyList<Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    return OmitLabelSpans(rows, span => StudioFlowchartGraph.IsEmptyStubRoutine(rows, span.First, span.Last));
  }

  /// <summary>
  /// When a mag-card is loaded, drop A–E routines that were not in the authoring <c>[Code]</c>
  /// (injected stubs or firmware leftover built-ins such as <c>LBL E · X&lt;&gt;Y · RTN</c>).
  /// </summary>
  public static IReadOnlyList<Row> OmitUnauthoredStripRoutines(
    IReadOnlyList<Row> rows,
    IReadOnlyList<string> cardAuthoringSteps)
  {
    ArgumentNullException.ThrowIfNull(rows);
    ArgumentNullException.ThrowIfNull(cardAuthoringSteps);

    bool[] authored = ClassicCardStripLabels.SubroutinePresenceFromSteps(cardAuthoringSteps);
    return OmitLabelSpans(rows, span =>
    {
      if (!StudioFlowchartGraph.TryGetLabelKey(rows[span.First], out string key)
          || !ClassicCardStripLabels.TryGetStripColumn(key, out int column))
      {
        return false;
      }

      return !authored[column];
    });
  }

  private static IReadOnlyList<Row> OmitLabelSpans(
    IReadOnlyList<Row> rows,
    Func<(int First, int Last), bool> shouldDrop)
  {
    if (rows.Count == 0)
    {
      return rows;
    }

    List<int> labelStarts = [];
    for (int i = 0; i < rows.Count; i++)
    {
      if (StudioFlowchartGraph.TryGetLabelKey(rows[i], out _))
      {
        labelStarts.Add(i);
      }
    }

    if (labelStarts.Count == 0)
    {
      return rows;
    }

    bool[] drop = new bool[rows.Count];
    for (int s = 0; s < labelStarts.Count; s++)
    {
      int first = labelStarts[s];
      int last = s + 1 < labelStarts.Count ? labelStarts[s + 1] - 1 : rows.Count - 1;
      if (!shouldDrop((first, last)))
      {
        continue;
      }

      for (int i = first; i <= last; i++)
      {
        drop[i] = true;
      }
    }

    if (!drop.Contains(true))
    {
      return rows;
    }

    List<Row> filtered = new(rows.Count);
    for (int i = 0; i < rows.Count; i++)
    {
      if (!drop[i])
      {
        filtered.Add(rows[i]);
      }
    }

    return filtered;
  }

  /// <summary>
  /// User-facing dual TSV: same filter as Studio paint (no START/PTR).
  /// Merged pairs stay as two TSV lines so paste/RAM encoding is unchanged.
  /// </summary>
  public static IEnumerable<ClassicProgramLine> FilterForClipboard(
    IEnumerable<ClassicProgramLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);
    foreach (ClassicProgramLine line in lines)
    {
      if (IsRuntimeMarker(line.Code) || IsRuntimeMarkerMnemonic(line.Mnemonic))
      {
        continue;
      }

      yield return line;
    }
  }

  /// <summary>
  /// Pointer highlight target among visible rows: prefer the row that contains the
  /// pointer index; if the pointer sits on a hidden START/PTR, use the next visible row.
  /// </summary>
  public static int ResolvePointerHighlightIndex(
    IReadOnlyList<ClassicProgramLine> lines,
    IReadOnlyList<Row> rows)
  {
    int pointer = ClassicProgramListing.FindPointerIndex(lines);
    if (pointer < 0 || rows.Count == 0)
    {
      return -1;
    }

    foreach (Row row in rows)
    {
      if (row.ContainsIndex(pointer))
      {
        return row.Index;
      }
    }

    foreach (Row row in rows)
    {
      if (row.Index > pointer)
      {
        return row.Index;
      }
    }

    return rows[^1].Index;
  }

  private static bool TryMerge(
    ClassicProgramLine first,
    ClassicProgramLine second,
    out MergeKind kind)
  {
    kind = MergeKind.Single;
    if (IsRuntimeMarker(second.Code) || IsRuntimeMarkerMnemonic(second.Mnemonic))
    {
      return false;
    }

    if (string.Equals(first.Mnemonic.Trim(), "LBL", StringComparison.OrdinalIgnoreCase))
    {
      kind = MergeKind.LabelPair;
      return true;
    }

    if (TryMergeBranchPair(first, second))
    {
      kind = MergeKind.BranchPair;
      return true;
    }

    if (TryMergeOpDigitPair(first, second))
    {
      kind = MergeKind.OpDigitPair;
      return true;
    }

    if (StudioShiftLegend.IsShiftPrefix(first.Mnemonic.Trim())
        && !StudioShiftLegend.IsShiftPrefix(second.Mnemonic.Trim())
        && !first.Mnemonic.Trim().Contains(' ', StringComparison.Ordinal))
    {
      kind = MergeKind.ShiftPair;
      return true;
    }

    return false;
  }

  private static bool TryMergeBranchPair(ClassicProgramLine first, ClassicProgramLine second)
  {
    string op = first.Mnemonic.Trim();
    if (!string.Equals(op, "GTO", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(op, "GSB", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return IsBranchTargetToken(second.Mnemonic);
  }

  private static bool TryMergeOpDigitPair(ClassicProgramLine first, ClassicProgramLine second)
  {
    string op = first.Mnemonic.Trim();
    if (!string.Equals(op, "STO", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(op, "RCL", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return IsRegisterDigitToken(second.Mnemonic);
  }

  private static bool IsBranchTargetToken(string? mnemonic)
  {
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return false;
    }

    string t = mnemonic.Trim();
    if (t.Length is 0 or > 2)
    {
      return false;
    }

    if (ClassicCardStripLabels.TryGetStripColumn(t, out _))
    {
      return true;
    }

    return t.Length == 1 && (char.ToUpperInvariant(t[0]) is >= 'A' and <= 'E' or >= '0' and <= '9');
  }

  private static bool IsRegisterDigitToken(string? mnemonic)
  {
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return false;
    }

    string t = mnemonic.Trim();
    return t.Length == 1 && t[0] is >= '0' and <= '9';
  }

  /// <summary>
  /// HP-65 register arithmetic is three unmerged keystrokes (<c>STO</c> <c>+</c> <c>2</c>);
  /// Studio paints one row <c>STO+2</c> / legend <c>STO + 2</c> without changing RAM.
  /// </summary>
  private static bool TryMergeRegisterArith(
    ClassicProgramLine first,
    ClassicProgramLine second,
    ClassicProgramLine third)
  {
    // Use mnemonic markers only — operator keycodes must not collide with PTR (61).
    if (IsRuntimeMarkerMnemonic(second.Mnemonic)
        || IsRuntimeMarkerMnemonic(third.Mnemonic)
        || IsRuntimeMarkerMnemonic(first.Mnemonic))
    {
      return false;
    }

    string op = first.Mnemonic.Trim();
    if (!string.Equals(op, "STO", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(op, "RCL", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    // Fused "STO 1" is a single vocab step — not this triple.
    if (op.Contains(' ', StringComparison.Ordinal))
    {
      return false;
    }

    string arith = second.Mnemonic.Trim();
    if (arith is not ("+" or "-" or "*" or "/" or "×" or "÷"))
    {
      return false;
    }

    string dig = third.Mnemonic.Trim();
    return dig.Length == 1 && dig[0] is >= '0' and <= '9';
  }
}
