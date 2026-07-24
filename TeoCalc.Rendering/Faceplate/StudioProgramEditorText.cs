using System.Globalization;
using System.Text;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Pure hydrate / parse helpers for Studio program text (clipboard / tests).
/// W/PRGM authoring uses the shared Code listing + faceplate keys (not a separate editor pane).
/// </summary>
public static class StudioProgramEditorText
{
  public sealed class EditorStep
  {
    public string MachineA { get; set; } = string.Empty;

    public string MachineB { get; set; } = string.Empty;

    public string Keys { get; set; } = string.Empty;

    /// <summary>RAM program address of the first byte in this row.</summary>
    public int RamIndex { get; set; }

    /// <summary>How many program bytes this row occupies (1 for fused, 2 for ShiftPair, …).</summary>
    public int ByteSpan { get; set; } = 1;

    /// <summary>1-based display step number (advances by <see cref="ByteSpan"/>).</summary>
    public int DisplayStep { get; set; }

    public string MachineLine
    {
      get
      {
        string a = MachineA.Trim();
        string b = MachineB.Trim();
        if (a.Length == 0)
        {
          return string.Empty;
        }

        return b.Length == 0 ? a : $"{a} {b}";
      }
    }
  }

  public static string Fingerprint(IEnumerable<ClassicProgramLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);
    StringBuilder sb = new();
    foreach (ClassicProgramLine line in ContentLines(lines))
    {
      if (sb.Length > 0)
      {
        sb.Append(',');
      }

      sb.Append(line.Code.ToString(CultureInfo.InvariantCulture));
    }

