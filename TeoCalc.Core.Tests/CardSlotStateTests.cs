using TeoCalc.Core;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CardSlotStateTests
{
  [TestMethod]
  public void Session_StartsWithNoCardInserted()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int index = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(index);

    Assert.IsFalse(session.CardInserted);
    Assert.IsNull(session.LoadedCardPath);
  }

  [TestMethod]
  public void SaveCardProgram_MarksInserted_AndEjectClears()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int index = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(index);
    session.PowerOnResume();

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card-state-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? error), error);
      Assert.IsTrue(session.CardInserted);
      Assert.AreEqual(path, session.LoadedCardPath);

      session.EjectCard();
      Assert.IsFalse(session.CardInserted);
      Assert.IsNull(session.LoadedCardPath);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void LoadModel_ResetsCardInserted()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int hp65 = Array.FindIndex(session.Models, id => id == "HP-65");
    session.LoadModel(hp65);
    session.PowerOnResume();

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card-reset-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out _));
      Assert.IsTrue(session.CardInserted);

      int hp67 = Array.FindIndex(session.Models, id => id == "HP-67");
      session.LoadModel(hp67);
      Assert.IsFalse(session.CardInserted);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void LoadSampleHp65_SetsStripLabels()
  {
    string path = Path.Combine(CalcCardPanelComponent.SampleCardsDirectory(), CalcCardPanelComponent.SampleHp65T65FileName);
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    session.LoadModel(Array.FindIndex(session.Models, id => id == "HP-65"));
    session.PowerOnResume();
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);

    Assert.IsNotNull(session.CardStripLabels);
    Assert.AreEqual("+123", session.CardStripLabels![0]);
    Assert.AreEqual(string.Empty, session.CardStripLabels[1]);
    Assert.IsTrue(session.CardStripLabelsEnabled![0]);
  }
}
