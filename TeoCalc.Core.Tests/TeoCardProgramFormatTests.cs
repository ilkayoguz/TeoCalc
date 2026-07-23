using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class TeoCardProgramFormatTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  private static TeoCardDocument LoadSampleAsTeoCard()
  {
    T6xDocument t65 = T6xCardFormat.ReadFile(
      Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName));
    return T6xCardFormat.ToTeoCardDocument(t65);
  }

  [TestMethod]
  public void FromT65_MatchesSampleProgramAndData()
  {
    TeoCardDocument document = LoadSampleAsTeoCard();
    ClassicCardSnapshot snapshot = TeoCardProgramFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    Assert.AreEqual("Add + GTO Demo", document.Title);
    Assert.AreEqual("+123", document.Labels[0]);
    Assert.AreEqual(string.Empty, document.Labels[1]);
    Assert.AreEqual(22, snapshot.ProgramCodes[6]);
    Assert.AreEqual(3.14, snapshot.Registers[1], 1e-6);
    Assert.AreEqual(42, snapshot.Registers[9], 1e-6);
  }

  [TestMethod]
  public void RoundTrip_T65_WriteThenRead_PreservesSteps()
  {
    TeoCardDocument original = LoadSampleAsTeoCard();
    string temp = Path.Combine(Path.GetTempPath(), $"t65-rt-{Guid.NewGuid():N}.t65");
    try
    {
      T6xDocument t65 = T6xCardFormat.FromTeoCardDocument(original);
      T6xCardFormat.WriteFile(temp, t65);
      TeoCardDocument roundTrip = T6xCardFormat.ToTeoCardDocument(T6xCardFormat.ReadFile(temp));
      CollectionAssert.AreEqual(original.Program.Steps, roundTrip.Program.Steps);
      Assert.AreEqual(original.Title, roundTrip.Title);
      CollectionAssert.AreEqual(original.Labels, roundTrip.Labels);
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
  public void RoundTrip_Json_WriteThenRead_PreservesSteps()
  {
    TeoCardDocument original = LoadSampleAsTeoCard();
    string temp = Path.Combine(Path.GetTempPath(), $"teo-json-rt-{Guid.NewGuid():N}.json");
    try
    {
      TeoCardProgramFormat.WriteFile(temp, original);
      TeoCardDocument roundTrip = TeoCardProgramFormat.ReadFile(temp);
      CollectionAssert.AreEqual(original.Program.Steps, roundTrip.Program.Steps);
      Assert.AreEqual(original.Title, roundTrip.Title);
      CollectionAssert.AreEqual(original.Labels, roundTrip.Labels);
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
  public void BundledT65_LoadsMetadataAndStripLabels()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);

    Assert.IsNotNull(session.LoadedTeoCard);
    Assert.AreEqual("Add + GTO Demo", session.CardTitle);
    Assert.AreEqual("RUN: A tuşu +123. Numeric label: GTO → 1 → R/S.", session.CardRunHint);
    Assert.AreEqual("+123", session.CardStripLabels![0]);
    Assert.AreEqual(string.Empty, session.CardStripLabels[1]);
    Assert.IsTrue(session.CardStripLabelsEnabled![0]);
  }
}
