using System.Globalization;
using System.Text.RegularExpressions;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// HP Museum W/PRGM display keycodes (row/column) for Studio Machine column.
/// TeoCalc stores one fused 6-bit byte per step; the physical calc shows museum pairs
/// for merged STO/RCL 1–8 (e.g. RCL 1 → <c>34 01</c>). Digits use 00–09.
/// </summary>
public static class StudioMuseumKeycodes
{
  private static readonly Regex FusedStoRcl = new(
    @"^(STO|RCL)\s*([1-8])$",
    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// Format Machine-column text: museum display when derivable, else TeoCalc decimal byte.
  /// Clipboard / card machine mode stay on TeoCalc bytes — this is display-only.
  /// </summary>
  public static string FormatMachineDisplay(byte code, string mnemonic, string? modelId)
  {
    if (TryFormatMuseum(mnemonic, modelId, out string museum))
    {
      return museum;
    }

    return code.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Machine column for a Studio display row (may be a merged LBL/shift pair
  /// or a fused mnemonic expanded to prefix+key for display).
  /// </summary>
  public static string FormatMachineDisplay(
    StudioListingView.Row row,
    string? modelId)
  {
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId);
    List<string> tokens = StudioMnemonicPaint.Tokenize(paint.KeysMnemonic);
    if (tokens.Count >= 2
        && TryFormatMuseumPair(tokens[0], tokens[1], modelId, out string pair))
    {
      return pair;
    }

    return FormatMachineDisplay(row.Code, paint.KeysMnemonic, modelId);
  }

  public static bool TryParseFusedStoRcl(string mnemonic, out string op, out int digit)
  {
    op = string.Empty;
    digit = 0;
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return false;
    }

    Match match = FusedStoRcl.Match(mnemonic.Trim());
    if (!match.Success)
    {
      return false;
    }

    op = match.Groups[1].Value.ToUpperInvariant();
    digit = match.Groups[2].Value[0] - '0';
    return true;
  }

  public static bool TryFormatMuseum(string mnemonic, string? modelId, out string museum)
  {
    museum = string.Empty;
    if (string.IsNullOrWhiteSpace(mnemonic) || string.IsNullOrWhiteSpace(modelId))
    {
      return false;
    }

    if (!IsClassicHp65Family(modelId))
    {
      // Gap: Woodstock/Spice/ACT museum maps not wired yet — Machine stays TeoCalc decimal.
      return false;
    }

    string body = mnemonic.Trim();
    if (TryParseFusedStoRcl(body, out string op, out int digit))
    {
      if (!TryMuseumCodeForCapFace(modelId, op, out string prefixCode))
      {
        return false;
      }

      museum = $"{prefixCode} {digit:00}";
      return true;
    }

    List<string> tokens = StudioMnemonicPaint.Tokenize(body);
    if (tokens.Count >= 2
        && TryFormatMuseumPair(tokens[0], tokens[1], modelId, out museum))
    {
      return true;
    }

    // Single-token — map when it is a faceplate key.
    string first = tokens.Count > 0 ? tokens[0] : string.Empty;
    if (first.Length == 0)
    {
      return false;
    }

    if (IsDigitToken(first))
    {
      museum = $"{first[0] - '0':00}";
      return true;
    }

    if (TryMuseumCodeForCapFace(modelId, first, out string code))
    {
      museum = code;
      return true;
    }

    return false;
  }

  public static bool TryFormatMuseumPair(
    string firstMnemonic,
    string secondMnemonic,
    string? modelId,
    out string museum)
  {
    museum = string.Empty;
    if (string.IsNullOrWhiteSpace(modelId)
        || string.IsNullOrWhiteSpace(firstMnemonic)
        || string.IsNullOrWhiteSpace(secondMnemonic)
        || !IsClassicHp65Family(modelId))
    {
      return false;
    }

    string a = firstMnemonic.Trim();
    string b = secondMnemonic.Trim();
    if (!TryMuseumToken(modelId, a, out string left)
        || !TryMuseumToken(modelId, b, out string right))
    {
      return false;
    }

    museum = $"{left} {right}";
    return true;
  }

  private static bool TryMuseumToken(string modelId, string token, out string code)
  {
    code = string.Empty;
    if (IsDigitToken(token))
    {
      code = $"{token[0] - '0':00}";
      return true;
    }

    return TryMuseumCodeForCapFace(modelId, token, out code);
  }

  public static bool TryMuseumCodeForCapFace(string modelId, string capFaceOrMnemonic, out string code)
  {
    code = string.Empty;
    if (!StudioShiftLegend.TryFindFaceplateEntry(modelId, capFaceOrMnemonic, out int keyIndex, out _))
    {
      return false;
    }

    return TryMuseumCodeForKeyIndex(modelId, keyIndex, out code);
  }

  public static bool TryMuseumCodeForKeyIndex(string modelId, int keyIndex, out string code)
  {
    code = string.Empty;
    FaceplateCell? cell = null;
    foreach (FaceplateCell c in CalcFaceplateLayout.GetPhysicalCells("Classic", modelId))
    {
      if (c.KeyChartIndex == keyIndex)
      {
        cell = c;
        break;
      }
    }

    if (cell is null)
    {
      return false;
    }

    // Digits: museum 00–09 (not row/column).
    if (ClassicKeyFaceplateLegend.TryGetEntryPublic(modelId, keyIndex) is { CapFace: { } face }
        && face.Length == 1
        && char.IsDigit(face[0]))
    {
      code = $"{face[0] - '0':00}";
      return true;
    }

    int row = cell.Value.Row + 1;
    int col = cell.Value.Column + 1;
    code = $"{row}{col}";
    return true;
  }

  /// <summary>
  /// Resolve one Machine-column line to a TeoCalc program byte.
  /// Accepts TeoCalc decimal (<c>43</c>) or T-65 museum display (<c>34 01</c> / <c>00</c>).
  /// </summary>
  public static bool TryResolveMachineLine(
    string line,
    string? modelId,
    Func<byte, string> formatMnemonic,
    out byte code,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(formatMnemonic);
    code = 0;
    error = null;
    string trimmed = line.Trim();
    if (trimmed.Length == 0)
    {
      error = "Empty machine code step.";
      return false;
    }

    // T-65 museum lines are two-digit tokens (`23` / `34 01`). Prefer museum map so
    // padded digits like `00` do not collapse to TeoCalc byte 0 via decimal parse.
    if (!string.IsNullOrWhiteSpace(modelId)
        && IsClassicHp65Family(modelId)
        && LooksLikeMuseumLine(trimmed))
    {
      string normalized = NormalizeMuseumLine(trimmed);
      IReadOnlyDictionary<string, byte> map = GetOrBuildMuseumMap(modelId, formatMnemonic);
      if (map.TryGetValue(normalized, out code))
      {
        return true;
      }

      error = $"Unknown museum machine code '{trimmed}'.";
      return false;
    }

    // Decimal TeoCalc byte (clipboard / card machine mode / non-T-65).
    if (!trimmed.Contains(' ', StringComparison.Ordinal)
        && !trimmed.Contains('\t', StringComparison.Ordinal)
        && byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte decimalByte))
    {
      code = decimalByte;
      return true;
    }

    error =
      $"Invalid machine byte '{trimmed}'. Expected decimal 0..255"
      + (IsClassicHp65Family(modelId ?? string.Empty)
        ? " or museum codes like '34 01'."
        : " (TeoCalc internal).");
    return false;
  }

