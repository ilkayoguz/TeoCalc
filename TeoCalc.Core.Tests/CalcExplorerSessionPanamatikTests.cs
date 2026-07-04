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

  private static void Warmup(CalcExplorerSession session, int ticks = 20)
  {
    session.PowerOnResume();
    for (int i = 0; i < ticks; i++)
    {
      session.Tick(0.05f);
    }
  }

  private static void PressCharacter(CalcExplorerSession session, ProgramVocabulary vocabulary, char character)
  {
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, character, out byte keyCode));
    session.PressKey(keyCode);
    session.SetKeyboardKeyHeld(true);
    for (int i = 0; i < 30; i++)
    {
      session.Tick(0.05f);
    }

    session.SetKeyboardKeyHeld(false);
    session.ReleaseMouseKey();
    for (int i = 0; i < 30; i++)
    {
      session.Tick(0.05f);
    }
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
  public void DisplayChanged_UpdatesSessionSnapshot()
  {
    CalcExplorerSession session = CreateSession();
    List<FirmwareDisplaySnapshot> snapshots = [];
    session.DisplayChanged += (_, args) => snapshots.Add(args.Snapshot);

    session.PowerOnResume();
    for (int i = 0; i < 20; i++)
    {
      session.Tick(0.05f);
    }

    Assert.IsTrue(snapshots.Count > 0);
    Assert.AreEqual(snapshots[^1], session.DisplaySnapshot);
    Assert.AreEqual(session.DisplayText, session.DisplaySnapshot.Text);
    Assert.AreEqual(session.IsDisplayVisible(), session.DisplaySnapshot.Visible);
    Assert.IsTrue(session.DisplaySnapshot.Revision > 0);
  }

  [TestMethod]
  public void PressKey7_AfterWarmup_UpdatesDisplay()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    PressCharacter(session, vocabulary, '7');

    Console.WriteLine("Display: [" + session.DisplayText.Replace(';', '.') + "]");
    Console.WriteLine("PC: " + session.Cpu!.State.ProgramCounter.ToString("X4"));
    Console.WriteLine("KeyBuf: " + session.Cpu.State.KeyBuffer.ToString("X2"));
    Console.WriteLine("K2R: " + session.LastBatch.KeysToRomAddressCount);
    Console.WriteLine("B2R: " + session.LastBatch.BufferToRomAddressCount);
    Assert.IsTrue(session.PowerOn);
    Assert.IsTrue(session.DisplayText.Contains('7'),
      "Digit entry display should follow the firmware key-dispatch path.");
  }

  [TestMethod]
  [DataRow('0')]
  [DataRow('1')]
  [DataRow('2')]
  [DataRow('3')]
  [DataRow('4')]
  [DataRow('5')]
  [DataRow('6')]
  [DataRow('7')]
  [DataRow('8')]
  [DataRow('9')]
  public void PressDigit_AfterWarmup_UpdatesDisplay(char digit)
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    PressCharacter(session, vocabulary, digit);

    Assert.IsTrue(session.IsDisplayVisible(), "Display should be visible after digit entry.");
    StringAssert.Contains(session.DisplayText, digit.ToString());
  }

  [TestMethod]
  [DataRow('6')]
  [DataRow('7')]
  [DataRow('9')]
  public void PressDigit_KeyDownBlankPulse_RefreshesNextFrame(char digit)
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, digit, out byte keyCode));
    session.PressKey(keyCode);
    int framesToVisible = 0;
    while (!session.IsDisplayVisible() && framesToVisible < 120)
    {
      session.EndDisplayFrame();
      session.Tick(0.016f);
      framesToVisible++;
    }

    Assert.IsTrue(session.IsDisplayVisible(), "Display should be visible after key-down batch.");
    Assert.IsTrue(framesToVisible <= 1, "Key blank pulse should clear on the next normal frame.");
    StringAssert.Contains(session.DisplayText, digit.ToString());
  }

  [TestMethod]
  [DataRow("76")]
  [DataRow("96")]
  [DataRow("796")]
  [DataRow("0123456789")]
  public void PressDigitSequence_AfterWarmup_AccumulatesDisplay(string digits)
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    foreach (char digit in digits)
    {
      PressCharacter(session, vocabulary, digit);
      Assert.IsTrue(session.IsDisplayVisible(), $"Display should be visible after digit {digit}.");
    }

    StringAssert.Contains(session.DisplayText, digits[^1].ToString());
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

  [TestMethod]
  public void PressKey_RaisesBatchCompletedSnapshot()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode);

    FirmwareBatchSnapshot? batch = null;
    session.BatchCompleted += (_, args) => batch = args.Snapshot;

    session.PressKey(21, keyCode);

    Assert.IsNotNull(batch);
    Assert.AreEqual(batch, session.LastBatch);
    Assert.AreEqual(21, batch.ActiveKey?.KeyChartIndex);
    Assert.AreEqual(keyCode, batch.ActiveKey?.KeyCode);
    Assert.IsTrue(batch.StepCount > 0);
    Assert.IsNotNull(batch.Display);
    Assert.AreEqual(session.Cpu!.State.ProgramCounter, batch.ProgramCounter);
    Assert.AreEqual(session.Cpu.State.P, batch.P);
    Assert.AreEqual(session.Cpu.State.Flags, batch.Flags);
    Assert.IsTrue(batch.KeysToRomAddressCount >= 0);
    Assert.IsTrue(batch.BufferToRomAddressCount >= 0);
  }
}
