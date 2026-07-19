using ImGuiNET;
using TeoCalc.Core.Firmware;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Faceplate shell + LED display when key vocabulary is unavailable.</summary>
public static class LedDisplayOnlyView
{
  public static void Draw(CalcExplorerSession session)
  {
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcModelDefinition model = CalcModelCatalog.Resolve(session.Model);
    CalcBodyLayout layout = CalcBodyLayoutCatalog.ResolveForFaceplate(
      model,
      session.Model.Family,
      session.Model.Model);
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available, layout);
    System.Numerics.Vector2 origin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();

    ImGui.Dummy(new System.Numerics.Vector2(metrics.Width, metrics.Height));

    CalcChassisRenderer.DrawShell(draw, origin, metrics, model);
    RectF display = metrics.DisplayRect(origin);
    FirmwareDisplaySnapshot displaySnapshot = session.DisplaySnapshot;
    CalcChassisRenderer.DrawLedDisplay(
      draw,
      display,
      session.ProgramMode,
      metrics.Scale,
      displaySnapshot.Visible,
      displaySnapshot.Text);

    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, session);
    _ = CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session, session.PowerOn);

    ImGui.PopStyleVar(2);
  }
}
