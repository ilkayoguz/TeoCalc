using ImGuiNET;
using System.Text;
using TeoCalc.Core;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Mag-card load/save UI — inline side strip or legacy floating window.</summary>
public static class CalcCardPanelComponent
{
  public const float PreferredWidthRef = 300f;

  public const string SampleHp65T65FileName = "teo-65-add123.t65";

  public const string SampleHp67T67FileName = "teo-67-add01.t67";

  public const string SampleCuveSoftPlistFileName = "cuveesoft-std-01b-day-of-week.plist";

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

  public static string SampleCardsDirectory() =>
    TeoCalcPaths.ResourcePath("Samples/Cards");

  /// <summary>Copies bundled sample cards into the user Cards folder when missing.</summary>
  public static void EnsureUserSampleCards()
  {
    string sampleDir = SampleCardsDirectory();
    if (!Directory.Exists(sampleDir))
    {
      return;
    }

    string userDir = DefaultCardsDirectory();
    Directory.CreateDirectory(userDir);
    foreach (string source in Directory.EnumerateFiles(sampleDir))
    {
      string ext = Path.GetExtension(source);
      if (!ext.Equals(".t65", StringComparison.OrdinalIgnoreCase)
          && !ext.Equals(".t67", StringComparison.OrdinalIgnoreCase)
          && !ext.Equals(".plist", StringComparison.OrdinalIgnoreCase)
          && !ext.Equals(".rpn65", StringComparison.OrdinalIgnoreCase)
          && !ext.Equals(".t6x", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string fileName = Path.GetFileName(source);
      string dest = Path.Combine(userDir, fileName);
      bool bundledSample = string.Equals(fileName, SampleHp65T65FileName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, SampleHp67T67FileName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileName, SampleCuveSoftPlistFileName, StringComparison.OrdinalIgnoreCase);
      if (!File.Exists(dest) || bundledSample)
      {
        File.Copy(source, dest, overwrite: bundledSample);
      }
    }
  }

  public static string DefaultCardPathForModel(string catalogOrEngineId)
  {
    EnsureUserSampleCards();
    string id = catalogOrEngineId.Trim().ToUpperInvariant();
    string fileName = id.Contains("67", StringComparison.Ordinal)
      ? SampleHp67T67FileName
      : id.Contains("65", StringComparison.Ordinal)
        ? SampleHp65T65FileName
        : $"program{DefaultExtensionForModel(id)}";
    return Path.Combine(DefaultCardsDirectory(), fileName);
  }

  private static string DefaultExtensionForModel(string id) =>
    id.Contains("67", StringComparison.Ordinal) ? ".t67" : ".t65";

  public static void DrawInline(
    ref string pathBuffer,
    ref string statusMessage,
    bool canLoadSave,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard,
    string fileExtension = ".t65",
    int programCapacity = ClassicCardProgramIo.ProgramCapacity,
    bool cardInserted = false,
    string? loadedCardPath = null,
    TeoCardDocument? loadedTeoCard = null,
    Action? onEject = null)
  {
    string ext = NormalizeExtension(fileExtension);
    ImGui.TextUnformatted("Magnetic card");
    if (cardInserted && !string.IsNullOrWhiteSpace(loadedTeoCard?.Title))
    {
      ImGui.TextWrapped(loadedTeoCard.Title);
    }
    else if (cardInserted && !string.IsNullOrWhiteSpace(loadedCardPath))
    {
      ImGui.TextDisabled(Path.GetFileName(loadedCardPath));
    }
    else
    {
      ImGui.TextDisabled($".t65/.t67 · Export CuveSoft/Teo ({ext})");
    }

    if (cardInserted && loadedTeoCard is not null)
    {
      if (!string.IsNullOrWhiteSpace(loadedTeoCard.Description))
      {
        ImGui.Spacing();
        ImGui.TextWrapped(loadedTeoCard.Description);
      }

      if (!string.IsNullOrWhiteSpace(loadedTeoCard.Usage))
      {
        ImGui.Spacing();
        ImGui.TextDisabled("Usage");
        ImGui.TextWrapped(loadedTeoCard.Usage);
      }

      if (!string.IsNullOrWhiteSpace(loadedTeoCard.RunHint))
      {
        ImGui.Spacing();
        ImGui.TextDisabled("Run");
        ImGui.TextWrapped(loadedTeoCard.RunHint);
      }
    }

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
        string? initialDir = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
        {
          initialDir = DefaultCardsDirectory();
        }

        Directory.CreateDirectory(initialDir);
        string suggested = string.IsNullOrWhiteSpace(path)
          ? loadedTeoCard?.Title is { Length: > 0 } title
            ? $"{SanitizeFileName(title)}{ext}"
            : $"program{ext}"
          : Path.GetFileName(path);

        if (CalcNativeFileDialog.TrySaveCardFile(initialDir, suggested, out string picked)
            && !string.IsNullOrWhiteSpace(picked))
        {
          path = picked;
          pathBuffer = picked;
        }
        else if (string.IsNullOrWhiteSpace(path))
        {
          path = Path.Combine(initialDir, suggested);
          pathBuffer = path;
        }
        else if (!Path.HasExtension(path)
                 || CuveSoftCardPlistFormat.IsCuveSoftCardPath(path)
                 || TeoCardProgramFormat.IsTeoCardPath(path))
        {
          path = Path.ChangeExtension(path, ext) ?? path + ext;
          pathBuffer = path;
        }

        string? error = saveCard(path);
        statusMessage = error is null ? $"Saved: {Path.GetFileName(path)}" : error;
      }

      ImGui.SameLine();
      ImGui.PushFont(CalcFaceplateFonts.Arial);
      if (ImGui.Button("Browse\u2026"))
      {
        string? initialDir = Path.GetDirectoryName(pathBuffer.Trim());
        if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
        {
          initialDir = DefaultCardsDirectory();
        }

        Directory.CreateDirectory(initialDir);
        if (CalcNativeFileDialog.TryPickCardFile(initialDir, out string picked) && !string.IsNullOrWhiteSpace(picked))
        {
          pathBuffer = picked;
          statusMessage = $"Selected: {Path.GetFileName(picked)}";
        }
      }

      ImGui.PopFont();

      ImGui.SameLine();
      if (ImGui.Button("Export"))
      {
        ImGui.OpenPopup("##card-export-menu");
      }

      if (cardInserted && onEject is not null)
      {
        ImGui.SameLine();
        if (ImGui.Button("Eject"))
        {
          onEject();
          statusMessage = "Card ejected.";
        }
      }

      if (ImGui.BeginPopup("##card-export-menu"))
      {
        if (ImGui.MenuItem("CuveSoft (.xml)"))
        {
          RunExport(ref pathBuffer, ref statusMessage, saveCard, loadedTeoCard, CardExportFormat.CuveSoftXml);
        }

        if (ImGui.MenuItem("Teo (.json)"))
        {
          RunExport(ref pathBuffer, ref statusMessage, saveCard, loadedTeoCard, CardExportFormat.TeoJson);
        }

        ImGui.EndPopup();
      }
    }

    if (!string.IsNullOrEmpty(statusMessage))
    {
      ImGui.Spacing();
      ImGui.TextWrapped(statusMessage);
    }

    ImGui.Spacing();
    ImGui.TextDisabled(
      $"Save: .t65/.t67 · Export: CuveSoft (.xml) / Teo (.json) · Import: .xml/.json/.plist ({programCapacity} steps)");
    if (!cardInserted)
    {
      ImGui.TextWrapped("After Load (RUN): use Run hint above or RCL for DATA constants.");
    }
  }

