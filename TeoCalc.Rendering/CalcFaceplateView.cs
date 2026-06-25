using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class CalcFaceplateView
{
  public static void Draw(CalcExplorerSession session)
  {
    if (!session.SupportsCpu || session.Vocabulary is null)
    {
      ImGui.TextDisabled("Faceplate requires a Classic CPU model with vocabulary.");
      return;
    }

    ImGui.Text("Faceplate");
    ImGui.BeginChild("faceplate", new System.Numerics.Vector2(0, 220), ImGuiChildFlags.Border);

    for (int row = 0; row < CalcFaceplateLayout.Rows; row++)
    {
      for (int column = 0; column < CalcFaceplateLayout.Columns; column++)
      {
        if (column > 0)
        {
          ImGui.SameLine();
        }

        int index = CalcFaceplateLayout.ToIndex(row, column);
        if (index >= session.Vocabulary.KeyChart.Count)
        {
          continue;
        }

        ProgramKeyEntry key = session.Vocabulary.KeyChart[index];
        string label = CalcFaceplateLayout.LabelForKey(key, session.Vocabulary);
        if (string.IsNullOrEmpty(label))
        {
          ImGui.Dummy(new System.Numerics.Vector2(52, 26));
          continue;
        }

        if (ImGui.Button($"{label}##key{index}", new System.Numerics.Vector2(52, 26)))
        {
          session.PressKeyAndRun((byte)key.KeyCode);
        }

        if (ImGui.IsItemHovered() && key.KeyCode > 0)
        {
          try
          {
            ProgramStepEntry step = session.Vocabulary.ResolveCode(key.KeyCode);
            ImGui.SetTooltip($"{step.Mnemonic}  (code {key.KeyCode})");
          }
          catch (KeyNotFoundException)
          {
            ImGui.SetTooltip($"code {key.KeyCode}");
          }
        }
      }
    }

    ImGui.EndChild();
  }
}
