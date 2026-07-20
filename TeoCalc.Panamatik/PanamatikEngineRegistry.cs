namespace TeoCalc.Panamatik;

/// <summary>Engine id → Panamatik source folder, WinForms type, and KML layout file.</summary>
internal readonly record struct PanamatikEngineBinding(
  string SourceFolderId,
  Type FormType,
  string KmlFileName);

/// <summary>Single table for Panamatik adapter bindings (replaces typeof/kml switches).</summary>
internal static class PanamatikEngineRegistry
{
  private static readonly Dictionary<string, PanamatikEngineBinding> ByEngineId =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["HP-01"] = new("HP01", typeof(global::Panamatik.Calc.HP01.HP01), "hp01.kml"),
      ["HP-19C"] = new("HP19", typeof(global::Panamatik.Calc.HP19.HP19C), "hp19C.kml"),
      ["HP-21"] = new("HP21", typeof(global::Panamatik.Calc.HP21.HP25), "hp21.kml"),
      ["HP-22"] = new("HP22", typeof(global::Panamatik.Calc.HP22.HP25), "hp22.kml"),
      ["HP-25"] = new("HP25", typeof(global::Panamatik.Calc.HP25.HP25), "hp25.kml"),
      ["HP-27"] = new("HP27", typeof(global::Panamatik.Calc.HP27.HP25), "hp27.kml"),
      ["HP-29"] = new("HP29", typeof(global::Panamatik.Calc.HP29.HP25), "hp29.kml"),
      ["HP-31"] = new("HP31", typeof(global::Panamatik.Calc.HP31.HPSpice), "hp31.kml"),
      ["HP-32"] = new("HP32", typeof(global::Panamatik.Calc.HP32.HPSpice), "hp32.kml"),
      ["HP-33"] = new("HP33", typeof(global::Panamatik.Calc.HP33.HPSpice), "hp33.kml"),
      ["HP-34"] = new("HP34", typeof(global::Panamatik.Calc.HP34.HPSpice), "hp34.kml"),
      ["HP-35"] = new("HP35", typeof(global::Panamatik.Calc.HP35.HPClassic), "hp35.kml"),
      ["HP-37"] = new("HP37", typeof(global::Panamatik.Calc.HP37.HPSpice), "hp37.kml"),
      ["HP-38"] = new("HP38", typeof(global::Panamatik.Calc.HP38.HPSpice), "hp38.kml"),
      ["HP-45"] = new("HP45", typeof(global::Panamatik.Calc.HP45.HPClassic), "hp45.kml"),
      ["HP-55"] = new("HP55", typeof(global::Panamatik.Calc.HP55.HPClassic), "hp55.kml"),
      ["HP-65"] = new("HP65", typeof(global::Panamatik.Calc.HP65.HPClassic), "hp65.kml"),
      ["HP-67"] = new("HP67", typeof(global::Panamatik.Calc.HP67.HP67), "hp67.kml"),
      ["HP-70"] = new("HP70", typeof(global::Panamatik.Calc.HP70.HPClassic), "hp70.kml"),
      ["HP-80"] = new("HP80", typeof(global::Panamatik.Calc.HP80.HPClassic), "hp80.kml"),
    };

  public static bool TryGet(string engineId, out PanamatikEngineBinding binding)
  {
    if (string.IsNullOrWhiteSpace(engineId))
    {
      binding = default;
      return false;
    }

    return ByEngineId.TryGetValue(engineId.Trim(), out binding);
  }
}
