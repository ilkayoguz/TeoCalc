using ImGuiNET;
using System.Numerics;

namespace TeoCalc.Rendering;

public static class CalculatorLauncherView
{
  private const int ColumnCount = 4;

  public static bool Draw(CalculatorLauncherModel launcher)
  {
    bool openSelected = UpdateKeyboard(launcher);

    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
    ImGui.Begin(
      "TeoCalc Launcher",
      ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);

    ImGui.TextUnformatted("TeoCalc");
    ImGui.TextDisabled("Classic calculator launcher");
    ImGui.Separator();

    ImGui.TextWrapped(
      "Open TeoCalc launches the in-app explorer. Open Reference launches the local Panamatik reference app when a runnable build is present.");
    ImGui.Spacing();

    if (ImGui.BeginTable("calculator-launcher", ColumnCount, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
    {
      for (int index = 0; index < launcher.Entries.Count; index++)
      {
        ImGui.TableNextColumn();
        openSelected |= DrawEntryCard(launcher, index, launcher.Entries[index]);
      }

      ImGui.EndTable();
    }

    ImGui.Separator();
    ImGui.TextDisabled(launcher.StatusLine);
    ImGui.SameLine();
    ImGui.TextDisabled("Arrow keys move, Enter opens TeoCalc when available.");

    ImGui.End();
    return openSelected;
  }

  private static bool UpdateKeyboard(CalculatorLauncherModel launcher)
  {
    if (ImGui.GetIO().WantTextInput)
    {
      return false;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(-1, 0, ColumnCount);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(1, 0, ColumnCount);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(0, -1, ColumnCount);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(0, 1, ColumnCount);
    }

    return ImGui.IsKeyPressed(ImGuiKey.Enter, repeat: false) && launcher.TryOpenSelectedTeoCalc(out _);
  }

  private static bool DrawEntryCard(CalculatorLauncherModel launcher, int index, CalculatorLauncherEntry entry)
  {
    bool selected = index == launcher.SelectedIndex;
    Vector4 cardColor = selected
      ? new Vector4(0.20f, 0.28f, 0.36f, 1f)
      : new Vector4(0.15f, 0.15f, 0.18f, 1f);

    ImGui.PushID(entry.ModelId);
    ImGui.PushStyleColor(ImGuiCol.ChildBg, cardColor);
    ImGui.BeginChild("card", new Vector2(0, 168), ImGuiChildFlags.Border);

    if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
    {
      launcher.Select(index);
    }

    ImGui.TextUnformatted(entry.DisplayName);
    ImGui.TextDisabled(entry.TeoCalcStatus);
    ImGui.TextDisabled(entry.ReferenceStatus);
    ImGui.Spacing();

    bool open = false;
    if (entry.CanOpenTeoCalc)
    {
      if (ImGui.Button("Open TeoCalc", new Vector2(-1, 30)))
      {
        launcher.Select(index);
        open = launcher.TryOpenSelectedTeoCalc(out _);
      }
    }
    else
    {
      ImGui.TextDisabled("TeoCalc pending");
    }

    if (entry.CanOpenReference)
    {
      if (ImGui.Button("Open Reference", new Vector2(-1, 30)))
      {
        launcher.OpenReference(index);
      }
    }
    else
    {
      ImGui.TextDisabled(entry.Reference is null ? "Reference pending" : "Reference build pending");
    }

    ImGui.EndChild();
    ImGui.PopStyleColor();
    ImGui.PopID();
    return open;
  }
}
