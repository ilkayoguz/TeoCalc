namespace TeoCalc.Rendering.Faceplate;

/// <summary>Pure helpers for Studio Text-tab autocomplete (no ImGui).</summary>
public static class StudioProgramCompletions
{
  public const int DefaultLimit = 12;

  public readonly record struct TokenSpan(int Start, int Length, string Text)
  {
    public int End => Start + Length;
  }

  /// <summary>
  /// Token under <paramref name="cursor"/> (whitespace-delimited). Cursor may sit at the
  /// end of the token (including after the last character while typing).
  /// </summary>
  public static bool TryExtractToken(string text, int cursor, out TokenSpan token)
  {
    token = default;
    if (string.IsNullOrEmpty(text))
    {
      return false;
    }

    int pos = Math.Clamp(cursor, 0, text.Length);
    // Prefer the token just finished when caret sits on whitespace / EOF.
    while (pos > 0 && (pos == text.Length || char.IsWhiteSpace(text[pos]) || text[pos] == '\n'))
    {
      pos--;
      if (pos < text.Length && !char.IsWhiteSpace(text[pos]) && text[pos] != '\n')
      {
        break;
      }
    }

    if (pos < 0 || pos >= text.Length || char.IsWhiteSpace(text[pos]) || text[pos] == '\n')
    {
      return false;
    }

    int start = pos;
    while (start > 0 && !char.IsWhiteSpace(text[start - 1]) && text[start - 1] != '\n')
    {
      start--;
    }

    int end = pos + 1;
    while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '\n')
    {
      end++;
    }

    token = new TokenSpan(start, end - start, text[start..end]);
    return token.Length > 0;
  }

  /// <summary>Case-insensitive starts-with filter; stable alphabetical order preserved from candidates.</summary>
  public static IReadOnlyList<string> Filter(
    IReadOnlyList<string> candidates,
    string prefix,
    int limit = DefaultLimit)
  {
    ArgumentNullException.ThrowIfNull(candidates);
    if (string.IsNullOrEmpty(prefix) || limit <= 0)
    {
      return [];
    }

    List<string> matches = [];
    foreach (string candidate in candidates)
    {
      if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      {
        matches.Add(candidate);
        if (matches.Count >= limit)
        {
          break;
        }
      }
    }

    return matches;
  }

  /// <summary>Replace [token.Start, token.End) with <paramref name="replacement"/>.</summary>
  public static string ReplaceToken(string text, TokenSpan token, string replacement)
  {
    ArgumentNullException.ThrowIfNull(text);
    ArgumentNullException.ThrowIfNull(replacement);
    if (token.Start < 0 || token.End > text.Length || token.Length < 0)
    {
      return text;
    }

    return string.Concat(text.AsSpan(0, token.Start), replacement, text.AsSpan(token.End));
  }
}
