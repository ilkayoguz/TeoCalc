using ImGuiNET;

namespace TeoCalc.Rendering;

public static class PanamatikDisplayOnlyView
{
  public static void Draw(CalcExplorerSession session)
  {
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available);
    System.Numerics.Vector2 origin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();

    ImGui.Dummy(new System.Numerics.Vector2(metrics.Width, metrics.Height));

    CalcChassisRenderer.DrawShell(draw, origin, metrics);
    RectF display = metrics.DisplayRect(origin);
    FirmwareDisplaySnapshot displaySnapshot = session.DisplaySnapshot;
    CalcChassisRenderer.DrawPanamatikDisplay(
      draw,
      display,
      session.ProgramMode,
      metrics.Scale,
      displaySnapshot.Visible,
      displaySnapshot.Text);

    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, session.PowerOn, session.ProgramMode);
    CalcChassisRenderer.SwitchPointerState switchPointer =
      CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session, session.PowerOn);
  }
}
