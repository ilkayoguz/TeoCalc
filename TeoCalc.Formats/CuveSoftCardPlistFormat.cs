using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace TeoCalc.Formats;

/// <summary>Import / export CuveSoft RPN-65 cards (T-65): product <c>.xml</c>, plus <c>.plist</c> / <c>.rpn65</c> aliases.</summary>
public static class CuveSoftCardPlistFormat
{
  public const string Extension = ".xml";

  private const byte ClassicStartCode = 63;

  public static CuveSoftCardPlistSnapshot ReadFile(string path)
  {
    string xml = File.ReadAllText(path, Encoding.UTF8);
    return Parse(xml);
  }

  public static CuveSoftCardPlistSnapshot Parse(string xml)
  {
    ArgumentNullException.ThrowIfNull(xml);
    XDocument document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
    XElement? rootDict = document.Root?.Element("dict")
      ?? throw new FormatException("Plist root dict not found.");

    string? title = ReadString(rootDict, "cardTitle");
    string? description = ReadString(rootDict, "cardDescription");
    string? usage = ReadString(rootDict, "cardUsage");
    int? category = ReadInt(rootDict, "cardCategory");
    int? cardType = ReadInt(rootDict, "cardType");
    int? cardPac = ReadInt(rootDict, "cardPac");
    DateTimeOffset? created = ReadDate(rootDict, "cardCreated");
    DateTimeOffset? modified = ReadDate(rootDict, "cardModified");
    IReadOnlyList<string> labels = ReadStringArray(rootDict, "cardLabels");
    IReadOnlyList<byte> programCodes = DecodeProgramCodes(ReadStringArray(rootDict, "cardData"));

    return new CuveSoftCardPlistSnapshot
    {
      Title = title,
      Description = description,
      Usage = usage,
      Category = category,
      CardType = cardType,
      CardPac = cardPac,
      Created = created,
      Modified = modified,
      Labels = labels,
      ProgramCodes = programCodes,
    };
  }

  public static TeoCardDocument ToTeoCardDocument(
    CuveSoftCardPlistSnapshot snapshot,
    Func<byte, string> mnemonicForCode,
    string model = "HP-65")
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    List<string> steps = [];
    for (int i = 1; i < snapshot.ProgramCodes.Count; i++)
    {
      byte code = snapshot.ProgramCodes[i];
      if (code == 0)
      {
        continue;
      }

      string mnemonic = mnemonicForCode(code);
      steps.Add(string.IsNullOrWhiteSpace(mnemonic) ? $"#{code}" : mnemonic);
    }

    List<string> labels = [];
    foreach (string label in snapshot.Labels)
    {
      if (labels.Count >= 5)
      {
        break;
      }

      labels.Add(label);
    }

    while (labels.Count < 5)
    {
      labels.Add(string.Empty);
    }

