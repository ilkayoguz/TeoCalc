using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioMnemonicPaintTests
{
  [TestMethod]
  public void ChromeForToken_LabelLetters_AreFixedBlackCapWhiteInk()
  {
    foreach (string letter in new[] { "A", "B", "C", "D", "E" })
    {
      StudioMnemonicPaint.ChromeForToken(letter, "HP-65", out uint face, out uint ink);
      Assert.AreEqual(CalcChassisPalette.KeyBlackFace, face, letter);
      Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Black), ink, letter);
    }
  }

  [TestMethod]
  public void ChromeForLabelKey_NumericLabel_IsBlackCapWhiteInk()
  {
    StudioMnemonicPaint.ChromeForLabelKey(out uint face, out uint ink);
    Assert.AreEqual(CalcChassisPalette.KeyBlackFace, face);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Black), ink);
  }

  [TestMethod]
  public void ChromeForToken_MapsFaceplateRoles()
  {
    StudioMnemonicPaint.ChromeForToken("f", null, out uint fFace, out uint fInk);
    Assert.AreEqual(CalcChassisPalette.KeyOrangeFace, fFace);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Orange), fInk);

    StudioMnemonicPaint.ChromeForToken("f-1", null, out uint fiFace, out _);
    Assert.AreEqual(CalcChassisPalette.KeyOrangeFace, fiFace);

    StudioMnemonicPaint.ChromeForToken("g", null, out uint gFace, out uint gInk);
    Assert.AreEqual(CalcChassisPalette.KeyBlueFace, gFace);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Blue), gInk);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, gInk);

    StudioMnemonicPaint.ChromeForToken("LBL", null, out uint lblFace, out uint lblInk);
    Assert.AreEqual(CalcChassisPalette.KeyOliveFace, lblFace);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Olive), lblInk);

    StudioMnemonicPaint.ChromeForToken("7", null, out uint digFace, out uint digInk);
    Assert.AreEqual(CalcChassisPalette.KeyWhiteFace, digFace);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.White), digInk);

    StudioMnemonicPaint.ChromeForToken("RCL", null, out uint rclFace, out uint rclInk);
    Assert.AreEqual(CalcChassisPalette.KeyBlackFace, rclFace);
    Assert.AreEqual(CalcKeyLabelPalette.PrimaryOnCap(CalcButtonStyle.Black), rclInk);
  }

  [TestMethod]
  public void ChromeForToken_Hp65G_UsesFaceplateBlueDarkInk()
  {
    StudioMnemonicPaint.ChromeForToken("g", "HP-65", out uint face, out uint ink);
    Assert.AreEqual(CalcChassisPalette.KeyBlueFace, face);
    Assert.AreEqual(CalcChassisPalette.KeyCapDarkText, ink);
  }

  [TestMethod]
  public void Tokenize_SplitsOnWhitespace()
  {
    CollectionAssert.AreEqual(
      new[] { "RCL", "1" },
      StudioMnemonicPaint.Tokenize("RCL 1"));
  }
}

