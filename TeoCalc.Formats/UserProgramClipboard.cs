using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Formats;

/// <summary>
/// Copy/paste helpers for Studio / card code lines under <see cref="CardCodeEncoding"/>.
/// Accepts either bare step bodies or <c>NNN  body</c> listing lines.
/// Dual listings use tab-separated <c>index\tmachine\tmnemonic</c> (Studio default copy).
/// </summary>
public static class UserProgramClipboard
{
  private static readonly Regex PrefixedLine = new(
    @"^\s*(\d+)\s+(.+?)\s*$",
    RegexOptions.CultureInvariant | RegexOptions.Compiled);

  public static ClassicProgramListingStyle ToListingStyle(string? encoding) =>
    CardCodeEncoding.IsMachine(encoding)
      ? ClassicProgramListingStyle.Machine
      : ClassicProgramListingStyle.Mnemonic;

  public static string Format(
    IEnumerable<ClassicProgramLine> lines,
    string? encoding)
  {
    ArgumentNullException.ThrowIfNull(lines);
    return ClassicProgramListing.Format(lines, ToListingStyle(encoding));
  }

  /// <summary>
  /// Dual listing for Studio copy: <c>index\tmachine\tmnemonic</c> per line (TSV).
  /// </summary>
  public static string FormatDual(IEnumerable<ClassicProgramLine> lines)
  {
    ArgumentNullException.ThrowIfNull(lines);
    StringBuilder sb = new();
    foreach (ClassicProgramLine line in lines)
    {
      sb.Append(line.Index.ToString(CultureInfo.InvariantCulture));
      sb.Append('\t');
      sb.Append(line.Body(ClassicProgramListingStyle.Machine));
      sb.Append('\t');
      sb.Append(line.Body(ClassicProgramListingStyle.Mnemonic));
      sb.AppendLine();
    }

    return sb.ToString();
  }

  /// <summary>
  /// Parse clipboard text into program bytes. Empty lines are skipped.
  /// Lines may be bare bodies (<c>RCL 1</c> / <c>43</c>), listing rows (<c>  2  RCL 1</c>),
  /// or dual TSV (<c>2\t43\tLBL</c> — machine column wins).
  /// </summary>
  public static bool TryParse(
    string text,
    string? encoding,
    Func<string, byte?> codeForMnemonic,
    out List<byte> codes,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(codeForMnemonic);
    codes = [];
    error = null;

    if (string.IsNullOrWhiteSpace(text))
    {
      error = "Clipboard is empty.";
      return false;
    }

    string[] rawLines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Split('\n');

    bool dual = LooksLikeDualListing(rawLines);
    string normalized = dual
      ? CardCodeEncoding.Machine
      : CardCodeEncoding.Normalize(encoding);

    foreach (string raw in rawLines)
    {
      string trimmed = raw.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      string body = dual ? ExtractMachineBody(trimmed) : ExtractBody(trimmed);
      try
      {
        codes.Add(CardCodeEncoding.ResolveStep(normalized, body, codeForMnemonic));
      }
      catch (FormatException ex)
      {
        error = ex.Message;
        codes = [];
        return false;
      }
    }

    if (codes.Count == 0)
    {
      error = "No code steps found on clipboard.";
      return false;
    }

    return true;
  }

  /// <summary>
  /// Paste without a chosen encoding: dual TSV → machine column; otherwise try mnemonic then machine.
  /// </summary>
  public static bool TryParseAuto(
    string text,
    Func<string, byte?> codeForMnemonic,
    out List<byte> codes,
    out string? error)
  {
    ArgumentNullException.ThrowIfNull(codeForMnemonic);
    codes = [];
    error = null;

    if (string.IsNullOrWhiteSpace(text))
    {
      error = "Clipboard is empty.";
      return false;
    }

    string[] rawLines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Split('\n');

    if (LooksLikeDualListing(rawLines))
    {
      return TryParse(text, CardCodeEncoding.Machine, codeForMnemonic, out codes, out error);
    }

    if (TryParse(text, CardCodeEncoding.Mnemonic, codeForMnemonic, out codes, out error))
    {
      return true;
    }

    string? mnemonicError = error;
    if (TryParse(text, CardCodeEncoding.Machine, codeForMnemonic, out codes, out error))
    {
      return true;
    }

    error = mnemonicError ?? error;
    return false;
  }

  /// <summary>True when non-empty lines look like Studio dual TSV (<c>idx\tmachine\tmnemonic</c> or <c>machine\tmnemonic</c>).</summary>
  public static bool LooksLikeDualListing(IEnumerable<string> rawLines)
  {
    ArgumentNullException.ThrowIfNull(rawLines);
    int dualRows = 0;
    int nonEmpty = 0;
    foreach (string raw in rawLines)
    {
      string trimmed = raw.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      nonEmpty++;
      if (TrySplitDual(trimmed, out _, out _))
      {
        dualRows++;
      }
    }

    return nonEmpty > 0 && dualRows == nonEmpty;
  }

  /// <summary>Machine body from a dual TSV line, else <see cref="ExtractBody"/>.</summary>
  public static string ExtractMachineBody(string line)
  {
    ArgumentNullException.ThrowIfNull(line);
    if (TrySplitDual(line.Trim(), out string machine, out _))
    {
      return machine;
    }

    return ExtractBody(line);
  }

  /// <summary>Strip optional leading step index from a listing line.</summary>
  public static string ExtractBody(string line)
  {
    ArgumentNullException.ThrowIfNull(line);
    Match match = PrefixedLine.Match(line);
    if (match.Success
        && int.TryParse(
          match.Groups[1].Value,
          NumberStyles.Integer,
          CultureInfo.InvariantCulture,
          out _))
    {
      return match.Groups[2].Value.Trim();
    }

    return line.Trim();
  }

  public static string JoinBodies(IEnumerable<string> bodies)
  {
    ArgumentNullException.ThrowIfNull(bodies);
    StringBuilder sb = new();
    foreach (string body in bodies)
    {
      sb.AppendLine(body);
    }

    return sb.ToString();
  }

  private static bool TrySplitDual(string trimmed, out string machine, out string mnemonic)
  {
    machine = string.Empty;
    mnemonic = string.Empty;
    if (!trimmed.Contains('\t', StringComparison.Ordinal))
    {
      return false;
    }

    string[] parts = trimmed.Split('\t');
    if (parts.Length >= 3
        && byte.TryParse(
          parts[1].Trim(),
          NumberStyles.Integer,
          CultureInfo.InvariantCulture,
          out _))
    {
      machine = parts[1].Trim();
      mnemonic = parts[2].Trim();
      return true;
    }

    if (parts.Length == 2
        && byte.TryParse(
          parts[0].Trim(),
          NumberStyles.Integer,
          CultureInfo.InvariantCulture,
          out _))
    {
      machine = parts[0].Trim();
      mnemonic = parts[1].Trim();
      return true;
    }

    return false;
  }
}
