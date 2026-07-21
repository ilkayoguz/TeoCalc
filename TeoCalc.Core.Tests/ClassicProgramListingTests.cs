using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class ClassicProgramListingTests
{
  [TestMethod]
  public void Enumerate_FromBytes_StopsAtTrailingZero()
  {
    byte[] codes = [ClassicProgramCodes.Start, ClassicProgramCodes.Pointer, 43, 1, 0, 99];
    ClassicProgramLine[] lines = ClassicProgramListing.ToList(codes, c => $"#{c}").ToArray();

    Assert.AreEqual(4, lines.Length);
    Assert.AreEqual(0, lines[0].Index);
    Assert.AreEqual(ClassicProgramCodes.Start, lines[0].Code);
    Assert.AreEqual(3, lines[3].Index);
    Assert.AreEqual(1, lines[3].Code);
  }

  [TestMethod]
  public void Format_Machine_UsesDecimalBytes()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 63, "START"),
      new(1, 61, "PTR"),
      new(2, 43, "LBL"),
    ];

    string text = ClassicProgramListing.Format(lines, ClassicProgramListingStyle.Machine);
    Assert.Contains("  0  63", text, StringComparison.Ordinal);
    Assert.Contains("  1  61", text, StringComparison.Ordinal);
    Assert.Contains("  2  43", text, StringComparison.Ordinal);
  }

  [TestMethod]
  public void FindPointerIndex_FindsClassicPointer()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Pointer, "PTR"),
      new(2, 1, "1"),
    ];

    Assert.AreEqual(1, ClassicProgramListing.FindPointerIndex(lines));
  }

  [TestMethod]
  public void FindPointerIndex_Missing_ReturnsMinusOne()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 1, "1"),
      new(1, 2, "2"),
    ];

    Assert.AreEqual(-1, ClassicProgramListing.FindPointerIndex(lines));
  }
}

[TestClass]
public sealed class UserProgramClipboardTests
{
  [TestMethod]
  public void TryParse_Mnemonic_PrefixedAndBareLines()
  {
    const string text = """
        0  START
        PTR
        2  LBL
        """;

    Assert.IsTrue(
      UserProgramClipboard.TryParse(
        text,
        CardCodeEncoding.Mnemonic,
        mnemonic => mnemonic.ToUpperInvariant() switch
        {
          "START" => ClassicProgramCodes.Start,
          "PTR" => ClassicProgramCodes.Pointer,
          "LBL" => ClassicProgramCodes.Label,
          _ => null,
        },
        out List<byte> codes,
        out string? error),
      error);

    CollectionAssert.AreEqual(
      new byte[] { ClassicProgramCodes.Start, ClassicProgramCodes.Pointer, ClassicProgramCodes.Label },
      codes);
  }

  [TestMethod]
  public void TryParse_Machine_DecimalBytes()
  {
    const string text = """
        63
          1  61
        43
        """;

    Assert.IsTrue(
      UserProgramClipboard.TryParse(
        text,
        CardCodeEncoding.Machine,
        _ => null,
        out List<byte> codes,
        out string? error),
      error);

    CollectionAssert.AreEqual(
      new byte[] { 63, 61, 43 },
      codes);
  }

  [TestMethod]
  public void Format_RespectsCodeEncoding()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 43, "LBL"),
      new(1, 1, "1"),
    ];

    string mnemonic = UserProgramClipboard.Format(lines, CardCodeEncoding.Mnemonic);
    string machine = UserProgramClipboard.Format(lines, CardCodeEncoding.Machine);

    Assert.Contains("LBL", mnemonic, StringComparison.Ordinal);
    Assert.Contains("  0  43", machine, StringComparison.Ordinal);
    Assert.DoesNotContain("LBL", machine, StringComparison.Ordinal);
  }

  [TestMethod]
  public void FormatDual_WritesIndexMachineMnemonicTsv()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 43, "LBL"),
      new(1, 1, "1"),
    ];

    string text = UserProgramClipboard.FormatDual(lines);
    Assert.Contains("0\t43\tLBL", text, StringComparison.Ordinal);
    Assert.Contains("1\t1\t1", text, StringComparison.Ordinal);
  }

  [TestMethod]
  public void TryParseAuto_DualTsv_UsesMachineColumn()
  {
    const string text = """
        0	43	LBL
        1	1	1
        """;

    Assert.IsTrue(
      UserProgramClipboard.TryParseAuto(
        text,
        mnemonic => mnemonic.ToUpperInvariant() switch
        {
          "LBL" => ClassicProgramCodes.Label,
          "1" => 1,
          _ => null,
        },
        out List<byte> codes,
        out string? error),
      error);

    CollectionAssert.AreEqual(new byte[] { 43, 1 }, codes);
  }

  [TestMethod]
  public void TryParse_DualTsv_IgnoresMnemonicEncodingPreference()
  {
    const string text = "2\t43\tLBL\n";

    Assert.IsTrue(
      UserProgramClipboard.TryParse(
        text,
        CardCodeEncoding.Mnemonic,
        _ => null,
        out List<byte> codes,
        out string? error),
      error);

    CollectionAssert.AreEqual(new byte[] { 43 }, codes);
  }

  [TestMethod]
  public void ExtractBody_StripsStepIndex()
  {
    Assert.AreEqual("RCL 1", UserProgramClipboard.ExtractBody("  12  RCL 1"));
    Assert.AreEqual("43", UserProgramClipboard.ExtractBody("43"));
  }

  [TestMethod]
  public void ExtractMachineBody_FromDualTsv()
  {
    Assert.AreEqual("43", UserProgramClipboard.ExtractMachineBody("2\t43\tLBL"));
    Assert.AreEqual("61", UserProgramClipboard.ExtractMachineBody("61\tPTR"));
  }

  [TestMethod]
  public void TryParse_Empty_Fails()
  {
    Assert.IsFalse(
      UserProgramClipboard.TryParse(
        " \n ",
        CardCodeEncoding.Mnemonic,
        _ => 1,
        out _,
        out string? error));
    Assert.IsFalse(string.IsNullOrEmpty(error));
  }
}
