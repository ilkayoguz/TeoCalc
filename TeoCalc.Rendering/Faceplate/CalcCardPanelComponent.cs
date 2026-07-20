using ImGuiNET;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Floating ImGui card reader panel for models with a mag-card slot.</summary>
public static class CalcCardPanelComponent
{
  public static string DefaultCardsDirectory()
  {
    string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    if (!string.IsNullOrWhiteSpace(documents))
    {
      return Path.Combine(documents, "TeoCalc", "Cards");
    }

    return Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      "TeoCalc",
      "Cards");
  }

  public static void Draw(
    ref bool open,
    ref string pathBuffer,
    ref string statusMessage,
    bool canLoadSave,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard)
  {
    ImGui.SetNextWindowSize(new System.Numerics.Vector2(360f, 200f), ImGuiCond.FirstUseEver);
    if (!ImGui.Begin("Card", ref open, ImGuiWindowFlags.NoCollapse))
    {
      ImGui.End();
      return;
    }

    ImGui.TextWrapped("Magnetic card — load / save HP-65 ASCII program (.hp65).");
    ImGui.Separator();

    ImGui.SetNextItemWidth(-1f);
    ImGui.InputText("##card-path", ref pathBuffer, 512);

    if (!canLoadSave)
    {
      ImGui.TextDisabled("Program memory not available for this engine.");
    }
    else
    {
      if (ImGui.Button("Load"))
      {
        string? error = loadCard(pathBuffer.Trim());
        statusMessage = error is null ? $"Loaded: {Path.GetFileName(pathBuffer)}" : error;
      }

      ImGui.SameLine();
      if (ImGui.Button("Save"))
      {
        string path = pathBuffer.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
          Directory.CreateDirectory(DefaultCardsDirectory());
          path = Path.Combine(DefaultCardsDirectory(), "program.hp65");
          pathBuffer = path;
        }
        else if (!Path.HasExtension(path))
        {
          path += ".hp65";
          pathBuffer = path;
        }

        string? error = saveCard(path);
        statusMessage = error is null ? $"Saved: {Path.GetFileName(path)}" : error;
      }

      ImGui.SameLine();
      if (ImGui.Button("Browse…"))
      {
        Directory.CreateDirectory(DefaultCardsDirectory());
        pathBuffer = Path.Combine(DefaultCardsDirectory(), "program.hp65");
        statusMessage = $"Default folder: {DefaultCardsDirectory()}";
      }
    }

    if (!string.IsNullOrEmpty(statusMessage))
    {
      ImGui.Spacing();
      ImGui.TextWrapped(statusMessage);
    }

    ImGui.TextDisabled($"Format: HP65 PROGRAM/DATA ({ClassicCardProgramIo.ProgramCapacity} steps)");
    ImGui.End();
  }
}
