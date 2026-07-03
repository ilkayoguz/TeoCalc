using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>F2 power and F4 PRGM — work without calculator panel focus.</summary>
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
  }
}
