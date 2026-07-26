namespace TeoCalc.Formats;

/// <summary>
/// Editable card-file metadata ([General] + [Label]) excluding program bytes and DATA.
/// </summary>
public sealed class CardMetadataFields
{
  public string Title { get; set; } = "";

  public string Category { get; set; } = "";

  public string Author { get; set; } = "";

  public string Profile { get; set; } = "";

  public string Description { get; set; } = "";

  public string Usage { get; set; } = "";

  public string RunHint { get; set; } = "";

  /// <summary><see cref="CardCodeEncoding.Mnemonic"/> or <see cref="CardCodeEncoding.Machine"/>.</summary>
  public string CodeEncoding { get; set; } = CardCodeEncoding.Mnemonic;

  /// <summary>Optional ISO-8601 / file timestamp text; empty keeps prior / sets on first save.</summary>
  public string Created { get; set; } = "";

  /// <summary>A–E strip captions (length 5).</summary>
  public string[] Labels { get; set; } = ["", "", "", "", ""];

  /// <summary>A–E strip tooltips (length 5).</summary>
  public string[] LabelHints { get; set; } = ["", "", "", "", ""];

  public static CardMetadataFields FromDocument(TeoCardDocument? document, string fallbackEncoding)
  {
    CardMetadataFields fields = new()
    {
      CodeEncoding = CardCodeEncoding.Normalize(
        document?.Program.CodeEncoding ?? fallbackEncoding),
    };

    if (document is null)
    {
      return fields;
    }

    fields.Title = document.Title ?? "";
    fields.Category = document.Category ?? "";
    fields.Author = document.Author ?? "";
    fields.Profile = document.Profile ?? "";
    fields.Description = document.Description ?? "";
    fields.Usage = document.Usage ?? "";
    fields.RunHint = document.RunHint ?? "";
    fields.Created = document.Created is { } created
      ? created.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture)
      : "";

    string[] labels = TeoCardProgramFormat.NormalizeStripLabels(document.Labels);
    string[] hints = TeoCardProgramFormat.NormalizeStripLabels(document.LabelHints);
    fields.Labels = labels;
    fields.LabelHints = hints;
    return fields;
  }
}
