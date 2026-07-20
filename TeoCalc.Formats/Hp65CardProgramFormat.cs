using System.Globalization;
using System.Text;

namespace TeoCalc.Formats;

/// <summary>
/// Panamatik HPClassic ASCII card format:
/// <c>HP65</c> header, optional <c>PROGRAM</c>…<c>END</c>, then <c>DATA</c>…<c>END</c>.
/// </summary>
public static class Hp65CardProgramFormat
{
  public const string Header = "HP65";

  public static string Format(
    Hp65CardSnapshot snapshot,
    Func<byte, string> mnemonicForCode)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    ArgumentNullException.ThrowIfNull(mnemonicForCode);

    StringBuilder sb = new();
    sb.Append(Header).Append("\r\n\r\n");

    int lastNonZero = 0;
    for (int i = 0; i < snapshot.ProgramCodes.Count; i++)
    {
      if (snapshot.ProgramCodes[i] != 0)
      {
        lastNonZero = i + 1;
      }
    }

    // Panamatik WriteProgram emits steps 1..lastNonZero-1 (skips START at 0).
    if (lastNonZero > 1)
    {
      sb.Append("PROGRAM\r\n");
      for (int i = 1; i < lastNonZero; i++)
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
    int registerCount = Math.Max(Hp65CardSnapshot.DefaultRegisterCount, snapshot.Registers.Count);
    for (int i = 0; i < registerCount; i++)
    {
      double value = i < snapshot.Registers.Count ? snapshot.Registers[i] : 0d;
      sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append("\r\n");
    }

    sb.Append("END\r\n");
    return sb.ToString();
  }

  public static Hp65CardSnapshot Parse(
    string text,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = Hp65CardSnapshot.DefaultProgramCapacity,
    int registerCount = Hp65CardSnapshot.DefaultRegisterCount)
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

        if (string.Equals(line, Header, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        if (string.Equals(line, "PROGRAM", StringComparison.OrdinalIgnoreCase))
        {
          programWrite = 0;
          program[programWrite++] = 63; // START
          dataWrite = -1;
          sawSection = true;
          continue;
        }

        if (string.Equals(line, "DATA", StringComparison.OrdinalIgnoreCase))
        {
          dataWrite = 0;
          programWrite = -1;
          sawSection = true;
          continue;
        }

        if (string.Equals(line, "END", StringComparison.OrdinalIgnoreCase))
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

          if (!double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
          {
            throw new FormatException($"Invalid register value: {line}");
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
      throw new FormatException("No PROGRAM or DATA section found");
    }

    return new Hp65CardSnapshot(program, registers);
  }

  public static void WriteFile(
    string path,
    Hp65CardSnapshot snapshot,
    Func<byte, string> mnemonicForCode)
  {
    string text = Format(snapshot, mnemonicForCode);
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    File.WriteAllText(path, text, Encoding.ASCII);
  }

  public static Hp65CardSnapshot ReadFile(
    string path,
    Func<string, byte?> codeForMnemonic,
    int programCapacity = Hp65CardSnapshot.DefaultProgramCapacity,
    int registerCount = Hp65CardSnapshot.DefaultRegisterCount)
  {
    string text = File.ReadAllText(path);
    return Parse(text, codeForMnemonic, programCapacity, registerCount);
  }
}
