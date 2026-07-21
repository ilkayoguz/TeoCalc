using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Formats;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Composite Dev Studio MVP0 shell: dual machine|keys|legend listing + clipboard,
/// with flowchart as a separate pane (placeholder until MVP1).
/// </summary>
public static class CalcStudioPanelComponent
{
  /// <summary>Wide enough for Machine | Keys | Legend | Flowchart at <see cref="StudioMnemonicPaint.StudioListingScale"/>.</summary>
  public const float PreferredWidthRef = 960f;

  /// <summary>Index column (#) — right-aligned 1-based display sequence.</summary>
  public const float IndexColumnWidthRef = 36f;

  /// <summary>Machine column: museum LED pairs like "34 01" / "35 07" (left-aligned).</summary>
  public const float MachineColumnWidthRef = 70f;

  /// <summary>Keys column: pair of fixed keycaps + gap (left-aligned), not stretch.</summary>
  public const float KeysColumnWidthRef =
    StudioMnemonicPaint.KeycapWidthRef * 2f + StudioMnemonicPaint.KeycapGapRef + 4f;

  private readonly record struct CodeTableWidths(
    float Index,
    float Machine,
    float Keys,
    float Legend)
  {
    public float ColumnsSum => Index + Machine + Keys + Legend;
  }

  public static void DrawInline(Session session)
  {
    ImGui.TextUnformatted("STUDIO");
    ImGui.TextDisabled(
      session.SupportsCardProgram
        ? "Machine | Keys | Legend | Flowchart"
        : "Program memory not available");
    ImGui.Separator();

    DrawToolbar(session);
    ImGui.Separator();

    Vector2 avail = ImGui.GetContentRegionAvail();
    float height = MathF.Max(120f, avail.Y - 4f);

    IReadOnlyList<StudioListingView.Row>? rows = null;
    if (session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) && lines.Count > 0)
    {
      rows = StudioListingView.Build(lines);
    }

    StudioMnemonicPaint.PushListingScale();
    CodeTableWidths widths = MeasureCodeTableWidths(rows, session.EngineModelId);
    StudioMnemonicPaint.PopListingScale();

    // Code pane = content-sized table (no stretch-fill). Remaining width goes to Flowchart.
    float codeWidth = MeasureCodePaneWidth(widths);
    float gap = ImGui.GetStyle().ItemSpacing.X;
    float fcWidth = MathF.Max(160f, avail.X - codeWidth - gap);
    if (codeWidth + fcWidth + gap > avail.X && avail.X > 200f)
    {
      fcWidth = MathF.Max(120f, avail.X - codeWidth - gap);
    }

    if (ImGui.BeginChild("##studio-code", new Vector2(codeWidth, height), ImGuiChildFlags.Border))
    {
      DrawCodePane(session, widths);
    }

    ImGui.EndChild();
    ImGui.SameLine();
    if (ImGui.BeginChild("##studio-fc", new Vector2(fcWidth, height), ImGuiChildFlags.Border))
    {
      DrawFlowchartPlaceholder(session);
    }