[TestClass]
public sealed class StudioShiftLegendTests
{
  [TestMethod]
  public void TryResolve_GPlus4_YieldsReciprocalBlueLegend()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryResolve(
        "HP-65", "g", "4", out string legend, out StudioShiftLegend.ShiftKind kind));
    Assert.AreEqual("1/x", legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Blue, kind);
  }

  [TestMethod]
  public void TryResolve_GPlus7_YieldsSwapBlueLegend()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryResolve(
        "HP-65", "g", "7", out string legend, out StudioShiftLegend.ShiftKind kind));
    Assert.AreEqual("x↔y", legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Blue, kind);
  }

  [TestMethod]
  public void TryResolve_FPlus4_YieldsSinGold()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryResolve(
        "HP-65", "f", "4", out string legend, out StudioShiftLegend.ShiftKind kind));
    Assert.AreEqual("SIN", legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Gold, kind);
  }

  [TestMethod]
  public void TryResolve_FInversePlus4_YieldsArcsin()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryResolve(
        "HP-65", "f-1", "4", out string legend, out StudioShiftLegend.ShiftKind kind));
    Assert.AreEqual("SIN^-1", legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.GoldInverse, kind);
  }

  [TestMethod]
  public void TryResolve_Hp65_UnicodeLegends_MatchFaceplateCapAboveCapSkirt()
  {
    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "g", "DSP", out string ne, out _));
    Assert.AreEqual("x≠y", ne);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "g", "GTO", out string le, out _));
    Assert.AreEqual("x≤y", le);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f", "9", out string sqrt, out _));
    Assert.AreEqual("√x", sqrt);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f-1", "9", out string sq, out _));
    Assert.AreEqual("x²", sq);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "g", "8", out string rdn, out _));
    Assert.AreEqual("R↓", rdn);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "g", "9", out string rup, out _));
    Assert.AreEqual("R↑", rup);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "g", "2", out string pi, out _));
    Assert.AreEqual("π", pi);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f", "1", out string r2p, out _));
    Assert.AreEqual("R→P", r2p);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f-1", "1", out string p2r, out _));
    Assert.AreEqual("P→R", p2r);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f", "3", out string dms, out _));
    Assert.AreEqual("→D.MS", dms);

    Assert.IsTrue(StudioShiftLegend.TryResolve("HP-65", "f", "0", out string oct, out _));
    Assert.AreEqual("→OCT", oct);
  }

  [TestMethod]
  public void TryResolve_Hp65_ShiftLegends_AreFaceplateStrings()
  {
    string[] digits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "-", "+", "*", "/",
      "ENTER", "CHS", "EEX", "CLX", "DSP", "GTO", "LBL", "RTN", "SST", "R/S"];
    string[] prefixes = ["f", "g", "f-1"];
    foreach (string prefix in prefixes)
    {
      foreach (string key in digits)
      {
        if (!StudioShiftLegend.TryResolve("HP-65", prefix, key, out string legend, out _))
        {
          continue;
        }

        Assert.IsFalse(string.IsNullOrWhiteSpace(legend), $"Empty legend for {prefix}+{key}");
        // Resolve must not strip to ASCII mnemonics like RDOWN / SQRT.
        Assert.AreNotEqual("RDOWN", legend);
        Assert.AreNotEqual("SQRT", legend);
        Assert.AreNotEqual("PI", legend);
      }
    }
  }

  [TestMethod]
  public void ToAsciiSafe_MapsFaceplateUnicodeSnapshots()
  {
    Assert.AreEqual("x<>y", StudioShiftLegend.ToAsciiSafe("x↔y"));
    Assert.AreEqual("x<=y", StudioShiftLegend.ToAsciiSafe("x≤y"));
    Assert.AreEqual("x!=y", StudioShiftLegend.ToAsciiSafe("x≠y"));
    Assert.AreEqual("SQRT", StudioShiftLegend.ToAsciiSafe("√x"));
    Assert.AreEqual("PI", StudioShiftLegend.ToAsciiSafe("π"));
    Assert.AreEqual("1/x", StudioShiftLegend.ToAsciiSafe("1/x"));
    Assert.AreEqual("SIN^-1", StudioShiftLegend.ToAsciiSafe("SIN^-1"));
    Assert.AreEqual("E+", StudioShiftLegend.ToAsciiSafe("Σ+"));
    Assert.AreEqual("P<>S", StudioShiftLegend.ToAsciiSafe("P↔S"));
    Assert.AreEqual("R->P", StudioShiftLegend.ToAsciiSafe("R→P"));
    Assert.AreEqual("RDOWN", StudioShiftLegend.ToAsciiSafe("R↓"));
  }

  [TestMethod]
  public void IsShiftPrefix_IncludesH()
  {
    Assert.IsTrue(StudioShiftLegend.IsShiftPrefix("h"));
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Black, StudioShiftLegend.KindForPrefix("h"));
  }

  [TestMethod]
  public void NormalizeLabel_MapsFaceplateGlyphs()
  {
    Assert.AreEqual("f-1", StudioShiftLegend.NormalizeLabel("f⁻¹"));
    Assert.AreEqual(".", StudioShiftLegend.NormalizeLabel("·"));
    Assert.AreEqual("*", StudioShiftLegend.NormalizeLabel("×"));
  }

  [TestMethod]
  public void TryExpandFusedMnemonic_Rdown_IsGPlus8()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65",
        "RDOWN",
        out string prefix,
        out string key,
        out string legend,
        out StudioShiftLegend.ShiftKind kind));
    Assert.AreEqual("g", prefix);
    Assert.AreEqual("8", key);
    Assert.AreEqual("R↓", legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Blue, kind);
  }

  [TestMethod]
  public void TryExpandFusedMnemonic_XSwap_IsGPlus7()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65",
        "X<>Y",
        out string prefix,
        out string key,
        out string legend,
        out _));
    Assert.AreEqual("g", prefix);
    Assert.AreEqual("7", key);
    Assert.AreEqual("x↔y", legend);
  }

  [TestMethod]
  public void TryExpandFusedMnemonic_Lstx_IsGPlus0()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65",
        "LSTX",
        out string prefix,
        out string key,
        out string legend,
        out _));
    Assert.AreEqual("g", prefix);
    Assert.AreEqual("0", key);
    Assert.AreEqual("LST X", legend);
  }

  [TestMethod]
  public void TryExpandFusedMnemonic_Nop_IsGPlus1()
  {
    Assert.IsTrue(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65",
        "NOP",
        out string prefix,
        out string key,
        out string legend,
        out _));
    Assert.AreEqual("g", prefix);
    Assert.AreEqual("1", key);
    Assert.AreEqual("NOP", legend);
  }

  [TestMethod]
  public void TryExpandFusedMnemonic_PrimaryCapFace_DoesNotExpand()
  {
    Assert.IsFalse(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65", "RCL", out _, out _, out _, out _));
    Assert.IsFalse(
      StudioShiftLegend.TryExpandFusedMnemonic(
        "HP-65", "ENTER", out _, out _, out _, out _));
  }
}

