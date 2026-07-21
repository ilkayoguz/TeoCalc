using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class T6xCardFormatTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  private static string SamplePath =>
    Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);

  [TestMethod]
  public void ReadFile_ParsesSampleMetadataCodeAndSparseData()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SamplePath);

    Assert.AreEqual(T6xDocument.FormatId, document.Format);
    Assert.AreEqual(1, document.SchemaVersion);
    Assert.AreEqual("T-65", document.TargetCpu);
    Assert.AreEqual("T-65", document.Profile);
    Assert.AreEqual("Add + GTO Demo", document.Title);
    Assert.AreEqual("+123", document.Labels[0].Caption);
    Assert.IsFalse(string.IsNullOrWhiteSpace(document.Labels[0].Hint));
    Assert.IsTrue(document.Code.Contains("RTN"));
    Assert.IsFalse(document.Code.Contains("PTR"));
    Assert.AreEqual(3.14, document.Data[1], 1e-6);
    Assert.AreEqual(42, document.Data[9], 1e-6);
  }

  [TestMethod]
  public void RoundTrip_WriteThenRead_PreservesCodeAndLabels()
  {
    T6xDocument original = T6xCardFormat.ReadFile(SamplePath);
    string temp = Path.Combine(Path.GetTempPath(), $"t65-rt-{Guid.NewGuid():N}.t65");
    try
    {
      T6xCardFormat.WriteFile(temp, original);
      string written = File.ReadAllText(temp);
      Assert.Contains("CodeEncoding = \"mnemonic\"", written, StringComparison.Ordinal);
      Assert.IsFalse(
        written.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
          .Any(static line => line.StartsWith("Encoding =", StringComparison.Ordinal)));
      T6xDocument roundTrip = T6xCardFormat.ReadFile(temp);
      CollectionAssert.AreEqual(original.Code, roundTrip.Code);
      Assert.AreEqual(original.Title, roundTrip.Title);
      Assert.AreEqual(CardCodeEncoding.Mnemonic, roundTrip.CodeEncoding);
      Assert.AreEqual(original.Labels[0].Caption, roundTrip.Labels[0].Caption);
      Assert.AreEqual(original.Labels[0].Hint, roundTrip.Labels[0].Hint);
      Assert.AreEqual(original.Data[9], roundTrip.Data[9], 1e-6);
    }
    finally
    {
      if (File.Exists(temp))
      {
        File.Delete(temp);
      }
    }
  }

  [TestMethod]
  public void ToClassicSnapshot_InjectsPtrAndLoadsRegisters()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SamplePath);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    Assert.AreEqual(ClassicProgramCodes.Start, snapshot.ProgramCodes[0]);
    Assert.IsTrue(snapshot.ProgramCodes.Contains(ClassicProgramCodes.Pointer));
    Assert.AreEqual(3.14, snapshot.Registers[1], 1e-6);
    Assert.AreEqual(42, snapshot.Registers[9], 1e-6);
  }

  [TestMethod]
  public void TargetCpuMatches_T65_AndHp65()
  {
    Assert.IsTrue(T6xCardFormat.TargetCpuMatches("T-65", "HP-65"));
    Assert.IsTrue(T6xCardFormat.TargetCpuMatches("T65", "65"));
    Assert.IsFalse(T6xCardFormat.TargetCpuMatches("T-65", "HP-67"));
  }

  [TestMethod]
  public void Session_LoadsSampleT6x_StripAndRunHint()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(SamplePath, out string? error), error);
    Assert.AreEqual("Add + GTO Demo", session.CardTitle);
    Assert.AreEqual("+123", session.CardStripLabels![0]);
    Assert.IsTrue(session.CardStripLabelsEnabled![0]);
    Assert.IsTrue(session.CardRunHint!.Contains("GTO", StringComparison.Ordinal));
  }

  [TestMethod]
  public void ExtensionForTargetCpu_Separates65And67()
  {
    Assert.AreEqual(".t65", T6xCardFormat.ExtensionForTargetCpu("T-65"));
    Assert.AreEqual(".t67", T6xCardFormat.ExtensionForTargetCpu("T-67"));
    Assert.AreEqual(".t65", T6xCardFormat.ExtensionForTargetCpu("HP-65"));
  }

  [TestMethod]
  public void WriteFile_RejectsMismatchedExtension()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SamplePath);
    string temp = Path.Combine(Path.GetTempPath(), $"t67-mismatch-{Guid.NewGuid():N}.t67");
    try
    {
      Assert.ThrowsExactly<FormatException>(() => T6xCardFormat.WriteFile(temp, document));
    }
    finally
    {
      if (File.Exists(temp))
      {
        File.Delete(temp);
      }
    }
  }

  [TestMethod]
  public void Parse_SupportsLineAndBlockComments()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      CodeEncoding = "mnemonic"
      // Title = "ignored"
      Title = "Commented"

      [Code]
      LBL
      A
      /* skip
      this */
      RTN
      """;

    T6xDocument document = T6xCardFormat.Parse(text);
    Assert.AreEqual("Commented", document.Title);
    Assert.AreEqual(CardCodeEncoding.Mnemonic, document.CodeEncoding);
    CollectionAssert.AreEqual(new[] { "LBL", "A", "RTN" }, document.Code);
  }

  [TestMethod]
  public void Parse_AcceptsLegacyEncodingKey()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      Encoding = "mnemonic"
      Title = "Legacy"

      [Code]
      RTN
      """;

    T6xDocument document = T6xCardFormat.Parse(text);
    Assert.AreEqual(CardCodeEncoding.Mnemonic, document.CodeEncoding);
  }

  [TestMethod]
  public void Parse_MachineMode_OneInternalBytePerLine()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      CodeEncoding = "machine"
      Title = "Machine bytes"

      [Code]
      43
      10
      46
      """;

    T6xDocument document = T6xCardFormat.Parse(text);
    Assert.AreEqual(CardCodeEncoding.Machine, document.CodeEncoding);
    CollectionAssert.AreEqual(new[] { "43", "10", "46" }, document.Code);

    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    Assert.AreEqual(ClassicProgramCodes.Start, snapshot.ProgramCodes[0]);
    Assert.AreEqual(43, snapshot.ProgramCodes[1]);
    Assert.AreEqual(10, snapshot.ProgramCodes[2]);
    Assert.AreEqual(46, snapshot.ProgramCodes[3]);
    Assert.AreEqual(ClassicProgramCodes.Pointer, snapshot.ProgramCodes[4]);
  }

  [TestMethod]
  public void Parse_MachineMode_RejectsMnemonicToken()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      CodeEncoding = "machine"

      [Code]
      LBL
      """;

    Assert.ThrowsExactly<FormatException>(() => T6xCardFormat.Parse(text));
  }

  [TestMethod]
  public void Parse_MachineMode_RejectsMultiTokenLine()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      CodeEncoding = "machine"

      [Code]
      34 01
      """;

    Assert.ThrowsExactly<FormatException>(() => T6xCardFormat.Parse(text));
  }

  [TestMethod]
  public void Parse_RejectsSeparateMachineSection()
  {
    const string text = """
      [General]
      Format = "TeoCalc.CardText"
      SchemaVersion = "1"
      TargetCpu = "T-65"
      CodeEncoding = "mnemonic"

      [Code]
      RTN

      [Machine]
      43
      """;

    Assert.ThrowsExactly<FormatException>(() => T6xCardFormat.Parse(text));
  }

  [TestMethod]
  public void ToClassicSnapshot_FillsMissingStripLabelsWithRtnStubs()
  {
    T6xDocument document = T6xCardFormat.ReadFile(SamplePath);
    Assert.IsFalse(document.Code.Contains("B"));

    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    List<string> mnemonics = [];
    for (int i = 1; i < snapshot.ProgramCodes.Count; i++)
    {
      byte code = snapshot.ProgramCodes[i];
      if (code == 0)
      {
        break;
      }

      mnemonics.Add(ClassicCardProgramIo.FormatMnemonic(Vocabulary, code));
    }

    CollectionAssert.Contains(mnemonics, "B");
    CollectionAssert.Contains(mnemonics, "RTN");
  }
}