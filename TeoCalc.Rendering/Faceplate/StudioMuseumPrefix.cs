namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Prefix / incomplete-step rules for the W/PRGM LED editor (HP-65 / T-65 museum pairs).
/// Aligns with <see cref="StudioListingView"/> merge prefixes: shift, LBL/GTO/GSB, bare STO/RCL.
/// </summary>
public static class StudioMuseumPrefix
{
  /// <summary>
  /// True when the first Keys token (or mnemonic resolved from the first museum box)
  /// requires a second key before the step is complete — e.g. <c>g</c>, <c>LBL</c>, bare <c>STO</c>.
  /// Fused forms like <c>RCL 1</c> are already complete (not a bare prefix).
  /// </summary>
  public static bool NeedsSecondToken(string? firstTokenOrMnemonic)
  {
    if (string.IsNullOrWhiteSpace(firstTokenOrMnemonic))
    {
      return false;
    }

    string body = firstTokenOrMnemonic.Trim();
    if (body.Contains(' ', StringComparison.Ordinal)
        || body.Contains('\t', StringComparison.Ordinal))
    {
      // Multi-token mnemonic (e.g. "RCL 1", "g 4") — already has a follow-up.
      List<string> tokens = StudioMnemonicPaint.Tokenize(body);
      if (tokens.Count >= 2)
      {
        return false;
      }

      body = tokens.Count > 0 ? tokens[0] : body;
    }

    if (StudioShiftLegend.IsShiftPrefix(body))
    {
      return true;
    }

    if (string.Equals(body, "LBL", StringComparison.OrdinalIgnoreCase)
        || string.Equals(body, "GTO", StringComparison.OrdinalIgnoreCase)
        || string.Equals(body, "GSB", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (string.Equals(body, "STO", StringComparison.OrdinalIgnoreCase)
        || string.Equals(body, "RCL", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// From the first museum LED box alone: resolve → mnemonic → <see cref="NeedsSecondToken"/>.
  /// Non-museum / unknown codes return false (single-box complete or invalid elsewhere).
  /// </summary>
  public static bool NeedsSecondMuseumBox(
    string? machineA,
    string? modelId,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    if (string.IsNullOrWhiteSpace(machineA))
    {
      return false;
    }

    if (!StudioMuseumKeycodes.TryResolveMachineLine(
          machineA.Trim(),
          modelId,
          formatMnemonic,
          out byte code,
          out _))
    {
      // Unknown while typing — if it looks like a 2-digit museum token, still show 2nd box
      // only after we know it is a prefix (can't yet). Keep single box until resolved.
      return false;
    }

    string mnemonic = formatMnemonic(code);
    return NeedsSecondToken(mnemonic);
  }

  /// <summary>
  /// Step is incomplete when a prefix is present without its follow-up
  /// (Machine B empty while NeedsSecond, or Keys is a bare prefix).
  /// </summary>
  public static bool IsIncompletePrefixStep(
    string? machineA,
    string? machineB,
    string? keys,
    string? modelId,
    Func<byte, string> formatMnemonic)
  {
    ArgumentNullException.ThrowIfNull(formatMnemonic);

    string keysTrim = keys?.Trim() ?? string.Empty;
    if (keysTrim.Length > 0 && NeedsSecondToken(keysTrim))
    {
      // Bare "g" / "LBL" / "STO" with no second token in Keys.
      List<string> tokens = StudioMnemonicPaint.Tokenize(keysTrim);
      if (tokens.Count < 2)
      {
        return true;
      }
    }

    string a = machineA?.Trim() ?? string.Empty;
    string b = machineB?.Trim() ?? string.Empty;
    if (a.Length == 0)
    {
      return false;
    }

    if (b.Length > 0)
    {
      return false;
    }

    return NeedsSecondMuseumBox(a, modelId, formatMnemonic);
  }
}