[TestClass]
public sealed class StudioMuseumKeycodesTests
{
  [TestMethod]
  public void FormatMachineDisplay_FusedRcl1_IsMuseumPair()
  {
    // TeoCalc fused byte 33 = RCL 1; museum W/PRGM shows RCL key 34 + digit 01.
    string display = StudioMuseumKeycodes.FormatMachineDisplay(33, "RCL 1", "HP-65");
    Assert.AreEqual("34 01", display);
  }

  [TestMethod]
  public void FormatMachineDisplay_FusedSto5_IsMuseumPair()
  {
    string display = StudioMuseumKeycodes.FormatMachineDisplay(45, "STO 5", "HP-65");
    Assert.AreEqual("33 05", display);
  }

  [TestMethod]
  public void FormatMachineDisplay_Digit_Uses00Through09()
  {
    Assert.AreEqual("04", StudioMuseumKeycodes.FormatMachineDisplay(20, "4", "HP-65"));
  }

  [TestMethod]
  public void FormatMachineDisplay_UnknownModel_FallsBackToTeoCalcByte()
  {
    Assert.AreEqual("33", StudioMuseumKeycodes.FormatMachineDisplay(33, "RCL 1", "HP-25"));
  }

  [TestMethod]
  public void FormatMachineDisplay_FusedRdown_IsMuseumGPlus8()
  {
    StudioListingView.Row row = new(
      Index: 3,
      Code: 13,
      Mnemonic: "RDOWN",
      SecondCode: null,
      SecondMnemonic: null,
      Kind: StudioListingView.MergeKind.Single);
    // g key museum + digit 08
    Assert.AreEqual("35 08", StudioMuseumKeycodes.FormatMachineDisplay(row, "HP-65"));
  }

  [TestMethod]
  public void FormatMachineDisplay_ShiftPair_GPlus7_IsMuseum35_07()
  {
    StudioListingView.Row row = new(
      Index: 5,
      Code: 8,
      Mnemonic: "g",
      SecondCode: 23,
      SecondMnemonic: "7",
      Kind: StudioListingView.MergeKind.ShiftPair);
    Assert.AreEqual("35 07", StudioMuseumKeycodes.FormatMachineDisplay(row, "HP-65"));
  }

