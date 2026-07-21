using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class SampleCardFilesTests
{
  [TestMethod]
  public void BundledT65_LoadsIntoSession()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(File.Exists(path), path);

    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.CardInserted);
  }

  [TestMethod]
  public void BundledT67_LoadsIntoSession()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp67T67FileName);
    Assert.IsTrue(File.Exists(path), path);

    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-67"));
    session.PowerOnResume();

    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    Assert.IsTrue(session.CardInserted);
  }

  [TestMethod]
  public void SampleT65_ParseToClassicSnapshot()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    T6xDocument document = T6xCardFormat.ReadFile(path);
    ClassicCardSnapshot snapshot = T6xCardFormat.ToClassicSnapshot(
      document,
      mnemonic => ClassicCardProgramIo.ResolveMnemonic(
        ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json")),
        mnemonic));

    Assert.AreEqual(22, snapshot.ProgramCodes[6]);
    Assert.AreEqual(0, snapshot.Registers[0], 1e-6);
    Assert.AreEqual(3.14, snapshot.Registers[1], 1e-6);
    Assert.AreEqual(42, snapshot.Registers[9], 1e-6);
  }

  [TestMethod]
  public void SampleT67_ParseToActSnapshot()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp67T67FileName);
    T6xDocument document = T6xCardFormat.ReadFile(path);
    Teo67CardSnapshot snapshot = T6xCardFormat.ToTeo67Snapshot(document, Teo67CardProgramIo.ResolveMnemonic);

    Assert.AreEqual(55, snapshot.ProgramCodes[2]);
    Assert.AreEqual(12.5, snapshot.Registers[0], 1e-6);
    Assert.AreEqual(-3, snapshot.Registers[25], 1e-6);
  }
}
