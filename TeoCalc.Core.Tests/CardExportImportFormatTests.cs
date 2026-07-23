using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CardExportImportFormatTests
{
  private static ProgramVocabulary Vocabulary =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  private static string SamplePath =>
    Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);

  [TestMethod]
  public void CuveSoftXml_RoundTrip_PreservesProgramCodesAndLabels()
  {
    T6xDocument original = T6xCardFormat.ReadFile(SamplePath);
    CuveSoftCardPlistSnapshot exported = CuveSoftCardPlistFormat.FromT6xDocument(
      original,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));

    string temp = Path.Combine(Path.GetTempPath(), $"cuvesoft-xml-{Guid.NewGuid():N}.xml");
    try
    {
      CuveSoftCardPlistFormat.WriteFile(temp, exported);
      Assert.IsTrue(CuveSoftCardPlistFormat.IsCuveSoftCardPath(temp));

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

  [TestMethod]
  public void TeoJson_RoundTrip_PreservesStepsAndMetadata()
  {
    TeoCardDocument original = T6xCardFormat.ToTeoCardDocument(T6xCardFormat.ReadFile(SamplePath));
    string temp = Path.Combine(Path.GetTempPath(), $"teo-json-{Guid.NewGuid():N}.json");
    try
    {
      TeoCardProgramFormat.WriteFile(temp, original);
      Assert.IsTrue(TeoCardProgramFormat.IsTeoCardPath(temp));

      TeoCardDocument roundTrip = TeoCardProgramFormat.ReadFile(temp);
      Assert.AreEqual(original.Title, roundTrip.Title);
      Assert.AreEqual(original.Model, roundTrip.Model);
      CollectionAssert.AreEqual(original.Program.Steps, roundTrip.Program.Steps);
      CollectionAssert.AreEqual(original.Labels, roundTrip.Labels);
      Assert.AreEqual(original.Data.Registers[9], roundTrip.Data.Registers[9], 1e-6);
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
  public void Session_LoadsCuveSoftXmlExport()
  {
    T6xDocument original = T6xCardFormat.ReadFile(SamplePath);
    CuveSoftCardPlistSnapshot exported = CuveSoftCardPlistFormat.FromT6xDocument(
      original,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(Vocabulary, mnemonic));
    string temp = Path.Combine(Path.GetTempPath(), $"cuvesoft-session-{Guid.NewGuid():N}.xml");
    try
    {
      CuveSoftCardPlistFormat.WriteFile(temp, exported);

      using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
      session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
      session.PowerOnResume();

      Assert.IsTrue(session.TryLoadCardProgram(temp, out string? error), error);
      Assert.AreEqual("Add + GTO Demo", session.CardTitle);
      Assert.IsTrue(session.CardInserted);
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
  public void Session_LoadsTeoJsonExport()
  {
    TeoCardDocument original = T6xCardFormat.ToTeoCardDocument(T6xCardFormat.ReadFile(SamplePath));
    string temp = Path.Combine(Path.GetTempPath(), $"teo-session-{Guid.NewGuid():N}.json");
    try
    {
      TeoCardProgramFormat.WriteFile(temp, original);

      using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
      session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
      session.PowerOnResume();

      Assert.IsTrue(session.TryLoadCardProgram(temp, out string? error), error);
      Assert.AreEqual("Add + GTO Demo", session.CardTitle);
      Assert.IsTrue(session.CardInserted);
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
  public void Session_SaveExport_RoundTripsXmlAndJson()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(SamplePath, out string? loadError), loadError);

    string xmlPath = Path.Combine(Path.GetTempPath(), $"export-xml-{Guid.NewGuid():N}.xml");
    string jsonPath = Path.Combine(Path.GetTempPath(), $"export-json-{Guid.NewGuid():N}.json");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(xmlPath, out string? xmlError), xmlError);
      Assert.IsTrue(session.TrySaveCardProgram(jsonPath, out string? jsonError), jsonError);

      session.EjectCard();
      Assert.IsTrue(session.TryLoadCardProgram(xmlPath, out string? reloadXml), reloadXml);
      Assert.AreEqual("Add + GTO Demo", session.CardTitle);

      session.EjectCard();
      Assert.IsTrue(session.TryLoadCardProgram(jsonPath, out string? reloadJson), reloadJson);
      Assert.AreEqual("Add + GTO Demo", session.CardTitle);
    }
    finally
    {
      if (File.Exists(xmlPath))
      {
        File.Delete(xmlPath);
      }

      if (File.Exists(jsonPath))
      {
        File.Delete(jsonPath);
      }
    }
  }

  [TestMethod]
  public void InvalidJson_SurfacesError()
  {
    string temp = Path.Combine(Path.GetTempPath(), $"bad-teo-{Guid.NewGuid():N}.json");
    try
    {
      File.WriteAllText(temp, "{ \"Format\": \"not-a-card\" }");
      using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
      session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
      session.PowerOnResume();

      Assert.IsFalse(session.TryLoadCardProgram(temp, out string? error));
      Assert.IsFalse(string.IsNullOrWhiteSpace(error));
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
  public void InvalidXml_SurfacesError()
  {
    string temp = Path.Combine(Path.GetTempPath(), $"bad-cuve-{Guid.NewGuid():N}.xml");
    try
    {
      File.WriteAllText(temp, "<root><not-a-plist/></root>");
      using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
      session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
      session.PowerOnResume();

      Assert.IsFalse(session.TryLoadCardProgram(temp, out string? error));
      Assert.IsFalse(string.IsNullOrWhiteSpace(error));
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
  public void Hp67_CuveSoftXmlExport_ReturnsClearError()
  {
    string sample67 = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp67T67FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-67"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(sample67, out string? loadError), loadError);

    string temp = Path.Combine(Path.GetTempPath(), $"hp67-cuve-{Guid.NewGuid():N}.xml");
    try
    {
      Assert.IsFalse(session.TrySaveCardProgram(temp, out string? error));
      StringAssert.Contains(error, "T-65");
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
  public void Hp67_TeoJson_RoundTripViaSession()
  {
    string sample67 = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp67T67FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-67"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(sample67, out string? loadError), loadError);

    string temp = Path.Combine(Path.GetTempPath(), $"hp67-teo-{Guid.NewGuid():N}.json");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(temp, out string? saveError), saveError);
      session.EjectCard();
      Assert.IsTrue(session.TryLoadCardProgram(temp, out string? reloadError), reloadError);
      Assert.IsTrue(session.CardInserted);
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
