using System.Diagnostics;

namespace TeoCalc.Rendering;

public static class ReferenceCalculatorLauncher
{
  public static bool TryLaunch(ReferenceCalculatorEntry reference, out string status)
  {
    try
    {
      if (reference.ExecutablePath is not null && File.Exists(reference.ExecutablePath))
      {
        Start(reference.ExecutablePath, Path.GetDirectoryName(reference.ExecutablePath)!);
        status = $"Opening {reference.ModelId} Panamatik reference.";
        return true;
      }

      if (File.Exists(reference.ProjectPath))
      {
        Start(
          "dotnet",
          Path.GetDirectoryName(reference.ProjectPath)!,
          $"run --project \"{reference.ProjectPath}\" -c Debug");
        status = $"Building and opening {reference.ModelId} Panamatik reference.";
        return true;
      }

      status = $"{reference.ModelId}: reference project not found.";
      return false;
    }
    catch (Exception exception)
    {
      status = $"{reference.ModelId}: failed to open reference ({exception.Message}).";
      return false;
    }
  }

  private static void Start(string fileName, string workingDirectory, string? arguments = null)
  {
    Process.Start(new ProcessStartInfo
    {
      FileName = fileName,
      Arguments = arguments ?? string.Empty,
      WorkingDirectory = workingDirectory,
      UseShellExecute = true,
    });
  }
}
