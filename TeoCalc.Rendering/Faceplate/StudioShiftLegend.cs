using System.Text;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Resolve faceplate CapAbove / CapSkirt legends for Studio shift prefixes (f / g / f-1 / h).
/// Returned strings are the same CapAbove / CapSkirt / CapFace-adjacent fields the faceplate paints
/// (Unicode as in key.faceplate.json). Studio draws them via <see cref="ClassicFaceplateGlyphs"/>.
/// </summary>
public static class StudioShiftLegend
{
  public enum ShiftKind
  {
    None,
    Gold,
    Blue,
    GoldInverse,
    /// <summary>HP-67 / Spice h-shift (black skirt ink on most models).</summary>
    Black,
    /// <summary>Inserted mag-card A–E caption (white on near-black chip).</summary>
    CardStrip,
    /// <summary>No-card built-in A–E legends (white on chassis chrome, no chip).</summary>
    NoCardStrip,
  }

  public static bool IsShiftPrefix(string token) =>
    KindForPrefix(token) != ShiftKind.None;

  public static ShiftKind KindForPrefix(string? token)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return ShiftKind.None;
    }

    string t = token.Trim();
    if (string.Equals(t, "g", StringComparison.OrdinalIgnoreCase))
    {
      return ShiftKind.Blue;
    }

    if (string.Equals(t, "f", StringComparison.OrdinalIgnoreCase))
    {
      return ShiftKind.Gold;
    }

    if (string.Equals(t, "f-1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "f⁻¹", StringComparison.OrdinalIgnoreCase))
    {
      return ShiftKind.GoldInverse;
    }

    if (string.Equals(t, "h", StringComparison.OrdinalIgnoreCase))
    {
      return ShiftKind.Black;
    }

    return ShiftKind.None;
  }

  public static string PrefixForKind(ShiftKind kind) =>
    kind switch
    {
      ShiftKind.Blue => "g",
      ShiftKind.Gold => "f",
      ShiftKind.GoldInverse => "f-1",
      ShiftKind.Black => "h",
      _ => string.Empty,
    };

  /// <summary>
  /// Legend printed when the previous step (or same-line prefix) is f/g/f-1/h.
  /// </summary>
  public static bool TryResolve(
    string? modelId,
    string? shiftPrefix,
    string keyToken,
    out string legend,
    out ShiftKind kind)
  {
    legend = string.Empty;
    kind = KindForPrefix(shiftPrefix);
    if (kind == ShiftKind.None || string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(keyToken))
    {
      return false;
    }

    if (!TryFindFaceplateEntry(modelId, keyToken, out _, out ClassicKeyFaceplateLegend.KeyFaceplateEntry entry))
    {
      return false;
    }

    string? raw = kind switch
    {
      ShiftKind.Blue => entry.Blue,
      ShiftKind.Gold => entry.Gold,
      ShiftKind.GoldInverse => entry.GoldInverse ?? entry.Gold,
      ShiftKind.Black => entry.Black,
      _ => null,
    };

    if (string.IsNullOrWhiteSpace(raw))
    {
      return false;
    }

    // Keep faceplate CapAbove / CapSkirt text verbatim (√x, R↓, x↔y, …).
    legend = raw.Trim();
    return true;
  }

  /// <summary>
  /// Expand a fused Classic/Teo67 vocab mnemonic (RDOWN, X&lt;&gt;Y, LSTX, …) back to
  /// shift-prefix + faceplate CapFace for Studio Keys display.
  /// </summary>
  public static bool TryExpandFusedMnemonic(
    string? modelId,
    string mnemonic,
    out string shiftPrefix,
    out string keyToken,
    out string legend,
    out ShiftKind kind)
  {
    shiftPrefix = string.Empty;
    keyToken = string.Empty;
    legend = string.Empty;
    kind = ShiftKind.None;

    if (string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(mnemonic))
    {
      return false;
    }

    string body = mnemonic.Trim();
    if (body.Contains(' ', StringComparison.Ordinal)
        || IsShiftPrefix(body)
        || StudioMuseumKeycodes.TryParseFusedStoRcl(body, out _, out _))
    {
      return false;
    }

    // Already a primary CapFace — show that key, do not treat as fused shift.
    if (TryFindFaceplateEntry(modelId, body, out _, out _))
    {
      return false;
    }

    string needle = CanonicalLegendKey(body);
    if (needle.Length == 0)
    {
      return false;
    }

    for (int i = 0; i < 64; i++)
    {
      ClassicKeyFaceplateLegend.KeyFaceplateEntry? e = ClassicKeyFaceplateLegend.TryGetEntryPublic(modelId, i);
      if (e is null || string.IsNullOrWhiteSpace(e.CapFace))
      {
        continue;
      }

      if (TryMatchShiftLegend(e, needle, out kind, out string rawLegend))
      {
        shiftPrefix = PrefixForKind(kind);
        keyToken = NormalizeLabel(e.CapFace);
        legend = rawLegend.Trim();
        return shiftPrefix.Length > 0 && keyToken.Length > 0;
      }
    }

    return false;
  }

  /// <summary>
  /// ASCII fallback when faceplate glyphs cannot be drawn (matching / headless only).
  /// Prefer raw CapAbove / CapSkirt + <see cref="ClassicFaceplateGlyphs"/> for display.
  /// </summary>
  public static string ToAsciiLegend(string legend)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return legend;
    }

    string t = legend.Trim();
    // Multi-char replacements first (longest / most specific first).
    t = t
      .Replace("f⁻¹", "f-1", StringComparison.Ordinal)
      .Replace("⁻¹", "^-1", StringComparison.Ordinal)
      .Replace("√x", "SQRT", StringComparison.OrdinalIgnoreCase)
      .Replace("∫yx", "Iy", StringComparison.Ordinal)
      .Replace("PRT Σ", "PRT E", StringComparison.Ordinal)
      .Replace("ΔDAYS", "DDAYS", StringComparison.Ordinal)
      .Replace("x²", "x^2", StringComparison.OrdinalIgnoreCase)
      .Replace("LST X", "LSTX", StringComparison.OrdinalIgnoreCase)
      .Replace("LSTx", "LSTX", StringComparison.OrdinalIgnoreCase)
      .Replace("R↓", "RDOWN", StringComparison.Ordinal)
      .Replace("R↑", "RUP", StringComparison.Ordinal)
      .Replace("x↔(i)", "x<>(i)", StringComparison.OrdinalIgnoreCase)
      .Replace("x↔y", "x<>y", StringComparison.OrdinalIgnoreCase)
      .Replace("X↔I", "X<>I", StringComparison.OrdinalIgnoreCase)
      .Replace("x↔I", "x<>I", StringComparison.OrdinalIgnoreCase)
      .Replace("P↔S", "P<>S", StringComparison.OrdinalIgnoreCase)
      .Replace("x≠0", "x!=0", StringComparison.OrdinalIgnoreCase)
      .Replace("x≠y", "x!=y", StringComparison.OrdinalIgnoreCase)
      .Replace("x≤y", "x<=y", StringComparison.OrdinalIgnoreCase)
      .Replace("x≥0", "x>=0", StringComparison.OrdinalIgnoreCase)
      .Replace("x≥y", "x>=y", StringComparison.OrdinalIgnoreCase)
      .Replace("Σ+", "E+", StringComparison.Ordinal)
      .Replace("Σ-", "E-", StringComparison.Ordinal)
      .Replace("→Σ", "->E", StringComparison.Ordinal)
      .Replace("%Σ", "%E", StringComparison.Ordinal)
      .Replace("Δ%", "D%", StringComparison.Ordinal)
      .Replace("°F→", "degF->", StringComparison.Ordinal)
      .Replace("→°C", "->degC", StringComparison.Ordinal)
      .Replace("→°F", "->degF", StringComparison.Ordinal)
      .Replace("←°C", "<-degC", StringComparison.Ordinal);

    StringBuilder sb = new(t.Length);
    foreach (char c in t)
    {
      if (c <= 0x7F)
      {
        sb.Append(c);
        continue;
      }

      sb.Append(c switch
      {
        '↓' => "DOWN",
        '↑' => "UP",
        '↔' => "<>",
        '≠' => "!=",
        '≤' => "<=",
        '≥' => ">=",
        '√' => "SQRT",
        'π' => "PI",
        '→' => "->",
        '←' => "<-",
        '×' => "*",
        '÷' => "/",
        '−' => "-",
        '·' => ".",
        '²' => "^2",
        'Σ' => "E",
        'Δ' or '∆' => "D",
        '∫' => "I",
        '°' => "deg",
        'ɑ' => "a",
        // Combining overline / circumflex (x̅, x̂) — drop the mark.
        '\u0305' or '\u0302' or '̅' or '̂' => string.Empty,
        _ => string.Empty,
      });
    }

    return sb.ToString();
  }

  /// <summary>Alias kept for older call sites / tests.</summary>
  public static string ToAsciiSafe(string legend) => ToAsciiLegend(legend);

  /// <summary>True when every char is ASCII (ImGui-default-font safe).</summary>
  public static bool IsAllAscii(string text)
  {
    foreach (char c in text)
    {
      if (c > 0x7F)
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>Normalize for matching vocab mnemonics to faceplate legends.</summary>
  public static string CanonicalLegendKey(string label)
  {
    if (string.IsNullOrWhiteSpace(label))
    {
      return string.Empty;
    }

    string t = ToAsciiLegend(label);
    t = t.Replace(" ", "", StringComparison.Ordinal)
      .Replace("-", "", StringComparison.Ordinal)
      .TrimEnd('?');
    return t.ToUpperInvariant();
  }

  public static bool TryFindFaceplateEntry(
    string modelId,
    string capFaceOrMnemonic,
    out int keyIndex,
    out ClassicKeyFaceplateLegend.KeyFaceplateEntry entry)
  {
    keyIndex = -1;
    entry = null!;
    if (string.IsNullOrWhiteSpace(modelId))
    {
      return false;
    }

    string needle = NormalizeLabel(capFaceOrMnemonic);
    if (needle.Length == 0)
    {
      return false;
    }

    // Prefer CapFace match over scanning KeyChart chars.
    for (int i = 0; i < 64; i++)
    {
      ClassicKeyFaceplateLegend.KeyFaceplateEntry? e = ClassicKeyFaceplateLegend.TryGetEntryPublic(modelId, i);
      if (e?.CapFace is not { Length: > 0 } face)
      {
        continue;
      }

      if (!LabelsMatch(face, needle))
      {
        continue;
      }

      keyIndex = i;
      entry = e;
      return true;
    }

    return false;
  }

  public static bool LabelsMatch(string a, string b) =>
    string.Equals(NormalizeLabel(a), NormalizeLabel(b), StringComparison.OrdinalIgnoreCase);

  public static string NormalizeLabel(string label)
  {
    if (string.IsNullOrWhiteSpace(label))
    {
      return string.Empty;
    }

    string t = label.Trim();
    return t switch
    {
      "f⁻¹" => "f-1",
      "·" => ".",
      "×" => "*",
      "÷" => "/",
      "−" => "-",
      _ => t,
    };
  }

  private static bool TryMatchShiftLegend(
    ClassicKeyFaceplateLegend.KeyFaceplateEntry entry,
    string needleCanonical,
    out ShiftKind kind,
    out string rawLegend)
  {
    if (Matches(entry.Blue, needleCanonical))
    {
      kind = ShiftKind.Blue;
      rawLegend = entry.Blue!;
      return true;
    }

    if (Matches(entry.Gold, needleCanonical))
    {
      kind = ShiftKind.Gold;
      rawLegend = entry.Gold!;
      return true;
    }

    if (Matches(entry.GoldInverse, needleCanonical))
    {
      kind = ShiftKind.GoldInverse;
      rawLegend = entry.GoldInverse!;
      return true;
    }

    if (Matches(entry.Black, needleCanonical))
    {
      kind = ShiftKind.Black;
      rawLegend = entry.Black!;
      return true;
    }

    kind = ShiftKind.None;
    rawLegend = string.Empty;
    return false;
  }

  private static bool Matches(string? raw, string needleCanonical) =>
    !string.IsNullOrWhiteSpace(raw)
    && string.Equals(CanonicalLegendKey(raw), needleCanonical, StringComparison.Ordinal);
}