  [TestMethod]
  public void FormatMachineDisplay_LabelPair_LblA_IsMuseumPair()
  {
    Assert.IsTrue(StudioMuseumKeycodes.TryMuseumCodeForCapFace("HP-65", "LBL", out string lbl));
    Assert.IsTrue(StudioMuseumKeycodes.TryMuseumCodeForCapFace("HP-65", "A", out string a));
    StudioListingView.Row row = new(
      Index: 2,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 11,
      SecondMnemonic: "A",
      Kind: StudioListingView.MergeKind.LabelPair);
    Assert.AreEqual($"{lbl} {a}", StudioMuseumKeycodes.FormatMachineDisplay(row, "HP-65"));
  }

  [TestMethod]
  public void TryParseFusedStoRcl_AcceptsMergedSteps()
  {
    Assert.IsTrue(StudioMuseumKeycodes.TryParseFusedStoRcl("RCL 1", out string op, out int d));
    Assert.AreEqual("RCL", op);
    Assert.AreEqual(1, d);
    Assert.IsTrue(StudioMuseumKeycodes.TryParseFusedStoRcl("STO4", out op, out d));
    Assert.AreEqual("STO", op);
    Assert.AreEqual(4, d);
    Assert.IsTrue(StudioMuseumKeycodes.TryParseFusedStoRcl("RCL1", out op, out d));
    Assert.AreEqual("RCL", op);
    Assert.AreEqual(1, d);
    Assert.IsFalse(StudioMuseumKeycodes.TryParseFusedStoRcl("RCL 9", out _, out _));
    Assert.IsFalse(StudioMuseumKeycodes.TryParseFusedStoRcl("RCL", out _, out _));
  }
}

