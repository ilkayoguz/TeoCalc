using ImGuiNET;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// Global explorer keys (VS-aligned debug transport where we have real capabilities):
/// F2 power, F4 PRGM,
/// F5 Continue, Shift+F5 Stop Debugging (leave pause / resume),
/// F6 Break (pause; VS Break All is Ctrl+Alt+Break — awkward),
/// F9 Toggle Studio breakpoint at selection / PTR,
/// F10 Step Over (Studio: Code row / FC box; else microcode),
/// F11 Step Into (Studio: one keystroke / FC element; else microcode).
/// Shift+F11 Step Out is unbound — gateway has no StepOut yet.
/// </summary>
public static class CalcExplorerGlobalKeyboard
{
  public static void Update(CalcExplorerSession session)
  {
    // Ctrl+F even while a text field is active — moves focus to Studio Find.
    if (IsCtrlDown() && ImGui.IsKeyPressed(ImGuiKey.F, repeat: false))
    {
      CalcStudioPanelComponent.RequestFindFocus();
    }

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
      session.ContinueExecution();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F6, repeat: false))
    {
      session.BreakExecution();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F9, repeat: false))
    {
      bool added = session.ToggleStudioBreakpointAtSelection();
      session.StudioStatusMessage = added
        ? $"Breakpoint + step {session.SelectedProgramStep}"
        : $"Breakpoint − step {session.SelectedProgramStep}";
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F10, repeat: true))
    {
      session.StepOver();
    }

    if (!shift && ImGui.IsKeyPressed(ImGuiKey.F11, repeat: true))
    {
      session.StepInto();
    }

    if (ImGui.IsKeyPressed(ImGuiKey.LeftBracket, repeat: true))
    {
      session.NudgeExecutionSpeed(-1);
    }

    if (ImGui.IsKeyPressed(ImGuiKey.RightBracket, repeat: true))
    {
      session.NudgeExecutionSpeed(1);
    }

    // Shift+F11 Step Out — not wired; ICalcFirmwareGateway has no StepOut.
  }

  private static bool IsShiftDown() =>
    ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);

  private static bool IsCtrlDown() =>
    ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);
}
