namespace System.Media;

public sealed class SoundPlayer
{
  public SoundPlayer(string soundLocation)
  {
  }

  public void Play()
  {
  }
}

public static class SystemSounds
{
  public static SystemSound Asterisk { get; } = new();

  public static SystemSound Beep { get; } = new();

  public static SystemSound Exclamation { get; } = new();

  public static SystemSound Hand { get; } = new();

  public static SystemSound Question { get; } = new();
}

public sealed class SystemSound
{
  public void Play()
  {
  }
}