    return new TeoCardDocument
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = model,
      InteropMagic = null,
      Title = snapshot.Title,
      Description = snapshot.Description,
      Usage = snapshot.Usage,
      Category = snapshot.Category?.ToString(CultureInfo.InvariantCulture),
      RunHint = InferRunHint(steps),
      Labels = labels,
      Program = new TeoCardProgramSection
      {
        CodeEncoding = CardCodeEncoding.Mnemonic,
        Steps = steps,
      },
      Data = new TeoCardDataSection
      {
        Registers = new List<double>(new double[ClassicCardSnapshot.DefaultRegisterCount]),
      },
      Created = snapshot.Created,
      Modified = snapshot.Modified,
    };
  }

  public static ClassicCardSnapshot ToClassicSnapshot(CuveSoftCardPlistSnapshot snapshot)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    byte[] program = new byte[ClassicCardSnapshot.DefaultProgramCapacity];
    int count = Math.Min(program.Length, snapshot.ProgramCodes.Count);
    for (int i = 0; i < count; i++)
    {
      program[i] = snapshot.ProgramCodes[i];
    }

    double[] registers = new double[ClassicCardSnapshot.DefaultRegisterCount];
    return new ClassicCardSnapshot(program, registers);
  }

  public static void WriteFile(string path, CuveSoftCardPlistSnapshot snapshot)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path, Format(snapshot), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
  }

  public static string Format(CuveSoftCardPlistSnapshot snapshot)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    XDocument document = new(
      new XDeclaration("1.0", "UTF-8", null),
      new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
      new XElement(
        "plist",
        new XAttribute("version", "1.0"),
        BuildDict(snapshot)));
    using StringWriter writer = new();
    document.Save(writer, SaveOptions.None);
    return writer.ToString();
  }

  public static CuveSoftCardPlistSnapshot FromT6xDocument(
    T6xDocument document,
    Func<string, byte?> codeForMnemonic)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(codeForMnemonic);

    ClassicCardSnapshot classic = T6xCardFormat.ToClassicSnapshot(document, codeForMnemonic);
    List<string> labels = [];
    string[] captions = TeoCardProgramFormat.NormalizeStripLabels(
      document.Labels.Select(label => label.Caption).ToList());
    labels.AddRange(captions);

    int? category = null;
    if (int.TryParse(document.Category, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCategory))
    {
      category = parsedCategory;
    }

    return new CuveSoftCardPlistSnapshot
    {
      Title = document.Title,
      Description = document.Description,
      Usage = document.Usage,
      Category = category,
      CardType = 0,
      CardPac = 0,
      Created = document.Created,
      Modified = document.Modified ?? DateTimeOffset.UtcNow,
      Labels = labels,
      ProgramCodes = classic.ProgramCodes,
    };
  }

  public static bool IsCuveSoftCardPath(string path)
  {
    string ext = Path.GetExtension(path);
    return ext.Equals(Extension, StringComparison.OrdinalIgnoreCase)
      || ext.Equals(".plist", StringComparison.OrdinalIgnoreCase)
      || ext.Equals(".rpn65", StringComparison.OrdinalIgnoreCase);
  }

  private static XElement BuildDict(CuveSoftCardPlistSnapshot snapshot)
  {
    XElement dict = new("dict");
    if (snapshot.Category is int category)
    {
      AppendKeyValue(dict, "cardCategory", new XElement("integer", category.ToString(CultureInfo.InvariantCulture)));
    }

    if (snapshot.Created is DateTimeOffset created)
    {
      AppendKeyValue(dict, "cardCreated", new XElement("date", FormatPlistDate(created)));
    }

    XElement cardData = new("array");
    for (int i = 1; i < snapshot.ProgramCodes.Count; i++)
    {
      byte code = snapshot.ProgramCodes[i];
      if (code == 0)
      {
        continue;
      }

      cardData.Add(new XElement("string", code.ToString("D2", CultureInfo.InvariantCulture)));
    }

    AppendKeyValue(dict, "cardData", cardData);

    if (!string.IsNullOrWhiteSpace(snapshot.Description))
    {
      AppendKeyValue(dict, "cardDescription", new XElement("string", snapshot.Description));
    }

    XElement cardLabels = new("array");
    foreach (string label in snapshot.Labels.Take(5))
    {
      cardLabels.Add(new XElement("string", label ?? string.Empty));
    }

    while (cardLabels.Elements().Count() < 5)
    {
      cardLabels.Add(new XElement("string", string.Empty));
    }

    AppendKeyValue(dict, "cardLabels", cardLabels);

    if (snapshot.Modified is DateTimeOffset modified)
    {
      AppendKeyValue(dict, "cardModified", new XElement("date", FormatPlistDate(modified)));
    }

    if (snapshot.CardPac is int pac)
    {
      AppendKeyValue(dict, "cardPac", new XElement("integer", pac.ToString(CultureInfo.InvariantCulture)));
    }

    if (!string.IsNullOrWhiteSpace(snapshot.Title))
    {
      AppendKeyValue(dict, "cardTitle", new XElement("string", snapshot.Title));
    }

    if (snapshot.CardType is int cardType)
    {
      AppendKeyValue(dict, "cardType", new XElement("integer", cardType.ToString(CultureInfo.InvariantCulture)));
    }

    if (!string.IsNullOrWhiteSpace(snapshot.Usage))
    {
      AppendKeyValue(dict, "cardUsage", new XElement("string", snapshot.Usage));
    }

    return dict;
  }

  private static void AppendKeyValue(XElement dict, string key, XElement value)
  {
    dict.Add(new XElement("key", key));
    dict.Add(value);
  }

  private static string FormatPlistDate(DateTimeOffset value) =>
    value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

  internal static IReadOnlyList<byte> DecodeProgramCodes(IReadOnlyList<string> cardDataLines)
  {
    List<byte> codes = [ClassicStartCode];
    foreach (string line in cardDataLines)
    {
      if (ShouldSkipListingLine(line))
      {
        continue;
      }

      foreach (byte code in ParseCardDataLine(line))
      {
        codes.Add(code);
      }
    }

    return codes;
  }

  private static bool ShouldSkipListingLine(string line)
  {
    string trimmed = line.Trim();
    if (trimmed.Length == 0)
    {
      return true;
    }

    if (!trimmed.Contains(' ', StringComparison.Ordinal)
        && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
        && value >= 100)
    {
      return true;
    }

    return false;
  }

  private static IEnumerable<byte> ParseCardDataLine(string line)
  {
    string trimmed = line.Trim();
    if (trimmed.Length == 0)
    {
      yield break;
    }

    if (trimmed.Contains(' ', StringComparison.Ordinal))
    {
      foreach (string token in trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        foreach (byte code in ParseCardDataToken(token))
        {
          yield return code;
        }
      }

      yield break;
    }

    foreach (byte code in ParseCardDataToken(trimmed))
    {
      yield return code;
    }
  }

  private static IEnumerable<byte> ParseCardDataToken(string token)
  {
    if (token.Length <= 2)
    {
      yield return ParseOpcodeByte(token);
      yield break;
    }

    for (int i = 0; i < token.Length; i += 2)
    {
      string chunk = token[i..Math.Min(i + 2, token.Length)];
      if (chunk.Length == 1)
      {
        chunk = "0" + chunk;
      }

      yield return ParseOpcodeByte(chunk);
    }
  }

  private static byte ParseOpcodeByte(string token) =>
    byte.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);

  private static string? InferRunHint(IReadOnlyList<string> steps)
  {
    for (int i = 0; i < steps.Count - 1; i++)
    {
      if (!string.Equals(steps[i], "LBL", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string label = steps[i + 1];
      if (label is "A" or "B" or "C" or "D" or "E")
      {
        return $"RUN modunda {label} tuşuna bas.";
      }

      if (label.Length == 1 && label[0] is >= '0' and <= '9')
      {
        return $"RUN modunda GTO → {label} → R/S.";
      }
    }

    return null;
  }

  private static string? ReadString(XElement dict, string key)
  {
    XElement? node = FindValueNode(dict, key);
    return node?.Value;
  }

  private static int? ReadInt(XElement dict, string key)
  {
    XElement? node = FindValueNode(dict, key);
    return node is not null && int.TryParse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
      ? value
      : null;
  }

  private static DateTimeOffset? ReadDate(XElement dict, string key)
  {
    XElement? node = FindValueNode(dict, key);
    return node is not null && DateTimeOffset.TryParse(node.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset value)
      ? value
      : null;
  }

  private static IReadOnlyList<string> ReadStringArray(XElement dict, string key)
  {
    XElement? array = FindValueNode(dict, key);
    if (array is null || !string.Equals(array.Name.LocalName, "array", StringComparison.Ordinal))
    {
      return [];
    }

    List<string> values = [];
    foreach (XElement child in array.Elements("string"))
    {
      values.Add(child.Value);
    }

    return values;
  }

  private static XElement? FindValueNode(XElement dict, string key)
  {
    XElement? lastKey = null;
    foreach (XElement child in dict.Elements())
    {
      if (string.Equals(child.Name.LocalName, "key", StringComparison.Ordinal))
      {
        lastKey = child;
        continue;
      }

      if (lastKey is not null
          && string.Equals(lastKey.Value, key, StringComparison.Ordinal))
      {
        return child;
      }

      lastKey = null;
    }

    return null;
  }
}
