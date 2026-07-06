using ImGuiNET;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

/// <summary>Keyboard → HP Classic faceplate key chart bindings (visual press + CPU key).</summary>
public static class CalcFaceplateKeyboard
{
  private static readonly KeyBinding[] Bindings =
  [
    new(0, ImGuiKey.A),
    new(1, ImGuiKey.B),
    new(2, ImGuiKey.C),
    new(3, ImGuiKey.D),
    new(4, ImGuiKey.E),
    new(5, ImGuiKey.P),
    new(6, ImGuiKey.O),
    new(7, ImGuiKey.L),
    new(8, ImGuiKey.Q),
    new(9, ImGuiKey.T),
    new(11, ImGuiKey.F, ImGuiModFlags.Shift),
    new(10, ImGuiKey.F),
    new(12, ImGuiKey.S),
    new(13, ImGuiKey.R),
    new(14, ImGuiKey.G),
    new(15, ImGuiKey.Enter),
    new(15, ImGuiKey.KeypadEnter),
    new(17, ImGuiKey.H),
    new(18, ImGuiKey.X),
    new(19, ImGuiKey.Backspace),
    new(19, ImGuiKey.Delete),
    new(20, ImGuiKey.Minus),
    new(20, ImGuiKey.KeypadSubtract),
    new(21, ImGuiKey._7),
    new(21, ImGuiKey.Keypad7),
    new(22, ImGuiKey._8),
    new(22, ImGuiKey.Keypad8),
    new(23, ImGuiKey._9),
    new(23, ImGuiKey.Keypad9),
    new(25, ImGuiKey.Equal, ImGuiModFlags.Shift),
    new(25, ImGuiKey.KeypadAdd),
    new(26, ImGuiKey._4),
    new(26, ImGuiKey.Keypad4),
    new(27, ImGuiKey._5),
    new(27, ImGuiKey.Keypad5),
    new(28, ImGuiKey._6),
    new(28, ImGuiKey.Keypad6),
    new(30, ImGuiKey._8, ImGuiModFlags.Shift),
    new(30, ImGuiKey.KeypadMultiply),
    new(31, ImGuiKey._1),
    new(31, ImGuiKey.Keypad1),
    new(32, ImGuiKey._2),
    new(32, ImGuiKey.Keypad2),
    new(33, ImGuiKey._3),
    new(33, ImGuiKey.Keypad3),
    new(35, ImGuiKey.Slash),
    new(35, ImGuiKey.KeypadDivide),
    new(36, ImGuiKey._0),
    new(36, ImGuiKey.Keypad0),
    new(37, ImGuiKey.Period),
    new(37, ImGuiKey.KeypadDecimal),
    new(38, ImGuiKey.Space),
  ];

  public static int HeldKeyChartIndex { get; private set; } = -1;

  public static void Update(CalcExplorerSession session, ProgramVocabulary vocabulary, bool enabled)
  {
    HeldKeyChartIndex = -1;
    if (!enabled)
    {
      session.SetKeyboardKeyHeld(false);
      return;
    }

    if (!session.PowerOn)
    {
      session.SetKeyboardKeyHeld(false);
      return;
    }

    if (ImGui.GetIO().WantTextInput)
    {
      session.SetKeyboardKeyHeld(false);
      return;
    }

    if (session.ShiftPreview.Mode != ShiftPreviewMode.None && ImGui.IsKeyPressed(ImGuiKey.Escape, repeat: false))
    {
      session.ClearShiftPreview();
      session.SetKeyboardKeyHeld(false);
      return;
    }

    bool fired = false;
    foreach (KeyBinding binding in Bindings)
    {
      if (!WasPressed(binding) || fired)
      {
        continue;
      }

      if (TryGetKeyCode(vocabulary, binding.KeyChartIndex, out byte keyCode))
      {
        session.PressKey(binding.KeyChartIndex, keyCode);
        fired = true;
      }
    }

    foreach (KeyBinding binding in Bindings)
    {
      if (!IsHeld(binding))
      {
        continue;
      }

      HeldKeyChartIndex = binding.KeyChartIndex;
      break;
    }

    session.SetKeyboardKeyHeld(HeldKeyChartIndex >= 0);
  }

  private static bool IsHeld(KeyBinding binding) =>
    ModifiersMatch(binding) && ImGui.IsKeyDown(binding.Key);

  private static bool WasPressed(KeyBinding binding) =>
    ModifiersMatch(binding) && ImGui.IsKeyPressed(binding.Key, repeat: false);

  private static bool ModifiersMatch(KeyBinding binding)
  {
    ImGuiModFlags mods = CurrentMods();
    return mods == binding.RequiredMods;
  }

  private static ImGuiModFlags CurrentMods()
  {
    ImGuiModFlags mods = ImGuiModFlags.None;
    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
    {
      mods |= ImGuiModFlags.Shift;
    }

    if (ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))
    {
      mods |= ImGuiModFlags.Ctrl;
    }

    if (ImGui.IsKeyDown(ImGuiKey.LeftAlt) || ImGui.IsKeyDown(ImGuiKey.RightAlt))
    {
      mods |= ImGuiModFlags.Alt;
    }

    if (ImGui.IsKeyDown(ImGuiKey.LeftSuper) || ImGui.IsKeyDown(ImGuiKey.RightSuper))
    {
      mods |= ImGuiModFlags.Super;
    }

    return mods;
  }

  private static bool TryGetKeyCode(ProgramVocabulary vocabulary, int keyChartIndex, out byte keyCode)
  {
    keyCode = 0;
    if ((uint)keyChartIndex >= vocabulary.KeyChart.Count)
    {
      return false;
    }

    ProgramKeyEntry entry = vocabulary.KeyChart[keyChartIndex];
    if (entry.KeyCode == 0)
    {
      return false;
    }

    keyCode = (byte)entry.KeyCode;
    return true;
  }

  private readonly record struct KeyBinding(int KeyChartIndex, ImGuiKey Key, ImGuiModFlags RequiredMods = ImGuiModFlags.None);
}
