using ImGuiNET;
using Silk.NET.Input;
using Teo;
using Teo.Surface.Portrayal;

namespace TeoCalc.Rendering;

/// <summary>
/// Lab Immediate faceplate overlay (T63) — Field SoT via <see cref="TeoImmediateFaceplatePilotGate"/>.
/// Toggle with Ctrl+Shift+I (F11 is Studio step).
/// </summary>
public static class ImmediateFaceplateOverlay
{
  private static bool s_visible;
  private static bool s_chordWasDown;
  private static IForm? s_pilot;

  public static void ProcessToggle(IKeyboard? keyboard)
  {
    if (keyboard is null)
      return;

    bool ctrl = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);
    bool shift = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
    bool down = ctrl && shift && keyboard.IsKeyPressed(Key.I);
    if (down && !s_chordWasDown)
      s_visible = !s_visible;
    s_chordWasDown = down;
  }

  /// <summary>Call once per ImGui frame after NewFrame / BeginFrame.</summary>
  public static void Draw()
  {
    if (!s_visible)
      return;

    s_pilot ??= TeoImmediateFaceplatePilotGate.CreateHudPilot();
    if (!ImGuiImmediatePortrayalPresenter.Present(s_pilot, windowId: "calc_immediate_pilot"))
      s_visible = false;
  }
}
