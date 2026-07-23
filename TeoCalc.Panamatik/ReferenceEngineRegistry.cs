using TeoCalc.Core.Catalog;

namespace TeoCalc.ReferenceEmulator;

/// <summary>Engine id → reference source folder, WinForms type, and KML layout file.</summary>
internal readonly record struct ReferenceEngineBinding(
  string SourceFolderId,
  Type FormType,
  string KmlFileName);

/// <summary>Single table for reference-emulator adapter bindings (replaces typeof/kml switches).</summary>
internal static class ReferenceEngineRegistry
{
  private static readonly Dictionary<string, ReferenceEngineBinding> ByEngineId =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["T-01"] = new("HP01", typeof(global::Panamatik.Calc.HP01.HP01), "hp01.kml"),
      ["T-19C"] = new("HP19", typeof(global::Panamatik.Calc.HP19.HP19C), "hp19C.kml"),
      ["T-21"] = new("HP21", typeof(global::Panamatik.Calc.HP21.HP25), "hp21.kml"),
      ["T-22"] = new("HP22", typeof(global::Panamatik.Calc.HP22.HP25), "hp22.kml"),
      ["T-25"] = new("HP25", typeof(global::Panamatik.Calc.HP25.HP25), "hp25.kml"),
      ["T-27"] = new("HP27", typeof(global::Panamatik.Calc.HP27.HP25), "hp27.kml"),
      ["T-29"] = new("HP29", typeof(global::Panamatik.Calc.HP29.HP25), "hp29.kml"),
      ["T-31"] = new("HP31", typeof(global::Panamatik.Calc.HP31.HPSpice), "hp31.kml"),
      ["T-32"] = new("HP32", typeof(global::Panamatik.Calc.HP32.HPSpice), "hp32.kml"),
      ["T-33"] = new("HP33", typeof(global::Panamatik.Calc.HP33.HPSpice), "hp33.kml"),
      ["T-34"] = new("HP34", typeof(global::Panamatik.Calc.HP34.HPSpice), "hp34.kml"),
      ["T-35"] = new("HP35", typeof(global::Panamatik.Calc.HP35.HPClassic), "hp35.kml"),
      ["T-37"] = new("HP37", typeof(global::Panamatik.Calc.HP37.HPSpice), "hp37.kml"),
      ["T-38"] = new("HP38", typeof(global::Panamatik.Calc.HP38.HPSpice), "hp38.kml"),
      ["T-45"] = new("HP45", typeof(global::Panamatik.Calc.HP45.HPClassic), "hp45.kml"),
      ["T-55"] = new("HP55", typeof(global::Panamatik.Calc.HP55.HPClassic), "hp55.kml"),
      ["T-65"] = new("HP65", typeof(global::Panamatik.Calc.HP65.HPClassic), "hp65.kml"),
      ["T-67"] = new("HP67", typeof(global::Panamatik.Calc.HP67.HP67), "hp67.kml"),
      ["T-70"] = new("HP70", typeof(global::Panamatik.Calc.HP70.HPClassic), "hp70.kml"),
      ["T-80"] = new("HP80", typeof(global::Panamatik.Calc.HP80.HPClassic), "hp80.kml"),
    };

  public static bool TryGet(string engineId, out ReferenceEngineBinding binding)
  {
    if (string.IsNullOrWhiteSpace(engineId))
    {
      binding = default;
      return false;
    }

    string id = CalcModelIds.ToEngineId(engineId.Trim());
    return ByEngineId.TryGetValue(id, out binding);
  }
}
