using System.Globalization;
using System.Numerics;
using ImGuiNET;
using TeoCalc.Formats;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Studio Card tab: every authoring field from <c>.t65</c>/<c>.t67</c>/<c>.t6x</c>
/// ([General] + [Label]). Program bytes stay on the Code tab; DATA stays in the footer.
/// </summary>
public static class StudioCardInfoPane
{
  private static readonly string[] StripKeys = ["A", "B", "C", "D", "E"];

  private static string s_syncKey = string.Empty;
  private static string s_title = string.Empty;
  private static string s_category = string.Empty;
  private static string s_author = string.Empty;
  private static string s_profile = string.Empty;
  private static string s_description = string.Empty;
  private static string s_usage = string.Empty;
  private static string s_runHint = string.Empty;
  private static string s_created = string.Empty;
  private static string s_encoding = CardCodeEncoding.Mnemonic;
  private static readonly string[] s_labels = ["", "", "", "", ""];
  private static readonly string[] s_hints = ["", "", "", "", ""];

  public static void Draw(Session session)
  {
    if (!session.SupportsCardProgram)
    {
      ImGui.TextDisabled("Card metadata is not available for this model.");
      return;
    }

    SyncBuffers(session);

    ImGui.TextUnformatted("Card info");
    ImGui.TextDisabled("Fields map to .t65 / .t67 [General] + [Label]. Save writes them with the program.");
    ImGui.Spacing();

    bool dirty = false;
    dirty |= Field("Title", ref s_title);
    dirty |= Field("Category", ref s_category);
    dirty |= Field("Author", ref s_author);
    dirty |= Field("Profile", ref s_profile);

    ImGui.Spacing();
    ImGui.TextDisabled("CodeEncoding");
    if (ImGui.BeginCombo("##card-encoding", s_encoding))
    {
      if (ImGui.Selectable(CardCodeEncoding.Mnemonic, s_encoding == CardCodeEncoding.Mnemonic))
      {
        s_encoding = CardCodeEncoding.Mnemonic;
        dirty = true;
      }

      if (ImGui.Selectable(CardCodeEncoding.Machine, s_encoding == CardCodeEncoding.Machine))
      {
        s_encoding = CardCodeEncoding.Machine;
        dirty = true;
      }

      ImGui.EndCombo();
    }

    ImGui.Spacing();
    dirty |= Multiline("Description", ref s_description, 4);
    dirty |= Multiline("Usage", ref s_usage, 4);
    dirty |= Multiline("RunHint", ref s_runHint, 3);
    dirty |= Field("Created", ref s_created);
    if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set("Optional ISO-8601 UTC (yyyy-MM-ddTHH:mm:ssZ). Empty keeps prior / first-save time.");
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.TextUnformatted("Labels (A–E strip)");
    ImGui.TextDisabled("Caption on the faceplate card strip; Hint is the optional tooltip.");
    ImGui.Spacing();

    if (ImGui.BeginTable(
          "##card-labels",
          3,
          ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV))
    {
      ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 28f);
      ImGui.TableSetupColumn("Caption");
      ImGui.TableSetupColumn("Hint");
      ImGui.TableHeadersRow();
      for (int i = 0; i < StripKeys.Length; i++)
      {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(StripKeys[i]);
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        dirty |= ImGui.InputText($"##lbl-cap-{i}", ref s_labels[i], 128);
        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(-1f);
        dirty |= ImGui.InputText($"##lbl-hint-{i}", ref s_hints[i], 256);
      }

      ImGui.EndTable();
    }

    ImGui.Spacing();
    ImGui.Separator();
    ImGui.TextDisabled($"TargetCpu / Model: {session.EngineModelId}");
    if (session.LoadedTeoCard?.Modified is { } modified)
    {
      ImGui.TextDisabled(
        $"Modified (last): {modified.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}");
    }

    ImGui.TextDisabled("Format / SchemaVersion are fixed by the writer on Save.");

    if (dirty)
    {
      Commit(session);
    }
  }

  private static void SyncBuffers(Session session)
  {
    string key = $"{session.LoadedCardPath}|{session.CardInserted}|{session.CardMetadataEpoch}";
    if (string.Equals(key, s_syncKey, StringComparison.Ordinal))
    {
      return;
    }

    s_syncKey = key;
    CardMetadataFields fields = session.GetCardMetadataFields();
    s_title = fields.Title;
    s_category = fields.Category;
    s_author = fields.Author;
    s_profile = fields.Profile;
    s_description = fields.Description;
    s_usage = fields.Usage;
    s_runHint = fields.RunHint;
    s_created = fields.Created;
    s_encoding = fields.CodeEncoding;
    for (int i = 0; i < 5; i++)
    {
      s_labels[i] = i < fields.Labels.Length ? fields.Labels[i] : "";
      s_hints[i] = i < fields.LabelHints.Length ? fields.LabelHints[i] : "";
    }
  }

  private static void Commit(Session session)
  {
    CardMetadataFields fields = new()
    {
      Title = s_title,
      Category = s_category,
      Author = s_author,
      Profile = s_profile,
      Description = s_description,
      Usage = s_usage,
      RunHint = s_runHint,
      Created = s_created,
      CodeEncoding = s_encoding,
      Labels = [.. s_labels],
      LabelHints = [.. s_hints],
    };

    if (!session.TryApplyCardMetadata(fields, out string? error))
    {
      CalcStudioPanelComponent.ShowKeyboardStatus(error ?? "Card info update failed.");
    }
  }

  private static bool Field(string label, ref string value)
  {
    ImGui.TextDisabled(label);
    ImGui.SetNextItemWidth(-1f);
    return ImGui.InputText($"##card-{label}", ref value, 512);
  }

  private static bool Multiline(string label, ref string value, int lines)
  {
    ImGui.TextDisabled(label);
    float h = ImGui.GetTextLineHeightWithSpacing() * Math.Max(2, lines);
    return ImGui.InputTextMultiline(
      $"##card-ml-{label}",
      ref value,
      4096,
      new Vector2(-1f, h));
  }
}
