using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class CalcFaceplateView
{
  public static void Draw(CalcExplorerSession session)
  {
    if (!session.SupportsCpu || session.Vocabulary is null || session.Cpu is null)
    {
      ImGui.TextDisabled("Calculator faceplate requires a Classic CPU model.");
      return;
    }

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(session.Model.Family, session.Model.Model);

    ImGui.BeginChild("calc_body", new System.Numerics.Vector2(metrics.Width, metrics.Height), ImGuiChildFlags.None);

    System.Numerics.Vector2 origin = ImGui.GetCursorScreenPos();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();

    CalcChassisRenderer.DrawShell(draw, origin, metrics);
    RectF display = metrics.DisplayRect(origin);
    CalcChassisRenderer.DrawDisplayDigits(draw, display, session.Cpu, session.ProgramMode, metrics.Scale);

    bool powerOn = (session.Cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0;
    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, powerOn, session.ProgramMode);

    RectF keypad = metrics.KeypadRect(origin);

    foreach (FaceplateCell cell in cells)
    {
      if (cell.KeyChartIndex >= session.Vocabulary.KeyChart.Count)
      {
        continue;
      }

      ProgramKeyEntry key = session.Vocabulary.KeyChart[cell.KeyChartIndex];
      if (key.KeyCode == 0)
      {
        continue;
      }

      HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
        session.Model.Model,
        key,
        session.Vocabulary,
        cell.LabelStyle);
      if (string.IsNullOrEmpty(visual.Primary))
      {
        continue;
      }

      System.Numerics.Vector2 cellMin = metrics.CellOrigin(keypad, cell);
      System.Numerics.Vector2 cellSize = metrics.CellSize(cell);
      System.Numerics.Vector2 cellMax = cellMin + cellSize;
      CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell);

      if (!string.IsNullOrEmpty(visual.GoldShift))
      {
        DrawGoldBodyLabel(draw, visual.GoldShift, cellMin, cellSize, metrics);
      }

      CalcButtonStyle style = CalcButton.StyleForKeyIndex(cell.KeyChartIndex);
      bool leftAlign = cell.ColSpan >= 2;
      if (CalcButton.Draw(
            draw,
            $"##hpkey{cell.KeyChartIndex}",
            cellMin,
            cellMax,
            style,
            kind,
            visual.Primary,
            goldOnBody: null,
            blueOnBody: visual.BlueShift,
            metrics.Scale,
            leftAlign))
      {
        session.PressKeyAndRun((byte)key.KeyCode);
      }

      if (ImGui.IsItemHovered())
      {
        ImGui.SetTooltip($"{visual.Primary}  (code {key.KeyCode})");
      }
    }

    Hp65FaceplateArt.DrawFrameOverlay(draw, origin, metrics);

    ImGui.Dummy(new System.Numerics.Vector2(metrics.Width, metrics.Height));
    ImGui.EndChild();
  }

  private static void DrawGoldBodyLabel(
    ImDrawListPtr draw,
    string text,
    System.Numerics.Vector2 cellMin,
    System.Numerics.Vector2 cellSize,
    CalcChassisMetrics metrics)
  {
    float fontSize = ImGui.GetFontSize() * 0.62f * metrics.Scale;
    System.Numerics.Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float y = cellMin.Y - metrics.GoldBand * 0.72f;
    float x = cellMin.X + (cellSize.X - size.X) * 0.5f;
    draw.AddText(ImGui.GetFont(), fontSize, new System.Numerics.Vector2(x, y), CalcChassisPalette.GoldLabel, text);
  }
}
