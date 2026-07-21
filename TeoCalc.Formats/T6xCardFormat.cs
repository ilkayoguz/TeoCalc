using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Formats;

/// <summary>
/// Read / write TeoCalc card text (<c>.t65</c> / <c>.t67</c>; legacy <c>.t6x</c> still loads).
/// TOML-inspired, not strict TOML. Supports <c>//</c> and <c>/* ... */</c> comments.
/// </summary>
public static partial class T6xCardFormat
{
  private static readonly string[] StripKeys = ["A", "B", "C", "D", "E"];

  public static bool IsCardTextPath(string path)
  {
    string ext = Path.GetExtension(path);
    return string.Equals(ext, T6xDocument.Extension65, StringComparison.OrdinalIgnoreCase)
      || string.Equals(ext, T6xDocument.Extension67, StringComparison.OrdinalIgnoreCase)
      || string.Equals(ext, T6xDocument.LegacyExtension, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>Preferred extension for a TargetCpu / engine id (<c>.t65</c> or <c>.t67</c>).</summary>
  public static string ExtensionForTargetCpu(string targetCpuOrEngineId)
  {
    string normalized = NormalizeTargetCpu(targetCpuOrEngineId);
    return normalized.EndsWith("67", StringComparison.OrdinalIgnoreCase)
      ? T6xDocument.Extension67
      : T6xDocument.Extension65;
  }

  [Obsolete("Use IsCardTextPath.")]
  public static bool IsT6xPath(string path) => IsCardTextPath(path);

  public static T6xDocument ReadFile(string path)
  {
    string text = File.ReadAllText(path, Encoding.UTF8);
    T6xDocument document = Parse(text);
    ValidateExtensionMatchesTargetCpu(path, document.TargetCpu);
    return document;
  }

  public static void WriteFile(string path, T6xDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    ValidateExtensionMatchesTargetCpu(path, document.TargetCpu);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path, Format(document), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
  }

  private static void ValidateExtensionMatchesTargetCpu(string path, string targetCpu)
  {
    string ext = Path.GetExtension(path);
    if (string.Equals(ext, T6xDocument.LegacyExtension, StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    string expected = ExtensionForTargetCpu(targetCpu);
    if (!string.Equals(ext, expected, StringComparison.OrdinalIgnoreCase))
    {
      throw new FormatException(
        $"Extension '{ext}' does not match TargetCpu '{targetCpu}' (expected '{expected}').");
    }
  }

  public static T6xDocument Parse(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    string cleaned = StripComments(text);
    Dictionary<string, List<string>> sections = SplitSections(cleaned);

    if (!sections.TryGetValue("General", out List<string>? generalLines))
    {
      throw new FormatException("Missing [General] section.");
    }

    Dictionary<string, string> general = ParseKeyValues(generalLines);
    string format = Require(general, "Format");
    if (!string.Equals(format, T6xDocument.FormatId, StringComparison.Ordinal))
    {
      throw new FormatException($"Unsupported Format '{format}'. Expected '{T6xDocument.FormatId}'.");
    }

    if (!int.TryParse(Require(general, "SchemaVersion"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int schema)
        || schema != T6xDocument.CurrentSchemaVersion)
    {
      throw new FormatException(
        $"Unsupported SchemaVersion '{GetOptional(general, "SchemaVersion")}'. Expected {T6xDocument.CurrentSchemaVersion}.");
    }

    string targetCpu = Require(general, "TargetCpu");
    // Prefer CodeEncoding; accept legacy Encoding for older card text files.
    string encoding = CardCodeEncoding.Normalize(
      GetOptional(general, CardCodeEncoding.Key) ?? GetOptional(general, CardCodeEncoding.LegacyKey));

    List<T6xLabelEntry> labels = [];
    if (sections.TryGetValue("Label", out List<string>? labelLines))
    {
      labels = ParseLabels(labelLines);
    }

    if (sections.ContainsKey("Machine"))
    {
      throw new FormatException(
        $"Separate [Machine] section is not supported. Use {CardCodeEncoding.Key} = \"{CardCodeEncoding.Machine}\" with a single [Code] section.");
    }

    List<string> code = [];
    if (sections.TryGetValue("Code", out List<string>? codeLines))
    {
      foreach (string line in codeLines)
      {
        string step = line.Trim();
        if (step.Length > 0)
        {
          code.Add(step);
        }
      }
    }
    else if (sections.TryGetValue("Program", out List<string>? programLines))
    {
      // Alias for early drafts.
      foreach (string line in programLines)
      {
        string step = line.Trim();
        if (step.Length > 0)
        {
          code.Add(step);
        }
      }
    }

    if (CardCodeEncoding.IsMachine(encoding))
    {
      foreach (string step in code)
      {
        _ = CardCodeEncoding.ParseMachineByte(step);
      }
    }

    Dictionary<int, double> data = new();
    if (sections.TryGetValue("Data", out List<string>? dataLines))
    {
      data = ParseData(dataLines);
    }

    return new T6xDocument
    {
      Format = format,
      SchemaVersion = schema,
      TargetCpu = targetCpu,
      Profile = GetOptional(general, "Profile"),
      Category = GetOptional(general, "Category"),
      Title = GetOptional(general, "Title"),
      Description = GetOptional(general, "Description"),
      Usage = GetOptional(general, "Usage"),
      RunHint = GetOptional(general, "RunHint"),
      CodeEncoding = encoding,
      Author = GetOptional(general, "Author"),
      Created = ParseTimestamp(GetOptional(general, "Created")),
      Modified = ParseTimestamp(GetOptional(general, "Modified")),
      Labels = labels,
      Code = code,
      Data = data,
    };
  }

  public static string Format(T6xDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    StringBuilder sb = new();
    sb.AppendLine("[General]");
    WriteKv(sb, "Format", document.Format);
    WriteKv(sb, "SchemaVersion", document.SchemaVersion.ToString(CultureInfo.InvariantCulture));
    WriteKv(sb, "TargetCpu", document.TargetCpu);
    WriteOptional(sb, "Profile", document.Profile);
    WriteOptional(sb, "Category", document.Category);
    WriteOptional(sb, "Title", document.Title);
    WriteOptional(sb, "Description", document.Description);
    WriteOptional(sb, "Usage", document.Usage);
    WriteOptional(sb, "RunHint", document.RunHint);
    WriteKv(sb, CardCodeEncoding.Key, CardCodeEncoding.Normalize(document.CodeEncoding));
    WriteOptional(sb, "Author", document.Author);
    if (document.Created is { } created)
    {
      WriteKv(sb, "Created", created.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }

    if (document.Modified is { } modified)
    {
      WriteKv(sb, "Modified", modified.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }

    if (document.Labels.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine("[Label]");
      foreach (string key in StripKeys)
      {
        T6xLabelEntry? entry = document.Labels.Find(
          label => string.Equals(label.Key, key, StringComparison.OrdinalIgnoreCase));
        if (entry is null || string.IsNullOrWhiteSpace(entry.Caption))
        {
          continue;
        }

        if (string.IsNullOrWhiteSpace(entry.Hint))
        {
          sb.Append(key).Append(" = ").Append(Quote(entry.Caption)).AppendLine();
        }
        else
        {
          sb.Append(key).Append(" = ").Append(Quote(entry.Caption)).Append(", ").Append(Quote(entry.Hint)).AppendLine();
        }
      }
    }

    sb.AppendLine();
    sb.AppendLine("[Code]");
    foreach (string step in document.Code)
    {
      if (!string.IsNullOrWhiteSpace(step)
          && !string.Equals(step.Trim(), "PTR", StringComparison.OrdinalIgnoreCase))
      {
        sb.AppendLine(step.Trim());
      }
    }

    if (document.Data.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine("[Data]");
      foreach (KeyValuePair<int, double> pair in document.Data.OrderBy(static entry => entry.Key))
      {
        sb.Append(pair.Key.ToString(CultureInfo.InvariantCulture))
          .Append(" = ")
          .Append(Convert.ToString(pair.Value, CultureInfo.InvariantCulture))
          .AppendLine();
      }
    }

    return sb.ToString();
  }

  public static TeoCardDocument ToTeoCardDocument(T6xDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    string[] captions = TeoCardProgramFormat.NormalizeStripLabels([]);
    string[] hints = TeoCardProgramFormat.NormalizeStripLabels([]);
    foreach (T6xLabelEntry label in document.Labels)
    {
      int column = Array.FindIndex(
        StripKeys,
        key => string.Equals(key, label.Key, StringComparison.OrdinalIgnoreCase));
      if (column < 0)
      {
        continue;
      }

      captions[column] = label.Caption ?? string.Empty;
      hints[column] = label.Hint ?? string.Empty;
    }

    List<double> registers = [];
    int maxIndex = document.Data.Count == 0 ? ClassicCardSnapshot.DefaultRegisterCount - 1
      : Math.Max(ClassicCardSnapshot.DefaultRegisterCount - 1, document.Data.Keys.Max());
    for (int i = 0; i <= maxIndex; i++)
    {
      registers.Add(document.Data.TryGetValue(i, out double value) ? value : 0d);
    }

    return new TeoCardDocument
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = document.TargetCpu,
      InteropMagic = null,
      Title = document.Title,
      Description = document.Description,
      Usage = document.Usage,
      Category = document.Category,
      RunHint = document.RunHint,
      Labels = captions.ToList(),
      LabelHints = hints.ToList(),
      Program = new TeoCardProgramSection
      {
        CodeEncoding = CardCodeEncoding.Normalize(document.CodeEncoding),
        Steps = document.Code
          .Where(step => !string.Equals(step.Trim(), "PTR", StringComparison.OrdinalIgnoreCase))
          .Select(step => step.Trim())
          .Where(step => step.Length > 0)
          .ToList(),
      },
      Data = new TeoCardDataSection
      {
        Registers = registers,
      },
      Created = document.Created,
      Modified = document.Modified,
    };
  }

  public static T6xDocument FromTeoCardDocument(TeoCardDocument document, string? profile = null)
  {
    ArgumentNullException.ThrowIfNull(document);
    List<T6xLabelEntry> labels = [];
    for (int i = 0; i < StripKeys.Length; i++)
    {
      string caption = i < document.Labels.Count ? document.Labels[i] : string.Empty;
      if (string.IsNullOrWhiteSpace(caption))
      {
        continue;
      }

      string? hint = i < document.LabelHints.Count && !string.IsNullOrWhiteSpace(document.LabelHints[i])
        ? document.LabelHints[i]
        : null;
      labels.Add(new T6xLabelEntry
      {
        Key = StripKeys[i],
        Caption = caption,
        Hint = hint,
      });
    }

    Dictionary<int, double> data = new();
    for (int i = 0; i < document.Data.Registers.Count; i++)
    {
      double value = document.Data.Registers[i];
      if (Math.Abs(value) > double.Epsilon)
      {
        data[i] = value;
      }
    }

    return new T6xDocument
    {
      Format = T6xDocument.FormatId,
      SchemaVersion = T6xDocument.CurrentSchemaVersion,
      TargetCpu = NormalizeTargetCpu(document.Model),
      Profile = profile ?? NormalizeTargetCpu(document.Model),
      Category = document.Category,
      Title = document.Title,
      Description = document.Description,
      Usage = document.Usage,
      RunHint = document.RunHint,
      CodeEncoding = CardCodeEncoding.Normalize(document.Program.CodeEncoding),
      Created = document.Created,
      Modified = document.Modified ?? DateTimeOffset.UtcNow,
      Labels = labels,
      Code = document.Program.Steps
        .Where(step => !string.Equals(step.Trim(), "PTR", StringComparison.OrdinalIgnoreCase))
        .Select(step => step.Trim())
        .Where(step => step.Length > 0)
        .ToList(),
      Data = data,
    };
  }

  public static ClassicCardSnapshot ToClassicSnapshot(
    T6xDocument document,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = ClassicCardSnapshot.DefaultProgramCapacity,
    int registerCount = ClassicCardSnapshot.DefaultRegisterCount)
  {
    TeoCardDocument teo = ToTeoCardDocument(document);
    List<string> steps = [.. teo.Program.Steps];
    string encoding = CardCodeEncoding.Normalize(teo.Program.CodeEncoding);
    if (CardCodeEncoding.IsMnemonic(encoding))
    {
      EnsureMissingStripLabelStubs(steps);
      // Classic program RAM needs an internal pointer marker; authoring format never writes PTR.
      if (!steps.Exists(step => string.Equals(step, "PTR", StringComparison.OrdinalIgnoreCase)))
      {
        steps.Add("PTR");
      }
    }
    else if (!steps.Exists(IsClassicPointerStep))
    {
      // Machine mode: pointer is the internal Classic byte, not the PTR mnemonic.
      steps.Add(ClassicProgramCodes.Pointer.ToString(CultureInfo.InvariantCulture));
    }

    TeoCardDocument withPointer = new()
    {
      Format = teo.Format,
      SchemaVersion = teo.SchemaVersion,
      Model = teo.Model,
      InteropMagic = teo.InteropMagic,
      Title = teo.Title,
      Description = teo.Description,
      Usage = teo.Usage,
      Category = teo.Category,
      RunHint = teo.RunHint,
      Labels = teo.Labels,
      LabelHints = teo.LabelHints,
      Program = new TeoCardProgramSection
      {
        CodeEncoding = encoding,
        Steps = steps,
      },
      Data = teo.Data,
      Created = teo.Created,
      Modified = teo.Modified,
    };

    return TeoCardProgramFormat.ToClassicSnapshot(
      withPointer,
      codeForMnemonic,
      programCapacity,
      registerCount);
  }

  private static bool IsClassicPointerStep(string step)
  {
    string trimmed = step.Trim();
    return string.Equals(trimmed, "PTR", StringComparison.OrdinalIgnoreCase)
      || (byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value)
          && value == ClassicProgramCodes.Pointer);
  }

  public static Teo67CardSnapshot ToTeo67Snapshot(
    T6xDocument document,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = Teo67CardSnapshot.DefaultProgramCapacity,
    int registerCount = Teo67CardSnapshot.DefaultRegisterCount)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(codeForMnemonic);

    byte[] program = new byte[programCapacity];
    int write = 0;
    string encoding = CardCodeEncoding.Normalize(document.CodeEncoding);
    foreach (string rawStep in document.Code)
    {
      string step = rawStep.Trim();
      if (step.Length == 0
          || string.Equals(step, "PTR", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      byte code = CardCodeEncoding.ResolveStep(encoding, step, codeForMnemonic);
      if (write >= programCapacity)
      {
        throw new FormatException("Program too large");
      }

      program[write++] = code;
    }

    double[] registers = new double[registerCount];
    foreach (KeyValuePair<int, double> pair in document.Data)
    {
      if (pair.Key < 0 || pair.Key >= registerCount)
      {
        throw new FormatException($"Data index {pair.Key} out of range (0..{registerCount - 1}).");
      }

      registers[pair.Key] = pair.Value;
    }

    return new Teo67CardSnapshot(program, registers);
  }

  public static T6xDocument FromTeo67Snapshot(
    Teo67CardSnapshot snapshot,
    Func<byte, string> mnemonicForCode,
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

    for (int i = 0; i < lastNonZero; i++)
    {
      string mnemonic = mnemonicForCode(snapshot.ProgramCodes[i]);
      if (!string.IsNullOrWhiteSpace(mnemonic))
      {
        steps.Add(mnemonic);
      }
    }

    Dictionary<int, double> data = new();
    for (int i = 0; i < snapshot.Registers.Count; i++)
    {
      double value = snapshot.Registers[i];
      if (Math.Abs(value) > double.Epsilon)
      {
        data[i] = value;
      }
    }

    List<T6xLabelEntry> labels = [];
    if (metadata?.Labels is { Count: > 0 })
    {
      for (int i = 0; i < StripKeys.Length && i < metadata.Labels.Count; i++)
      {
        if (string.IsNullOrWhiteSpace(metadata.Labels[i]))
        {
          continue;
        }

        string? hint = metadata.LabelHints is { Count: > 0 } && i < metadata.LabelHints.Count
          ? metadata.LabelHints[i]
          : null;
        labels.Add(new T6xLabelEntry
        {
          Key = StripKeys[i],
          Caption = metadata.Labels[i],
          Hint = string.IsNullOrWhiteSpace(hint) ? null : hint,
        });
      }
    }

    return new T6xDocument
    {
      Format = T6xDocument.FormatId,
      SchemaVersion = T6xDocument.CurrentSchemaVersion,
      TargetCpu = "T-67",
      Profile = metadata is null ? "T-67" : NormalizeTargetCpu(metadata.Model),
      Category = metadata?.Category,
      Title = metadata?.Title,
      Description = metadata?.Description,
      Usage = metadata?.Usage,
      RunHint = metadata?.RunHint,
      CodeEncoding = CardCodeEncoding.Mnemonic,
      Created = metadata?.Created,
      Modified = DateTimeOffset.UtcNow,
      Labels = labels,
      Code = steps,
      Data = data,
    };
  }

  /// <summary>
  /// Authoring files stay sparse (omit unused A–E). Classic RUN keys A–E still need a stop
  /// target so undefined columns do not fall through into the next routine.
  /// </summary>
  private static void EnsureMissingStripLabelStubs(List<string> steps)
  {
    bool[] present = ClassicCardStripLabels.SubroutinePresenceFromSteps(steps);
    for (int column = 0; column < StripKeys.Length; column++)
    {
      if (present[column])
      {
        continue;
      }

      steps.Add("LBL");
      steps.Add(StripKeys[column]);
      steps.Add("RTN");
    }
  }

  public static bool TargetCpuMatches(string targetCpu, string engineModelId, string? displayModelId = null)
  {
    if (string.IsNullOrWhiteSpace(targetCpu))
    {
      return false;
    }

    string card = NormalizeTargetCpu(targetCpu);
    string engine = NormalizeTargetCpu(engineModelId);
    if (string.Equals(card, engine, StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (!string.IsNullOrWhiteSpace(displayModelId)
        && string.Equals(card, NormalizeTargetCpu(displayModelId), StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    return TeoCardProgramFormat.ModelMatches(targetCpu, engineModelId, displayModelId);
  }

  public static string NormalizeTargetCpu(string modelOrCpu)
  {
    string raw = modelOrCpu.Trim();
    if (raw.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      raw = raw[3..];
    }
    else if (raw.StartsWith("HP", StringComparison.OrdinalIgnoreCase)
             && raw.Length > 2
             && char.IsDigit(raw[2]))
    {
      raw = raw[2..];
    }
    else if (raw.StartsWith("T-", StringComparison.OrdinalIgnoreCase))
    {
      raw = raw[2..];
    }
    else if (raw.StartsWith("T", StringComparison.OrdinalIgnoreCase)
             && raw.Length > 1
             && char.IsDigit(raw[1]))
    {
      raw = raw[1..];
    }

    return $"T-{raw}";
  }

  private static string StripComments(string text)
  {
    string withoutBlock = BlockCommentRegex().Replace(text, string.Empty);
    StringBuilder sb = new(withoutBlock.Length);
    using StringReader reader = new(withoutBlock);
    string? line;
    while ((line = reader.ReadLine()) is not null)
    {
      int comment = IndexOfLineComment(line);
      sb.AppendLine(comment >= 0 ? line[..comment] : line);
    }

    return sb.ToString();
  }

  private static int IndexOfLineComment(string line)
  {
    bool inQuote = false;
    for (int i = 0; i < line.Length - 1; i++)
    {
      char c = line[i];
      if (c == '"' && (i == 0 || line[i - 1] != '\\'))
      {
        inQuote = !inQuote;
        continue;
      }

      if (!inQuote && c == '/' && line[i + 1] == '/')
      {
        return i;
      }
    }

    return -1;
  }

  private static Dictionary<string, List<string>> SplitSections(string text)
  {
    Dictionary<string, List<string>> sections = new(StringComparer.OrdinalIgnoreCase);
    string? current = null;
    using StringReader reader = new(text);
    string? line;
    int lineNumber = 0;
    while ((line = reader.ReadLine()) is not null)
    {
      lineNumber++;
      string trimmed = line.Trim();
      if (trimmed.Length == 0)
      {
        continue;
      }

      if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
      {
        current = trimmed[1..^1].Trim();
        if (current.Length == 0)
        {
          throw new FormatException($"Empty section name at line {lineNumber}.");
        }

        if (!sections.ContainsKey(current))
        {
          sections[current] = [];
        }

        continue;
      }

      if (current is null)
      {
        throw new FormatException($"Content outside section at line {lineNumber}.");
      }

      sections[current].Add(line);
    }

    return sections;
  }

  private static Dictionary<string, string> ParseKeyValues(List<string> lines)
  {
    Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
    foreach (string raw in lines)
    {
      int eq = raw.IndexOf('=');
      if (eq <= 0)
      {
        throw new FormatException($"Expected key = value: {raw.Trim()}");
      }

      string key = raw[..eq].Trim();
      string value = Unquote(raw[(eq + 1)..].Trim());
      map[key] = value;
    }

    return map;
  }

  private static List<T6xLabelEntry> ParseLabels(List<string> lines)
  {
    List<T6xLabelEntry> labels = [];
    foreach (string raw in lines)
    {
      int eq = raw.IndexOf('=');
      if (eq <= 0)
      {
        throw new FormatException($"Expected Label key = value: {raw.Trim()}");
      }

      string key = raw[..eq].Trim().ToUpperInvariant();
      if (!StripKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
      {
        throw new FormatException($"Unsupported label key '{key}'. Expected A–E.");
      }

      List<string> parts = SplitCommaSeparatedValues(raw[(eq + 1)..].Trim());
      if (parts.Count == 0 || parts.Count > 2)
      {
        throw new FormatException($"Label {key} expects caption[, hint].");
      }

      labels.Add(new T6xLabelEntry
      {
        Key = key,
        Caption = parts[0],
        Hint = parts.Count > 1 ? parts[1] : null,
      });
    }

    return labels;
  }

  private static Dictionary<int, double> ParseData(List<string> lines)
  {
    Dictionary<int, double> data = new();
    foreach (string raw in lines)
    {
      int eq = raw.IndexOf('=');
      if (eq <= 0)
      {
        throw new FormatException($"Expected Data index = value: {raw.Trim()}");
      }

      string indexText = raw[..eq].Trim();
      string valueText = raw[(eq + 1)..].Trim();
      if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)
          || index < 0)
      {
        throw new FormatException($"Invalid Data index '{indexText}'.");
      }

      if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        throw new FormatException($"Invalid Data value '{valueText}'.");
      }

      data[index] = value;
    }

    return data;
  }

  private static List<string> SplitCommaSeparatedValues(string text)
  {
    List<string> parts = [];
    StringBuilder current = new();
    bool inQuote = false;
    for (int i = 0; i < text.Length; i++)
    {
      char c = text[i];
      if (c == '"' && (i == 0 || text[i - 1] != '\\'))
      {
        inQuote = !inQuote;
        current.Append(c);
        continue;
      }

      if (c == ',' && !inQuote)
      {
        parts.Add(Unquote(current.ToString().Trim()));
        current.Clear();
        continue;
      }

      current.Append(c);
    }

    if (current.Length > 0 || parts.Count > 0)
    {
      parts.Add(Unquote(current.ToString().Trim()));
    }

    return parts;
  }

  private static string Require(Dictionary<string, string> map, string key) =>
    map.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
      ? value
      : throw new FormatException($"Missing required General.{key}.");

  private static string? GetOptional(Dictionary<string, string> map, string key) =>
    map.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

  private static DateTimeOffset? ParseTimestamp(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return null;
    }

    return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
  }

  private static void WriteOptional(StringBuilder sb, string key, string? value)
  {
    if (!string.IsNullOrWhiteSpace(value))
    {
      WriteKv(sb, key, value);
    }
  }

  private static void WriteKv(StringBuilder sb, string key, string value) =>
    sb.Append(key).Append(" = ").Append(Quote(value)).AppendLine();

  private static string Quote(string value)
  {
    string escaped = value.Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal);
    return $"\"{escaped}\"";
  }

  private static string Unquote(string value)
  {
    string trimmed = value.Trim();
    if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
    {
      string inner = trimmed[1..^1];
      return inner.Replace("\\\"", "\"", StringComparison.Ordinal)
        .Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    // Tolerate trailing commas from draft samples.
    return trimmed.TrimEnd(',');
  }

  [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
  private static partial Regex BlockCommentRegex();
}
