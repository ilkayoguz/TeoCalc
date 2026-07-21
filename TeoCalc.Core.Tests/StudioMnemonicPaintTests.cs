using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioMnemonicPaintTests
{
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
    // RAM address of first user step remains 2 (0=START, 1=PTR); Studio # column
    // paints a separate 1-based display sequence over visible rows.
    Assert.AreEqual(2, rows[0].Index);
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
}
