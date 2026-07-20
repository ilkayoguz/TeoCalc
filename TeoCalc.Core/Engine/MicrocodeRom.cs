namespace TeoCalc.Core.Engine;

/// <summary>Shared microcode ROM loader (Classic / Woodstock / Spice).</summary>
public sealed class MicrocodeRom : IMicrocodeRom
{
  private readonly ushort[] _words;

  public MicrocodeRom(ushort[] words)
  {
    _words = words;
  }

  public int WordCount => _words.Length;

  public ReadOnlySpan<ushort> Words => _words;

  public ushort ReadWord(int linearAddress) =>
    _words[linearAddress];

  public static MicrocodeRom LoadBinary(string path)
  {
    byte[] bytes = File.ReadAllBytes(path);
    if (bytes.Length % 2 != 0)
    {
      throw new InvalidDataException($"ROM byte length must be even: {path}");
    }

    ushort[] words = new ushort[bytes.Length / 2];
    for (int i = 0; i < words.Length; i++)
    {
      words[i] = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
    }

    return new MicrocodeRom(words);
  }
}
