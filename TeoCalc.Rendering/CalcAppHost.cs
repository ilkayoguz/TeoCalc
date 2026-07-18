namespace TeoCalc.Rendering;

/// <summary>Silk / ImGui TeoCalc shell (launcher or standalone calculator window).</summary>
public static class CalcAppHost
{
  public static int Run(string[]? args = null) => CalcExplorerApp.Run(args);
}