    ImGui.EndChild();
  }

  /// <summary>Explorer / compact listing (dual encoding, no FC pane).</summary>
  public static void DrawListingOnly(Session session, float height = 0f)
  {
    DrawToolbar(session);
    ImGui.Separator();
    Vector2 size = height > 0f
      ? new Vector2(0f, height)
      : new Vector2(0f, MathF.Max(80f, ImGui.GetContentRegionAvail().Y));
    if (ImGui.BeginChild("##studio-listing-only", size, ImGuiChildFlags.Border))
    {
      DrawCodePane(session, widths: null);
    }

    ImGui.EndChild();
  }

  private static void DrawToolbar(Session session)
  {
    ImGui.TextDisabled("Copy dual TSV · Paste auto");
    bool canEdit = session.SupportsCardProgram;
    if (!canEdit)
    {
      ImGui.BeginDisabled();
    }

    if (ImGui.Button("Copy"))
    {
      string text = session.FormatProgramListingText();
      if (string.IsNullOrWhiteSpace(text))
      {
        session.StudioStatusMessage = "Nothing to copy.";
      }
      else
      {
        ImGui.SetClipboardText(text);
        session.StudioStatusMessage = "Copied machine|mnemonic listing.";
      }
    }

    ImGui.SameLine();
    if (ImGui.Button("Paste"))
    {
      string clip = ImGui.GetClipboardText() ?? string.Empty;
      if (session.TryPasteProgramListing(clip, out string? error))
      {
        // status set by session
      }
      else
      {
        session.StudioStatusMessage = error ?? "Paste failed.";
      }
    }

    if (!canEdit)
    {
      ImGui.EndDisabled();
    }

    if (!string.IsNullOrEmpty(session.StudioStatusMessage))
    {
      ImGui.TextDisabled(session.StudioStatusMessage);
    }
  }

  private static void DrawCodePane(Session session, CodeTableWidths? widths)
  {
    ImGui.PushStyleColor(ImGuiCol.ChildBg, StudioMnemonicPaint.CodePaneBg);
    ImGui.PushStyleColor(ImGuiCol.Text, StudioMnemonicPaint.DefaultInk);
    ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, 0xFF3A3E42u);
    ImGui.PushStyleColor(ImGuiCol.TableRowBg, 0x00FFFFFF);
    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, 0x14FFFFFF);
    ImGui.PushStyleColor(ImGuiCol.Header, 0x4028A0FFu);
    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x5528A0FFu);
    ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0x6628A0FFu);
    StudioMnemonicPaint.PushListingScale();

    ImGui.TextUnformatted("Code");
    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines)
        || lines.Count == 0)
    {
      ImGui.TextDisabled(
        session.SupportsCardProgram
          ? "Empty program (power on / load a card)."
          : "Not available for this model.");
      StudioMnemonicPaint.PopListingScale();
      ImGui.PopStyleColor(8);
      return;
    }

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(lines);
    int pointerHighlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
    string modelId = session.EngineModelId;

    CodeTableWidths col = widths ?? MeasureCodeTableWidths(rows, modelId);
    float tableOuterW = MeasureTableOuterWidth(col);
    float tableH = MathF.Max(1f, ImGui.GetContentRegionAvail().Y);

    // ScrollY + explicit outer width (= column sum + scrollbar). No stretch weights —
    // WidthFixed on every column so Legend never absorbs leftover host width.
    // (NoHostExtendX is incompatible with ScrollY; content-sized outer_size replaces it.)
    if (ImGui.BeginTable(
          "##studio-code-table",
          4,
          ImGuiTableFlags.RowBg
            | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.BordersInnerV,
          new Vector2(tableOuterW, tableH)))
    {
      ImGui.TableSetupScrollFreeze(0, 1);
      const ImGuiTableColumnFlags fixedCol =
        ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;
      ImGui.TableSetupColumn("#", fixedCol, col.Index);
      ImGui.TableSetupColumn("Machine", fixedCol, col.Machine);
      ImGui.TableSetupColumn("Keys", fixedCol, col.Keys);
      ImGui.TableSetupColumn("Legend", fixedCol, col.Legend);
      DrawCodeTableHeaders();

      for (int i = 0; i < rows.Count; i++)
      {
        StudioListingView.Row row = rows[i];
        StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId);
        ImGui.TableNextRow();
        bool selected = row.ContainsIndex(session.SelectedProgramStep);
        bool atPtr = row.Index == pointerHighlight;
        if (atPtr)
        {
          ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, StudioMnemonicPaint.PointerRowBg);
        }

        // # column: 1-based display sequence (not RAM address). Empty SpanAllColumns
        // selectable for row hit-testing; label drawn via DrawList so SelectableTextAlign
        // cannot park the glyph outside column-0 clip (which made numbers invisible).
        ImGui.TableSetColumnIndex(0);
        Vector2 indexCellPos = ImGui.GetCursorScreenPos();
        float indexCellW = ImGui.GetContentRegionAvail().X;
        float indexLineH = ImGui.GetTextLineHeight();
        if (ImGui.Selectable(
              $"##ps{row.Index}",
              selected,
              ImGuiSelectableFlags.SpanAllColumns,
              new Vector2(0f, indexLineH)))
        {
          session.SelectedProgramStep = row.Index;
        }

        int displayIndex = i + 1;
        string indexText = displayIndex.ToString();
        Vector2 indexSize = ImGui.CalcTextSize(indexText);
        ImGui.GetWindowDrawList().AddText(
          new Vector2(
            indexCellPos.X + MathF.Max(0f, indexCellW - indexSize.X),
            indexCellPos.Y + MathF.Max(0f, (indexLineH - indexSize.Y) * 0.5f)),
          StudioMnemonicPaint.DefaultInk,
          indexText);

        ImGui.TableSetColumnIndex(1);
        StudioMnemonicPaint.DrawMachineLedCell(
          StudioMuseumKeycodes.FormatMachineDisplay(row, modelId),
          align: 0f);

        ImGui.TableSetColumnIndex(2);
        StudioMnemonicPaint.DrawMnemonicKeycaps(paint.KeysMnemonic, modelId, align: 0f);

        ImGui.TableSetColumnIndex(3);
        StudioMnemonicPaint.DrawLegend(paint.Legend, paint.LegendKind, align: 0.5f);
      }

      ImGui.EndTable();
    }

    StudioMnemonicPaint.PopListingScale();
    ImGui.PopStyleColor(8);
  }

  /// <summary>
  /// Fixed <see cref="ImGuiTableColumnFlags.WidthFixed"/> request widths under listing font scale.
  /// WidthRequest is the cell work area (ImGui adds CellPadding outside it). Legend is at least
  /// full <c>CalcTextSize("Legend")</c> plus FramePadding (TableHeader clip) and breath, and at
  /// least the widest legend glyph content — verified under <see cref="StudioMnemonicPaint.StudioListingScale"/>.
  /// </summary>
  private static CodeTableWidths MeasureCodeTableWidths(
    IReadOnlyList<StudioListingView.Row>? rows,
    string? modelId)
  {
    float s = StudioMnemonicPaint.StudioListingScale;
    ImGuiStylePtr style = ImGui.GetStyle();
    // Breath inside WidthRequest so TableHeader / centered glyphs don't clip to "Lege...".
    float breath = style.CellPadding.X * 2f;
    // TableHeader Selectable uses FramePadding; without it the header ellipsizes first.
    float headerChrome = style.FramePadding.X * 2f;

    float index = MathF.Max(IndexColumnWidthRef * s, ImGui.CalcTextSize("#").X + breath);
    float machine = MathF.Max(MachineColumnWidthRef * s, ImGui.CalcTextSize("Machine").X + breath + headerChrome);
    float keys = MathF.Max(KeysColumnWidthRef * s, ImGui.CalcTextSize("Keys").X + breath + headerChrome);
    // Min width must keep the full "Legend" label visible when centered at StudioListingScale.
    float legend = ImGui.CalcTextSize("Legend").X + breath + headerChrome;

    if (rows is { Count: > 0 })
    {
      for (int i = 0; i < rows.Count; i++)
      {
        StudioListingView.Paint paint = StudioListingView.ResolvePaint(rows[i], modelId);
        float content = StudioMnemonicPaint.MeasureLegendContentWidth(paint.Legend);
        if (content > 0f)
        {
          legend = MathF.Max(legend, content + breath);
        }
      }
    }

    return new CodeTableWidths(index, machine, keys, legend);
  }

  /// <summary>
  /// Exact ScrollY table outer width for BordersInnerV + all WidthFixed (matches imgui
  /// <c>width_spacings + sum(WidthRequest + CellPaddingX*2)</c>) plus scrollbar gutter.
  /// </summary>
  private static float MeasureTableOuterWidth(in CodeTableWidths widths)
  {
    const int columnCount = 4;
    const float tableBorderSize = 1f; // ImGui TABLE_BORDER_SIZE with BordersInnerV
    float cellPadX = ImGui.GetStyle().CellPadding.X;
    // BordersInnerV, no BordersOuterV: OuterPaddingX = -CellPaddingX,
    // CellSpacingX1 = border, CellSpacingX2 = 0 → spacings = -2*pad + border*(n-1)
    float widthSpacings = -2f * cellPadX + tableBorderSize * (columnCount - 1);
    float sumRequests = widths.ColumnsSum + cellPadX * 2f * columnCount;
    return widthSpacings + sumRequests + ImGui.GetStyle().ScrollbarSize;
  }

  /// <summary>Code child width so the table is content-sized (padding + border around table).</summary>
  private static float MeasureCodePaneWidth(in CodeTableWidths widths)
  {
    ImGuiStylePtr style = ImGui.GetStyle();
    float chrome = style.WindowPadding.X * 2f + style.ChildBorderSize * 2f;
    return MeasureTableOuterWidth(widths) + chrome;
  }

  /// <summary>
  /// Header text alignment matches cell content: # right; Machine / Keys left; Legend center.
  /// </summary>
  private static void DrawCodeTableHeaders()
  {
    ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
    DrawAlignedTableHeader(0, "#", alignX: 1f);
    DrawAlignedTableHeader(1, "Machine", alignX: 0f);
    DrawAlignedTableHeader(2, "Keys", alignX: 0f);
    DrawAlignedTableHeader(3, "Legend", alignX: 0.5f);
  }

  /// <summary>
  /// Pad within the column before <see cref="ImGui.TableHeader"/> — SelectableTextAlign is
  /// unreliable on table headers and fights SpanAllColumns on body cells.
  /// <paramref name="alignX"/>: 0 = left, 0.5 = center, 1 = right.
  /// </summary>
  private static void DrawAlignedTableHeader(int column, string label, float alignX)
  {
    ImGui.TableSetColumnIndex(column);
    float avail = ImGui.GetContentRegionAvail().X;
    float textW = ImGui.CalcTextSize(label).X;
    float pad = MathF.Max(0f, (avail - textW) * Math.Clamp(alignX, 0f, 1f));
    if (pad > 0f)
    {
      ImGui.SetCursorPosX(ImGui.GetCursorPosX() + pad);
    }

    ImGui.TableHeader(label);
  }

  private static void DrawFlowchartPlaceholder(Session session)
  {
    _ = session;
    ImGui.TextUnformatted("Flowchart");
    ImGui.Spacing();
    ImGui.TextDisabled("Flowchart — coming soon");
  }
}
