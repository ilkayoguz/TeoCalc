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
/// Studio edit: Ins/Del, Home/End/PgUp/PgDn, clipboard, Undo/Redo, Ctrl+S / Ctrl+R.
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
    bool ctrl = IsCtrlDown();

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

    if (session.SupportsCardProgram)
    {
      UpdateStudioEditKeys(session, ctrl, shift);
    }

    // Shift+F11 Step Out — not wired; ICalcFirmwareGateway has no StepOut.
  }

  private static void UpdateStudioEditKeys(CalcExplorerSession session, bool ctrl, bool shift)
  {
    if (ctrl && ImGui.IsKeyPressed(ImGuiKey.S, repeat: false))
    {
      CalcStudioPanelComponent.RequestStudioSaveFromKeyboard();
      return;
    }

    if (ctrl && ImGui.IsKeyPressed(ImGuiKey.R, repeat: false))
    {
      session.RequestStudioRevertConfirm();
      if (!session.PendingStudioRevertConfirm
          && session.StudioStatusMessage.Length > 0)
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(session.StudioStatusMessage);
        session.StudioStatusMessage = string.Empty;
      }

      return;
    }

    if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Z, repeat: false))
    {
      if (session.TryUndoProgramEdit(out string? error))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Undo.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Nothing to undo.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    if (ctrl && ImGui.IsKeyPressed(ImGuiKey.Y, repeat: false))
    {
      if (session.TryRedoProgramEdit(out string? error))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Redo.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Nothing to redo.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    // Copy: Ctrl+C / Ctrl+Insert
    if ((ctrl && ImGui.IsKeyPressed(ImGuiKey.C, repeat: false))
        || (ctrl && !shift && ImGui.IsKeyPressed(ImGuiKey.Insert, repeat: false)))
    {
      string text = session.FormatSelectedProgramListingForClipboard();
      if (string.IsNullOrWhiteSpace(text))
      {
        text = session.FormatProgramListingText();
      }

      if (string.IsNullOrWhiteSpace(text))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus("Nothing to copy.");
      }
      else
      {
        ImGui.SetClipboardText(text);
        CalcStudioPanelComponent.ShowKeyboardStatus("Copied.");
      }

      return;
    }

    // Paste: Ctrl+V / Shift+Insert
    if ((ctrl && ImGui.IsKeyPressed(ImGuiKey.V, repeat: false))
        || (shift && !ctrl && ImGui.IsKeyPressed(ImGuiKey.Insert, repeat: false)))
    {
      string clip = ImGui.GetClipboardText() ?? string.Empty;
      if (session.TryPasteProgramListing(clip, out string? error))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Pasted.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Paste failed.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    // Cut: Ctrl+X / Shift+Delete
    if ((ctrl && ImGui.IsKeyPressed(ImGuiKey.X, repeat: false))
        || (shift && !ctrl && ImGui.IsKeyPressed(ImGuiKey.Delete, repeat: false)))
    {
      if (session.TryCutSelectedProgramLine(out string clipboardText, out string? error))
      {
        ImGui.SetClipboardText(clipboardText);
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Cut.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Cut failed.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    if (!ctrl && !shift && ImGui.IsKeyPressed(ImGuiKey.Insert, repeat: false))
    {
      if (session.TryInsertEmptyProgramLineAtSelection(out string? error))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Inserted.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Insert failed.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    if (!ctrl && !shift && ImGui.IsKeyPressed(ImGuiKey.Delete, repeat: false))
    {
      if (session.TryDeleteProgramLineAtSelection(out string? error))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Deleted.");
      }
      else
      {
        CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Delete failed.");
      }

      session.StudioStatusMessage = string.Empty;
      return;
    }

    if (!ctrl && ImGui.IsKeyPressed(ImGuiKey.Home, repeat: false))
    {
      _ = session.TryNavigateProgramSelection(StudioProgramNav.Home);
      return;
    }

    if (!ctrl && ImGui.IsKeyPressed(ImGuiKey.End, repeat: false))
    {
      _ = session.TryNavigateProgramSelection(StudioProgramNav.End);
      return;
    }

    if (!ctrl && ImGui.IsKeyPressed(ImGuiKey.PageUp, repeat: true))
    {
      _ = session.TryNavigateProgramSelection(StudioProgramNav.PageUp);
      return;
    }

    if (!ctrl && ImGui.IsKeyPressed(ImGuiKey.PageDown, repeat: true))
    {
      _ = session.TryNavigateProgramSelection(StudioProgramNav.PageDown);
    }
  }

  private static bool IsShiftDown() =>
    ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift);

  private static bool IsCtrlDown() =>
    ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);
}
