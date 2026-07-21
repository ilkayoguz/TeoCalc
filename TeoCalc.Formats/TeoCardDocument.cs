using System.Text.Json.Serialization;

namespace TeoCalc.Formats;

/// <summary>In-memory TeoCalc card document used by T6x / CuveSoft / Teo JSON converters.</summary>
public sealed class TeoCardDocument
{
  public const string FormatId = "TeoCalc.Card";

  public const int CurrentSchemaVersion = 1;

  [JsonPropertyName("Format")]
  public string Format { get; init; } = FormatId;

  [JsonPropertyName("SchemaVersion")]
  public int SchemaVersion { get; init; } = CurrentSchemaVersion;

  [JsonPropertyName("Model")]
  public string Model { get; init; } = "";

  [JsonPropertyName("InteropMagic")]
  public string? InteropMagic { get; init; }

  [JsonPropertyName("Title")]
  public string? Title { get; init; }

  [JsonPropertyName("Description")]
  public string? Description { get; init; }

  [JsonPropertyName("Usage")]
  public string? Usage { get; init; }

  [JsonPropertyName("Category")]
  public string? Category { get; init; }

  [JsonPropertyName("RunHint")]
  public string? RunHint { get; init; }

  [JsonPropertyName("Labels")]
  public List<string> Labels { get; init; } = [];

  /// <summary>Optional per-column tooltips matching <see cref="Labels"/> (A–E).</summary>
  [JsonPropertyName("LabelHints")]
  public List<string> LabelHints { get; init; } = [];

  [JsonPropertyName("Program")]
  public TeoCardProgramSection Program { get; init; } = new();

  [JsonPropertyName("Data")]
  public TeoCardDataSection Data { get; init; } = new();

  [JsonPropertyName("Created")]
  public DateTimeOffset? Created { get; init; }

  [JsonPropertyName("Modified")]
  public DateTimeOffset? Modified { get; init; }
}

public sealed class TeoCardProgramSection
{
  /// <summary>
  /// Exclusive program encoding: <c>mnemonic</c> (default) or <c>machine</c>.
  /// JSON key is <c>CodeEncoding</c>; legacy <c>Encoding</c> is accepted when reading.
  /// </summary>
  [JsonPropertyName("CodeEncoding")]
  public string CodeEncoding { get; init; } = CardCodeEncoding.Default;

  [JsonPropertyName("Steps")]
  public List<string> Steps { get; init; } = [];
}

public sealed class TeoCardDataSection
{
  [JsonPropertyName("Registers")]
  public List<double> Registers { get; init; } = [];
}
