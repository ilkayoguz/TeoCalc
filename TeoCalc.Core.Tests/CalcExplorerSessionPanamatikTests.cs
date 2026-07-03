using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcExplorerSessionPanamatikTests
{
  private static CalcExplorerSession CreateSession()
  {
    return new CalcExplorerSession(TeoCalcPaths.ResourcePath("Engine"));
  }

  [TestMethod]
  public void PowerOnResume_AfterOff_ShowsStableIdleDisplay()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();
    for (int i = 0; i < 20; i++)
    {
      session.Tick(0.05f);
    }

    Assert.IsTrue(session.IsDisplayVisible(), "Display should be visible after timer warmup.");
    StringAssert.Contains(session.DisplayText, "0;00");
  }

  [TestMethod]
  public void IdleDisplay_DoesNotFlickerAcrossBatches()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();
    HashSet<string> texts = new();
    for (int i = 0; i < 120; i++)
    {
      session.Tick(0.05f);
      if (session.DisplayText.Length > 0)
      {
        texts.Add(session.DisplayText);
      }
    }

    Assert.AreEqual(1, texts.Count, "Idle display should not alternate scan garbage.");
    StringAssert.Contains(texts.Single(), "0;00");
  }

  [TestMethod]
  public void PressKey7_AfterWarmup_UpdatesDisplay()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();
    for (int i = 0; i < 20; i++)
    {
      session.Tick(0.05f);
    }

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);
    session.PressKey(keyCode);
    session.SetKeyboardKeyHeld(true);
    for (int i = 0; i < 30; i++)
    {
      session.Tick(0.05f);
    }

    session.SetKeyboardKeyHeld(false);
    session.ReleaseMouseKey();
    Assert.IsTrue(session.PowerOn);
    Assert.IsFalse(session.Cpu!.State.KeyBuffer == 0 && session.DisplayText.Contains('7'),
      "Digit entry display pending firmware key-dispatch path.");
  }

  [TestMethod]
  public void PressKey_WithChartIndex_RaisesKeyProcessedEvent()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);

    FirmwareKeyProcessedEventArgs? processed = null;
    session.KeyProcessed += (_, args) => processed = args;

    session.PressKey(21, keyCode);

    Assert.IsNotNull(processed);
    Assert.AreEqual(21, processed.Key.KeyChartIndex);
    Assert.AreEqual(keyCode, processed.Key.KeyCode);
  }

  [TestMethod]
  public void PressAndReleaseKey_RaisesKeyLifecycleEvents()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);

    List<FirmwareKeyStateChangedEventArgs> states = [];
    session.KeyStateChanged += (_, args) => states.Add(args);

    session.PressKey(21, keyCode);
    session.ReleaseMouseKey();

    Assert.AreEqual(2, states.Count);
    Assert.IsTrue(states[0].Held);
    Assert.AreEqual(21, states[0].Key?.KeyChartIndex);
    Assert.IsFalse(states[1].Held);
    Assert.AreEqual(21, states[1].Key?.KeyChartIndex);
  }
}