[TestClass]
public sealed class StudioListingViewTests
{
  [TestMethod]
  public void Build_HidesStartAndPtr()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Pointer, "PTR"),
      new(2, ClassicProgramCodes.Label, "LBL"),
      new(3, 1, "A"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(1, rows.Count);
    Assert.AreEqual(StudioListingView.MergeKind.LabelPair, rows[0].Kind);
    Assert.AreEqual("LBL A", rows[0].DisplayMnemonic);
    // RAM address of first user step remains 2 (0=START, 1=PTR). Studio # uses
    // DisplayStepNumber (1-based filtered span), not the RAM Index.
    Assert.AreEqual(2, rows[0].Index);
    Assert.AreEqual(1, StudioListingView.DisplayStepNumber(rows, 0));
  }

  [TestMethod]
  public void DisplayStepNumber_FusedStoRegister_AdvancesByTwo()
  {
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 45, "STO 1", null, null, StudioListingView.MergeKind.Single),
      new(3, 1, "2", null, null, StudioListingView.MergeKind.Single),
      new(4, 45, "STO 2", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.AreEqual(2, rows[1].StepSpan);
    Assert.AreEqual(1, StudioListingView.DisplayStepNumber(rows, 0));
    Assert.AreEqual(3, StudioListingView.DisplayStepNumber(rows, 1));
    Assert.AreEqual(5, StudioListingView.DisplayStepNumber(rows, 2));
    Assert.AreEqual(6, StudioListingView.DisplayStepNumber(rows, 3));
  }

  [TestMethod]
  public void Build_MergesGtoTargetPair()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Label, "LBL"),
      new(1, 11, "A"),
      new(2, 22, "GTO"),
      new(3, 1, "1"),
      new(4, 24, "RTN"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(2, rows.Count(r => r.Kind != StudioListingView.MergeKind.LabelPair));
    StudioListingView.Row branch = rows.First(r => r.DisplayMnemonic.StartsWith("GTO", StringComparison.OrdinalIgnoreCase));
    Assert.AreEqual(StudioListingView.MergeKind.BranchPair, branch.Kind);
    Assert.AreEqual("GTO 1", branch.DisplayMnemonic);
    Assert.AreEqual(2, branch.StepSpan);
  }

  [TestMethod]
  public void DisplayStepNumber_AdvancesByStepSpan_OnMergedPairs()
  {
    // LBL+A (span 2) → #1; next single → #3; shift pair (span 2) → #4; last → #6
    IReadOnlyList<StudioListingView.Row> rows =
    [
      new(0, ClassicProgramCodes.Label, "LBL", 11, "A", StudioListingView.MergeKind.LabelPair),
      new(2, 1, "1", null, null, StudioListingView.MergeKind.Single),
      new(3, 8, "g", 22, "GTO", StudioListingView.MergeKind.ShiftPair),
      new(5, 24, "RTN", null, null, StudioListingView.MergeKind.Single),
    ];

    Assert.AreEqual(2, rows[0].StepSpan);
    Assert.AreEqual(1, rows[1].StepSpan);
    Assert.AreEqual(2, rows[2].StepSpan);
    Assert.AreEqual(1, StudioListingView.DisplayStepNumber(rows, 0));
    Assert.AreEqual(3, StudioListingView.DisplayStepNumber(rows, 1));
    Assert.AreEqual(4, StudioListingView.DisplayStepNumber(rows, 2));
    Assert.AreEqual(6, StudioListingView.DisplayStepNumber(rows, 3));
    Assert.AreEqual(6, StudioListingView.MaxDisplayStepNumber(rows));
  }

  [TestMethod]
  public void Build_MergesShiftPair_GPlus7()
  {
    ClassicProgramLine[] lines =
    [
      new(0, 8, "g"),
      new(1, 23, "7"),
      new(2, 34, "R/S"),
    ];

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(2, rows.Count);
    Assert.AreEqual(StudioListingView.MergeKind.ShiftPair, rows[0].Kind);
    Assert.AreEqual("g 7", rows[0].DisplayMnemonic);
    Assert.AreEqual(StudioListingView.MergeKind.Single, rows[1].Kind);
  }

  [TestMethod]
  public void ResolvePaint_Rdown_ExpandsToGPlus8AndLegend()
  {
    StudioListingView.Row row = new(
      Index: 3,
      Code: 13,
      Mnemonic: "RDOWN",
      SecondCode: null,
      SecondMnemonic: null,
      Kind: StudioListingView.MergeKind.Single);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65");
    Assert.AreEqual("g 8", paint.KeysMnemonic);
    Assert.AreEqual("R↓", paint.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Blue, paint.LegendKind);
  }

  [TestMethod]
  public void ResolvePaint_Rcl1_KeepsKeycaps_NoShiftLegend()
  {
    StudioListingView.Row row = new(
      Index: 4,
      Code: 33,
      Mnemonic: "RCL 1",
      SecondCode: null,
      SecondMnemonic: null,
      Kind: StudioListingView.MergeKind.Single);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65");
    Assert.AreEqual("RCL 1", paint.KeysMnemonic);
    Assert.AreEqual(string.Empty, paint.Legend);
  }

  [TestMethod]
  public void ResolvePaint_ShiftPair_GPlus4_LegendIs1OverX()
  {
    StudioListingView.Row row = new(
      Index: 1,
      Code: 8,
      Mnemonic: "g",
      SecondCode: 20,
      SecondMnemonic: "4",
      Kind: StudioListingView.MergeKind.ShiftPair);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65");
    Assert.AreEqual("g 4", paint.KeysMnemonic);
    Assert.AreEqual("1/x", paint.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.Blue, paint.LegendKind);
  }

  [TestMethod]
  public void ResolvePaint_LabelPair_LblA_ShowsStripCaptionAsCardStrip()
  {
    string[] captions = ["+123", "", "", "", ""];
    StudioListingView.Row row = new(
      Index: 2,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 11,
      SecondMnemonic: "A",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65", captions);
    Assert.AreEqual("LBL A", paint.KeysMnemonic);
    Assert.AreEqual("+123", paint.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.CardStrip, paint.LegendKind);
  }

  [TestMethod]
  public void ResolvePaint_LabelPair_NoCaption_LegendEmpty()
  {
    string[] captions = ["+123", "", "", "", ""];
    StudioListingView.Row row = new(
      Index: 4,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 12,
      SecondMnemonic: "B",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65", captions);
    Assert.AreEqual("LBL B", paint.KeysMnemonic);
    Assert.AreEqual(string.Empty, paint.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.None, paint.LegendKind);
  }

  [TestMethod]
  public void ResolvePaint_LabelPair_NoCard_FallsBackToBuiltInStripLegends()
  {
    StudioListingView.Row rowA = new(
      Index: 2,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 11,
      SecondMnemonic: "A",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paintA = StudioListingView.ResolvePaint(rowA, "HP-65", cardStripCaptions: null);
    Assert.AreEqual("LBL A", paintA.KeysMnemonic);
    Assert.AreEqual("1/x", paintA.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.NoCardStrip, paintA.LegendKind);

    StudioListingView.Row rowB = new(
      Index: 4,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 12,
      SecondMnemonic: "B",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paintB = StudioListingView.ResolvePaint(rowB, "HP-65");
    Assert.AreEqual("\u221ax", paintB.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.NoCardStrip, paintB.LegendKind);

    StudioListingView.Row rowC = new(
      Index: 6,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 13,
      SecondMnemonic: "C",
      Kind: StudioListingView.MergeKind.LabelPair);
    Assert.AreEqual("y^x", StudioListingView.ResolvePaint(rowC, "HP-65").Legend);

    StudioListingView.Row rowD = new(
      Index: 8,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 14,
      SecondMnemonic: "D",
      Kind: StudioListingView.MergeKind.LabelPair);
    Assert.AreEqual("R\u2193", StudioListingView.ResolvePaint(rowD, "HP-65").Legend);

    StudioListingView.Row rowE = new(
      Index: 10,
      Code: 43,
      Mnemonic: "LBL",
      SecondCode: 15,
      SecondMnemonic: "E",
      Kind: StudioListingView.MergeKind.LabelPair);
    StudioListingView.Paint paintE = StudioListingView.ResolvePaint(rowE, "HP-65");
    Assert.AreEqual("x\u2194y", paintE.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.NoCardStrip, paintE.LegendKind);
  }

  [TestMethod]
  public void ResolvePaint_FusedLblA_ShowsStripCaption()
  {
    string[] captions = ["+123", "GTO1", "", "", ""];
    StudioListingView.Row row = new(
      Index: 2,
      Code: 43,
      Mnemonic: "LBL A",
      SecondCode: null,
      SecondMnemonic: null,
      Kind: StudioListingView.MergeKind.Single);
    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, "HP-65", captions);
    Assert.AreEqual("LBL A", paint.KeysMnemonic);
    Assert.AreEqual("+123", paint.Legend);
    Assert.AreEqual(StudioShiftLegend.ShiftKind.CardStrip, paint.LegendKind);
  }

  [TestMethod]
  public void FilterForClipboard_DropsRuntimeMarkers()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, 43, "LBL"),
      new(2, ClassicProgramCodes.Pointer, "PTR"),
      new(3, 1, "A"),
    ];

    ClassicProgramLine[] filtered = StudioListingView.FilterForClipboard(lines).ToArray();
    Assert.AreEqual(2, filtered.Length);
    Assert.AreEqual("LBL", filtered[0].Mnemonic);
    Assert.AreEqual("A", filtered[1].Mnemonic);
  }

  [TestMethod]
  public void ResolvePointerHighlight_SkipsHiddenPtr()
  {
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, ClassicProgramCodes.Pointer, "PTR"),
      new(2, 43, "LBL"),
      new(3, 1, "A"),
    ];
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(2, StudioListingView.ResolvePointerHighlightIndex(lines, rows));
  }

  [TestMethod]
  public void Build_PtrBetweenLabelPair_StillMerges()
  {
    // SST parked between LBL and A must not split the Studio row / shrink the listing.
    ClassicProgramLine[] lines =
    [
      new(0, ClassicProgramCodes.Start, "START"),
      new(1, 43, "LBL"),
      new(2, ClassicProgramCodes.Pointer, "PTR"),
      new(3, 11, "A"),
      new(4, 1, "1"),
      new(5, 24, "RTN"),
    ];
    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    Assert.AreEqual(StudioListingView.MergeKind.LabelPair, rows[0].Kind);
    Assert.AreEqual("LBL", rows[0].Mnemonic);
    Assert.AreEqual("A", rows[0].SecondMnemonic);
    Assert.IsTrue(rows.Count >= 3);
  }
}
