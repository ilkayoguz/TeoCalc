using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcBodyLayoutTests
{
  [TestMethod]
  public void Hp65Layout_MatchesBodyFaceplateLayout()
  {
    CalcBodyLayout layout = Hp65CalcBodyLayout.Instance;
    BodyFaceplateLayout.EnsureLoaded();

    Assert.AreEqual(Hp65CalcBodyLayout.LayoutId, layout.Id);
    Assert.AreEqual(BodyFaceplateLayout.ReferenceWidth, layout.ReferenceWidth);
    Assert.AreEqual(BodyFaceplateLayout.DisplayWindow, layout.DisplaySlot);
    Assert.AreEqual(BodyFaceplateLayout.KeypadPanel, layout.KeypadSlot);
    Assert.IsTrue(layout.TryGetKeySlot(0, out RectF key0));
    Assert.IsTrue(BodyFaceplateLayout.TryGetKeyRect(0, out RectF legacyKey0));
    Assert.AreEqual(legacyKey0, key0);
  }

  [TestMethod]
  public void ModelCatalog_Resolves_Hp65BodyLayout()
  {
    CalcBodyLayout layout = CalcBodyLayoutCatalog.Resolve(CalcModelCatalog.Hp65);
    Assert.AreEqual(Calc00dBodyLayout.LayoutId, layout.Id);
  }

  [TestMethod]
  public void BodySlots_MeasureFromMetrics()
  {
    CalcChassisMetrics metrics = new(Hp65CalcBodyLayout.Instance, 1f);
    CalcBodySlots slots = CalcBodyComponent.MeasureSlots(System.Numerics.Vector2.Zero, metrics);

    Assert.AreEqual(metrics.DisplayRect(System.Numerics.Vector2.Zero), slots.Display);
    Assert.AreEqual(metrics.LogoRect(System.Numerics.Vector2.Zero), slots.Logo);
  }

  [TestMethod]
  public void Hp21Layout_HasWoodstockSlots()
  {
    CalcBodyLayout layout = Hp21CalcBodyLayout.Instance;
    Assert.AreEqual(Hp21CalcBodyLayout.LayoutId, layout.Id);
    Assert.AreEqual(360f, layout.ReferenceWidth);
    Assert.IsFalse(layout.HasCardSlots);
    Assert.AreEqual(CalcSwitchLabels.WoodstockAngle, layout.SwitchLabels);
    Assert.IsTrue(layout.TryGetKeySlot(0, out _));
    Assert.IsTrue(layout.TryGetKeySlot(33, out RectF dsp));
    Assert.IsTrue(dsp.Width > dsp.Height);
  }

  [TestMethod]
  public void ModelCatalog_Hp21_UsesWoodstockBody()
  {
    CalcModelDefinition model = CalcModelCatalog.Hp21;
    CalcBodyLayout layout = CalcBodyLayoutCatalog.Resolve(model);
    Assert.AreEqual(Calc00dBodyLayout.LayoutId, layout.Id);
    Assert.AreEqual(CalcSwitchLabels.WoodstockAngle, layout.SwitchLabels);
  }

  [TestMethod]
  public void SwitchLabels_MatchFaceplatePhotos()
  {
    Assert.AreEqual(CalcSwitchLabels.ClassicPrgmRun, CalcSwitchLabels.ForModelId("65"));
    Assert.AreEqual(CalcSwitchLabels.ClassicPrgmRun, CalcSwitchLabels.ForModelId("67"));
    Assert.AreEqual(CalcSwitchLabels.WoodstockPrgmRun, CalcSwitchLabels.ForModelId("25"));
    Assert.AreEqual(CalcSwitchLabels.WoodstockPrgmRun, CalcSwitchLabels.ForModelId("29C"));
    Assert.AreEqual(CalcSwitchLabels.WoodstockPrgmRun, CalcSwitchLabels.ForModelId("34C"));
    Assert.AreEqual(CalcSwitchLabels.WoodstockAngle, CalcSwitchLabels.ForModelId("21"));
    Assert.AreEqual(CalcSwitchLabels.BeginEnd, CalcSwitchLabels.ForModelId("22"));
    Assert.AreEqual(CalcSwitchLabels.BeginEnd, CalcSwitchLabels.ForModelId("37E"));
    Assert.AreEqual(CalcSwitchLabels.DateBeginEnd, CalcSwitchLabels.ForModelId("38E"));
    Assert.AreEqual(CalcSwitchLabels.DateBeginEnd, CalcSwitchLabels.ForModelId("38"));
    Assert.AreEqual(CalcSwitchLabels.TimerRun, CalcSwitchLabels.ForModelId("55"));
    Assert.AreEqual(new CalcSwitchLabels("MAN", "NORM"), CalcSwitchLabels.ForModelId("19C"));
    Assert.AreEqual(CalcSwitchLabels.PowerOnly, CalcSwitchLabels.ForModelId("35"));
    Assert.AreEqual(CalcSwitchLabels.PowerOnly, CalcSwitchLabels.ForModelId("31E"));
    Assert.IsFalse(CalcSwitchLabels.PowerOnly.HasModeSwitch);
  }

  [TestMethod]
  public void SwitchCatalog_Hp65_StartsOffAndRun()
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId("65");
    Assert.AreEqual(2, bank.Count);
    Assert.AreEqual(0f, bank[0].InitialNorm);
    Assert.AreEqual(1f, bank[1].InitialNorm);
    Assert.AreEqual("OFF", bank[0].LeftLabel);
    Assert.AreEqual("W/PRGM", bank[1].LeftLabel);
  }

  [TestMethod]
  public void SwitchCatalog_Hp55_HasTopPrgmLabel()
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId("55");
    Assert.AreEqual(2, bank.Count);
    Assert.AreEqual(3, bank[1].PositionCount);
    Assert.AreEqual("PRGM", bank[1].TopLabel);
    Assert.AreEqual("", bank[1].BottomLabel);
    Assert.AreEqual(1f, bank[1].InitialNorm);
  }

  [TestMethod]
  public void SwitchCatalog_Hp19C_HasBottomLabels()
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId("19C");
    Assert.AreEqual(2, bank.Count);
    Assert.AreEqual("OFF", bank[0].LeftLabel);
    Assert.AreEqual("RUN", bank[0].RightLabel);
    Assert.AreEqual("PRGM", bank[0].BottomLabel);
    Assert.AreEqual("MAN", bank[1].LeftLabel);
    Assert.AreEqual("NORM", bank[1].RightLabel);
    Assert.AreEqual("TRACE", bank[1].BottomLabel);
  }

  [TestMethod]
  public void SwitchCatalog_Hp38E_HasDualRowDateBeginEnd()
  {
    IReadOnlyList<CalcSwitchSpec> bank = CalcSwitchCatalog.ForModelId("38E");
    Assert.AreEqual(2, bank.Count);
    Assert.AreEqual("OFF", bank[0].LeftLabel);
    Assert.AreEqual("ON", bank[0].RightLabel);
    Assert.AreEqual("D.MY\nBEGIN", bank[1].LeftLabel);
    Assert.AreEqual("M.DY\nEND", bank[1].RightLabel);
    Assert.AreEqual(2, bank[1].PositionCount);

    IReadOnlyList<CalcSwitchSpec> hp37 = CalcSwitchCatalog.ForModelId("37E");
    Assert.AreEqual("BEGIN", hp37[1].LeftLabel);
    Assert.AreEqual("END", hp37[1].RightLabel);
  }

  [TestMethod]
  public void Modern00d_SwitchPanel_AlignsWithDisplayAndShrinksWithoutAuxLabels()
  {
    CalcBodyLayout hp65 = Calc00dBodyLayout.Resolve("Classic", "65", CalcModelCatalog.Hp65);
    Assert.AreEqual(hp65.DisplaySlot.X, hp65.SwitchSlot.X);
    Assert.AreEqual(hp65.DisplaySlot.Width, hp65.SwitchSlot.Width);
    Assert.IsGreaterThanOrEqualTo(8f, hp65.SwitchSlot.Y - (hp65.DisplaySlot.Y + hp65.DisplaySlot.Height));

    CalcBodyLayout hp55 = Calc00dBodyLayout.Resolve("Classic", "55", CalcModelCatalog.Resolve("HP-55"));
    Assert.IsTrue(hp55.SwitchSlot.Height > hp65.SwitchSlot.Height);
    Assert.AreEqual("PRGM", hp55.Switches[1].TopLabel);
  }

  [TestMethod]
  public void Modern00d_Hp65_CardSlot_IsLegendFrameAboveAE()
  {
    CalcBodyLayout hp65 = Calc00dBodyLayout.Resolve("Classic", "65", CalcModelCatalog.Hp65);
    Assert.IsTrue(hp65.HasCardSlots);
    Assert.IsNotNull(hp65.CardSlotBand);
    RectF card = hp65.CardSlotBand!.Value;

    Assert.AreEqual(hp65.DisplaySlot.X, card.X, 0.01f);
    Assert.AreEqual(hp65.DisplaySlot.Width, card.Width, 0.01f);
    Assert.AreEqual(CalcLogoPanelComponent.HeightRef, card.Height, 0.01f);
    Assert.AreEqual(CalcCardSlotComponent.HeightRef, card.Height, 0.01f);

    Assert.IsTrue(hp65.TryGetKeySlot(0, out RectF keyA));
    Assert.IsGreaterThanOrEqualTo(card.Y + card.Height, keyA.Y);
    // Flush under the frame (gutter cancelled): A–E slot top ≈ card bottom.
    Assert.AreEqual(card.Y + card.Height, keyA.Y, 0.05f);

    CalcBodyLayout hp55 = Calc00dBodyLayout.Resolve("Classic", "55", CalcModelCatalog.Resolve("HP-55"));
    Assert.IsFalse(hp55.HasCardSlots);
  }

  [TestMethod]
  public void Modern00d_KeyPanel_ColumnGridAlignsWithBand()
  {
    CalcBodyLayout hp65 = Calc00dBodyLayout.Resolve("Classic", "65", CalcModelCatalog.Hp65);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "65");
    Assert.AreEqual(8, CalcKeyPanelComponent.CountRows(cells));

    float g = CalcKeyPanelComponent.GutterRef;
    // Modern path draws the hp logo as a fixed-height window-level bottom band, so the
    // resolved in-faceplate logo slot collapses to zero height (no footer inside the body).
    Assert.AreEqual(0f, hp65.LogoSlot.Height, 0.01f);

    Assert.AreEqual(hp65.KeypadSlot.Width, hp65.DisplaySlot.Width, 0.01f);
    Assert.AreEqual(hp65.KeypadSlot.X, hp65.DisplaySlot.X, 0.01f);

    Assert.IsTrue(hp65.TryGetKeySlot(0, out RectF topLeft));
    Assert.AreEqual(0f, topLeft.X - hp65.KeypadSlot.X, 0.05f);

    Assert.IsTrue(hp65.TryGetKeySlot(4, out RectF topRight));
    Assert.AreEqual(0f, hp65.KeypadSlot.X + hp65.KeypadSlot.Width - (topRight.X + topRight.Width), 0.05f);

    // Sparse bottom row: original key widths, leftover space as equal gaps, L/R flush.
    Assert.IsTrue(hp65.TryGetKeySlot(35, out RectF bottomLeft));
    Assert.IsTrue(hp65.TryGetKeySlot(38, out RectF bottomRight));
    Assert.AreEqual(0f, bottomLeft.X - hp65.KeypadSlot.X, 0.05f);
    Assert.AreEqual(0f, hp65.KeypadSlot.X + hp65.KeypadSlot.Width - (bottomRight.X + bottomRight.Width), 0.05f);
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCellWidthRef, bottomLeft.Width, 0.05f);
    Assert.AreEqual(CalcKeyPanelComponent.PreferredCellWidthRef, bottomRight.Width, 0.05f);
    Assert.IsGreaterThan(CalcKeyPanelComponent.GutterRef, bottomRight.X - (bottomLeft.X + bottomLeft.Width));

    float keypadBottom = hp65.KeypadSlot.Y + hp65.KeypadSlot.Height;
    Assert.AreEqual(g, keypadBottom - (bottomLeft.Y + bottomLeft.Height), 0.05f);
  }

  [TestMethod]
  public void Modern00d_Hp45_TopRow_IncludesGoldAndFlushes()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-45");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Classic", "HP-45", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-45");
    Assert.IsTrue(cells.Any(cell => cell.KeyChartIndex == 4), "HP-45 gold prefix at index 4");

    Assert.IsTrue(layout.TryGetKeySlot(0, out RectF left));
    Assert.IsTrue(layout.TryGetKeySlot(4, out RectF right));
    Assert.AreEqual(0f, left.X - layout.KeypadSlot.X, 0.05f);
    Assert.AreEqual(0f, layout.KeypadSlot.X + layout.KeypadSlot.Width - (right.X + right.Width), 0.05f);
  }

  [TestMethod]
  public void Modern00d_Hp35_TopRow_IncludesClrAndFlushes()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-35");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("Classic", "HP-35", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic", "HP-35");
    Assert.IsTrue(cells.Any(cell => cell.KeyChartIndex == 4));
    Assert.IsTrue(layout.TryGetKeySlot(0, out RectF left));
    Assert.IsTrue(layout.TryGetKeySlot(4, out RectF right));
    Assert.AreEqual(0f, left.X - layout.KeypadSlot.X, 0.05f);
    Assert.AreEqual(0f, layout.KeypadSlot.X + layout.KeypadSlot.Width - (right.X + right.Width), 0.05f);
  }

  [TestMethod]
  public void Modern00d_Hp19C_SparseLowerRows_Flush()
  {
    CalcModelDefinition model = CalcModelCatalog.Resolve("HP-19C");
    CalcBodyLayout layout = Calc00dBodyLayout.Resolve("HP19C", "HP-19C", model);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("HP19C", "HP-19C");
    Assert.IsFalse(cells.Any(cell => cell.KeyChartIndex is 17 or 23 or 29 or 35));
    Assert.AreEqual(6, CalcKeyPanelComponent.CountColumns(cells));

    Assert.IsTrue(layout.TryGetKeySlot(18, out RectF row3Left));
    Assert.IsTrue(layout.TryGetKeySlot(22, out RectF row3Right));
    Assert.AreEqual(0f, row3Left.X - layout.KeypadSlot.X, 0.05f);
    Assert.AreEqual(0f, layout.KeypadSlot.X + layout.KeypadSlot.Width - (row3Right.X + row3Right.Width), 0.05f);

    Assert.IsTrue(layout.TryGetKeySlot(30, out RectF row5Left));
    Assert.IsTrue(layout.TryGetKeySlot(34, out RectF row5Right));
    Assert.AreEqual(0f, row5Left.X - layout.KeypadSlot.X, 0.05f);
    Assert.AreEqual(0f, layout.KeypadSlot.X + layout.KeypadSlot.Width - (row5Right.X + row5Right.Width), 0.05f);
  }

  [TestMethod]
  public void Modern00d_Hp01And19C_KeyPanel_FitsAllColumns()
  {
    CalcModelDefinition hp01Model = CalcModelCatalog.Resolve("HP-01");
    CalcBodyLayout hp01 = Calc00dBodyLayout.Resolve("HP01", "HP-01", hp01Model);
    IReadOnlyList<FaceplateCell> hp01Cells = CalcFaceplateLayout.GetPhysicalCells("HP01", "HP-01");
    Assert.AreEqual(7, CalcKeyPanelComponent.CountColumns(hp01Cells));
    Assert.IsTrue(hp01.TryGetKeySlot(6, out RectF hp01Right));
    Assert.AreEqual(0f, hp01.KeypadSlot.X + hp01.KeypadSlot.Width - (hp01Right.X + hp01Right.Width), 0.05f);

    CalcModelDefinition hp19Model = CalcModelCatalog.Resolve("HP-19C");
    CalcBodyLayout hp19 = Calc00dBodyLayout.Resolve("HP19C", "HP-19C", hp19Model);
    IReadOnlyList<FaceplateCell> hp19Cells = CalcFaceplateLayout.GetPhysicalCells("HP19C", "HP-19C");
    Assert.AreEqual(6, CalcKeyPanelComponent.CountColumns(hp19Cells));
    Assert.IsTrue(hp19.TryGetKeySlot(5, out RectF hp19Right));
    Assert.AreEqual(0f, hp19.KeypadSlot.X + hp19.KeypadSlot.Width - (hp19Right.X + hp19Right.Width), 0.05f);
  }

  [TestMethod]
  public void ThemeCatalog_Loads_RetroAndModern()
  {
    IReadOnlyList<CalcThemePack> themes = CalcThemeCatalog.LoadAll();
    Assert.IsTrue(themes.Count >= 2);
    Assert.IsTrue(themes.Any(theme => theme.Id == "Retro"));
    Assert.IsTrue(themes.Any(theme => theme.Id == "Modern"));
  }
}
