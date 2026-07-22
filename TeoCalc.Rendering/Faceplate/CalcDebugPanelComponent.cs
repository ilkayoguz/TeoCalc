using System.Numerics;
using System.Text;
using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>DEBUG/TRACE side strip: step controls, ROM watch, registers, DUMP.</summary>
public static class CalcDebugPanelComponent
{
  public const float PreferredWidthRef = 320f;

  public static void DrawInline(Session session, ref string dumpStatusMessage)
  {
    ImGui.TextUnformatted("DEBUG / TRACE");
    ImGui.TextDisabled(session.SupportsInstructionStep ? "Native microcode step" : "Batch step (emulator)");
    ImGui.Separator();

    FirmwareBatchSnapshot batch = session.LastBatch;
    bool paused = session.ExecutionPaused;
    ImGui.TextDisabled(
      $"PC={batch.ProgramCounter:X4}  ROM={batch.Grp:X1}{batch.Rom:X1}  P={batch.P:X1}");
    ImGui.TextDisabled($"S={batch.Status:X3}  steps={batch.StepCount}  {(paused ? "PAUSED" : "RUN")}");
    ImGui.TextDisabled(batch.LastHandlerId ?? "-");

    ImGui.Separator();
    DrawTransport(session);
    ImGui.Separator();

    bool follow = session.FollowRomWatch;
    if (ImGui.Checkbox("Follow ROM", ref follow))
    {
      session.FollowRomWatch = follow;
      if (follow)
      {
        session.SelectedAddress = Math.Max(0, session.LastBatch.ProgramCounter);
        session.MicrocodeScroll = Math.Max(0, session.SelectedAddress - 6);
      }
    }

    DrawRomWatch(session);
    ImGui.Separator();
    DrawRegisters(session);
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

    if (ImGui.Button("Step Into (F11)"))
    {
      session.StepMicrocodeInto();
    }

    ImGui.SameLine();
    if (ImGui.Button("Step Over (F10)"))
    {
      session.StepMicrocodeOver();
    }

    ImGui.SameLine();
    if (powered)
    {
      ImGui.BeginDisabled();
    }

    ImGui.Button("Step Out (Shift+F11)");
    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
    {
      ImGui.SetTooltip("Step Out not supported yet.");
    }

    if (powered)
    {
      ImGui.EndDisabled();
    }

    if (!powered)
    {
      ImGui.EndDisabled();
      ImGui.TextDisabled("Power on (F2) to step.");
    }
    else
    {
      ImGui.TextDisabled("F5 Cont  Shift+F5 Stop  F6 Break  |  Buttons = microcode F10/F11");
    }
  }

  private static void DrawRomWatch(Session session)
  {
    ImGui.TextUnformatted("ROM watch");
    MicrocodeMapCatalog? map = session.Map;
    if (map is null)
    {
      ImGui.TextDisabled("No microcode map for this model.");
      return;
    }

    float height = MathF.Max(120f, ImGui.GetContentRegionAvail().Y * 0.45f);
    if (ImGui.BeginChild("##debug-rom", new Vector2(0f, height), ImGuiChildFlags.Border))
    {
      if (ImGui.BeginTable(
            "##debug-rom-table",
            4,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
      {
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Addr", ImGuiTableColumnFlags.WidthFixed, 44f);
        ImGui.TableSetupColumn("Word", ImGuiTableColumnFlags.WidthFixed, 44f);
        ImGui.TableSetupColumn("Mnem", ImGuiTableColumnFlags.WidthFixed, 52f);
        ImGui.TableSetupColumn("Handler", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        int pc = session.LastBatch.ProgramCounter;
        int first = Math.Clamp(session.MicrocodeScroll, 0, Math.Max(0, map.WordCount - 1));
        int last = Math.Min(map.WordCount, first + 48);
        for (int address = first; address < last; address++)
        {
          MicrocodeMapEntry? entry = map.TryGetAddress(address);
          if (entry is null)
          {
            continue;
          }

          ImGui.TableNextRow();
          bool atPc = address == pc;
          bool selected = address == session.SelectedAddress;
          if (atPc)
          {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x5540A0FFu);
          }

          ImGui.TableSetColumnIndex(0);
          if (ImGui.Selectable(
                $"{entry.AddressHex}##r{address}",
                selected,
                ImGuiSelectableFlags.SpanAllColumns))
          {
            session.SelectedAddress = address;
            session.FollowRomWatch = false;
          }

          ImGui.TableSetColumnIndex(1);
          ImGui.TextUnformatted(entry.RomWordHex);
          ImGui.TableSetColumnIndex(2);
          ImGui.TextUnformatted(entry.Mnemonic);
          ImGui.TableSetColumnIndex(3);
          ImGui.TextDisabled(ShortHandler(entry.HandlerId));
        }

        ImGui.EndTable();
      }
    }

    ImGui.EndChild();
  }

  private static void DrawRegisters(Session session)
  {
    ImGui.TextUnformatted("Registers");
    FirmwareDebugRegisters? regs = session.TryGetDebugRegisters();
    if (regs is null || regs.Working.Count == 0)
    {
      ImGui.TextDisabled("Not available on this gateway.");
      return;
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
  }

  private static void DrawDump(Session session, ref string dumpStatusMessage)
  {
    ImGui.TextUnformatted("DUMP");
    if (ImGui.Button("Copy dump"))
    {
      string dump = session.CaptureDebugDump();
      ImGui.SetClipboardText(dump);
      dumpStatusMessage = "Copied to clipboard.";
    }

    ImGui.SameLine();
    if (ImGui.Button("Save dump…"))
    {
      string dump = session.CaptureDebugDump();
      string? path = TrySaveDump(dump);
      dumpStatusMessage = path is null ? "Save failed." : $"Saved {Path.GetFileName(path)}";
    }

    if (!string.IsNullOrEmpty(dumpStatusMessage))
    {
      ImGui.TextDisabled(dumpStatusMessage);
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

  private static string ShortHandler(string handlerId)
  {
    int dot = handlerId.LastIndexOf('.');
    return dot >= 0 && dot + 1 < handlerId.Length ? handlerId[(dot + 1)..] : handlerId;
  }
}
