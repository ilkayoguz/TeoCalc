using System.Numerics;
using System.Text;
using ImGuiNET;
using TeoCalc.Core.Firmware;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Machine debug strip: microcode transport, ROM watch, registers, DUMP.
/// Distinct from Studio program transport (card row/key stepping).
/// </summary>
public static class CalcDebugPanelComponent
{
  public const float PreferredWidthRef = 320f;

  public static void DrawInline(Session session, ref string dumpStatusMessage)
  {
    CalcAppTheme.EnsureInitialized();
    CalcDebugChrome.SectionHeader("Machine debug");
    CalcDebugChrome.Muted(
      session.SupportsInstructionStep
        ? "Microcode grain | F10/F11 while open | Ctrl+F10/F11 always"
        : "Batch step (emulator gateway)");

    FirmwareBatchSnapshot batch = session.LastBatch;
    ImGui.Spacing();
    CalcDebugChrome.DrawExecutionStatus(
      session.ExecutionPaused,
      batch.ProgramCounter,
      batch.StepCount,
      batch.LastHandlerId);
    CalcDebugChrome.Muted(
      $"ROM={batch.Grp:X1}{batch.Rom:X1}  P={batch.P:X1}  S={batch.Status:X3}");

    ImGui.Separator();
    DrawTransport(session);
    ImGui.Separator();

    CalcRomWatchPane.Draw(
      session,
      height: MathF.Max(100f, ImGui.GetContentRegionAvail().Y * 0.38f),
      showTitle: true);
    ImGui.Separator();
    DrawRegisters(session);
    ImGui.Separator();
    DrawCallStack(session);
    ImGui.Separator();
    DrawDump(session, ref dumpStatusMessage);
  }

  private static void DrawTransport(Session session)
  {
    bool powered = session.PowerOn;
    if (!powered)
    {
      ImGui.BeginDisabled();
    }

    if (ImGui.Button("Break (F6)"))
    {
      session.BreakExecution();
    }

    ImGui.SameLine();
    if (ImGui.Button("Continue (F5)"))
    {
      session.ContinueExecution();
    }

    ImGui.SameLine();
    if (ImGui.Button("Stop (Shift+F5)"))
    {
      // Leave pause / resume free run (VS Stop Debugging). Does not power off.
      session.ContinueExecution();
    }

    if (ImGui.Button("u Into (F11)"))
    {
      session.StepMicrocodeInto();
    }

    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("One microcode instruction. Open Debug steals F10/F11 from Studio.");
    }

