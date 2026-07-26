using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Shared microcode ROM watch table — Studio ROM tab (composite with editor) and Debug strip.
/// </summary>
public static class CalcRomWatchPane
{
  /// <param name="height">Child height; when ≤0 fills remaining content region.</param>
  /// <param name="showTitle">When false, caller draws its own heading.</param>
  public static void Draw(Session session, float height = 0f, bool showTitle = true)
  {
    if (showTitle)
    {
      CalcDebugChrome.SectionHeader("ROM watch");
    }

    MicrocodeMapCatalog? map = session.Map;

    bool follow = session.FollowRomWatch;
    if (ImGui.Checkbox("Follow ROM", ref follow))
    {
      session.FollowRomWatch = follow;
      if (follow)
      {
        int pc = Math.Max(0, session.LastBatch.ProgramCounter);
        session.SelectedAddress = pc;
        session.MicrocodeScroll = RomWatchFollowScroll.CenterOn(pc, map?.WordCount ?? 0);
      }
    }

    ImGui.SameLine();
    FirmwareBatchSnapshot batch = session.LastBatch;
    CalcDebugChrome.Muted($"PC={batch.ProgramCounter:X4}");

    if (map is null)
    {
      CalcDebugChrome.Muted("No microcode map for this model.");
      return;
    }

    float childH = height > 0f
      ? height
      : MathF.Max(120f, ImGui.GetContentRegionAvail().Y);
    if (!ImGui.BeginChild("##rom-watch", new Vector2(0f, childH), ImGuiChildFlags.Border))
    {
      ImGui.EndChild();
      return;
    }

    if (ImGui.BeginTable(
          "##rom-watch-table",
          4,
          ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
    {
      ImGui.TableSetupScrollFreeze(0, 1);
      ImGui.TableSetupColumn("Addr", ImGuiTableColumnFlags.WidthFixed, 44f);
      ImGui.TableSetupColumn("Word", ImGuiTableColumnFlags.WidthFixed, 44f);
      ImGui.TableSetupColumn("Mnem", ImGuiTableColumnFlags.WidthFixed, 52f);
      ImGui.TableSetupColumn("Handler", ImGuiTableColumnFlags.WidthStretch);
      ImGui.TableHeadersRow();

      int pc = session.LastBatch.ProgramCounter;
      uint pcBg = CalcDebugChrome.PcRowBackColor();
      uint selectedBg = CalcDebugChrome.SelectedRowBackColor();
      int first = Math.Clamp(session.MicrocodeScroll, 0, Math.Max(0, map.WordCount - 1));
      int last = Math.Min(map.WordCount, first + 64);
      for (int address = first; address < last; address++)
      {
        MicrocodeMapEntry? entry = map.TryGetAddress(address);
        if (entry is null)
        {
          continue;
        }

        ImGui.TableNextRow();
        bool atPc = address == pc;
        bool selected = address == session.SelectedAddress;
        if (atPc)
        {
          ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, pcBg);
        }
        else if (selected)
        {
          ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, selectedBg);
        }

        ImGui.TableSetColumnIndex(0);
        if (ImGui.Selectable(
              $"{entry.AddressHex}##rw{address}",
              selected,
              ImGuiSelectableFlags.SpanAllColumns))
        {
          session.SelectedAddress = address;
          session.FollowRomWatch = false;
        }

        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(entry.RomWordHex);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextUnformatted(entry.Mnemonic);
        ImGui.TableSetColumnIndex(3);
        ImGui.TextDisabled(ShortHandler(entry.HandlerId));
        if (ImGui.IsItemHovered())
        {
          MicrocodeCrossRefEntry? cross = session.CrossRef?.TryGetHandler(entry.HandlerId);
          if (cross is null)
          {
            CalcAppTooltip.Set(entry.HandlerId);
          }
          else
          {
            CalcAppTooltip.Set(
              $"{entry.HandlerId}\n{cross.NonpareilMnemonic}  |  {cross.PatentTerm}");
          }
        }
      }

      ImGui.EndTable();
    }

    ImGui.EndChild();
  }

  private static string ShortHandler(string handlerId)
  {
    int dot = handlerId.LastIndexOf('.');
    return dot >= 0 && dot + 1 < handlerId.Length ? handlerId[(dot + 1)..] : handlerId;
  }
}
