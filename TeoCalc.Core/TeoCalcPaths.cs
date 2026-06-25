namespace TeoCalc.Core;

public static class TeoCalcPaths
{
  public static string FindRepositoryRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "TeoCalc.slnx")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new DirectoryNotFoundException("TeoCalc repository root not found.");
  }

  public static string ResourcePath(string relativePath)
  {
    return Path.Combine(FindRepositoryRoot(), "Resource", relativePath);
  }
}
