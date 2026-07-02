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

    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(session.Model.Family, session.Model.Model);

    System.Numerics.Vector2 origin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();

    ImGui.Dummy(new System.Numerics.Vector2(metrics.Width, metrics.Height));

    CalcChassisRenderer.DrawShell(draw, origin, metrics);
    RectF display = metrics.DisplayRect(origin);
    CalcChassisRenderer.DrawDisplayDigits(draw, display, session.Cpu, session.ProgramMode, metrics.Scale);

    bool powerOn = (session.Cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0;
    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, powerOn, session.ProgramMode);
    CalcEnterRowLabels.Draw(draw, origin, metrics);

    bool anyKeyHovered = false;
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

      RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
      if (keyRect.Width <= 0f || keyRect.Height <= 0f)
      {
        continue;
      }

      System.Numerics.Vector2 cellMin = keyRect.Min;
      System.Numerics.Vector2 cellMax = keyRect.Max;
      System.Numerics.Vector2 cellSize = keyRect.Size;
      CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell);

      if (!string.IsNullOrEmpty(visual.GoldShift) && !CalcEnterRowLabels.IsEnterRowKey(cell.KeyChartIndex))
      {
        DrawGoldBodyLabel(draw, visual.GoldShift, cellMin, cellSize.X, metrics.GoldBandForKey(cell.KeyChartIndex), metrics);
      }

      CalcButtonStyle style = CalcButton.StyleForKeyIndex(cell.KeyChartIndex);
      bool leftAlign = kind != CalcButtonKind.EnterWide && cell.ColSpan >= 2;
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
        anyKeyHovered = true;
        ImGui.SetTooltip($"{visual.Primary}  (code {key.KeyCode})");
      }
    }

    bool anySwitchHovered = CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session);

    CalcFaceplatePointer.ApplyHandCursorIfHovering(
      origin,
      metrics,
      cells,
      session.Vocabulary.KeyChart,
      anyKeyHovered,
      anySwitchHovered);

    ImGui.PopStyleVar(2);
  }

  private static void DrawGoldBodyLabel(
    ImDrawListPtr draw,
    string text,
    System.Numerics.Vector2 cellMin,
    float cellWidth,
    float goldBand,
    CalcChassisMetrics metrics)
  {
    float fontSize = CalcFaceplateTypography.GoldShift(metrics.Scale);
    HpClassicFaceplateGlyphs.LabelSize size = HpClassicFaceplateGlyphs.MeasureBodyLabel(text, fontSize);
    float bandCenterY = cellMin.Y - goldBand * 0.24f;
    float y = bandCenterY - size.Height * 0.5f;
    float x = cellMin.X + (cellWidth - size.Width) * 0.5f;
    HpClassicFaceplateGlyphs.DrawBodyLabel(draw, new System.Numerics.Vector2(x, y), text, fontSize, CalcKeyLabelPalette.GoldOnBody, metrics.Scale);
  }
}