  private static void RunExport(
    ref string pathBuffer,
    ref string statusMessage,
    Func<string, string?> saveCard,
    TeoCardDocument? loadedTeoCard,
    CardExportFormat format)
  {
    string exportExt = format == CardExportFormat.CuveSoftXml
      ? CuveSoftCardPlistFormat.Extension
      : TeoCardProgramFormat.Extension;

    string? initialDir = Path.GetDirectoryName(pathBuffer.Trim());
    if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
    {
      initialDir = DefaultCardsDirectory();
    }

    Directory.CreateDirectory(initialDir);
    string baseName = loadedTeoCard?.Title is { Length: > 0 } title
      ? SanitizeFileName(title)
      : Path.GetFileNameWithoutExtension(pathBuffer.Trim());
    if (string.IsNullOrWhiteSpace(baseName))
    {
      baseName = "program";
    }

    string suggested = $"{baseName}{exportExt}";
    string path;
    if (CalcNativeFileDialog.TryExportCardFile(initialDir, suggested, format, out string picked)
        && !string.IsNullOrWhiteSpace(picked))
    {
      path = picked;
    }
    else
    {
      // Dialog cancelled or unavailable: do not write without an explicit path.
      return;
    }

    if (!Path.HasExtension(path)
        || !string.Equals(Path.GetExtension(path), exportExt, StringComparison.OrdinalIgnoreCase))
    {
      path = Path.ChangeExtension(path, exportExt) ?? path + exportExt;
    }

    pathBuffer = path;
    string? error = saveCard(path);
    string label = format == CardExportFormat.CuveSoftXml ? "CuveSoft" : "Teo";
    statusMessage = error is null ? $"Exported {label}: {Path.GetFileName(path)}" : error;
  }

  [Obsolete("Use inline side panel via CalcCapabilitySidePanelComponent.")]
  public static void Draw(
    ref bool open,
    ref string pathBuffer,
    ref string statusMessage,
    bool canLoadSave,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard,
    string fileExtension = ".t65",
    int programCapacity = ClassicCardProgramIo.ProgramCapacity)
  {
    ImGui.SetNextWindowSize(new System.Numerics.Vector2(360f, 200f), ImGuiCond.FirstUseEver);
    if (!ImGui.Begin("Card", ref open, ImGuiWindowFlags.NoCollapse))
    {
      ImGui.End();
      return;
    }

    DrawInline(
      ref pathBuffer,
      ref statusMessage,
      canLoadSave,
      loadCard,
      saveCard,
      fileExtension,
      programCapacity);
    ImGui.End();
  }

  private static string NormalizeExtension(string fileExtension)
  {
    if (string.IsNullOrWhiteSpace(fileExtension))
    {
      return ".t65";
    }

    return fileExtension.StartsWith('.') ? fileExtension : "." + fileExtension;
  }

  private static string SanitizeFileName(string title)
  {
    char[] invalid = Path.GetInvalidFileNameChars();
    StringBuilder builder = new(title.Length);
    foreach (char c in title)
    {
      builder.Append(invalid.Contains(c) ? '_' : c);
    }

    string sanitized = builder.ToString().Trim();
    return string.IsNullOrWhiteSpace(sanitized) ? "card" : sanitized;
  }
}
