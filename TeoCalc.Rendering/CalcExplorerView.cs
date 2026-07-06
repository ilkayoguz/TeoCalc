using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcExplorerView
{
  public static void Draw(CalcExplorerSession session, Action? openLauncher = null)
  {
    ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
    ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
    ImGui.Begin(
      "TeoCalc Explorer",
      ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);

    CalcExplorerGlobalKeyboard.Update(session);
    DrawToolbar(session, openLauncher);
    DrawFirmwareInspector(session);
    ImGui.Separator();

    if (ImGui.BeginTable("main", 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
    {
      ImGui.TableSetupColumn("Calculator", ImGuiTableColumnFlags.WidthStretch, 0.36f);
      ImGui.TableSetupColumn("Microcode", ImGuiTableColumnFlags.WidthStretch, 0.40f);
      ImGui.TableSetupColumn("Program", ImGuiTableColumnFlags.WidthStretch, 0.24f);
      ImGui.TableHeadersRow();
      ImGui.TableNextRow();

      ImGui.TableSetColumnIndex(0);
      DrawCalculatorPanel(session);

      ImGui.TableSetColumnIndex(1);
      DrawMicrocodePanel(session);

      ImGui.TableSetColumnIndex(2);
      DrawProgramPanel(session);

      ImGui.EndTable();
    }

    ImGui.End();
  }

  private static void DrawCalculatorPanel(CalcExplorerSession session)
  {
    ImGui.TextUnformatted(session.Model.DisplayName);
    ImGui.SameLine();
    ImGui.TextDisabled("(Panamatik engine)");
    ImGui.PushStyleColor(ImGuiCol.ChildBg, 0);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.BeginChild("calculator", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);
    if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
    {
      ImGui.SetWindowFocus();
    }

    CalcFaceplateView.Draw(session);

    ImGui.EndChild();
    ImGui.PopStyleVar();
    ImGui.PopStyleColor();
  }

  private static void DrawToolbar(CalcExplorerSession session, Action? openLauncher)
  {
    if (openLauncher is not null)
    {
      if (ImGui.Button("Launcher"))
      {
        openLauncher();
      }

      ImGui.SameLine();
    }

    if (ImGui.BeginCombo("Model", session.Model.DisplayName))
    {
      for (int index = 0; index < session.Models.Length; index++)
      {
        bool selected = index == session.ModelIndex;
        if (ImGui.Selectable(session.Models[index], selected))
        {
          session.LoadModel(index);
        }

        if (selected)
        {
          ImGui.SetItemDefaultFocus();
        }
      }

      ImGui.EndCombo();
    }

    ImGui.SameLine();
    CalcFaceplateThemeView.DrawThemeCombo();

    ImGui.SameLine();
    if (ImGui.Button("Step"))
    {
      session.StepCpu();
    }

    ImGui.SameLine();
    if (ImGui.Button("Reset"))
    {
      session.ResetCpu();
    }

    ImGui.SameLine();
    bool programMode = session.ProgramMode;
    if (ImGui.Checkbox("PRGM", ref programMode))
    {
      session.ProgramMode = programMode;
    }

    ImGui.SameLine();
    ImGui.TextDisabled(
      $"Engine=Panamatik  PC={session.LastBatch.ProgramCounter:X4}  ROM={session.LastBatch.Rom}  steps={session.LastBatch.StepCount}");
  }

  private static void DrawFirmwareInspector(CalcExplorerSession session)
  {
    if (!ImGui.CollapsingHeader("Firmware inspector", ImGuiTreeNodeFlags.DefaultOpen))
    {
      return;
    }

    FirmwareDisplaySnapshot display = session.DisplaySnapshot;
    FirmwareBatchSnapshot batch = session.LastBatch;
    string displayText = display.Text.Length == 0 ? "<blank>" : display.Text.Replace(';', '.');
    string displayState = display.Visible
      ? "visible"
      : display.BlankPulse ? "blank pulse" : "off";
    string keyText = batch.ActiveKey is { } key ? $"{key.KeyChartIndex}:{key.KeyCode:X2}" : "-";

    ImGui.TextDisabled(
      $"Display #{display.Revision}: {displayState} \"{displayText}\" @PC={display.ProgramCounter:X4}");
    ImGui.TextDisabled(
      $"Key: active={keyText} held={(batch.KeyLineHeld ? "yes" : "no")} buffer={batch.KeyBuffer:X2}");
    ImGui.TextDisabled(
      $"CPU: PC={batch.ProgramCounter:X4} ROM={batch.Grp:X1}{batch.Rom:X1} P={batch.P:X1} F={(byte)batch.Flags:X2} S={batch.Status:X3}");
    ImGui.TextDisabled($"Engine: {batch.LastHandlerId ?? "-"}");
    if (session.LoadWarnings.Count > 0)
    {
      ImGui.TextColored(new System.Numerics.Vector4(1f, 0.72f, 0.2f, 1f), string.Join("  ", session.LoadWarnings));
    }
  }

  private static void DrawMicrocodePanel(CalcExplorerSession session)
  {
    ImGui.Text("Microcode ROM (.mlist)");
    ImGui.BeginChild("microcode", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);

    if (session.Map is null)
    {
      ImGui.TextDisabled("Microcode map is not available for this model yet.");
      ImGui.EndChild();
      return;
    }

    if (ImGui.BeginTable("rom", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
    {
      ImGui.TableSetupScrollFreeze(0, 1);
      ImGui.TableSetupColumn("Addr", ImGuiTableColumnFlags.WidthFixed, 52);
      ImGui.TableSetupColumn("Word", ImGuiTableColumnFlags.WidthFixed, 52);
      ImGui.TableSetupColumn("Mnem", ImGuiTableColumnFlags.WidthFixed, 48);
      ImGui.TableSetupColumn("Handler", ImGuiTableColumnFlags.WidthStretch);
      ImGui.TableSetupColumn("Ref", ImGuiTableColumnFlags.WidthFixed, 72);
      ImGui.TableHeadersRow();

      int first = session.MicrocodeScroll;
      int last = Math.Min(session.Map.WordCount, first + 256);
      for (int address = first; address < last; address++)
      {
        MicrocodeMapEntry? entry = session.Map.TryGetAddress(address);
        if (entry is null)
        {
          continue;
        }

        ImGui.TableNextRow();
        bool selected = address == session.SelectedAddress;
        ImGui.TableSetColumnIndex(0);
        if (ImGui.Selectable($"{entry.AddressHex}", selected, ImGuiSelectableFlags.SpanAllColumns))
        {
          session.SelectedAddress = address;
        }

        ImGui.TableSetColumnIndex(1);
        ImGui.Text(entry.RomWordHex);
        ImGui.TableSetColumnIndex(2);
        ImGui.Text(entry.Mnemonic);
        ImGui.TableSetColumnIndex(3);
        ImGui.Text(entry.HandlerId);
        ImGui.TableSetColumnIndex(4);
        MicrocodeCrossRefEntry? cross = session.CrossRef?.TryGetHandler(entry.HandlerId);
        ImGui.TextDisabled(cross?.NonpareilMnemonic ?? "");
      }

      ImGui.EndTable();
    }

    ImGui.EndChild();
  }

  private static void DrawProgramPanel(CalcExplorerSession session)
  {
    ImGui.Text("User program");
    ImGui.BeginChild("program", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);

    if (session.Model.Program is null)
    {
      ImGui.TextDisabled("User program view is not available for this model yet.");
    }
    else
    {
      ImGui.TextDisabled("Program listing will follow Panamatik act_ram in a later phase.");
      ImGui.TextDisabled($"Max steps: {session.Model.Program.MaxSteps}");
    }

    ImGui.EndChild();
  }
}
