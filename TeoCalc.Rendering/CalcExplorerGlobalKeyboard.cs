using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>
/// Global explorer keys (VS-aligned debug transport where we have real capabilities):
/// F2 power, F4 PRGM,
/// F5 Continue, Shift+F5 Stop Debugging (leave pause / resume),
/// F6 Break (pause; VS Break All is Ctrl+Alt+Break — awkward; F9 reserved for Toggle Breakpoint later),
/// F10 Step Over, F11 Step Into.
/// Shift+F11 Step Out is unbound — gateway has no StepOut yet.
/// </summary>
public static class CalcExplorerGlobalKeyboard
{
  public static void Update(CalcExplorerSession session)
  {
    if (ImGui.GetIO().WantTextInput)
    {
      return;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.F2, repeat: false))
    {
      if (session.PowerOn)
      {
        session.PowerOff();
      }
      else
      {
        session.PowerOnResume();
      }
    }

    if (session.PowerOn && ImGui.IsKeyPressed(ImGuiKey.F4, repeat: false))
    {
      session.ToggleProgramMode();
    }

    if (!session.PowerOn)
    {
      return;
    }

    bool shift = IsShiftDown();

    if (ImGui.IsKeyPressed(ImGuiKey.F5, repeat: false))
    {
      // F5 Continue; Shift+F5 Stop Debugging — both leave pause / resume free run.
      // Stop is the VS “done stepping” muscle memory; panel stay open (title-bar Debug toggles it).
      session.ContinueExecution();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F6, repeat: false))
    {
      session.BreakExecution();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F10, repeat: true))
    {
      session.StepOver();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F11, repeat: true))
    {
      session.StepInto();
    }

    // Shift+F11 Step Out — not wired; ICalcFirmwareGateway has no StepOut.
  }

  private static bool IsShiftDown() =>
    ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);
}
