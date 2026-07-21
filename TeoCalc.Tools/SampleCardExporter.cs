using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Formats;

namespace TeoCalc.Tools;

internal static class SampleCardExporter
{
  public const string T65FileName = "teo-65-add123.t65";
  public const string T67FileName = "teo-67-add01.t67";

  public static int ExportAll(string? outputDirectory = null)
  {
    string outDir = outputDirectory ?? TeoCalcPaths.ResourcePath("Samples/Cards");
    Directory.CreateDirectory(outDir);

    ExportT65(Path.Combine(outDir, T65FileName));
    ExportT67(Path.Combine(outDir, T67FileName));

    Console.WriteLine($"Wrote {Path.Combine(outDir, T65FileName)}");
    Console.WriteLine($"Wrote {Path.Combine(outDir, T67FileName)}");
    return 0;
  }

  private static void ExportT65(string path)
  {
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

    T6xDocument document = new()
    {
      Format = T6xDocument.FormatId,
      SchemaVersion = T6xDocument.CurrentSchemaVersion,
      TargetCpu = "T-65",
      Profile = "T-65",
      Category = "Demo",
      Title = "Add + GTO Demo",
      CodeEncoding = CardCodeEncoding.Mnemonic,
      Labels =
      [
        new T6xLabelEntry { Key = "A", Caption = "+123", Hint = "X := X + 123" },
      ],
      Code = ["LBL", "A", "1", "2", "3", "+", "RTN", "LBL", "1", "4", "5", "6", "+", "RTN"],
      Data = new Dictionary<int, double>
      {
        [1] = 3.14,
        [2] = 2.718281828,
        [9] = 42,
      },
    };

    // Validate mnemonics resolve before write.
    _ = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(vocabulary, mnemonic));
    T6xCardFormat.WriteFile(path, document);
  }

  private static void ExportT67(string path)
  {
    T6xDocument document = new()
    {
      Format = T6xDocument.FormatId,
      SchemaVersion = T6xDocument.CurrentSchemaVersion,
      TargetCpu = "T-67",
      Profile = "T-67",
      Category = "Demo",
      Title = "Add 0+1",
      CodeEncoding = CardCodeEncoding.Mnemonic,
      Code = ["0", "1", "+"],
      Data = new Dictionary<int, double>
      {
        [0] = 12.5,
        [1] = 3,
        [25] = -3,
      },
    };

    _ = T6xCardFormat.ToTeo67Snapshot(document, Teo67CardProgramIo.ResolveMnemonic);
    T6xCardFormat.WriteFile(path, document);
  }
}
