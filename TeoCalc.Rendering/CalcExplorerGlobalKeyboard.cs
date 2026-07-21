using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>
/// Global explorer keys: F2 power, F4 PRGM, F5 continue, F9 break, F10 step over, F11 step into.
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

    if (ImGui.IsKeyPressed(ImGuiKey.F5, repeat: false))
    {
      session.ContinueExecution();
    }

    if (ImGui.IsKeyPressed(ImGuiKey.F9, repeat: false))
    {
      session.BreakExecution();
    }

    if (ImGui.IsKeyPressed(ImGuiKey.F10, repeat: true))
    {
      session.StepOver();
    }

    if (ImGui.IsKeyPressed(ImGuiKey.F11, repeat: true))
    {
      session.StepInto();
    }
  }
}
