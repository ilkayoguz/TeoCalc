using TeoCalc.Core;

namespace TeoCalc.Rendering.Faceplate;

public static class CalcThemeCatalog
{
  public const string DefaultThemeId = "Modern";

  public static CalcThemePack LoadDefault() => Load(DefaultThemeId);

  public static CalcThemePack Load(string themeId)
  {
    string path = TeoCalcPaths.ResourcePath(Path.Combine("Config", "CalcTheme", $"{themeId}.json"));
    if (!File.Exists(path))
    {
      throw new FileNotFoundException($"Calc theme '{themeId}' was not found.", path);
    }

    return JsonCalcThemePack.LoadFile(path);
  }

  public static IReadOnlyList<CalcThemePack> LoadAll()
  {
    string directory = TeoCalcPaths.ResourcePath(Path.Combine("Config", "CalcTheme"));
    if (!Directory.Exists(directory))
    {
      return [];
    }

    List<CalcThemePack> packs = [];
    foreach (string path in Directory.EnumerateFiles(directory, "*.json"))
    {
      try
      {
        packs.Add(JsonCalcThemePack.LoadFile(path));
      }
      catch (InvalidOperationException)
      {
      }
    }

    return packs.OrderBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
  }
}
