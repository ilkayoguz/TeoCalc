using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CuveSoftCardPlistFormatTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  private static string SamplePlistPath =>
    Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), "cuveesoft-std-01b-day-of-week.plist");

  [TestMethod]
  public void ReadFile_ParsesMetadataAndLabels()
  {
    CuveSoftCardPlistSnapshot snapshot = CuveSoftCardPlistFormat.ReadFile(SamplePlistPath);

    Assert.AreEqual("STD-01B - Day of the Week", snapshot.Title);
    Assert.AreEqual("M", snapshot.Labels[0]);
    Assert.AreEqual("DAY", snapshot.Labels[3]);
    Assert.IsTrue(snapshot.ProgramCodes.Count > 10);
    Assert.AreEqual(63, snapshot.ProgramCodes[0]);
  }

  [TestMethod]
  public void DecodeCardData_StartsWithRcl4()
  {
    CuveSoftCardPlistSnapshot snapshot = CuveSoftCardPlistFormat.ReadFile(SamplePlistPath);
    Assert.AreEqual(23, snapshot.ProgramCodes[1]);
  }

  [TestMethod]
  public void ToTeoCardDocument_MapsStripLabels()
  {
    CuveSoftCardPlistSnapshot snapshot = CuveSoftCardPlistFormat.ReadFile(SamplePlistPath);
    TeoCardDocument document = CuveSoftCardPlistFormat.ToTeoCardDocument(
      snapshot,
      code => ClassicCardProgramIo.FormatMnemonic(Vocabulary, code));

    Assert.AreEqual("STD-01B - Day of the Week", document.Title);
    Assert.AreEqual("M", document.Labels[0]);
    Assert.AreEqual("DAY", document.Labels[3]);
    Assert.IsTrue(document.Program.Steps.Contains("RCL 4"));
  }

  [TestMethod]
  public void BundledPlist_LoadsIntoSession()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(SamplePlistPath, out string? error), error);
    Assert.AreEqual("STD-01B - Day of the Week", session.CardTitle);
    Assert.AreEqual("M", session.CardStripLabels![0]);
    Assert.IsNotNull(session.LoadedTeoCard);
  }

  [TestMethod]
  public void ExportFromT65_RoundTrip_PreservesProgramCodesAndLabels()
  {
    string sampleT65 = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    T6xDocument t65 = T6xCardFormat.ReadFile(sampleT65);
    CuveSoftCardPlistSnapshot exported = CuveSoftCardPlistFormat.FromT6xDocument(
      t65,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    string temp = Path.Combine(Path.GetTempPath(), $"cuvesoft-rt-{Guid.NewGuid():N}.xml");
    try
    {
      CuveSoftCardPlistFormat.WriteFile(temp, exported);
      CuveSoftCardPlistSnapshot roundTrip = CuveSoftCardPlistFormat.ReadFile(temp);

      Assert.AreEqual(exported.Title, roundTrip.Title);
      Assert.AreEqual(exported.Labels[0], roundTrip.Labels[0]);
      Assert.AreEqual(63, roundTrip.ProgramCodes[0]);
      CollectionAssert.AreEqual(
        exported.ProgramCodes.Where(c => c != 0).ToList(),
        roundTrip.ProgramCodes.Where(c => c != 0).ToList());
    }
    finally
    {
      if (File.Exists(temp))
      {
        File.Delete(temp);
      }
    }
  }
}
