using ImGuiNET;
using System.Numerics;

namespace TeoCalc.Rendering;

public static class CalculatorLauncherView
{
  private const int ColumnCount = 5;

  public static bool Draw(CalculatorLauncherModel launcher)
  {
    bool openSelected = UpdateKeyboard(launcher);

    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 4f));
    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
    ImGui.Begin(
      "TeoCalc Launcher",
      ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);

    ImGui.AlignTextToFramePadding();
    ImGui.TextUnformatted("TeoCalc");
    ImGui.SameLine();
    ImGui.TextDisabled("· pick a model");

    ImGui.Spacing();

    if (ImGui.BeginTable("calculator-launcher", ColumnCount, ImGuiTableFlags.SizingStretchSame))
    {
      for (int index = 0; index < launcher.Entries.Count; index++)
      {
        ImGui.TableNextColumn();
        openSelected |= DrawModelButton(launcher, index, launcher.Entries[index]);
      }

      ImGui.EndTable();
    }

    ImGui.Spacing();
    ImGui.TextDisabled("Enter / click opens · right-click: Reference");

    ImGui.End();
    ImGui.PopStyleVar(3);
    return openSelected;
  }

  private static bool UpdateKeyboard(CalculatorLauncherModel launcher)
  {
    if (ImGui.GetIO().WantTextInput)
    {
      return false;
    }

    int columns = ColumnCount;
    if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(-1, 0, columns);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(1, 0, columns);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(0, -1, columns);
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, repeat: true))
    {
      launcher.MoveSelectionByGrid(0, 1, columns);
    }

    return ImGui.IsKeyPressed(ImGuiKey.Enter, repeat: false) && launcher.TryOpenSelectedTeoCalc(out _);
  }

  private static bool DrawModelButton(
    CalculatorLauncherModel launcher,
    int index,
    CalculatorLauncherEntry entry)
  {
    bool selected = index == launcher.SelectedIndex;
    bool open = false;
    Vector2 size = new(-1f, 26f);

    ImGui.PushID(entry.ModelId);
    if (selected)
    {
      ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.28f, 0.40f, 0.52f, 1f));
      ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.34f, 0.48f, 0.62f, 1f));
      ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.22f, 0.34f, 0.46f, 1f));
    }
    else if (!entry.CanOpenTeoCalc)
    {
      ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.18f, 0.18f, 0.20f, 1f));
      ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.22f, 0.22f, 0.24f, 1f));
      ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.16f, 0.16f, 0.18f, 1f));
    }

    int styleCount = selected || !entry.CanOpenTeoCalc ? 3 : 0;
    if (ImGui.Button(entry.DisplayName, size))
    {
      launcher.Select(index);
      if (entry.CanOpenTeoCalc)
      {
        open = launcher.TryOpenSelectedTeoCalc(out _);
      }
    }

    if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && entry.CanOpenReference)
    {
      launcher.OpenReference(index);
    }

    if (ImGui.IsItemHovered())
    {
      launcher.Select(index);
      ImGui.SetTooltip(
        entry.CanOpenTeoCalc
          ? $"{entry.DisplayName}\nClick / Enter: TeoCalc"
          : $"{entry.DisplayName}\nTeoCalc pending");
    }

    if (styleCount > 0)
    {
      ImGui.PopStyleColor(styleCount);
    }

    ImGui.PopID();
    return open;
  }
}