    return sb.ToString();
  }

  /// <summary>Non-runtime lines up to the last non-NOP (mid-program NOPs kept).</summary>
  public static IReadOnlyList<ClassicProgramLine> ContentLines(IEnumerable<ClassicProgramLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);
    List<ClassicProgramLine> filtered = StudioListingView.FilterForClipboard(lines).ToList();
    int last = filtered.FindLastIndex(static l => l.Code != 0);
    if (last < 0)
    {
      return [];
    }

    return filtered.GetRange(0, last + 1);
  }

  public static void Hydrate(
    IEnumerable<ClassicProgramLine> lines,
    string? modelId,
    out string machineText,
    out string keysText)
  {
    ArgumentNullException.ThrowIfNull(lines);
    StringBuilder machine = new();
    StringBuilder keys = new();
    foreach (ClassicProgramLine line in ContentLines(lines))
    {
      if (machine.Length > 0)
      {
        machine.AppendLine();
        keys.AppendLine();
      }

      machine.Append(StudioMuseumKeycodes.FormatMachineDisplay(line.Code, line.Mnemonic, modelId));
      keys.Append(line.Mnemonic);
    }

    machineText = machine.ToString();
    keysText = keys.ToString();
  }

  public static List<EditorStep> HydrateSteps(
    IEnumerable<ClassicProgramLine> lines,
    string? modelId)
  {
    ArgumentNullException.ThrowIfNull(lines);
    IReadOnlyList<ClassicProgramLine> content = ContentLines(lines);
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(content, null);
    List<EditorStep> steps = [];
    for (int i = 0; i < rows.Count; i++)
    {
      StudioListingView.Row row = rows[i];
      StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId);
      string museum = StudioMuseumKeycodes.FormatMachineDisplay(row, modelId);
      SplitMuseum(museum, out string a, out string b);
      steps.Add(
        new EditorStep
        {
          MachineA = a,
          MachineB = b,
          Keys = paint.KeysMnemonic,
          RamIndex = row.Index,
          ByteSpan = Math.Max(1, row.StepSpan),
          DisplayStep = StudioListingView.DisplayStepNumber(rows, i),
        });
    }

    return steps;
  }

  /// <summary>Sum of committed row byte spans (program bytes used).</summary>
  public static int CountUsedBytes(IReadOnlyList<EditorStep> steps)
  {
    ArgumentNullException.ThrowIfNull(steps);
    int n = 0;
    foreach (EditorStep step in steps)
    {
      n += Math.Max(1, step.ByteSpan);
    }

    return n;
  }

  public static void SplitMuseum(string museum, out string machineA, out string machineB)
  {
    machineA = string.Empty;
    machineB = string.Empty;
    if (string.IsNullOrWhiteSpace(museum))
    {
      return;
    }

    string[] parts = museum.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length >= 1)
    {
      machineA = parts[0];
    }

    if (parts.Length >= 2)
    {
      machineB = parts[1];
    }
  }

  /// <summary>Machine LED boxes → Keys (fused single-byte or prefix+key pair).</summary>
  public static void SyncFromMachine(
    EditorStep step,
    string? modelId,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(step);
    ArgumentNullException.ThrowIfNull(formatMnemonic);

    string line = step.MachineLine;
    if (line.Length == 0)
    {
      step.Keys = string.Empty;
      return;
    }

    // Fused single RAM byte with museum pair display (e.g. RCL 1 → 34 01).
    if (StudioMuseumKeycodes.TryResolveMachineLine(
          line,
          modelId,
          formatMnemonic,
          out byte fused,
          out _))
    {
      step.Keys = formatMnemonic(fused);
      string museum = StudioMuseumKeycodes.FormatMachineDisplay(fused, step.Keys, modelId);
      if (LooksLikeMuseumOrPair(museum))
      {
        SplitMuseum(museum, out string a, out string b);
        step.MachineA = a;
        step.MachineB = b;
      }

      return;
    }

    // Two museum boxes → two key tokens (ShiftPair / LBL A / …).
    string aTok = step.MachineA.Trim();
    string bTok = step.MachineB.Trim();
    if (aTok.Length == 0)
    {
      step.Keys = string.Empty;
      return;
    }

    if (!StudioMuseumKeycodes.TryResolveMachineLine(
          aTok,
          modelId,
          formatMnemonic,
          out byte codeA,
          out _))
    {
      return;
    }

    string keysA = formatMnemonic(codeA);
    if (bTok.Length == 0)
    {
      step.Keys = keysA;
      return;
    }

    if (!StudioMuseumKeycodes.TryResolveMachineLine(
          bTok,
          modelId,
          formatMnemonic,
          out byte codeB,
          out _))
    {
      step.Keys = keysA;
      return;
    }

    step.Keys = $"{keysA} {formatMnemonic(codeB)}";
  }

  /// <summary>Keys mnemonic → Machine LED boxes.</summary>
  public static void SyncFromKeys(
    EditorStep step,
    string? modelId,
    Func<string, byte?> resolveMnemonic,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(step);
    ArgumentNullException.ThrowIfNull(resolveMnemonic);
    ArgumentNullException.ThrowIfNull(formatMnemonic);

    string keys = step.Keys.Trim();
    if (keys.Length == 0)
    {
      step.MachineA = string.Empty;
      step.MachineB = string.Empty;
      return;
    }

    List<string> tokens = StudioMnemonicPaint.Tokenize(keys);

    // Incomplete bare prefix: show first museum box only.
    if (StudioMuseumPrefix.NeedsSecondToken(keys) && tokens.Count < 2)
    {
      if (StudioMuseumKeycodes.TryFormatMuseum(keys, modelId, out string prefixMuseum))
      {
        SplitMuseum(prefixMuseum, out string a, out string b);
        step.MachineA = a;
        step.MachineB = b;
      }

      return;
    }

    // Fused single-token mnemonic (RCL 1, RDOWN, …).
    if (resolveMnemonic(keys) is byte fused)
    {
      string museum = StudioMuseumKeycodes.FormatMachineDisplay(fused, keys, modelId);
      if (LooksLikeMuseumOrPair(museum))
      {
        SplitMuseum(museum, out string a, out string b);
        step.MachineA = a;
        step.MachineB = b;
      }
      else
      {
        step.MachineA = museum;
        step.MachineB = string.Empty;
      }

      return;
    }

    // Multi-token pair (g 4 / LBL A): one museum box per token.
    if (tokens.Count >= 2
        && StudioMuseumKeycodes.TryFormatMuseumPair(tokens[0], tokens[1], modelId, out string pair))
    {
      SplitMuseum(pair, out string a, out string b);
      step.MachineA = a;
      step.MachineB = b;
      return;
    }

    if (tokens.Count >= 1
        && StudioMuseumKeycodes.TryFormatMuseum(tokens[0], modelId, out string firstMuseum))
    {
      SplitMuseum(firstMuseum, out string a, out string b);
      step.MachineA = a;
      step.MachineB = b;
    }
  }

  public static bool ShowSecondMachineBox(
    EditorStep step,
    string? modelId,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(step);
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    if (step.MachineB.Trim().Length > 0)
    {
      return true;
    }

    if (StudioMuseumPrefix.NeedsSecondToken(step.Keys))
    {
      return true;
    }

    return StudioMuseumPrefix.NeedsSecondMuseumBox(step.MachineA, modelId, formatMnemonic);
  }

  /// <summary>
  /// Convert editor steps to program bytes; rejects incomplete prefixes.
  /// A Keys/Machine pair may expand to one fused byte (RCL 1) or two bytes (g + 4).
  /// </summary>
  public static bool TryApplySteps(
    IReadOnlyList<EditorStep> steps,
    string? modelId,
    Func<string, byte?> resolveMnemonic,
    Func<byte, string> formatMnemonic,
    out List<byte> codes,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(steps);
    ArgumentNullException.ThrowIfNull(resolveMnemonic);
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    codes = [];
    error = null;

    List<EditorStep> nonEmpty = [];
    for (int i = 0; i < steps.Count; i++)
    {
      EditorStep step = steps[i];
      bool empty = step.MachineLine.Length == 0 && string.IsNullOrWhiteSpace(step.Keys);
      if (empty)
      {
        continue;
      }

      if (StudioMuseumPrefix.IsIncompletePrefixStep(
            step.MachineA,
            step.MachineB,
            step.Keys,
            modelId,
            formatMnemonic))
      {
        error =
          $"Step {i + 1}: incomplete prefix (needs a follow-up key, e.g. g 4 / 35 04).";
        return false;
      }

      nonEmpty.Add(step);
    }

    if (nonEmpty.Count == 0)
    {
      error = "Editor is empty.";
      return false;
    }

    foreach (EditorStep step in nonEmpty)
    {
      if (!TryStepToCodes(step, modelId, resolveMnemonic, formatMnemonic, out List<byte> stepCodes, out error))
      {
        return false;
      }

      codes.AddRange(stepCodes);
    }

    return true;
  }

  private static bool TryStepToCodes(
    EditorStep step,
    string? modelId,
    Func<string, byte?> resolveMnemonic,
    Func<byte, string> formatMnemonic,
    out List<byte> codes,
    out string? error)
  {
    codes = [];
    error = null;
    string keys = step.Keys.Trim();
    string machine = step.MachineLine;

    if (keys.Length > 0)
    {
      if (resolveMnemonic(keys) is byte fused)
      {
        codes.Add(fused);
        return true;
      }

      List<string> tokens = StudioMnemonicPaint.Tokenize(keys);
      if (tokens.Count == 0)
      {
        error = $"Could not parse Keys '{keys}'.";
        return false;
      }

      foreach (string token in tokens)
      {
        byte? code = resolveMnemonic(token);
        if (code is null)
        {
          error = $"Mnemonic not found: {token}";
          return false;
        }

        codes.Add(code.Value);
      }

      return true;
    }

    if (machine.Length == 0)
    {
      error = "Empty step.";
      return false;
    }

    if (StudioMuseumKeycodes.TryResolveMachineLine(
          machine,
          modelId,
          formatMnemonic,
          out byte one,
          out _))
    {
      codes.Add(one);
      return true;
    }

    // Two museum boxes as two program bytes.
    string a = step.MachineA.Trim();
    string b = step.MachineB.Trim();
    if (a.Length == 0 || b.Length == 0)
    {
      error = $"Unknown museum machine code '{machine}'.";
      return false;
    }

    if (!StudioMuseumKeycodes.TryResolveMachineLine(a, modelId, formatMnemonic, out byte codeA, out error)
        || !StudioMuseumKeycodes.TryResolveMachineLine(b, modelId, formatMnemonic, out byte codeB, out error))
    {
      return false;
    }

    codes.Add(codeA);
    codes.Add(codeB);
    return true;
  }

  /// <summary>
  /// Parse dual panes into program bytes. When both sides have content, both must resolve
  /// to the same sequence; otherwise the non-empty side wins.
  /// </summary>
  public static bool TryParseDual(
    string machineText,
    string keysText,
    string? modelId,
    Func<string, byte?> resolveMnemonic,
    Func<byte, string> formatMnemonic,
    out List<byte> codes,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(resolveMnemonic);
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    codes = [];
    error = null;

    bool hasMachine = HasNonEmptyLine(machineText);
    bool hasKeys = HasNonEmptyLine(keysText);
    if (!hasMachine && !hasKeys)
    {
      error = "Editor is empty.";
      return false;
    }

    // Reject incomplete prefix lines (g alone / 35 alone) before bulk parse.
    if (hasMachine)
    {
      string[] machineLines = SplitLines(machineText);
      string[] keyLines = SplitLines(keysText);
      for (int i = 0; i < machineLines.Length; i++)
      {
        string m = machineLines[i];
        if (m.Length == 0)
        {
          continue;
        }

        SplitMuseum(m, out string a, out string b);
        string k = i < keyLines.Length ? keyLines[i] : string.Empty;
        if (StudioMuseumPrefix.IsIncompletePrefixStep(a, b, k, modelId, formatMnemonic))
        {
          error =
            $"Line {i + 1}: incomplete prefix (needs a follow-up key, e.g. g 4 / 35 04).";
          return false;
        }
      }
    }
    else if (hasKeys)
    {
      string[] keyLines = SplitLines(keysText);
      for (int i = 0; i < keyLines.Length; i++)
      {
        string k = keyLines[i];
        if (k.Length == 0)
        {
          continue;
        }

        if (StudioMuseumPrefix.IsIncompletePrefixStep(
              string.Empty,
              string.Empty,
              k,
              modelId,
              formatMnemonic))
        {
          error =
            $"Line {i + 1}: incomplete prefix (needs a follow-up key, e.g. g 4 / 35 04).";
          return false;
        }
      }
    }

    List<byte> keyCodes = [];
    List<byte> machineCodes = [];

    if (hasKeys
        && !TryParseKeys(keysText, resolveMnemonic, out keyCodes, out error))
    {
      return false;
    }

    if (hasMachine
        && !TryParseMachine(machineText, modelId, formatMnemonic, out machineCodes, out error))
    {
      return false;
    }

    if (hasKeys && hasMachine)
    {
      if (keyCodes.Count != machineCodes.Count)
      {
        error =
          $"Machine/Keys line count mismatch ({machineCodes.Count} vs {keyCodes.Count}).";
        return false;
      }

      for (int i = 0; i < keyCodes.Count; i++)
      {
        if (keyCodes[i] != machineCodes[i])
        {
          error =
            $"Line {i + 1}: Machine and Keys disagree "
            + $"({machineCodes[i]} vs {keyCodes[i]}).";
          return false;
        }
      }

      codes = keyCodes;
      return true;
    }

    codes = hasKeys ? keyCodes : machineCodes;
    return true;
  }

  public static bool TryParseKeys(
    string text,
    Func<string, byte?> resolveMnemonic,
    out List<byte> codes,
    out string? error) =>
    UserProgramClipboard.TryParse(
      text,
      CardCodeEncoding.Mnemonic,
      resolveMnemonic,
      out codes,
      out error);

  public static bool TryParseMachine(
    string text,
    string? modelId,
    Func<byte, string> formatMnemonic,
    out List<byte> codes,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    codes = [];
    error = null;
    if (string.IsNullOrWhiteSpace(text))
    {
      error = "Machine pane is empty.";
      return false;
    }

    foreach (string trimmed in SplitLines(text))
    {
      if (trimmed.Length == 0)
      {
        continue;
      }

      if (!StudioMuseumKeycodes.TryResolveMachineLine(
            trimmed,
            modelId,
            formatMnemonic,
            out byte code,
            out error))
      {
        codes = [];
        return false;
      }

      codes.Add(code);
    }

    if (codes.Count == 0)
    {
      error = "Machine pane is empty.";
      return false;
    }

    return true;
  }

  private static bool LooksLikeMuseumOrPair(string museum)
  {
    if (string.IsNullOrWhiteSpace(museum))
    {
      return false;
    }

    string[] parts = museum.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length is < 1 or > 2)
    {
      return false;
    }

    foreach (string part in parts)
    {
      if (part.Length != 2 || !char.IsDigit(part[0]) || !char.IsDigit(part[1]))
      {
        return false;
      }
    }

    return true;
  }

  private static string[] SplitLines(string text) =>
    text.Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Split('\n')
      .Select(static l => l.Trim())
      .ToArray();

  private static bool HasNonEmptyLine(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return false;
    }

    foreach (string raw in SplitLines(text))
    {
      if (raw.Length > 0)
      {
        return true;
      }
    }

    return false;
  }
}
