using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class CalcExplorerView
{
  public static void Draw(CalcExplorerSession session)
  {
    ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
    ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
    ImGui.Begin(
      "TeoCalc Explorer",
      ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);

    DrawToolbar(session);
    if (session.SupportsCpu)
    {
      CalcFaceplateView.Draw(session);
      ImGui.Separator();
    }

    if (ImGui.BeginTable("split", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
    {
      ImGui.TableSetupColumn("Microcode", ImGuiTableColumnFlags.WidthStretch, 0.62f);
      ImGui.TableSetupColumn("Program", ImGuiTableColumnFlags.WidthStretch, 0.38f);
      ImGui.TableHeadersRow();
      ImGui.TableNextRow();

      ImGui.TableSetColumnIndex(0);
      DrawMicrocodePanel(session);

      ImGui.TableSetColumnIndex(1);
      DrawProgramPanel(session);

      ImGui.EndTable();
    }

    ImGui.End();
  }

  private static void DrawToolbar(CalcExplorerSession session)
  {
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

    if (session.SupportsCpu)
    {
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
      string display = ClassicDisplayFormatter.FormatXRegister(session.Cpu!, session.ProgramMode);
      ImGui.Text($"X: {display}");
      ImGui.SameLine();
      ImGui.TextDisabled(
        $"PC={session.Cpu!.State.ProgramCounter:X4}  ROM={session.Cpu.State.Rom}  steps={session.Cpu.StepCount}");
    }
    else
    {
      ImGui.SameLine();
      ImGui.TextDisabled($"{session.Model.Family} — ROM study (CPU pending)");
    }
  }

  private static void DrawMicrocodePanel(CalcExplorerSession session)
  {
    ImGui.Text("Microcode ROM (.mlist)");
    ImGui.BeginChild("microcode", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);

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
    ImGui.Text("User program (act_ram)");
    ImGui.BeginChild("program", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);

    if (session.Cpu is null)
    {
      ImGui.TextDisabled("User program requires a Classic CPU model.");
      ImGui.EndChild();
      return;
    }

    if (session.Model.Program is null)
    {
      ImGui.TextDisabled("This model has no user program layer in Panamatik.");
      ImGui.EndChild();
      return;
    }

    foreach (ClassicProgramLine line in ClassicProgramListing.Enumerate(session.Cpu.Program))
    {
      ImGui.Text(line.ToString());
    }

    ImGui.Separator();
    ImGui.Text($"Pointer @ {session.Cpu.Program.PointerPosition()}");
    ImGui.Text($"Buffer code: {session.Cpu.State.Buffer}");
    ImGui.EndChild();
  }
}
