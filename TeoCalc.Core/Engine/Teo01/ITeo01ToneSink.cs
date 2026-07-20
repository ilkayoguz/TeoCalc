namespace TeoCalc.Core.Engine.Teo01;

/// <summary>
/// Optional tone hook for T-01 alarm / stopwatch events.
/// Default is <see cref="NullTeo01ToneSink"/> (no audio). Hosts may supply a real sink later.
/// </summary>
public interface ITeo01ToneSink
{
  /// <summary>Short beep (e.g. stopwatch underflow).</summary>
  void Beep();

  /// <summary>Alarm notification tone.</summary>
  void Alarm();
}

/// <summary>No-op tone sink — audio is intentionally stubbed for Core.</summary>
public sealed class NullTeo01ToneSink : ITeo01ToneSink
{
  public static NullTeo01ToneSink Instance { get; } = new();

  public void Beep()
  {
  }

  public void Alarm()
  {
  }
}
