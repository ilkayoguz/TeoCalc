using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Modal Studio card-file browser: list, OK/Cancel, Enter/Esc, double-click load,
/// folder button to change directory. Full path shown read-only.
/// </summary>
public static class CalcCardFilePicker
{
  private static bool s_open;
  private static bool s_requestOpen;
  private static string s_directory = string.Empty;
  private static string[] s_files = [];
  private static int s_selected = -1;
  private static string s_filterHint = "*.t65;*.t67;*.xml;*.json;*.plist;*.rpn65;*.t6x";

  private static readonly string[] Extensions =
  [
    ".t65",
    ".t67",
    ".xml",
    ".json",
    ".plist",
    ".rpn65",
    ".t6x",
  ];

  public static bool IsOpen => s_open || s_requestOpen;

  public static void Open(string? initialDirectory)
  {
    string dir = initialDirectory ?? string.Empty;
    if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
    {
      dir = CalcCardPanelComponent.DefaultCardsDirectory();
    }

    Directory.CreateDirectory(dir);
    s_directory = Path.GetFullPath(dir);
    RefreshFiles();
    s_requestOpen = true;
  }

  /// <summary>
  /// Draw the modal when open. Returns <c>true</c> when the user confirms a file
  /// (path in <paramref name="pickedPath"/>).
  /// </summary>
  public static bool Draw(out string? pickedPath)
  {
    pickedPath = null;
    if (s_requestOpen)
    {
      ImGui.OpenPopup("##studio-card-file-picker");
      s_open = true;
      s_requestOpen = false;
    }

    if (!s_open && !ImGui.IsPopupOpen("##studio-card-file-picker"))
    {
      return false;
    }

    ImGuiIOPtr io = ImGui.GetIO();
    Vector2 center = io.DisplaySize * 0.5f;
    ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
    ImGui.SetNextWindowSize(new Vector2(520f, 420f), ImGuiCond.Appearing);

    CalcStudioChromeStyle.PushToolbar();
    bool visible = ImGui.BeginPopupModal(
      "##studio-card-file-picker",
      ref s_open,
      ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
    if (!visible)
    {
      CalcStudioChromeStyle.PopToolbar();
      s_open = false;
      return false;
    }

    ImGui.TextUnformatted("Load card");
    ImGui.Separator();

    float folderW = ImGui.CalcTextSize("Folder…").X + ImGui.GetStyle().FramePadding.X * 2f + 8f;
    ImGui.SetNextItemWidth(MathF.Max(80f, ImGui.GetContentRegionAvail().X - folderW - ImGui.GetStyle().ItemSpacing.X));
    string pathDisplay = s_directory;
    ImGui.InputText("##picker-dir", ref pathDisplay, 1024, ImGuiInputTextFlags.ReadOnly);
    ImGui.SameLine();
    if (ImGui.Button("Folder…"))
    {
      if (CalcNativeFileDialog.TryPickFolder(s_directory, out string folder)
          && !string.IsNullOrWhiteSpace(folder)
          && Directory.Exists(folder))
      {
        s_directory = Path.GetFullPath(folder);
        RefreshFiles();
      }
    }

    ImGui.TextDisabled(s_filterHint);
    ImGui.BeginChild(
      "##picker-list",
      new Vector2(0f, -ImGui.GetFrameHeightWithSpacing() * 1.6f),
      ImGuiChildFlags.Border);

    if (s_files.Length == 0)
    {
      ImGui.TextDisabled("No card files in this folder.");
    }
    else
    {
      for (int i = 0; i < s_files.Length; i++)
      {
        bool selected = i == s_selected;
        string name = Path.GetFileName(s_files[i]);
        if (ImGui.Selectable(name, selected, ImGuiSelectableFlags.AllowDoubleClick))
        {
          s_selected = i;
          if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
          {
            pickedPath = s_files[i];
            CloseModal();
            ImGui.EndChild();
            ImGui.EndPopup();
            CalcStudioChromeStyle.PopToolbar();
            return true;
          }
        }
      }
    }

    ImGui.EndChild();

    bool canOk = s_selected >= 0 && s_selected < s_files.Length;
    if (!canOk)
    {
      ImGui.BeginDisabled();
    }

    CalcStudioChromeStyle.PushPrimary();
    bool ok = ImGui.Button("OK", new Vector2(96f, 0f));
    CalcStudioChromeStyle.PopPrimary();
    if (!canOk)
    {
      ImGui.EndDisabled();
    }

    ImGui.SameLine();
    bool cancel = ImGui.Button("Cancel", new Vector2(96f, 0f));

    if (ImGui.IsKeyPressed(ImGuiKey.Enter) && canOk)
    {
      ok = true;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.Escape) || cancel)
    {
      CloseModal();
      ImGui.EndPopup();
      CalcStudioChromeStyle.PopToolbar();
      return false;
    }

    if (ok && canOk)
    {
      pickedPath = s_files[s_selected];
      CloseModal();
      ImGui.EndPopup();
      CalcStudioChromeStyle.PopToolbar();
      return true;
    }

    ImGui.EndPopup();
    CalcStudioChromeStyle.PopToolbar();
    return false;
  }

  private static void CloseModal()
  {
    s_open = false;
    ImGui.CloseCurrentPopup();
  }

  private static void RefreshFiles()
  {
    s_selected = -1;
    if (!Directory.Exists(s_directory))
    {
      s_files = [];
      return;
    }

    try
    {
      s_files = Directory.EnumerateFiles(s_directory)
        .Where(IsCardFile)
        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }
    catch
    {
      s_files = [];
    }
  }

  private static bool IsCardFile(string path)
  {
    string ext = Path.GetExtension(path);
    for (int i = 0; i < Extensions.Length; i++)
    {
      if (ext.Equals(Extensions[i], StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }
}
