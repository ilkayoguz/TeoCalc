namespace TeoCalc.Formats;

/// <summary>TeoCalc card text v1 (<c>.t65</c> / <c>.t67</c>) — human-authored card document.</summary>
public sealed class T6xDocument
{
  public const string FormatId = "TeoCalc.CardText";

  public const int CurrentSchemaVersion = 1;

  public const string Extension65 = ".t65";

  public const string Extension67 = ".t67";

  /// <summary>Legacy unified extension; still accepted on load.</summary>
  public const string LegacyExtension = ".t6x";

  public string Format { get; init; } = FormatId;

  public int SchemaVersion { get; init; } = CurrentSchemaVersion;

  /// <summary>Command-set family: <c>T-65</c> or <c>T-67</c>.</summary>
  public string TargetCpu { get; init; } = "";

  /// <summary>Optional soft device-profile id (e.g. <c>T-65</c>, <c>T-65-Print</c>).</summary>
  public string? Profile { get; init; }

  public string? Category { get; init; }

  public string? Title { get; init; }

  public string? Description { get; init; }

  public string? Usage { get; init; }

  public string? RunHint { get; init; }

  /// <summary>
  /// Exclusive <c>[Code]</c> mode: <c>mnemonic</c> (default) or <c>machine</c> (one internal byte per line).
  /// Written as <c>CodeEncoding</c>; legacy <c>Encoding</c> is accepted when reading.
  /// </summary>
  public string CodeEncoding { get; init; } = CardCodeEncoding.Default;

  public string? Author { get; init; }

  public DateTimeOffset? Created { get; init; }

  public DateTimeOffset? Modified { get; init; }

  /// <summary>Strip columns A–E (caption + optional tooltip).</summary>
  public List<T6xLabelEntry> Labels { get; init; } = [];

  /// <summary>
  /// User program steps (one per line). Mnemonics or decimal internal bytes per <see cref="CodeEncoding"/>.
  /// No internal <c>PTR</c> marker. Single <c>[Code]</c> section only — no separate machine section.
  /// </summary>
  public List<string> Code { get; init; } = [];

  /// <summary>Sparse 1-based register map (HP-65 RCL n ↔ index n).</summary>
  public Dictionary<int, double> Data { get; init; } = new();
}

public sealed class T6xLabelEntry
{
  public string Key { get; init; } = "";

  public string Caption { get; init; } = "";

  public string? Hint { get; init; }
}
