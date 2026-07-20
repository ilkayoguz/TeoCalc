using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Floating ImGui printer strip panel for models with a thermal printer.</summary>
public static class CalcPrinterPanelComponent
{
  public static void Draw(
    ref bool open,
    IReadOnlyList<string> lines,
    Action? onTestPrint,
    Action? onClear)
  {
    ImGui.SetNextWindowSize(new System.Numerics.Vector2(320f, 260f), ImGuiCond.FirstUseEver);
    if (!ImGui.Begin("Printer", ref open, ImGuiWindowFlags.NoCollapse))
    {
      ImGui.End();
      return;
    }

    if (onTestPrint is not null && ImGui.Button("Test print"))
    {
      onTestPrint();
    }

    if (onClear is not null)
    {
      ImGui.SameLine();
      if (ImGui.Button("Clear"))
      {
        onClear();
      }
    }

    ImGui.Separator();
    if (ImGui.BeginChild("##printer-log", System.Numerics.Vector2.Zero, ImGuiChildFlags.None))
    {
      if (lines.Count == 0)
      {
        ImGui.TextDisabled("(empty)");
      }
      else
      {
        for (int i = 0; i < lines.Count; i++)
        {
          ImGui.TextUnformatted(lines[i]);
        }

        if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 1f)
        {
          ImGui.SetScrollHereY(1f);
        }
      }
    }

    ImGui.EndChild();
    ImGui.End();
  }
}
