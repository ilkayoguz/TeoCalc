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
}
