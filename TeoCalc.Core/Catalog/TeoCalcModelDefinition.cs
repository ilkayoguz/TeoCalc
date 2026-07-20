using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeoCalc.Core.Catalog;

public sealed class TeoCalcModelDefinition
{
  [JsonPropertyName("Model")]
  public string Model { get; init; } = "";

  [JsonPropertyName("DisplayName")]
  public string DisplayName { get; init; } = "";

  [JsonPropertyName("Family")]
  public string Family { get; init; } = "";

  [JsonPropertyName("Hardware")]
  public TeoCalcModelHardware Hardware { get; init; } = new();

  [JsonPropertyName("Program")]
  public TeoCalcModelProgram? Program { get; init; }

  [JsonPropertyName("Firmware")]
  public TeoCalcModelFirmware Firmware { get; init; } = new();

  /// <summary>Optional faceplate/UI metadata; when absent, Rendering applies family heuristics.</summary>
  [JsonPropertyName("Faceplate")]
  public TeoCalcModelFaceplate? Faceplate { get; init; }

  public static TeoCalcModelDefinition Load(string path)
  {
    string json = File.ReadAllText(path);
    TeoCalcModelDefinition? model = JsonSerializer.Deserialize<TeoCalcModelDefinition>(json, JsonOptions);
    return model ?? throw new InvalidDataException($"Failed to load {path}");
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };
}

public sealed class TeoCalcModelHardware
{
  [JsonPropertyName("ButtonCount")]
  public int ButtonCount { get; init; }

  [JsonPropertyName("RamBytes")]
  public int RamBytes { get; init; } = 448;

  [JsonPropertyName("RegisterDigits")]
  public int RegisterDigits { get; init; } = 14;

  [JsonPropertyName("ProgramRamBase")]
  public int ProgramRamBase { get; init; }

  [JsonPropertyName("RomWordCount")]
  public int RomWordCount { get; init; }
}

public sealed class TeoCalcModelProgram
{
  [JsonPropertyName("MaxSteps")]
  public int MaxSteps { get; init; }

  [JsonPropertyName("Vocabulary")]
  public string Vocabulary { get; init; } = "";
}

public sealed class TeoCalcModelFirmware
{
  [JsonPropertyName("RomBinary")]
  public string RomBinary { get; init; } = "";

  [JsonPropertyName("RomListing")]
  public string RomListing { get; init; } = "";

  [JsonPropertyName("RomMap")]
  public string RomMap { get; init; } = "";

  [JsonPropertyName("HandlerCatalog")]
  public string HandlerCatalog { get; init; } = "";
}

public sealed class TeoCalcModelFaceplate
{
  /// <summary>Runtime body layout id (canonical Modern geometry is <c>00d</c>).</summary>
  [JsonPropertyName("BodyLayoutId")]
  public string BodyLayoutId { get; init; } = "00d";

  [JsonPropertyName("ThemeId")]
  public string ThemeId { get; init; } = "Modern";

  /// <summary>Optional override for logo-strip short id (defaults from model id).</summary>
  [JsonPropertyName("ShortId")]
  public string? ShortId { get; init; }

  /// <summary>
  /// Named modifier→slot pack id (e.g. ClassicFg, SpiceFgh, ClassicHp67Fgh, None).
  /// Semantic only — no px/pt.
  /// </summary>
  [JsonPropertyName("AnnotationStyleId")]
  public string? AnnotationStyleId { get; init; }

  /// <summary>Named switch bank id (e.g. Classic65, WoodstockPrgm, PowerOnly).</summary>
  [JsonPropertyName("SwitchBankId")]
  public string? SwitchBankId { get; init; }

  /// <summary>When set, overrides card-slot presence for the body layout.</summary>
  [JsonPropertyName("HasCardSlot")]
  public bool? HasCardSlot { get; init; }

  /// <summary>When set, enables the title-bar printer capability icon (HP-19C).</summary>
  [JsonPropertyName("HasPrinter")]
  public bool? HasPrinter { get; init; }
}
