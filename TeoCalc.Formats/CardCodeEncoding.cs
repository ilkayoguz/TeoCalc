using System.Globalization;

namespace TeoCalc.Formats;

/// <summary>
/// Exclusive <c>[Code]</c> encoding for card text / Teo card program sections.
/// Separate <c>[Machine]</c> / dual sections are not used — mode is chosen via <c>CodeEncoding</c> only.
/// </summary>
public static class CardCodeEncoding
{
  public const string Mnemonic = "mnemonic";

  public const string Machine = "machine";

  public const string Default = Mnemonic;

  /// <summary>Legacy General / Program key still accepted when reading.</summary>
  public const string LegacyKey = "Encoding";

  public const string Key = "CodeEncoding";

  public static string Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return Default;
    }

    string trimmed = value.Trim();
    if (string.Equals(trimmed, Mnemonic, StringComparison.OrdinalIgnoreCase))
    {
      return Mnemonic;
    }

    if (string.Equals(trimmed, Machine, StringComparison.OrdinalIgnoreCase))
    {
      return Machine;
    }

    throw new FormatException(
      $"Unsupported {Key} '{value}'. Expected '{Mnemonic}' or '{Machine}'.");
  }

  public static bool IsMachine(string? encoding) =>
    string.Equals(Normalize(encoding), Machine, StringComparison.Ordinal);

  public static bool IsMnemonic(string? encoding) =>
    string.Equals(Normalize(encoding), Mnemonic, StringComparison.Ordinal);

  /// <summary>
  /// Resolves one <c>[Code]</c> step to an internal program byte.
  /// Machine mode: one TeoCalc internal byte per line as decimal (e.g. <c>43</c>), not HP Museum W/PRGM pairs.
  /// Mnemonic mode: vocabulary token or <c>#n</c> escape.
  /// </summary>
  public static byte ResolveStep(string encoding, string step, Func<string, byte?> codeForMnemonic)
  {
    ArgumentNullException.ThrowIfNull(codeForMnemonic);
    string trimmed = step.Trim();
    if (trimmed.Length == 0)
    {
      throw new FormatException("Empty code step.");
    }

    if (IsMachine(encoding))
    {
      return ParseMachineByte(trimmed);
    }

    byte? code = codeForMnemonic(trimmed);
    if (code is null)
    {
      throw new FormatException($"Mnemonic not found: {trimmed}");
    }

    return code.Value;
  }

  /// <summary>
  /// Machine <c>[Code]</c> lines are one internal byte per line (CuveSoft-style listings).
  /// Space-separated multi-byte lines are rejected here; museum display pairs belong in CuveSoft import, not TeoCalc machine mode.
  /// </summary>
  public static byte ParseMachineByte(string line)
  {
    string trimmed = line.Trim();
    if (trimmed.Length == 0)
    {
      throw new FormatException("Empty machine code step.");
    }

    if (trimmed.Contains(' ', StringComparison.Ordinal)
        || trimmed.Contains('\t', StringComparison.Ordinal))
    {
      throw new FormatException(
        $"Machine {Key} expects one internal byte per line; got multi-token '{trimmed}'.");
    }

    if (!byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value))
    {
      throw new FormatException(
        $"Invalid machine byte '{trimmed}'. Expected decimal 0..255 (TeoCalc internal), not a mnemonic.");
    }

    return value;
  }
}
