using System.Reflection;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioProgramEditorTests
{
  private static CalcExplorerSession CreateHp65Session()
  {
    CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int idx = Array.FindIndex(session.Models, id => id.Contains("65", StringComparison.Ordinal));
    Assert.IsTrue(idx >= 0, "HP-65 / T-65 model missing");
    session.LoadModel(idx);
    session.PowerOnResume();
    return session;
  }

  private static ProgramVocabulary LoadHp65Vocabulary() =>
    ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/T-65/Program/program.vocabulary.json"));

  [TestMethod]
  public void Hydrate_Apply_Keys_RoundTrips()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([1, 2, 3], out string? applyError), applyError);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));

    StudioProgramEditorText.Hydrate(
      lines,
      session.EngineModelId,
      out string machine,
      out string keys);

    Assert.IsFalse(string.IsNullOrWhiteSpace(keys));
    Assert.IsTrue(
      StudioProgramEditorText.TryParseDual(
        machine,
        keys,
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor,
        out List<byte> parsed,
        out string? parseError),
      parseError);
    CollectionAssert.AreEqual(
      StudioProgramEditorText.ContentLines(lines).Select(l => l.Code).ToList(),
      parsed);
  }

  [TestMethod]
  public void TryParseDual_InvalidMnemonic_IsBlocked()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(
      StudioProgramEditorText.TryParseDual(
        string.Empty,
        "NOT_A_REAL_MNEMONIC_XYZ",
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor,
        out _,
        out string? error));
    Assert.IsFalse(string.IsNullOrWhiteSpace(error));
  }

  [TestMethod]
  public void TryParseDual_MuseumMachine_Rcl1_Resolves()
  {
    using CalcExplorerSession session = CreateHp65Session();
    ProgramVocabulary vocabulary = LoadHp65Vocabulary();
    byte? rcl1 = ClassicCardProgramIo.ResolveMnemonic(vocabulary, "RCL 1");
    Assert.IsTrue(rcl1.HasValue);

    Assert.IsTrue(
      StudioMuseumKeycodes.TryFormatMuseum("RCL 1", session.EngineModelId, out string museum));
    Assert.IsTrue(
      StudioMuseumKeycodes.TryResolveMachineLine(
        museum,
        session.EngineModelId,
        session.FormatProgramCodeForEditor,
        out byte code,
        out string? error),
      error);
    Assert.AreEqual(rcl1.Value, code);

    Assert.IsTrue(
      StudioProgramEditorText.TryParseDual(
        museum,
        string.Empty,
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor,
        out List<byte> codes,
        out string? parseError),
      parseError);
    Assert.AreEqual(1, codes.Count);
    Assert.AreEqual(rcl1.Value, codes[0]);
  }

  [TestMethod]
  public void TryParseDual_DisagreeingPanes_IsBlocked()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(
      StudioProgramEditorText.TryParseDual(
        "1",
        "2",
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor,
        out _,
        out string? error));
    StringAssert.Contains(error, "disagree", StringComparison.OrdinalIgnoreCase);
  }

  [TestMethod]
  public void TryApplyProgramCodes_PreservesRegisters()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryGetLiveRegisters(out IReadOnlyList<double> before));
    double[] regs = before.ToArray();
    regs[1] = 42d;
    Assert.IsTrue(session.TrySetLiveRegisters(regs));

    Assert.IsTrue(session.TryApplyProgramCodes([7, 8, 9], out string? error), error);
    Assert.IsTrue(session.TryGetLiveRegisters(out IReadOnlyList<double> after));
    Assert.AreEqual(42d, after[1]);
  }

  [TestMethod]
  public void NeedsSecondToken_G_And_BareSto_True_DigitFalse()
  {
    Assert.IsTrue(StudioMuseumPrefix.NeedsSecondToken("g"));
    Assert.IsTrue(StudioMuseumPrefix.NeedsSecondToken("STO"));
    Assert.IsTrue(StudioMuseumPrefix.NeedsSecondToken("LBL"));
    Assert.IsFalse(StudioMuseumPrefix.NeedsSecondToken("RCL 1"));
    Assert.IsFalse(StudioMuseumPrefix.NeedsSecondToken("4"));
    Assert.IsFalse(StudioMuseumPrefix.NeedsSecondToken("RTN"));
  }

  [TestMethod]
  public void IncompletePrefix_G_Or_35_Alone_Blocked()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Func<byte, string> format = session.FormatProgramCodeForEditor;

    Assert.IsTrue(
      StudioMuseumPrefix.IsIncompletePrefixStep(
        "35",
        string.Empty,
        "g",
        session.EngineModelId,
        format));

    Assert.IsFalse(
      StudioProgramEditorText.TryParseDual(
        "35",
        "g",
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        format,
        out _,
        out string? error));
    StringAssert.Contains(error, "incomplete", StringComparison.OrdinalIgnoreCase);

    Assert.IsFalse(
      StudioProgramEditorText.TryApplySteps(
        [
          new StudioProgramEditorText.EditorStep
          {
            MachineA = "35",
            Keys = "g",
          },
        ],
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        format,
        out _,
        out string? stepError));
    StringAssert.Contains(stepError, "incomplete", StringComparison.OrdinalIgnoreCase);
  }

  [TestMethod]
  public void Sync_Machine35_04_To_Keys_G4()
  {
    using CalcExplorerSession session = CreateHp65Session();
    var step = new StudioProgramEditorText.EditorStep
    {
      MachineA = "35",
      MachineB = "04",
    };
    StudioProgramEditorText.SyncFromMachine(
      step,
      session.EngineModelId,
      session.FormatProgramCodeForEditor);
    Assert.AreEqual("g 4", step.Keys, ignoreCase: true);
  }

  [TestMethod]
  public void Sync_Keys_G4_To_Machine35_04()
  {
    using CalcExplorerSession session = CreateHp65Session();
    var step = new StudioProgramEditorText.EditorStep { Keys = "g 4" };
    StudioProgramEditorText.SyncFromKeys(
      step,
      session.EngineModelId,
      session.ResolveProgramMnemonicForEditor,
      session.FormatProgramCodeForEditor);
    Assert.AreEqual("35", step.MachineA);
    Assert.AreEqual("04", step.MachineB);
  }

  [TestMethod]
  public void Sync_Rcl1_MuseumPair()
  {
    using CalcExplorerSession session = CreateHp65Session();
    var step = new StudioProgramEditorText.EditorStep { Keys = "RCL 1" };
    StudioProgramEditorText.SyncFromKeys(
      step,
      session.EngineModelId,
      session.ResolveProgramMnemonicForEditor,
      session.FormatProgramCodeForEditor);
    Assert.AreEqual("34", step.MachineA);
    Assert.AreEqual("01", step.MachineB);

    step.Keys = string.Empty;
    StudioProgramEditorText.SyncFromMachine(
      step,
      session.EngineModelId,
      session.FormatProgramCodeForEditor);
    Assert.AreEqual("RCL 1", step.Keys, ignoreCase: true);
  }

  [TestMethod]
  public void SingleBox_DigitOrRtn_NoSecondBox()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Func<byte, string> format = session.FormatProgramCodeForEditor;

    var digit = new StudioProgramEditorText.EditorStep { Keys = "4" };
    StudioProgramEditorText.SyncFromKeys(
      digit,
      session.EngineModelId,
      session.ResolveProgramMnemonicForEditor,
      format);
    Assert.AreEqual("04", digit.MachineA);
    Assert.AreEqual(string.Empty, digit.MachineB);
    Assert.IsFalse(
      StudioProgramEditorText.ShowSecondMachineBox(digit, session.EngineModelId, format));

    // RTN museum code on T-65 faceplate (row/col) — single complete key.
    Assert.IsTrue(StudioMuseumKeycodes.TryFormatMuseum("RTN", session.EngineModelId, out string rtnMuseum));
    StudioProgramEditorText.SplitMuseum(rtnMuseum, out string a, out string b);
    var rtn = new StudioProgramEditorText.EditorStep
    {
      MachineA = a,
      MachineB = b,
      Keys = "RTN",
    };
    Assert.IsFalse(
      StudioProgramEditorText.ShowSecondMachineBox(rtn, session.EngineModelId, format));
    Assert.IsFalse(
      StudioMuseumPrefix.IsIncompletePrefixStep(
        rtn.MachineA,
        rtn.MachineB,
        rtn.Keys,
        session.EngineModelId,
        format));
  }

  [TestMethod]
  public void ShowSecondBox_AfterPrefixMuseum35()
  {
    using CalcExplorerSession session = CreateHp65Session();
    var step = new StudioProgramEditorText.EditorStep { MachineA = "35" };
    StudioProgramEditorText.SyncFromMachine(
      step,
      session.EngineModelId,
      session.FormatProgramCodeForEditor);
    Assert.AreEqual("g", step.Keys, ignoreCase: true);
    Assert.IsTrue(
      StudioProgramEditorText.ShowSecondMachineBox(
        step,
        session.EngineModelId,
        session.FormatProgramCodeForEditor));
  }

  [TestMethod]
  public void TryApplySteps_G4_WritesProgram()
  {
    using CalcExplorerSession session = CreateHp65Session();
    var step = new StudioProgramEditorText.EditorStep
    {
      MachineA = "35",
      MachineB = "04",
      Keys = "g 4",
    };
    Assert.IsTrue(
      StudioProgramEditorText.TryApplySteps(
        [step],
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor,
        out List<byte> codes,
        out string? error),
      error);
    Assert.AreEqual(2, codes.Count, "g + 4 is two RAM steps (ShiftPair).");
    Assert.IsTrue(session.TryApplyProgramCodes(codes, out string? applyError), applyError);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    List<StudioProgramEditorText.EditorStep> hydrated =
      StudioProgramEditorText.HydrateSteps(lines, session.EngineModelId);
    Assert.IsTrue(hydrated.Count >= 1);
    Assert.AreEqual("35", hydrated[0].MachineA);
    Assert.AreEqual("04", hydrated[0].MachineB);
    Assert.AreEqual("g 4", hydrated[0].Keys, ignoreCase: true);
  }

  [TestMethod]
  public void InsertEmptyLine_ShiftsTail_AndMarksDirty()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([7, 8, 9], out string? applyError), applyError);
    // Capture clean snapshot so dirty detection works.
    string path = Path.Combine(Path.GetTempPath(), $"teocalc-ins-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out _), "export baseline");
      Assert.IsFalse(session.IsProgramDirty);

      session.SelectedProgramStep = 1;
      Assert.IsTrue(session.TryInsertEmptyProgramLineAtSelection(out string? error), error);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
      Assert.IsTrue(lines.Count >= 3);
      Assert.AreEqual(0, lines.First(l => l.Index == 1).Code);
      Assert.AreEqual(8, lines.First(l => l.Index == 2).Code);
      Assert.AreEqual(1, session.SelectedProgramStep);
      Assert.IsTrue(session.IsProgramDirty);
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
  public void DeleteLine_RemovesSelection_AndUndoRestores()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([7, 8, 9], out _), "seed");
    string path = Path.Combine(Path.GetTempPath(), $"teocalc-del-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out _), "baseline");
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> before));
      IReadOnlyList<StudioListingView.Row> rowsBefore = StudioListingView.Build(before);
      Assert.IsTrue(rowsBefore.Count >= 2, "need at least two listing rows");
      byte keepCode = rowsBefore[0].Code;
      byte dropCode = rowsBefore[1].Code;
      session.SelectedProgramStep = rowsBefore[1].Index;

      Assert.IsTrue(session.TryDeleteProgramLineAtSelection(out string? delError), delError);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> afterDel));
      IReadOnlyList<StudioListingView.Row> rowsAfter = StudioListingView.Build(afterDel);
      Assert.IsTrue(rowsAfter.Any(r => r.Code == keepCode));
      Assert.IsFalse(rowsAfter.Any(r => r.Code == dropCode));

      Assert.IsTrue(session.TryUndoProgramEdit(out string? undoError), undoError);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> afterUndo));
      IReadOnlyList<StudioListingView.Row> rowsUndo = StudioListingView.Build(afterUndo);
      Assert.IsTrue(rowsUndo.Any(r => r.Code == keepCode));
      Assert.IsTrue(rowsUndo.Any(r => r.Code == dropCode));

      Assert.IsTrue(session.TryRedoProgramEdit(out string? redoError), redoError);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> afterRedo));
      IReadOnlyList<StudioListingView.Row> rowsRedo = StudioListingView.Build(afterRedo);
      Assert.IsTrue(rowsRedo.Any(r => r.Code == keepCode));
      Assert.IsFalse(rowsRedo.Any(r => r.Code == dropCode));
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
  public void Revert_RestoresSavedSnapshot()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([1, 2, 3], out _), "seed");
    string path = Path.Combine(Path.GetTempPath(), $"teocalc-rev-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out _), "baseline");
      Assert.IsTrue(session.TryApplyProgramCodes([9, 9, 9], out _), "edit");
      Assert.IsTrue(session.IsProgramDirty);
      Assert.IsTrue(session.TryRevertProgramToSnapshot(out string? error), error);
      Assert.IsFalse(session.IsProgramDirty);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
      CollectionAssert.AreEqual(
        new byte[] { 1, 2, 3 },
        lines.Select(l => l.Code).Take(3).ToArray());
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
  public void SaveBuiltInCopy_DoesNotMarkInserted_PowerReloadStaysRom()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsFalse(session.CardInserted);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> romBefore));
    byte[] romCodes = romBefore.Select(l => l.Code).ToArray();

    Assert.IsTrue(session.TryApplyProgramCodes([4, 5, 6], out _), "edit built-in RAM");
    string path = Path.Combine(Path.GetTempPath(), $"teocalc-builtin-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? saveError), saveError);
      Assert.IsTrue(File.Exists(path));
      Assert.IsFalse(session.CardInserted);
      Assert.IsNull(session.LoadedCardPath);

      session.PowerOff();
      session.PowerOnResume();
      Assert.IsFalse(session.CardInserted);
      Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> romAfter));
      CollectionAssert.AreEqual(
        romCodes,
        romAfter.Select(l => l.Code).ToArray(),
        "Power cycle without inserted card must restore firmware ROM program, not the export file.");
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
  public void Wprgm_OverwriteKey_ReplacesCurrentLineAndKeepsTail()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.ProgramMode);

    ClassicFirmwareGateway gateway = GetClassicGateway(session);

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = session.BuildStudioListingRows(lines);
    Assert.IsTrue(rows.Count > 4, "sample needs several rows");

    StudioListingView.Row target = rows[3];
    byte original = target.Code;
    Assert.IsTrue(session.TrySetProgramStartStep(target.Index));
    int step = session.SelectedProgramStep;
    Assert.AreEqual(target.Index, step);

    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] before, out _));
    int beforeLast = LastNonZero(before);
    int beforeRows = rows.Count;

    // Digit "2" faceplate key (chart index 32, keycode 3).
    session.PressKey(32, 3);
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }

    Assert.AreEqual(step, session.SelectedProgramStep, "Edit must stay on the same Studio line.");
    byte replaced = gateway.Cpu!.Program.ReadCode(step);
    Assert.AreEqual(
      3,
      replaced,
      $"Current line must become keycode 3 (was {original}, got {replaced}).");

    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] after, out _));
    int afterLast = LastNonZero(after);
    Assert.AreEqual(
      beforeLast,
      afterLast,
      $"Program tail must stay intact (lastNonZero before={beforeLast} after={afterLast}).");

    Assert.IsTrue(session.TryGetProgramListing(out lines));
    Assert.IsTrue(
      session.BuildStudioListingRows(lines).Count >= beforeRows - 1,
      "Studio listing must not hide the program body after an overwrite + idle ticks.");

    // Pressing another digit must still stay on the same line (no auto-advance).
    session.PressKey(32, 3);
    Assert.AreEqual(step, session.SelectedProgramStep);
    Assert.AreEqual(3, gateway.Cpu.Program.ReadCode(step));
  }

  [TestMethod]
  public void Wprgm_PrefixThenSecondKey_StaysOnLineAndKeepsTail()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    session.ToggleProgramModeTo(true);

    ClassicFirmwareGateway gateway = GetClassicGateway(session);
    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.StudioCardAuthoringSteps);
    StudioListingView.Row target = rows[3];
    Assert.IsTrue(session.TrySetProgramStartStep(target.Index));
    int step = session.SelectedProgramStep;
    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] before, out _));
    int beforeLast = LastNonZero(before);

    // LBL (chart 7, keycode 43) then A (chart 0, keycode 30) → museum 23 11 pair.
    session.PressKey(7, ClassicProgramCodes.Label);
    Assert.AreEqual(step, session.SelectedProgramStep, "After LBL must stay on the line (no ↓).");
    Assert.AreEqual(ClassicProgramCodes.Label, gateway.Cpu!.Program.ReadCode(step));

    // Bare LBL must remain visible as its own Studio row (not merge-eat the next opcode).
    Assert.IsTrue(session.TryGetProgramListing(out lines));
    IReadOnlyList<StudioListingView.Row> afterLbl = session.BuildStudioListingRows(lines);
    Assert.IsTrue(
      afterLbl.Any(r => r.Index == step && r.Code == ClassicProgramCodes.Label),
      "Bare LBL must appear in the listing.");
    string museumLbl = StudioMuseumKeycodes.FormatMachineDisplay(
      afterLbl.First(r => r.Index == step),
      session.EngineModelId);
    StringAssert.Contains(museumLbl, "23");
    // LED shows left museum box only while waiting for the right-box key.
    string ledDigits = string.Concat(session.DisplayText.Where(char.IsDigit));
    StringAssert.Contains(ledDigits, "23");

    session.PressKey(0, 30); // faceplate A keycode (museum target displays as 11)
    Assert.AreEqual(step, session.SelectedProgramStep, "After A must stay on the line (no ↓).");
    Assert.AreEqual(ClassicProgramCodes.Label, gateway.Cpu.Program.ReadCode(step));
    Assert.AreEqual(30, gateway.Cpu.Program.ReadCode(step + 1));

    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] after, out _));
    // Pair may grow length by at most 1 vs a former single opcode.
    Assert.IsTrue(
      LastNonZero(after) <= beforeLast + 1,
      "Pair edit must not wipe/rebuild the program tail.");
    Assert.IsTrue(
      LastNonZero(after) >= beforeLast,
      "Pair edit must not shrink the program away.");

    // Idle firmware batches must not MemoryInitialize / AdvancePointer / omit the tail.
    for (int i = 0; i < 40; i++)
    {
      session.Tick(0.05f);
    }

    Assert.AreEqual(step, session.SelectedProgramStep, "Idle ticks must not change the Studio line.");
    Assert.IsTrue(session.TryGetProgramListing(out lines));
    IReadOnlyList<StudioListingView.Row> afterPair = session.BuildStudioListingRows(lines);
    Assert.IsTrue(
      afterPair.Count >= afterLbl.Count - 1,
      "Completing LBL·A must not omit the program body from the Studio listing.");
    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] afterTicks, out _));
    Assert.AreEqual(
      LastNonZero(after),
      LastNonZero(afterTicks),
      "Idle W/PRGM ticks must not shrink program RAM.");
  }

  [TestMethod]
  public void Wprgm_ArrowDown_MovesOneListingRow()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string path = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(path, out string? error), error);
    session.ToggleProgramModeTo(true);

    Assert.IsTrue(session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines));
    IReadOnlyList<StudioListingView.Row> rows = session.BuildStudioListingRows(lines);
    Assert.IsTrue(rows.Count > 5);

    Assert.IsTrue(session.TrySelectStudioProgramLine(rows[2].Index));
    Assert.AreEqual(rows[2].Index, session.SelectedProgramStep);

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Down));
    Assert.AreEqual(rows[3].Index, session.SelectedProgramStep, "Down must move exactly one Studio row.");

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Down));
    Assert.AreEqual(rows[4].Index, session.SelectedProgramStep);

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Up));
    Assert.AreEqual(rows[3].Index, session.SelectedProgramStep);
  }

  private static ClassicFirmwareGateway GetClassicGateway(CalcExplorerSession session)
  {
    FieldInfo field = typeof(CalcExplorerSession).GetField(
      "_firmware",
      BindingFlags.Instance | BindingFlags.NonPublic)!;
    return (ClassicFirmwareGateway)field.GetValue(session)!;
  }

  private static int LastNonZero(byte[] codes)
  {
    for (int i = codes.Length - 1; i >= 0; i--)
    {
      if (codes[i] != 0)
      {
        return i;
      }
    }

    return 0;
  }

  [TestMethod]
  public void Navigate_HomeEnd_MovesSelection()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([1, 2, 3, 4, 5], out _), "seed");
    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Home));
    int first = session.SelectedProgramStep;
    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.End));
    Assert.IsTrue(session.SelectedProgramStep > first);
  }

  [TestMethod]
  public void Wprgm_ArrowNav_SetsCurrentLineAndLed()
  {
    using CalcExplorerSession session = CreateHp65Session();
    Assert.IsTrue(session.TryApplyProgramCodes([1, 2, 3, 4, 5], out _), "seed");
    session.ToggleProgramModeTo(true);
    Assert.IsTrue(session.ProgramMode);

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.End));
    int last = session.SelectedProgramStep;
    string ledLast = session.DisplayText;

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Up));
    Assert.AreNotEqual(last, session.SelectedProgramStep);
    Assert.AreNotEqual(ledLast, session.DisplayText, "Calc LED should follow current line in W/PRGM.");

    Assert.IsTrue(session.TryNavigateProgramSelection(StudioProgramNav.Down));
    Assert.AreEqual(last, session.SelectedProgramStep);
  }

  [TestMethod]
  public void Wprgm_SelectEmptyLine_KeepsNopCurrent()
  {
    using CalcExplorerSession session = CreateHp65Session();
    // Real ops with an intentional mid-program empty (NOP) row.
    Assert.IsTrue(session.TryApplyProgramCodes([1, 0, 3], out _), "seed");
    session.ToggleProgramModeTo(true);

    Assert.IsTrue(session.TrySelectStudioProgramLine(2));
    Assert.AreEqual(2, session.SelectedProgramStep, "Empty/NOP line must stay current in W/PRGM.");
  }
}
