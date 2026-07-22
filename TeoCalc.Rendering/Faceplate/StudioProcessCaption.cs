using System.Linq;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Human-readable PROCESS box labels: strip captions, compact register algebra
/// (<c>R2 / R1</c>, <c>STO + 1</c>, <c>√R1</c>), else middot-joined Studio legends.
/// </summary>
public static class StudioProcessCaption
{
  /// <summary>
  /// Build a PROCESS caption for rows <paramref name="first"/>…<paramref name="last"/>.
  /// When the chunk is the sole body under LBL A–E and a strip caption exists, prefer that
  /// caption (<c>+123</c>, <c>Σ+</c>, <c>x̄</c>). Otherwise compact register expressions.
  /// </summary>
  public static string Build(
    IReadOnlyList<StudioListingView.Row> rows,
    int first,
    int last,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    string? labelKey = null,
    bool preferStripCaption = false)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (first < 0 || last < first || last >= rows.Count)
    {
      return string.Empty;
    }

    if (preferStripCaption
        && !string.IsNullOrEmpty(labelKey)
        && ClassicCardStripLabels.TryGetStripColumn(labelKey, out _))
    {
      string strip = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, labelKey);
      if (!string.IsNullOrWhiteSpace(strip)
          && !ClassicCardStripLabels.UsesNoCardStripChrome(cardStripCaptions))
      {
        return strip.Trim();
      }
    }

    List<string> parts = [];
    for (int i = first; i <= last; i++)
    {
      if (StudioFlowchartGraph.IsNopRow(rows[i]))
      {
        continue;
      }

      string part = ResolvePart(rows[i], modelId, cardStripCaptions);
      if (part.Length > 0)
      {
        parts.Add(part);
      }
    }

    if (parts.Count == 0)
    {
      return string.Empty;
    }

    // Fold digit-run + trailing op anywhere in the chunk (1 + → +1; 1 2 3 + → +123)
    // before register algebra / middot join — chebyshev "RCL 2 · 1 · +" etc.
    parts = RewriteDigitOpRuns(parts);

    if (TryCompact(parts, out string compact))
    {
      return RewriteDigitOpInCaption(compact);
    }

    // Digit-run + trailing op (1 2 3 +) → "+123" even without a strip caption.
    if (TryFormatDigitOpSoup(parts, out string soup))
    {
      if (preferStripCaption
          && !string.IsNullOrEmpty(labelKey)
          && !ClassicCardStripLabels.UsesNoCardStripChrome(cardStripCaptions))
      {
        string strip = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, labelKey);
        if (!string.IsNullOrWhiteSpace(strip))
        {
          return strip.Trim();
        }
      }

      return soup;
    }

    if (parts.Count <= 4)
    {
      string joined = string.Join(" · ", parts);
      if (joined.Length <= 40)
      {
        return RewriteDigitOpInCaption(joined);
      }
    }

    return RewriteDigitOpInCaption(string.Join("\n", parts));
  }

  /// <summary>
  /// Legacy arrow glyph (→). Kept for callers that still probe; new captions use STO/RCL words.
  /// </summary>
  public const char RegisterArrow = '\u2192'; // →

  /// <summary>
  /// Normalize store/recall tokens to colored-word forms (no arrows):
  /// <c>STO R1</c>/<c>STO + 1</c>, <c>RCL R1</c>/<c>RCL + 1</c>. Lone <c>R1</c> stays for algebra.
  /// </summary>
  public static string ToRegisterLabel(string token)
  {
    string s = token.Trim();
    if (s.Length == 0)
    {
      return s;
    }

    // Already STO… / RCL…
    if (s.StartsWith("STO", StringComparison.OrdinalIgnoreCase)
        || s.StartsWith("RCL", StringComparison.OrdinalIgnoreCase))
    {
      if (TryParseStoOpDigit(s, out string head, out string op, out string dig))
      {
        return FormatRegisterArith(head, op, dig);
      }

      if (TryParseStoRExpr(s, out string norm))
      {
        return norm;
      }

      if (s.StartsWith("STO", StringComparison.OrdinalIgnoreCase))
      {
        string rest = s[3..].Trim();
        if (rest.StartsWith('R') || rest.StartsWith('r'))
        {
          rest = rest[1..];
        }

        if (IsDigitToken(rest))
        {
          return "STO R" + rest.Trim();
        }
      }

      if (s.StartsWith("RCL", StringComparison.OrdinalIgnoreCase))
      {
        string rest = s[3..].Trim();
        if (rest.StartsWith('R') || rest.StartsWith('r'))
        {
          rest = rest[1..];
        }

        if (IsDigitToken(rest))
        {
          return "RCL R" + rest.Trim();
        }
      }

      return s;
    }

    // Legacy arrow forms → word forms.
    if (s[0] == RegisterArrow)
    {
      string rest = s[1..];
      if (rest.Length >= 2 && rest[0] is '+' or '-' or '*' or '/')
      {
        string reg = rest[1..].Trim();
        if (reg.StartsWith('R') || reg.StartsWith('r'))
        {
          reg = reg[1..];
        }

        return FormatRegisterArith("STO", rest[0].ToString(), reg);
      }

      if (rest.StartsWith('R') || rest.StartsWith('r'))
      {
        return "STO " + "R" + rest[1..].TrimStart('R', 'r');
      }

      if (IsDigitToken(rest))
      {
        return "STO R" + rest;
      }

      return "STO " + rest;
    }

    // R1→ / R2→+ → RCL R1 / RCL+R2
    int arrowAt = s.IndexOf(RegisterArrow);
    if (arrowAt >= 2
        && (s[0] is 'R' or 'r')
        && char.IsDigit(s[1]))
    {
      string rn = "R" + s[1..arrowAt].Trim();
      string trail = arrowAt + 1 < s.Length ? s[(arrowAt + 1)..] : string.Empty;
      if (trail.Length > 0 && trail[0] is '+' or '-' or '*' or '/')
      {
        string reg = rn.Length > 1 ? rn[1..] : string.Empty;
        return FormatRegisterArith("RCL", trail[0].ToString(), reg);
      }

      return "RCL " + rn;
    }

    return s;
  }

  /// <summary>Obsolete alias — prefer <see cref="ToRegisterLabel"/>.</summary>
  public static string ToArrowGlyph(string token) => ToRegisterLabel(token);

  /// <summary>Legend / Keys text for one listing row (before chunk compaction).</summary>
  public static string ResolvePart(
    StudioListingView.Row row,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    string part;
    if (row.Kind == StudioListingView.MergeKind.RegisterArith
        && !string.IsNullOrEmpty(row.SecondMnemonic)
        && !string.IsNullOrEmpty(row.ThirdMnemonic))
    {
      string op = NormalizeBinaryOp(row.SecondMnemonic);
      string dig = row.ThirdMnemonic.Trim();
      string head = row.Mnemonic.Trim().ToUpperInvariant();
      if (op.Length > 0 && IsDigitToken(dig) && head is "STO" or "RCL")
      {
        // PROCESS / Legend: STO + 2 (STO drawn red in FC); listing Keys stay STO + 2.
        part = FormatRegisterArith(head, op, dig);
      }
      else
      {
        part = $"{row.Mnemonic.Trim()}{row.SecondMnemonic.Trim()}{row.ThirdMnemonic.Trim()}";
      }
    }
    else
    {
      StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId, cardStripCaptions);
      if (!string.IsNullOrWhiteSpace(paint.Legend)
          && row.Kind != StudioListingView.MergeKind.LabelPair
          && paint.LegendKind is not (
            StudioShiftLegend.ShiftKind.CardStrip
            or StudioShiftLegend.ShiftKind.NoCardStrip))
      {
        part = paint.Legend.Trim();
      }
      else if (!string.IsNullOrWhiteSpace(paint.KeysMnemonic))
      {
        part = paint.KeysMnemonic.Trim();
      }
      else
      {
        part = row.DisplayMnemonic.Trim();
      }
    }

    return part.Length == 0 ? part : ToRegisterLabel(part);
  }

  /// <summary>
  /// Compact a list of row captions into algebraic form when patterns match.
  /// Examples: <c>RCL 2,RCL 1,/</c> → <c>R2 / R1</c>; <c>R1,√x</c> → <c>√R1</c>;
  /// <c>STO,+ ,1</c> (already fused) → <c>STO + 1</c>.
  /// </summary>
  public static bool TryCompact(IReadOnlyList<string> parts, out string compact)
  {
    compact = string.Empty;
    if (parts.Count == 0)
    {
      return false;
    }

    List<string> tokens = [];
    for (int i = 0; i < parts.Count;)
    {
      if (TryConsumeRegisterArith(parts, i, out string fused, out int consumed)
          || TryConsumeFusedStoRcl(parts, i, out fused, out consumed))
      {
        tokens.Add(fused);
        i += consumed;
        continue;
      }

      tokens.Add(parts[i]);
      i++;
    }

    List<string> folded = [];
    for (int i = 0; i < tokens.Count;)
    {
      if (i + 1 < tokens.Count
          && TryRegisterRef(tokens[i], out string rn)
          && TryFormatUnary(tokens[i + 1], rn, out string unary))
      {
        folded.Add(unary);
        i += 2;
        continue;
      }

      if (i + 2 < tokens.Count
          && TryRegisterRef(tokens[i], out string left)
          && TryRegisterRef(tokens[i + 1], out string right)
          && IsBinaryOp(tokens[i + 2], out string op))
      {
        folded.Add(left + " " + op + " " + right);
        i += 3;
        continue;
      }

      if (TryRegisterRef(tokens[i], out string alone))
      {
        folded.Add(alone);
        i++;
        continue;
      }

      folded.Add(ToRegisterLabel(tokens[i]));
      i++;
    }

    if (folded.Count == 1
        && !string.Equals(folded[0], parts[0], StringComparison.Ordinal)
        && !IsPlainMiddotJoin(folded[0]))
    {
      compact = folded[0];
      return true;
    }

    if (folded.Count >= 1 && WasCompacted(parts, folded))
    {
      compact = folded.Count <= 4 && string.Join(" · ", folded).Length <= 40
        ? string.Join(" · ", folded)
        : string.Join("\n", folded);
      return true;
    }

    return false;
  }

  private static bool WasCompacted(IReadOnlyList<string> original, IReadOnlyList<string> folded)
  {
    if (folded.Count < original.Count)
    {
      return true;
    }

    for (int i = 0; i < folded.Count && i < original.Count; i++)
    {
      if (!string.Equals(folded[i], original[i], StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static bool IsPlainMiddotJoin(string s) => s.Contains(" · ", StringComparison.Ordinal);

  private static bool TryConsumeRegisterArith(
    IReadOnlyList<string> parts,
    int index,
    out string fused,
    out int consumed)
  {
    fused = string.Empty;
    consumed = 0;
    if (index + 2 >= parts.Count)
    {
      return false;
    }

    string a = parts[index].Trim();
    string b = parts[index + 1].Trim();
    string c = parts[index + 2].Trim();
    if (!IsBareStoRcl(a, out string head)
        || !IsBinaryOp(b, out string op)
        || !IsDigitToken(c))
    {
      return false;
    }

    fused = FormatRegisterArith(head, op, c);
    consumed = 3;
    return true;
  }

  private static bool TryConsumeFusedStoRcl(
    IReadOnlyList<string> parts,
    int index,
    out string fused,
    out int consumed)
  {
    fused = string.Empty;
    consumed = 0;
    string t = parts[index].Trim();

    // Already compacted "STO+2" / "STO+R2" from listing RegisterArith merge.
    if (TryParseStoRExpr(t, out string norm))
    {
      fused = norm;
      consumed = 1;
      return true;
    }

    if (StudioMuseumKeycodes.TryParseFusedStoRcl(t, out string op, out int digit))
    {
      fused = "R" + digit.ToString();
      // STO n alone → Rn only when used as recall-like value? Prefer STO stays "STO n"→Rn for RCL, STO+R for arith.
      if (string.Equals(op, "RCL", StringComparison.OrdinalIgnoreCase))
      {
        // Bare RCL n in a PROCESS chunk that doesn't fold into algebra: show "RCL Rn".
        fused = "RCL R" + digit;
        consumed = 1;
        return true;
      }

      // Bare STO n (store X into Rn).
      fused = "STO R" + digit;
      consumed = 1;
      return true;
    }

    // "STO+2" / "STO+ 2" fused mnemonic without R.
    if (TryParseStoOpDigit(t, out string head, out string bop, out string dig))
    {
      fused = FormatRegisterArith(head, bop, dig);
      consumed = 1;
      return true;
    }

    return false;
  }

  private static bool TryParseStoRExpr(string text, out string normalized)
  {
    normalized = string.Empty;
    string t = text.Trim();
    foreach (string head in new[] { "STO", "RCL" })
    {
      if (!t.StartsWith(head, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string rest = t[head.Length..].Trim();
      if (rest.Length < 2)
      {
        continue;
      }

      char opCh = rest[0];
      if (opCh is not ('+' or '-' or '*' or '/' or '×' or '÷'))
      {
        continue;
      }

      string reg = rest[1..].Trim();
      if (reg.StartsWith('R') || reg.StartsWith('r'))
      {
        reg = reg[1..];
      }

      if (!IsDigitToken(reg))
      {
        continue;
      }

      string op = NormalizeBinaryOp(opCh.ToString());
      normalized = FormatRegisterArith(head, op, reg);
      return true;
    }

    return false;
  }

  private static bool TryParseStoOpDigit(
    string text,
    out string head,
    out string op,
    out string digit)
  {
    head = string.Empty;
    op = string.Empty;
    digit = string.Empty;
    string t = text.Trim();
    foreach (string h in new[] { "STO", "RCL" })
    {
      if (!t.StartsWith(h, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string rest = t[h.Length..].Trim();
      if (rest.Length < 2)
      {
        continue;
      }

      if (!IsBinaryOp(rest[..1], out string bop))
      {
        continue;
      }

      string dig = rest[1..].Trim();
      if (!IsDigitToken(dig))
      {
        continue;
      }

      head = h.ToUpperInvariant();
      op = bop;
      digit = dig;
      return true;
    }

    return false;
  }

  private static bool TryRegisterRef(string token, out string rn)
  {
    rn = string.Empty;
    string t = token.Trim();
    if (t.Length >= 2
        && (t[0] is 'R' or 'r')
        && IsDigitToken(t[1..]))
    {
      rn = "R" + t[1..].Trim();
      return true;
    }

    // "RCL R1" / "RCL 1" as a recall ref for algebra left/right sides.
    if (t.StartsWith("RCL", StringComparison.OrdinalIgnoreCase))
    {
      string rest = t[3..].Trim();
      if (rest.StartsWith('R') || rest.StartsWith('r'))
      {
        rest = rest[1..];
      }

      if (IsDigitToken(rest))
      {
        rn = "R" + rest.Trim();
        return true;
      }
    }

    if (StudioMuseumKeycodes.TryParseFusedStoRcl(t, out string op, out int digit)
        && string.Equals(op, "RCL", StringComparison.OrdinalIgnoreCase))
    {
      rn = "R" + digit;
      return true;
    }

    // "STO R1" from fused store — not a recall ref for unary/binary left side.
    return false;
  }

  private static bool TryFormatUnary(string legendOrOp, string rn, out string formatted)
  {
    formatted = string.Empty;
    string t = legendOrOp.Trim();
    if (t.Length == 0)
    {
      return false;
    }

    // √x / √X / SQRT
    if (t.Equals("√x", StringComparison.OrdinalIgnoreCase)
        || t.Equals("SQRT", StringComparison.OrdinalIgnoreCase)
        || t.Equals("√", StringComparison.Ordinal))
    {
      formatted = "√" + rn;
      return true;
    }

    if (t.Length >= 1 && t[0] == '√' && t.Contains('x', StringComparison.OrdinalIgnoreCase))
    {
      formatted = "√" + rn;
      return true;
    }

    if (t.Equals("1/x", StringComparison.OrdinalIgnoreCase))
    {
      formatted = "1 / " + rn;
      return true;
    }

    if (t.Equals("n!", StringComparison.OrdinalIgnoreCase) || t.Equals("N!", StringComparison.Ordinal))
    {
      formatted = rn + "!";
      return true;
    }

    if (t.Equals("x²", StringComparison.OrdinalIgnoreCase)
        || t.Equals("x^2", StringComparison.OrdinalIgnoreCase)
        || t.Equals("x2", StringComparison.OrdinalIgnoreCase))
    {
      formatted = rn + "²";
      return true;
    }

    if (t.Equals("LN", StringComparison.OrdinalIgnoreCase)
        || t.Equals("LOG", StringComparison.OrdinalIgnoreCase)
        || t.Equals("EXP", StringComparison.OrdinalIgnoreCase)
        || t.Equals("SIN", StringComparison.OrdinalIgnoreCase)
        || t.Equals("COS", StringComparison.OrdinalIgnoreCase)
        || t.Equals("TAN", StringComparison.OrdinalIgnoreCase))
    {
      formatted = t.ToUpperInvariant() + "(" + rn + ")";
      return true;
    }

    if (t.Equals("CHS", StringComparison.OrdinalIgnoreCase))
    {
      formatted = "−" + rn;
      return true;
    }

    return false;
  }

  private static bool TryFormatDigitOpSoup(IReadOnlyList<string> parts, out string soup)
  {
    soup = string.Empty;
    if (parts.Count < 2)
    {
      return false;
    }

    if (!IsBinaryOp(parts[^1], out string op))
    {
      return false;
    }

    for (int i = 0; i < parts.Count - 1; i++)
    {
      if (!IsDigitToken(parts[i]))
      {
        return false;
      }
    }

    soup = op + string.Concat(parts.Take(parts.Count - 1));
    return true;
  }

  /// <summary>
  /// Collapse every <c>digit+</c> / <c>digit digit … op</c> run inside a part list
  /// (<c>1,+</c> → <c>+1</c>; <c>R2,1,+,STO R2</c> → <c>R2,+1,STO R2</c>).
  /// </summary>
  private static List<string> RewriteDigitOpRuns(List<string> parts)
  {
    if (parts.Count < 2)
    {
      return parts;
    }

    List<string> result = new(parts.Count);
    for (int i = 0; i < parts.Count;)
    {
      if (IsDigitToken(parts[i]))
      {
        int digStart = i;
        int j = i;
        while (j < parts.Count && IsDigitToken(parts[j]))
        {
          j++;
        }

        if (j < parts.Count && IsBinaryOp(parts[j], out string op))
        {
          result.Add(op + string.Concat(parts.Skip(digStart).Take(j - digStart)));
          i = j + 1;
          continue;
        }
      }

      result.Add(parts[i]);
      i++;
    }

    return result;
  }

  /// <summary>
  /// Rewrite middot / newline digit soup inside an already-joined caption.
  /// </summary>
  private static string RewriteDigitOpInCaption(string caption)
  {
    if (string.IsNullOrEmpty(caption))
    {
      return caption;
    }

    string[] lines = caption.Split('\n');
    for (int li = 0; li < lines.Length; li++)
    {
      string line = lines[li];
      if (line.Contains(" · ", StringComparison.Ordinal))
      {
        List<string> bits = [.. line.Split(" · ", StringSplitOptions.None)];
        bits = RewriteDigitOpRuns(bits);
        lines[li] = string.Join(" · ", bits);
      }
      else
      {
        // Single-token lines stay; whole-line digit soup without middots is rare.
        List<string> one = RewriteDigitOpRuns([line]);
        lines[li] = string.Join(" · ", one);
      }
    }

    return string.Join("\n", lines);
  }

  private static bool IsBareStoRcl(string token, out string head)
  {
    head = string.Empty;
    string t = token.Trim();
    if (string.Equals(t, "STO", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "RCL", StringComparison.OrdinalIgnoreCase))
    {
      head = t.ToUpperInvariant();
      return true;
    }

    return false;
  }

  private static bool IsBinaryOp(string token, out string op)
  {
    op = NormalizeBinaryOp(token);
    return op.Length > 0;
  }

  private static string NormalizeBinaryOp(string token)
  {
    string t = token.Trim();
    return t switch
    {
      "+" => "+",
      "-" => "-",
      "*" or "×" or "x" => "*",
      "/" or "÷" => "/",
      _ => string.Empty,
    };
  }

  private static bool IsDigitToken(string token)
  {
    string t = token.Trim();
    return t.Length == 1 && t[0] is >= '0' and <= '9';
  }

  /// <summary>Register arithmetic caption: <c>STO + 3</c>, <c>RCL - 2</c>.</summary>
  private static string FormatRegisterArith(string head, string op, string digit)
    => head.ToUpperInvariant() + " " + op + " " + digit.Trim();
}
