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
    @"^(STO|RCL)\s+([1-8])$",
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

  private static bool IsClassicHp65Family(string modelId) =>
    string.Equals(modelId, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(CalcModelIds.ToEngineId(modelId), "HP-65", StringComparison.OrdinalIgnoreCase);

  private static bool IsDigitToken(string token) =>
    token.Length == 1 && char.IsDigit(token[0]);
}
