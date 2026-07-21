using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Formats;

/// <summary>TeoCalc card JSON (<c>.json</c>) — convert between <see cref="TeoCardDocument"/> and Classic snapshots.</summary>
public static class TeoCardProgramFormat
{
  public const string Extension = ".json";

  private const byte ClassicStartCode = 63;

  public static bool IsTeoCardPath(string path)
  {
    string ext = Path.GetExtension(path);
    return ext.Equals(Extension, StringComparison.OrdinalIgnoreCase)
      || ext.Equals(".teocard", StringComparison.OrdinalIgnoreCase);
  }

  public static TeoCardDocument ReadFile(string path)
  {
    string json = File.ReadAllText(path, Encoding.UTF8);
    return Parse(json);
  }

  public static TeoCardDocument Parse(string json)
  {
    ArgumentNullException.ThrowIfNull(json);
    TeoCardDocument? document = JsonSerializer.Deserialize<TeoCardDocument>(json, JsonOptions)
      ?? throw new FormatException("TeoCard document is empty.");

    if (!string.Equals(document.Format, TeoCardDocument.FormatId, StringComparison.Ordinal))
    {
      throw new FormatException($"Unsupported Format '{document.Format}'. Expected '{TeoCardDocument.FormatId}'.");
    }

    if (document.SchemaVersion != TeoCardDocument.CurrentSchemaVersion)
    {
      throw new FormatException(
        $"Unsupported SchemaVersion {document.SchemaVersion}. Expected {TeoCardDocument.CurrentSchemaVersion}.");
    }

    if (string.IsNullOrWhiteSpace(document.Model))
    {
      throw new FormatException("Model is required.");
    }

    if (!string.Equals(document.Program.Encoding, "mnemonic", StringComparison.OrdinalIgnoreCase))
    {
      throw new FormatException($"Unsupported Program.Encoding '{document.Program.Encoding}'.");
    }

    return document;
  }

  public static void WriteFile(string path, TeoCardDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    string json = Format(document);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path, json, Encoding.UTF8);
  }

  public static string Format(TeoCardDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    return JsonSerializer.Serialize(document, JsonWriteOptions);
  }

  public static ClassicCardSnapshot ToClassicSnapshot(
    TeoCardDocument document,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = ClassicCardSnapshot.DefaultProgramCapacity,
    int registerCount = ClassicCardSnapshot.DefaultRegisterCount)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(codeForMnemonic);

    byte[] program = new byte[programCapacity];
    int programWrite = 0;
    program[programWrite++] = ClassicStartCode;

    foreach (string rawStep in document.Program.Steps)
    {
      string step = rawStep.Trim();
      if (step.Length == 0)
      {
        continue;
      }

      byte? code = codeForMnemonic(step);
      if (code is null)
      {
        throw new FormatException($"Mnemonic not found: {step}");
      }

      if (programWrite >= programCapacity)
      {
        throw new FormatException("Program too large");
      }

      program[programWrite++] = code.Value;
    }

    double[] registers = new double[registerCount];
    for (int i = 0; i < registerCount; i++)
    {
      registers[i] = i < document.Data.Registers.Count ? document.Data.Registers[i] : 0d;
    }

    return new ClassicCardSnapshot(program, registers);
  }

  public static TeoCardDocument FromClassicSnapshot(
    ClassicCardSnapshot snapshot,
    Func<byte, string> mnemonicForCode,
    string model,
    TeoCardDocument? metadata = null)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    List<string> steps = [];
    int lastNonZero = 0;
    for (int i = 0; i < snapshot.ProgramCodes.Count; i++)
    {
      if (snapshot.ProgramCodes[i] != 0)
      {
        lastNonZero = i + 1;
      }
    }

    for (int i = 1; i < lastNonZero; i++)
    {
      byte code = snapshot.ProgramCodes[i];
      string mnemonic = mnemonicForCode(code);
      if (string.IsNullOrWhiteSpace(mnemonic))
      {
        mnemonic = $"#{code}";
      }

      steps.Add(mnemonic);
    }

    List<double> registers = [];
    int registerCount = Math.Max(ClassicCardSnapshot.DefaultRegisterCount, snapshot.Registers.Count);
    for (int i = 0; i < registerCount; i++)
    {
      registers.Add(i < snapshot.Registers.Count ? snapshot.Registers[i] : 0d);
    }

    List<string> labels = [];
    if (metadata?.Labels is { Count: > 0 } && ClassicCardStripLabels.HasAnyLabel(metadata.Labels))
    {
      labels.AddRange(metadata.Labels);
    }
    else
    {
      labels.AddRange(ClassicCardStripLabels.InferFromSteps(steps));
    }

    return new TeoCardDocument
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = model,
      InteropMagic = metadata?.InteropMagic,
      Title = metadata?.Title,
      Description = metadata?.Description,
      Usage = metadata?.Usage,
      Category = metadata?.Category,
      RunHint = metadata?.RunHint,
      Labels = labels,
      Program = new TeoCardProgramSection
      {
        Encoding = "mnemonic",
        Steps = steps,
      },
      Data = new TeoCardDataSection
      {
        Registers = registers,
      },
      Created = metadata?.Created ?? DateTimeOffset.UtcNow,
      Modified = DateTimeOffset.UtcNow,
    };
  }

  public static string[] NormalizeStripLabels(IReadOnlyList<string>? labels)
  {
    string[] strip = new string[5];
    if (labels is null)
    {
      return strip;
    }

    for (int i = 0; i < strip.Length; i++)
    {
      strip[i] = i < labels.Count ? labels[i] : string.Empty;
    }

    return strip;
  }

  public static bool ModelMatches(string cardModel, string engineModelId, string? displayModelId = null)
  {
    if (string.IsNullOrWhiteSpace(cardModel))
    {
      return false;
    }

    string card = cardModel.Trim();
    if (string.Equals(card, engineModelId, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (!string.IsNullOrWhiteSpace(displayModelId)
        && string.Equals(card, displayModelId, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    static string ShortId(string id)
    {
      string raw = id.Trim();
      return raw.StartsWith("HP-", StringComparison.OrdinalIgnoreCase) ? raw[3..] : raw;
    }

    return string.Equals(ShortId(card), ShortId(engineModelId), StringComparison.OrdinalIgnoreCase);
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
  };

  private static JsonSerializerOptions JsonWriteOptions => new()
  {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
  };
}
