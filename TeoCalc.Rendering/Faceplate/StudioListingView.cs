using TeoCalc.Core.Engine.Classic;

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
  }

  /// <summary>
  /// One painted Studio row (may span two program steps).
  /// <see cref="Index"/> is the RAM program address of the first step in the row
  /// (used for selection / pointer); the Studio <c>#</c> column shows a separate
  /// 1-based display sequence over visible rows only.
  /// </summary>
  public readonly record struct Row(
    int Index,
    byte Code,
    string Mnemonic,
    byte? SecondCode,
    string? SecondMnemonic,
    MergeKind Kind)
  {
    public string DisplayMnemonic =>
      Kind == MergeKind.Single || string.IsNullOrEmpty(SecondMnemonic)
        ? Mnemonic
        : $"{Mnemonic} {SecondMnemonic}";

    public bool ContainsIndex(int stepIndex) =>
      Index == stepIndex
      || (Kind != MergeKind.Single && Index + 1 == stepIndex);
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
  /// </summary>
  public static Paint ResolvePaint(Row row, string? modelId)
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
      return new Paint(row.DisplayMnemonic, string.Empty, StudioShiftLegend.ShiftKind.None);
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

    return new Paint(row.DisplayMnemonic, string.Empty, StudioShiftLegend.ShiftKind.None);
  }

  public static bool IsRuntimeMarker(byte code) =>
    code is ClassicProgramCodes.Start or ClassicProgramCodes.Pointer;

  public static bool IsRuntimeMarkerMnemonic(string? mnemonic) =>
    !string.IsNullOrWhiteSpace(mnemonic)
    && (string.Equals(mnemonic.Trim(), "START", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mnemonic.Trim(), "PTR", StringComparison.OrdinalIgnoreCase));

  /// <summary>
  /// Filter START/PTR and merge LBL+target / f|g|f-1|h+key into single display rows.
  /// </summary>
  public static IReadOnlyList<Row> Build(IReadOnlyList<ClassicProgramLine> lines)
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

      if (i + 1 < lines.Count
          && TryMerge(line, lines[i + 1], out MergeKind kind))
      {
        ClassicProgramLine next = lines[i + 1];
        rows.Add(new Row(
          line.Index,
          line.Code,
          line.Mnemonic,
          next.Code,
          next.Mnemonic,
          kind));
        i++;
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

    return rows;
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

    if (StudioShiftLegend.IsShiftPrefix(first.Mnemonic.Trim())
        && !StudioShiftLegend.IsShiftPrefix(second.Mnemonic.Trim())
        && !first.Mnemonic.Trim().Contains(' ', StringComparison.Ordinal))
    {
      kind = MergeKind.ShiftPair;
      return true;
    }

    return false;
  }
}
