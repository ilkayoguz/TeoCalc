using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcExplorerSessionPanamatikTests
{
  private static CalcExplorerSession CreateSession()
  {
    return new CalcExplorerSession(TeoCalcPaths.ResourcePath("Engine"));
  }

  private static ProgramVocabulary LoadHp65Vocabulary() =>
    ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));

  private static string NormalizeLedText(string text) =>
    string.Join(' ', text.Replace(';', '.').Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    PressCharacter(session, vocabulary, '7');

    Console.WriteLine("Display: [" + session.DisplayText.Replace(';', '.') + "]");
    Console.WriteLine("PC: " + session.LastBatch.ProgramCounter.ToString("X4"));
    Console.WriteLine("KeyBuf: " + session.LastBatch.KeyBuffer.ToString("X2"));
    Console.WriteLine("K2R: " + (session.LastBatch.Classic?.KeysToRomAddressCount ?? 0));
    Console.WriteLine("B2R: " + (session.LastBatch.Classic?.BufferToRomAddressCount ?? 0));
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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    Assert.IsTrue(ClassicProgramInput.TryResolveKeyCode(vocabulary, digit, out byte keyCode));
    session.PressKey(keyCode);
    int framesToVisible = 0;
    while (!session.IsDisplayVisible() && framesToVisible < 120)
    {
      session.EndDisplayFrame();
      session.Tick(0.05f);
      framesToVisible++;
    }

    Assert.IsTrue(session.IsDisplayVisible(), "Display should be visible after key-down batch.");
    int maxFrames = 5;
    Assert.IsTrue(framesToVisible <= maxFrames, "Key blank pulse should clear on the next timer frame.");
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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    foreach (char digit in digits)
    {
      PressCharacter(session, vocabulary, digit);
      Assert.IsTrue(session.IsDisplayVisible(), $"Display should be visible after digit {digit}.");
    }

    StringAssert.Contains(session.DisplayText, digits[^1].ToString());
  }

  [TestMethod]
  public void NumberEntryDisplay_SingleDigit1_MatchesPanamatik()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    PressCharacter(session, LoadHp65Vocabulary(), '1');

    Assert.AreEqual("1.", NormalizeLedText(session.DisplayText));
  }

  [TestMethod]
  public void NumberEntryDisplay_LongDigitSequence_MatchesPanamatik()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();

    foreach (char digit in "1234567890")
    {
      PressCharacter(session, vocabulary, digit);
    }

    Assert.AreEqual("1234567890.", NormalizeLedText(session.DisplayText));
  }

  [TestMethod]
  public void NumberEntryDisplay_LongDigitSequenceThenEnter_MatchesPanamatik()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();

    foreach (char digit in "1234567890")
    {
      PressCharacter(session, vocabulary, digit);
    }

    PressCharacter(session, vocabulary, Convert.ToChar(13));

    Assert.AreEqual("1.234567890 09", NormalizeLedText(session.DisplayText));
  }

  [TestMethod]
  public void DivideByZero_BlanksOnDisplayOffBatchesLikePanamatik()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();

    PressCharacter(session, vocabulary, '1');
    PressCharacter(session, vocabulary, Convert.ToChar(13));
    PressCharacter(session, vocabulary, '0');
    PressCharacter(session, vocabulary, '/');

    int visibleBatches = 0;
    int blankBatches = 0;
    for (int i = 0; i < 80; i++)
    {
      session.Tick(0.05f);
      if (session.DisplayText.Length == 0)
      {
        blankBatches++;
      }
      else if (NormalizeLedText(session.DisplayText) == "0.00")
      {
        visibleBatches++;
      }
    }

    Assert.IsTrue(visibleBatches > 0, "Panamatik keeps the 0.00 display registers during the error blink.");
    Assert.IsTrue(blankBatches > 0, "Panamatik blanks timer batches when DISPLAY_ON is clear during the error blink.");
  }

  [TestMethod]
  public void NormalMappedKeys_DispatchWithoutBlankingDisplayPipeline()
  {
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();

    foreach (FaceplateCell cell in CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-65"))
    {
      ProgramKeyEntry key = vocabulary.KeyChart[cell.KeyChartIndex];
      if (key.KeyCode == 0)
      {
        continue;
      }

      CalcExplorerSession session = CreateSession();
      Warmup(session);
      FirmwareKeyProcessedEventArgs? processed = null;
      session.KeyProcessed += (_, args) => processed = args;

      session.PressKey(cell.KeyChartIndex, (byte)key.KeyCode);
      session.SetKeyboardKeyHeld(true);
      for (int i = 0; i < 10; i++)
      {
        session.Tick(0.05f);
      }

      session.SetKeyboardKeyHeld(false);
      session.ReleaseMouseKey();
      for (int i = 0; i < 10; i++)
      {
        session.Tick(0.05f);
      }

      Assert.IsNotNull(processed, $"Key chart {key.Index} ({key.Char}) should dispatch.");
      Assert.AreEqual(key.Index, processed.Key.KeyChartIndex);
      Assert.AreEqual((byte)key.KeyCode, processed.Key.KeyCode);
      Assert.IsTrue(session.PowerOn, $"Key chart {key.Index} ({key.Char}) should not power off the app.");
      Assert.IsNotNull(session.LastBatch.Display, $"Key chart {key.Index} ({key.Char}) should refresh display state.");
      if (key.Char.Length == 1 && key.Char[0] is >= '0' and <= '9')
      {
        Assert.IsTrue(
          session.DisplayText.Length > 0,
          $"Key chart {cell.KeyChartIndex} ({key.Char}) should not leave the display pipeline blank after settling.");
      }
    }
  }

  [TestMethod]
  [DataRow(10, ShiftPreviewMode.Gold, 14)]
  [DataRow(11, ShiftPreviewMode.GoldInverse, 12)]
  [DataRow(14, ShiftPreviewMode.Blue, 8)]
  public void ShiftKeys_UpdatePreviewAndDispatchFirmwareKeyCode(
    int keyChartIndex,
    ShiftPreviewMode expectedPreview,
    int expectedKeyCode)
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    ProgramKeyEntry key = vocabulary.KeyChart[keyChartIndex];
    FirmwareKeyProcessedEventArgs? processed = null;
    session.KeyProcessed += (_, args) => processed = args;

    session.PressKey(keyChartIndex, (byte)key.KeyCode);

    Assert.AreEqual(expectedPreview, session.ShiftPreview.Mode);
    Assert.IsNotNull(processed);
    Assert.AreEqual(keyChartIndex, processed.Key.KeyChartIndex);
    Assert.AreEqual((byte)expectedKeyCode, processed.Key.KeyCode);
  }

  [TestMethod]
  public void ProgramMode_ToggleAndKeyPress_KeepsFirmwareInspectorStateValid()
  {
    CalcExplorerSession session = CreateSession();
    Warmup(session);

    session.ToggleProgramMode();
    for (int i = 0; i < 20; i++)
    {
      session.Tick(0.05f);
    }

    Assert.IsTrue(session.ProgramMode);
    Assert.IsTrue(session.PowerOn);
    Assert.IsNotNull(session.LastBatch.Display);
    Assert.IsTrue(session.DisplayText.Length > 0);

    PressCharacter(session, LoadHp65Vocabulary(), '7');

    Assert.IsTrue(session.LastBatch.ProgramCounter > 0, "Panamatik program key entry should advance firmware state.");
    Assert.IsTrue(session.DisplayText.Contains('7'), "Program-mode digit entry should update the Panamatik display.");

    Assert.IsTrue(session.PowerOn);
    Assert.IsTrue(session.ProgramMode);
    Assert.IsNotNull(session.LastBatch.Display);
  }

  [TestMethod]
  public void PressKey_WithChartIndex_RaisesKeyProcessedEvent()
  {
    CalcExplorerSession session = CreateSession();
    session.PowerOnResume();

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
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

    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
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
    Assert.IsFalse(
      string.Equals(batch.LastHandlerId, "Emulator.Engine", StringComparison.Ordinal),
      "HP-65 pilot must use native ClassicFirmwareGateway, not Panamatik emulator.");
    Assert.IsFalse(string.IsNullOrWhiteSpace(batch.LastHandlerId));
    Assert.IsTrue(batch.ProgramCounter > 0);

    Assert.IsNotNull(batch.Classic);
    Assert.IsTrue(batch.Classic.KeysToRomAddressCount >= 0);
    Assert.IsTrue(batch.Classic.BufferToRomAddressCount >= 0);
  }
}
