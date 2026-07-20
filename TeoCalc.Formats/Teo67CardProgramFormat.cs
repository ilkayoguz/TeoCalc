using System.Globalization;
using System.Text;

namespace TeoCalc.Formats;

/// <summary>
/// ASCII card format for ACT HasCardSlot models:
/// <c>HP67</c> header, optional <c>PROGRAM</c>…<c>END</c>, <c>DATA</c>…<c>END</c>, optional <c>MODE</c>.
/// Header magic kept for interop with existing .hp67 files.
/// </summary>
public static class Teo67CardProgramFormat
{
  public const string Header = "HP67";

  public static string Format(
    Teo67CardSnapshot snapshot,
    Func<byte, string> mnemonicForCode)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    StringBuilder sb = new();
    sb.Append(Header).Append("\r\n");

    int lastNonZero = 0;
    for (int i = 0; i < snapshot.ProgramCodes.Count; i++)
    {
      if (snapshot.ProgramCodes[i] != 0)
      {
        lastNonZero = i + 1;
      }
    }

    if (lastNonZero > 0)
    {
      sb.Append("PROGRAM\r\n");
      for (int i = 0; i < lastNonZero; i++)
      {
        byte code = snapshot.ProgramCodes[i];
        string mnemonic = mnemonicForCode(code);
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
          mnemonic = $"#{code}";
        }

        sb.Append(mnemonic).Append("\r\n");
      }

      sb.Append("END\r\n\r\n");
    }

    sb.Append("DATA\r\n");
    int registerCount = Math.Max(Teo67CardSnapshot.DefaultRegisterCount, snapshot.Registers.Count);
    for (int i = 0; i < registerCount; i++)
    {
      double value = i < snapshot.Registers.Count ? snapshot.Registers[i] : 0d;
      sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append("\r\n");
    }

    sb.Append("END\r\n\r\n");

    if (snapshot.Mode is not null)
    {
      Teo67CardModeSnapshot mode = snapshot.Mode;
      sb.Append("MODE ")
        .Append(mode.Angle).Append(' ')
        .Append(mode.Display).Append(' ')
        .Append(mode.Digits & 0xF).Append(' ');
      AppendFlagNibbles(sb, mode.FlagsHi);
      AppendFlagNibbles(sb, mode.FlagsLo);
      sb.Append("\r\n");
    }

    return sb.ToString();
  }

  public static Teo67CardSnapshot Parse(
    string text,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = Teo67CardSnapshot.DefaultProgramCapacity,
    int registerCount = Teo67CardSnapshot.DefaultRegisterCount)
  {
    ArgumentNullException.ThrowIfNull(text);
    ArgumentNullException.ThrowIfNull(codeForMnemonic);

    string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Split('\n');

    byte[] program = new byte[programCapacity];
    double[] registers = new double[registerCount];
    int programWrite = -1;
    int dataWrite = -1;
    bool sawSection = false;
    Teo67CardModeSnapshot? mode = null;
    int lineNumber = 0;

    try
    {
      foreach (string raw in lines)
      {
        lineNumber++;
        string line = raw.Trim();
        if (line.Length == 0 || line[0] == ';')
        {
          continue;
        }

        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
          continue;
        }

        if (string.Equals(tokens[0], Header, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        if (string.Equals(tokens[0], "PROGRAM", StringComparison.OrdinalIgnoreCase))
        {
          programWrite = 0;
          dataWrite = -1;
          sawSection = true;
          continue;
        }

        if (string.Equals(tokens[0], "DATA", StringComparison.OrdinalIgnoreCase))
        {
          dataWrite = 0;
          programWrite = -1;
          sawSection = true;
          continue;
        }

        if (string.Equals(tokens[0], "MODE", StringComparison.OrdinalIgnoreCase))
        {
          mode = ParseMode(tokens);
          sawSection = true;
          programWrite = -1;
          dataWrite = -1;
          continue;
        }

        if (string.Equals(tokens[0], "END", StringComparison.OrdinalIgnoreCase))
        {
          programWrite = -1;
          dataWrite = -1;
          continue;
        }

        if (programWrite >= 0)
        {
          byte? code = codeForMnemonic(line);
          if (code is null)
          {
            throw new FormatException($"Mnemonic not found: {line}");
          }

          if (programWrite >= programCapacity)
          {
            throw new FormatException("Program too large");
          }

          program[programWrite++] = code.Value;
          continue;
        }

        if (dataWrite >= 0)
        {
          if (dataWrite >= registerCount)
          {
            throw new FormatException("Data too large");
          }

          if (!double.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
          {
            throw new FormatException($"Invalid register value: {tokens[0]}");
          }

          registers[dataWrite++] = value;
        }
      }
    }
    catch (FormatException ex)
    {
      throw new FormatException($"Syntax error at line {lineNumber}: {ex.Message}", ex);
    }

    if (!sawSection)
    {
      throw new FormatException("No PROGRAM, DATA, or MODE section found");
    }

    return new Teo67CardSnapshot(program, registers, mode);
  }

  public static void WriteFile(
    string path,
    Teo67CardSnapshot snapshot,
    Func<byte, string> mnemonicForCode)
  {
    string text = Format(snapshot, mnemonicForCode);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path, text, Encoding.ASCII);
  }

  public static Teo67CardSnapshot ReadFile(
    string path,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = Teo67CardSnapshot.DefaultProgramCapacity,
    int registerCount = Teo67CardSnapshot.DefaultRegisterCount)
  {
    string text = File.ReadAllText(path);
    return Parse(text, codeForMnemonic, programCapacity, registerCount);
  }

  private static Teo67CardModeSnapshot ParseMode(string[] tokens)
  {
    if (tokens.Length < 8)
    {
      throw new FormatException("MODE line incomplete");
    }

    if (!int.TryParse(tokens[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int digits))
    {
      throw new FormatException("MODE digits invalid");
    }

    byte flagsHi = PackNibbles(tokens[4], tokens[5]);
    byte flagsLo = PackNibbles(tokens[6], tokens[7]);
    return new Teo67CardModeSnapshot(tokens[1], tokens[2], digits & 0xF, flagsHi, flagsLo);
  }

  private static byte PackNibbles(string highToken, string lowToken)
  {
    byte high = Convert.ToByte(highToken, CultureInfo.InvariantCulture);
    byte low = Convert.ToByte(lowToken, CultureInfo.InvariantCulture);
    return (byte)(((high & 0xF) << 4) | (low & 0xF));
  }

  private static void AppendFlagNibbles(StringBuilder sb, byte packed)
  {
    sb.Append((packed >> 4) & 0xF).Append(' ');
    sb.Append(packed & 0xF).Append(' ');
  }
}
