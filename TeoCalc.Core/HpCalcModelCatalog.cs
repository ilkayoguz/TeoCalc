namespace TeoCalc.Core;

/// <summary>Known HP vintage calculator models targeted by TeoCalc.</summary>
public static class HpCalcModelCatalog
{
  public static readonly IReadOnlyList<string> SupportedModels =
  [
    "HP-01",
    "HP-19C",
    "HP-21",
    "HP-22",
    "HP-25",
    "HP-27",
    "HP-29C",
    "HP-31E",
    "HP-32E",
    "HP-33",
    "HP-34C",
    "HP-35",
    "HP-37E",
    "HP-38E",
    "HP-45",
    "HP-55",
    "HP-65",
    "HP-67",
    "HP-70",
    "HP-80",
  ];

  public const string PriorityModel = "HP-65";
}