  private static bool LooksLikeMuseumLine(string trimmed)
  {
    string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
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

  private static string NormalizeMuseumLine(string line)
  {
    string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    return string.Join(' ', parts);
  }

  private static readonly Dictionary<string, IReadOnlyDictionary<string, byte>> MuseumMaps = new(
    StringComparer.OrdinalIgnoreCase);

  private static IReadOnlyDictionary<string, byte> GetOrBuildMuseumMap(
    string modelId,
    Func<byte, string> formatMnemonic)
  {
    string engine = CalcModelIds.ToEngineId(modelId);
    lock (MuseumMaps)
    {
      if (MuseumMaps.TryGetValue(engine, out IReadOnlyDictionary<string, byte>? cached))
      {
        return cached;
      }

      Dictionary<string, byte> map = new(StringComparer.Ordinal);
      for (int i = 0; i <= 255; i++)
      {
        byte code = (byte)i;
        string mnemonic = formatMnemonic(code);
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
          continue;
        }

        if (TryFormatMuseum(mnemonic, modelId, out string museum))
        {
          string key = NormalizeMuseumLine(museum);
          // First writer wins — fused STO/RCL and single-token share the same display.
          map.TryAdd(key, code);
        }
      }

      MuseumMaps[engine] = map;
      return map;
    }
  }

  private static bool IsClassicHp65Family(string modelId) =>
    CalcModelIds.IsEngine(modelId, "T-65");

  private static bool IsDigitToken(string token) =>
    token.Length == 1 && char.IsDigit(token[0]);
}
