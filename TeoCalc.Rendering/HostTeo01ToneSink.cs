using TeoCalc.Core.Engine.Teo01;

namespace TeoCalc.Rendering;

/// <summary>
/// Desktop host tone sink for T-01 alarm / stopwatch events.
/// Uses <see cref="Console.Beep"/> on Windows; no-op elsewhere.
/// </summary>
public sealed class HostTeo01ToneSink : ITeo01ToneSink
{
  public static HostTeo01ToneSink Instance { get; } = new();

  public void Beep()
  {
    if (OperatingSystem.IsWindows())
    {
      TryConsoleBeep(880, 120);
    }
  }

  public void Alarm()
  {
    if (!OperatingSystem.IsWindows())
    {
      return;
    }

    TryConsoleBeep(660, 180);
    TryConsoleBeep(880, 180);
  }

  private static void TryConsoleBeep(int frequency, int durationMs)
  {
    if (!OperatingSystem.IsWindows())
    {
      return;
    }

    try
    {
      Console.Beep(frequency, durationMs);
    }
    catch
    {
      // Console.Beep can throw when no console / audio device is available.
    }
  }
}