    ImGui.SameLine();
    if (ImGui.Button("u Over (F10)"))
    {
      session.StepMicrocodeOver();
    }

    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("Step over microcode call. Ctrl+F10/F11 also force microcode when Studio is active.");
    }

    ImGui.SameLine();
    if (ImGui.Button("u Out (Shift+F11)"))
    {
      session.StepMicrocodeOut();
    }

    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("Run until current microcode subroutine returns.");
    }

    if (!powered)
    {
      ImGui.EndDisabled();
      CalcDebugChrome.Muted("Power on (F2) to step.");
    }
  }

  private static bool s_editingRegisters;
  private static string[] s_registerEditFields = [];
  private static string[] s_registerEditNames = [];
  private static string? s_registerEditStatus;

  private static void DrawRegisters(Session session)
  {
    CalcDebugChrome.SectionHeader("Registers");
    FirmwareDebugRegisters? regs = session.TryGetDebugRegisters();
    if (regs is null || regs.Working.Count == 0)
    {
      CalcDebugChrome.Muted("Not available on this gateway.");
      s_editingRegisters = false;
      return;
    }

    if (!s_editingRegisters)
    {
      if (ImGui.SmallButton("Edit##debug-regs"))
      {
        BeginRegisterEdit(regs);
      }

      if (ImGui.IsItemHovered())
      {
        CalcAppTooltip.Set("Edit working registers (hex). Apply to commit.");
      }

      if (ImGui.BeginTable("##debug-regs", 2, ImGuiTableFlags.SizingStretchProp))
      {
        ImGui.TableSetupColumn("n", ImGuiTableColumnFlags.WidthFixed, 18f);
        ImGui.TableSetupColumn("v", ImGuiTableColumnFlags.WidthStretch);
        foreach (FirmwareRegisterDigest dig in regs.Working)
        {
          ImGui.TableNextRow();
          ImGui.TableSetColumnIndex(0);
          ImGui.TextDisabled(dig.Name);
          ImGui.TableSetColumnIndex(1);
          ImGui.TextUnformatted(dig.DigitsHex);
        }

        ImGui.EndTable();
      }

      return;
    }

    ImGui.TextDisabled("Hex digests (MSB left). Apply commits to CPU.");
    if (ImGui.BeginTable("##debug-regs-edit", 2, ImGuiTableFlags.SizingStretchProp))
    {
      ImGui.TableSetupColumn("n", ImGuiTableColumnFlags.WidthFixed, 18f);
      ImGui.TableSetupColumn("v", ImGuiTableColumnFlags.WidthStretch);
      for (int i = 0; i < s_registerEditFields.Length; i++)
      {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(s_registerEditNames[i]);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText($"##reg-{s_registerEditNames[i]}", ref s_registerEditFields[i], 32u);
      }

      ImGui.EndTable();
    }

    CalcAppDialogStyle.PushAffirmative();
    if (ImGui.Button("Apply##debug-regs"))
    {
      if (TryApplyRegisterEdit(session, out string? error))
      {
        s_editingRegisters = false;
        s_registerEditStatus = "Registers applied.";
        session.BreakExecution();
      }
      else
      {
        s_registerEditStatus = error;
      }
    }

    CalcAppDialogStyle.PopButton();
    ImGui.SameLine();
    CalcAppDialogStyle.PushNeutral();
    if (ImGui.Button("Cancel##debug-regs"))
    {
      s_editingRegisters = false;
      s_registerEditStatus = null;
    }

    CalcAppDialogStyle.PopButton();

    if (!string.IsNullOrEmpty(s_registerEditStatus))
    {
      ImGui.TextDisabled(s_registerEditStatus);
    }
  }

  private static void BeginRegisterEdit(FirmwareDebugRegisters regs)
  {
    s_registerEditNames = regs.Working.Select(d => d.Name).ToArray();
    s_registerEditFields = regs.Working.Select(d => d.DigitsHex).ToArray();
    s_editingRegisters = true;
    s_registerEditStatus = null;
  }

  private static bool TryApplyRegisterEdit(Session session, out string? error)
  {
    error = null;
    for (int i = 0; i < s_registerEditFields.Length; i++)
    {
      if (!session.TrySetDebugRegister(
            s_registerEditNames[i],
            s_registerEditFields[i],
            out error))
      {
        error = $"{s_registerEditNames[i]}: {error}";
        return false;
      }
    }

    return true;
  }

  private static void DrawCallStack(Session session)
  {
    CalcDebugChrome.SectionHeader("Call stack");
    FirmwareCallStackSnapshot? stack = session.TryGetCallStack();
    if (stack is null || stack.Slots.Count == 0)
    {
      CalcDebugChrome.Muted("Not available on this gateway.");
      return;
    }

    if (stack.StackPointer is int sp)
    {
      CalcDebugChrome.Muted($"SP={sp}  > = next Return");
    }
    else
    {
      CalcDebugChrome.Muted("> Ret0 = next Return (Classic JSB/RTN)");
    }

    if (!ImGui.BeginTable("##debug-callstack", 3, ImGuiTableFlags.SizingStretchProp))
    {
      return;
    }

    ImGui.TableSetupColumn("m", ImGuiTableColumnFlags.WidthFixed, 14f);
    ImGui.TableSetupColumn("slot", ImGuiTableColumnFlags.WidthFixed, 40f);
    ImGui.TableSetupColumn("addr", ImGuiTableColumnFlags.WidthStretch);
    foreach (FirmwareCallStackSlot slot in stack.Slots)
    {
      ImGui.TableNextRow();
      ImGui.TableSetColumnIndex(0);
      ImGui.TextUnformatted(slot.IsTop ? ">" : " ");
      ImGui.TableSetColumnIndex(1);
      ImGui.TextDisabled($"Ret{slot.Index}");
      ImGui.TableSetColumnIndex(2);
      string addr = $"{slot.Address:X4}";
      string? mnem = session.Map?.TryGetAddress(slot.Address)?.Mnemonic;
      string label = mnem is null ? addr : $"{addr}  {mnem}";
      if (ImGui.Selectable($"{label}##cs{slot.Index}"))
      {
        session.SelectedAddress = slot.Address;
        session.FollowRomWatch = false;
        session.MicrocodeScroll = RomWatchFollowScroll.CenterOn(
          slot.Address,
          session.Map?.WordCount ?? 0);
      }

      if (ImGui.IsItemHovered())
      {
        CalcAppTooltip.Set("Select in ROM watch");
      }
    }

    ImGui.EndTable();
  }

  private static void DrawDump(Session session, ref string dumpStatusMessage)
  {
    CalcDebugChrome.SectionHeader("DUMP");
    if (ImGui.Button("Copy dump"))
    {
      string dump = session.CaptureDebugDump();
      ImGui.SetClipboardText(dump);
      dumpStatusMessage = "Copied to clipboard.";
    }

    ImGui.SameLine();
    if (ImGui.Button("Save dump..."))
    {
      string dump = session.CaptureDebugDump();
      string? path = TrySaveDump(dump);
      dumpStatusMessage = path is null ? "Save failed." : $"Saved {Path.GetFileName(path)}";
    }

    if (!string.IsNullOrEmpty(dumpStatusMessage))
    {
      CalcDebugChrome.Muted(dumpStatusMessage);
    }
  }

  private static string? TrySaveDump(string dump)
  {
    try
    {
      string dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "TeoCalc",
        "Dumps");
      Directory.CreateDirectory(dir);
      string path = Path.Combine(dir, $"teo-dump-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
      File.WriteAllText(path, dump, Encoding.UTF8);
      return path;
    }
    catch
    {
      return null;
    }
  }
}
