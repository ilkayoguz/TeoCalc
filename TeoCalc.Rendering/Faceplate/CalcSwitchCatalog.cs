namespace TeoCalc.Rendering.Faceplate;

/// <summary>Per-model switch bank from Catalog/Documents/HPs/* front photos.</summary>
public static class CalcSwitchCatalog
{
  /// <summary>HP-65 / 67: OFF·ON (start OFF) + W/PRGM·RUN (start RUN).</summary>
  public static IReadOnlyList<CalcSwitchSpec> Classic65 { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.ClassicPrgmRun(initialIndex: 1),
  ];

  public static IReadOnlyList<CalcSwitchSpec> WoodstockPrgm { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.WoodstockPrgmRun(initialIndex: 1),
  ];

  public static IReadOnlyList<CalcSwitchSpec> WoodstockAngle { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.WoodstockAngle(initialIndex: 0),
  ];

  public static IReadOnlyList<CalcSwitchSpec> FinancialBeginEnd { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.BeginEnd(initialIndex: 0),
  ];

  /// <summary>HP-38E/38C: OFF·ON + D.MY/M.DY · BEGIN/END (one dual-row switch).</summary>
  public static IReadOnlyList<CalcSwitchSpec> FinancialDateBeginEnd { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.DateBeginEnd(initialIndex: 0),
  ];

  /// <summary>HP-55: OFF·ON + TIMER·PRGM·RUN (PRGM on top).</summary>
  public static IReadOnlyList<CalcSwitchSpec> Classic55 { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
    CalcSwitchSpec.TimerPrgmRun(initialIndex: 2),
  ];

  /// <summary>HP-19C: OFF·RUN/PRGM + MAN·NORM/TRACE.</summary>
  public static IReadOnlyList<CalcSwitchSpec> Classic19C { get; } =
  [
    CalcSwitchSpec.OffRunPrgm(initialIndex: 0),
    CalcSwitchSpec.ManNormTrace(initialIndex: 0),
  ];

  public static IReadOnlyList<CalcSwitchSpec> PowerOnly { get; } =
  [
    CalcSwitchSpec.Power(initialIndex: 0),
  ];

  public static IReadOnlyList<CalcSwitchSpec> ForModel(CalcModelDefinition model)
  {
    if (!string.IsNullOrWhiteSpace(model.SwitchBankId)
        && TryBank(model.SwitchBankId, out IReadOnlyList<CalcSwitchSpec> fromId))
    {
      return fromId;
    }

    return ForModelId(model.Id, model.DisplayName);
  }

  public static IReadOnlyList<CalcSwitchSpec> ForModelId(string modelId, string? displayName = null)
  {
    string id = NormalizeId(modelId, displayName);
    return ForNormalizedId(id);
  }

  public static string HeuristicBankId(string shortId, string? displayName = null)
  {
    string id = NormalizeId(shortId, displayName);
    return id switch
    {
      "65" or "67" => "Classic65",
      "55" => "Classic55",
      "19" or "19C" => "Classic19C",
      "21" => "WoodstockAngle",
      "22" or "37" or "37E" => "FinancialBeginEnd",
      "38" or "38E" or "38C" => "FinancialDateBeginEnd",
      "25" or "25C" or "29" or "29C" or "33" or "33C" or "33E"
        or "34" or "34C" => "WoodstockPrgm",
      "35" or "45" or "70" or "80" or "27"
        or "31" or "31E" or "32" or "32E" or "01" => "PowerOnly",
      _ => "Classic65",
    };
  }

  public static bool TryBank(string? bankId, out IReadOnlyList<CalcSwitchSpec> bank)
  {
    bank = [];
    if (string.IsNullOrWhiteSpace(bankId))
    {
      return false;
    }

    bank = bankId.Trim() switch
    {
      "Classic65" => Classic65,
      "WoodstockPrgm" => WoodstockPrgm,
      "WoodstockAngle" => WoodstockAngle,
      "FinancialBeginEnd" => FinancialBeginEnd,
      "FinancialDateBeginEnd" => FinancialDateBeginEnd,
      "Classic55" => Classic55,
      "Classic19C" => Classic19C,
      "PowerOnly" => PowerOnly,
      _ => [],
    };
    return bank.Count > 0 || string.Equals(bankId, "PowerOnly", StringComparison.OrdinalIgnoreCase);
  }

  private static IReadOnlyList<CalcSwitchSpec> ForNormalizedId(string id) =>
    id switch
    {
      "65" or "67" => Classic65,
      "55" => Classic55,
      "19" or "19C" => Classic19C,
      "21" => WoodstockAngle,
      "22" or "37" or "37E" => FinancialBeginEnd,
      "38" or "38E" or "38C" => FinancialDateBeginEnd,
      "25" or "25C" or "29" or "29C" or "33" or "33C" or "33E"
        or "34" or "34C" => WoodstockPrgm,
      "35" or "45" or "70" or "80" or "27"
        or "31" or "31E" or "32" or "32E" or "01" => PowerOnly,
      _ => Classic65,
    };

  private static string NormalizeId(string modelId, string? displayName)
  {
    string raw = string.IsNullOrWhiteSpace(modelId) && displayName is not null
      ? displayName
      : modelId;
    raw = raw.Trim();
    if (raw.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      raw = raw[3..];
    }

    return raw.ToUpperInvariant();
  }
}
