namespace TeoCalc.Core.Engine.Classic;

/// <summary>HP Classic firmware ROM (little-endian ushort image).</summary>
public sealed class ClassicMicrocodeRom
{
  private readonly ushort[] _words;

  public ClassicMicrocodeRom(ushort[] words)
  {
    _words = words;
  }

  public int WordCount => _words.Length;

  public ReadOnlySpan<ushort> Words => _words;

  public ushort ReadWord(int linearAddress)
  {
    return _words[linearAddress];
  }

  public static ClassicMicrocodeRom LoadBinary(string path)
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

    return new ClassicMicrocodeRom(words);
  }
}
