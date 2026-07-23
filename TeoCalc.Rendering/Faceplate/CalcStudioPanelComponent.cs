using ImGuiNET;
using System.Numerics;
using System.Text;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Composite Dev Studio shell: dual machine|keys|legend listing + clipboard,
/// with read-only flowchart pane (Stage B / MVP1).
/// </summary>
public static class CalcStudioPanelComponent
{
  /// <summary>Wide enough for Machine | Keys | Legend | Flowchart at <see cref="StudioMnemonicPaint.StudioListingScale"/>.</summary>
  public const float PreferredWidthRef = 960f;

  /// <summary>
  /// PC ▶ height as a fraction of listing line height (modest, readable — not full-line).
  /// </summary>
  private const float PointerMarkerHeightFrac = 0.52f;

  /// <summary>Tip extent relative to marker height (right-pointing play triangle).</summary>
  private const float PointerMarkerTipFrac = 0.9f;

  private const float PointerMarkerLeftPad = 1f;

  /// <summary>Gap between PC ▶ tip and right-aligned display index digits.</summary>
  private const float PointerMarkerDigitGap = 2f;

  /// <summary>Machine column: museum LED pairs like "34 01" / "35 07" (left-aligned).</summary>
  public const float MachineColumnWidthRef = 70f;

  /// <summary>Keys column: up to three fixed keycaps + gaps (LBL pair / shift / STO+n).</summary>
  public const float KeysColumnWidthRef =
    StudioMnemonicPaint.KeycapWidthRef * 3f + StudioMnemonicPaint.KeycapGapRef * 2f + 4f;

  private readonly record struct CodeTableWidths(
    float Index,
    float Machine,
    float Keys,
    float Legend)
  {
    public float ColumnsSum => Index + Machine + Keys + Legend;
  }

  private static string s_statusBalloon = string.Empty;
  private static float s_statusBalloonUntil;
  private static Vector2 s_statusBalloonAnchor;
  private static bool s_codeDragArmed;
  private static bool s_codeDragMoved;
  private static Vector2 s_codeDragOrigin;
  private static Vector2 s_codeDragLast;
  private static int s_codeContextStep = -1;
  private static string s_findQuery = string.Empty;
  private static int s_findMatchRow = -1;
  private static bool s_focusFind;

  /// <summary>Focus the Studio Find box (e.g. Ctrl+F).</summary>
  public static void RequestFindFocus() => s_focusFind = true;

  private static void ShowStatusBalloon(string message)
  {
    s_statusBalloon = message;
    s_statusBalloonUntil = (float)ImGui.GetTime() + 1.25f;
    // Anchor above the cursor so layout never shifts.
    s_statusBalloonAnchor = ImGui.GetMousePos();
  }

  private static void DrawStatusBalloon()
  {
    if (s_statusBalloon.Length == 0 || ImGui.GetTime() > s_statusBalloonUntil)
    {
      s_statusBalloon = string.Empty;
      return;
    }

    Vector2 size = ImGui.CalcTextSize(s_statusBalloon);
    Vector2 pad = new(8f, 5f);
    Vector2 p0 = new(
      s_statusBalloonAnchor.X,
      s_statusBalloonAnchor.Y - size.Y - pad.Y * 2f - 8f);
    Vector2 p1 = p0 + size + pad * 2f;
    ImDrawListPtr draw = ImGui.GetForegroundDrawList();
    draw.AddRectFilled(p0, p1, 0xE0101014u, 4f);
    draw.AddRect(p0, p1, 0xFF808890u, 4f);
    draw.AddText(p0 + pad, 0xFFFFFFFFu, s_statusBalloon);
  }

  public static void DrawInline(
    Session session,
    ref string cardPathBuffer,
    ref string cardStatusMessage,
    bool canLoadSaveCard,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard,
    bool cardInserted,
    string? loadedCardPath,
    TeoCardDocument? loadedTeoCard,
    Action? onEjectCard)
  {
    ImGui.TextUnformatted("STUDIO");
    ImGui.TextDisabled(
      session.SupportsCardProgram
        ? "Machine | Keys | Legend | Flowchart"
        : "Program memory not available");
    ImGui.Separator();

    DrawStudioToolsBar(
      session,
      ref cardPathBuffer,
      ref cardStatusMessage,
      canLoadSaveCard,
      loadCard,
      saveCard,
      cardInserted,
      loadedCardPath,
      loadedTeoCard,
      onEjectCard);
    if (session.StudioStatusMessage.Length > 0)
    {
      ShowStatusBalloon(session.StudioStatusMessage);
      session.StudioStatusMessage = string.Empty;
    }

    ImGui.Separator();

    Vector2 avail = ImGui.GetContentRegionAvail();
    IReadOnlyList<StudioListingView.Row>? rows = null;
    if (session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) && lines.Count > 0)
    {
      rows = StudioListingView.Build(
        lines,
        session.IsProgramDirty ? null : session.LoadedTeoCard?.Program.Steps);
    }

    // Footer (Lines + R1–R9) stays pinned — no outer Studio scroll to reach Data.
    float footerH = MeasureProgramStatusFooterHeight(session, rows, avail.X);
    float height = MathF.Max(80f, avail.Y - footerH);
    Vector2 panesOrigin = ImGui.GetCursorScreenPos();

    StudioMnemonicPaint.PushListingScale();
    CodeTableWidths widths = MeasureCodeTableWidths(
      rows,
      session.EngineModelId,
      session.CardStripLabels);
    StudioMnemonicPaint.PopListingScale();

    // Code pane = content-sized table (no stretch-fill). Remaining width goes to Flowchart.
    float codeWidth = MeasureCodePaneWidth(widths);
    float gap = ImGui.GetStyle().ItemSpacing.X;
    float fcWidth = MathF.Max(160f, avail.X - codeWidth - gap);
    if (codeWidth + fcWidth + gap > avail.X && avail.X > 200f)
    {
      fcWidth = MathF.Max(120f, avail.X - codeWidth - gap);
    }

    // Outer panes must not scroll — the code table / FC canvas own wheel + bars.
    // Nested scroll on this host fights the inner child (content height oscillates → “fly”).
    const ImGuiWindowFlags paneHostFlags =
      ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    if (ImGui.BeginChild(
          "##studio-code",
          new Vector2(codeWidth, height),
          ImGuiChildFlags.Border,
          paneHostFlags))
    {
      if (ImGui.IsWindowHovered(
            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
            | ImGuiHoveredFlags.ChildWindows))
      {
        CalcFaceplatePointer.MarkScrollableUiHovered();
      }

      DrawCodePane(session, widths);
    }

    ImGui.EndChild();
    ImGui.SameLine();
    if (ImGui.BeginChild(
          "##studio-fc",
          new Vector2(fcWidth, height),
          ImGuiChildFlags.Border,
          paneHostFlags))
    {
      if (ImGui.IsWindowHovered(
            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
            | ImGuiHoveredFlags.ChildWindows))
      {
        CalcFaceplatePointer.MarkScrollableUiHovered();
      }

      DrawFlowchartPane(session, rows);
    }

    ImGui.EndChild();
    StudioPaneSync.EndFrame();

    ImGui.SetCursorScreenPos(new Vector2(panesOrigin.X, panesOrigin.Y + height));
    DrawProgramStatusFooter(session, rows, avail.X);
    // Claim the reserved footer band so the side panel does not grow/scroll past it.
    ImGui.SetCursorScreenPos(panesOrigin + new Vector2(0f, avail.Y));
    ImGui.Dummy(new Vector2(1f, 1f));
  }

  private static void DrawStudioToolsBar(
    Session session,
    ref string cardPathBuffer,
    ref string cardStatusMessage,
    bool canLoadSaveCard,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard,
    bool cardInserted,
    string? loadedCardPath,
    TeoCardDocument? loadedTeoCard,
    Action? onEjectCard)
  {
    // One VS-like strip: card I/O + clipboard left, debug transport right.
    CalcStudioChromeStyle.PushToolbar();

    bool showSave = canLoadSaveCard && (session.ProgramMode || session.IsProgramDirty);
    if (!canLoadSaveCard)
    {
      ImGui.TextDisabled("No program memory");
    }
    else
    {
      CalcStudioChromeStyle.PushPrimary();
      ImGui.PushFont(CalcFaceplateFonts.Arial);
      if (ImGui.Button("Browse\u2026"))
      {
        string? initialDir = Path.GetDirectoryName(cardPathBuffer.Trim());
        if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
        {
          initialDir = CalcCardPanelComponent.DefaultCardsDirectory();
        }

        CalcCardFilePicker.Open(initialDir);
      }

      ImGui.PopFont();
      CalcStudioChromeStyle.PopPrimary();

      if (showSave)
      {
        ImGui.SameLine();
        if (ImGui.Button(session.IsProgramDirty ? "Save*" : "Save"))
        {
          if (TryStudioSaveCard(session, ref cardPathBuffer, saveCard, out string status))
          {
            ShowStatusBalloon(status);
            cardStatusMessage = status;
          }
          else
          {
            cardStatusMessage = status;
            ShowStatusBalloon(status);
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip(
            session.ProgramMode
              ? "Write program RAM to the card file (W/PRGM)."
              : "Write unsaved program RAM to the card file.");
        }
      }

      if (cardInserted && onEjectCard is not null)
      {
        ImGui.SameLine();
        if (ImGui.Button("Eject"))
        {
          onEjectCard();
          cardPathBuffer = string.Empty;
          cardStatusMessage = "Card ejected.";
        }
      }
    }

    if (CalcCardFilePicker.Draw(out string? picked)
        && !string.IsNullOrWhiteSpace(picked))
    {
      cardPathBuffer = picked;
      string? error = loadCard(picked);
      cardStatusMessage = error is null
        ? $"Loaded: {Path.GetFileName(picked)}"
        : error;
      if (error is null)
      {
        ShowStatusBalloon(cardStatusMessage);
      }
    }

    string cardLabel = loadedTeoCard?.Title
      ?? (cardInserted && !string.IsNullOrWhiteSpace(loadedCardPath)
        ? Path.GetFileName(loadedCardPath)
        : string.Empty);
    if (cardLabel.Length > 0)
    {
      ImGui.SameLine();
      ImGui.TextDisabled(cardLabel);
    }

    if (session.IsProgramDirty)
    {
      ImGui.SameLine();
      ImGui.TextColored(new Vector4(0.95f, 0.75f, 0.35f, 1f), "unsaved");
    }

    ImGui.SameLine();
    ImGui.TextDisabled("|");
    ImGui.SameLine();
    DrawClipboardAndStartButtons(session);

    float transportW = MeasureCompactDebugTransportWidth(session);
    float rightX = ImGui.GetWindowContentRegionMax().X - transportW;
    if (rightX > ImGui.GetCursorPosX() + 12f)
    {
      ImGui.SameLine(rightX);
    }
    else
    {
      ImGui.SameLine();
    }

    DrawCompactDebugTransport(session);
    DrawStatusBalloon();
    CalcStudioChromeStyle.PopToolbar();

    // Path under the action strip (does not steal transport alignment).
    float pathW = MathF.Max(80f, ImGui.GetContentRegionAvail().X);
    ImGui.SetNextItemWidth(pathW);
    ImGui.InputText(
      "##studio-card-path",
      ref cardPathBuffer,
      512,
      ImGuiInputTextFlags.ReadOnly);
  }

  private static void DrawClipboardAndStartButtons(Session session)
  {
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
        ShowStatusBalloon("Nothing to copy.");
      }
      else
      {
        ImGui.SetClipboardText(text);
        ShowStatusBalloon("Copied machine|mnemonic listing.");
      }
    }

    ImGui.SameLine();
    if (ImGui.Button("Paste"))
    {
      string clip = ImGui.GetClipboardText() ?? string.Empty;
      if (session.TryPasteProgramListing(clip, out string? error))
      {
        ShowStatusBalloon(
          session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Pasted.");
        session.StudioStatusMessage = string.Empty;
      }
      else
      {
        ShowStatusBalloon(error ?? "Paste failed.");
      }
    }

    ImGui.SameLine();
    if (ImGui.Button("Set start")
        && session.SelectedProgramStep >= 0
        && session.TrySetProgramStartStep(session.SelectedProgramStep))
    {
      ShowStatusBalloon($"Start → step {session.SelectedProgramStep}");
      StudioPaneSync.FollowPointer(session.SelectedProgramStep);
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Set Classic PTR (also: double-click row, Ctrl+Shift+F10, FC right-click).");
    }

    ImGui.SameLine();
    ImGui.TextDisabled("|");
    ImGui.SameLine();
    DrawFindControls(session);

    if (!canEdit)
    {
      ImGui.EndDisabled();
    }
  }

  private static void DrawFindControls(Session session)
  {
    if (s_focusFind)
    {
      ImGui.SetKeyboardFocusHere();
      s_focusFind = false;
    }

    ImGui.SetNextItemWidth(110f);
    bool enter = ImGui.InputText(
      "##studio-find",
      ref s_findQuery,
      96,
      ImGuiInputTextFlags.EnterReturnsTrue);
    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Find in Machine / Keys / Legend (Ctrl+F)");
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("Find") || enter)
    {
      if (TryFindListingMatch(session, forward: true, out int step, out string status))
      {
        session.SelectedProgramStep = step;
        StudioPaneSync.FollowPointer(step);
        ShowStatusBalloon(status);
      }
      else
      {
        ShowStatusBalloon(status);
      }
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("Prev##find"))
    {
      if (TryFindListingMatch(session, forward: false, out int step, out string status))
      {
        session.SelectedProgramStep = step;
        StudioPaneSync.FollowPointer(step);
        ShowStatusBalloon(status);
      }
      else
      {
        ShowStatusBalloon(status);
      }
    }
  }

  /// <summary>
  /// Search Studio listing Machine/Keys/Legend (and mnemonic) for <see cref="s_findQuery"/>.
  /// </summary>
  internal static bool TryFindListingMatch(
    Session session,
    bool forward,
    out int stepIndex,
    out string status)
  {
    stepIndex = -1;
    status = "Nothing to find.";
    string query = s_findQuery.Trim();
    if (query.Length == 0)
    {
      status = "Enter a find string.";
      return false;
    }

    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines)
        || lines.Count == 0)
    {
      status = "Empty program.";
      return false;
    }

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.IsProgramDirty ? null : session.LoadedTeoCard?.Program.Steps);
    if (rows.Count == 0)
    {
      status = "Empty listing.";
      return false;
    }

    string modelId = session.EngineModelId;
    IReadOnlyList<string>? strip = session.CardStripLabels;
    int start = s_findMatchRow;
    if (start < 0 || start >= rows.Count)
    {
      start = forward ? -1 : rows.Count;
    }

    for (int n = 0; n < rows.Count; n++)
    {
      int i = forward
        ? (start + 1 + n) % rows.Count
        : (start - 1 - n + rows.Count * 2) % rows.Count;
      if (!RowMatchesFind(rows[i], query, modelId, strip))
      {
        continue;
      }

      s_findMatchRow = i;
      stepIndex = rows[i].Index;
      status = $"Find → #{StudioListingView.DisplayStepNumber(rows, i)} {rows[i].DisplayMnemonic}";
      return true;
    }

    status = $"No match for '{query}'.";
    return false;
  }

  internal static bool RowMatchesFind(
    StudioListingView.Row row,
    string query,
    string modelId,
    IReadOnlyList<string>? stripCaptions)
  {
    if (string.IsNullOrWhiteSpace(query))
    {
      return false;
    }

    StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId, stripCaptions);
    string machine = StudioMuseumKeycodes.FormatMachineDisplay(row, modelId);
    string haystack =
      $"{machine} {row.Code} {paint.KeysMnemonic} {paint.Legend} {row.DisplayMnemonic} {row.Mnemonic}";
    return haystack.Contains(query, StringComparison.OrdinalIgnoreCase);
  }

  private static float MeasureCompactDebugTransportWidth(Session session)
  {
    // − 1× + | Break Cont Step Over + PC status — approximate for right-align.
    float pad = ImGui.GetStyle().ItemSpacing.X;
    float w = ImGui.CalcTextSize("−").X + 16f
      + pad + ImGui.CalcTextSize(session.ExecutionSpeedLabel).X
      + pad + ImGui.CalcTextSize("+").X + 16f
      + pad + ImGui.CalcTextSize("|").X
      + pad + ImGui.CalcTextSize("Break").X + 16f
      + pad + ImGui.CalcTextSize("Cont").X + 16f
      + pad + ImGui.CalcTextSize("Step").X + 16f
      + pad + ImGui.CalcTextSize("Over").X + 16f;
    FirmwareBatchSnapshot batch = session.LastBatch;
    string status =
      $"PC={batch.ProgramCounter:X4}  steps={batch.StepCount}  {(session.ExecutionPaused ? "PAUSED" : "RUN")}";
    return w + pad + ImGui.CalcTextSize(status).X + 8f;
  }

  private static bool TryStudioSaveCard(
    Session session,
    ref string cardPathBuffer,
    Func<string, string?> saveCard,
    out string status)
  {
    string path = cardPathBuffer.Trim();
    if (string.IsNullOrWhiteSpace(path))
    {
      path = session.LoadedCardPath ?? string.Empty;
    }

    if (string.IsNullOrWhiteSpace(path)
        || !Path.HasExtension(path))
    {
      string? initialDir = Path.GetDirectoryName(path);
      if (string.IsNullOrWhiteSpace(initialDir) || !Directory.Exists(initialDir))
      {
        initialDir = CalcCardPanelComponent.DefaultCardsDirectory();
      }

      string suggested = string.IsNullOrWhiteSpace(session.LoadedTeoCard?.Title)
        ? $"program{session.CardProgramExtension}"
        : $"{SanitizeStudioFileName(session.LoadedTeoCard!.Title)}{session.CardProgramExtension}";
      if (!CalcNativeFileDialog.TrySaveCardFile(initialDir, suggested, out string picked))
      {
        status = "Save cancelled.";
        return false;
      }

      path = picked;
      cardPathBuffer = path;
    }

    string? error = saveCard(path);
    if (error is not null)
    {
      status = error;
      return false;
    }

    status = $"Saved: {Path.GetFileName(path)}";
    return true;
  }

  private static string SanitizeStudioFileName(string title)
  {
    char[] invalid = Path.GetInvalidFileNameChars();
    StringBuilder sb = new(title.Length);
    foreach (char c in title.Trim())
    {
      sb.Append(Array.IndexOf(invalid, c) >= 0 ? '-' : c);
    }

    string name = sb.ToString().Trim();
    return name.Length > 0 ? name : "program";
  }

  /// <summary>
  /// Leave-W/PRGM dirty confirm — call once per frame from the faceplate host
  /// so it still appears when Studio is closed.
  /// </summary>
  public static void DrawPendingLeaveProgramConfirm(
    Session session,
    ref string cardPathBuffer,
    Func<string, string?> saveCard) =>
    DrawLeaveProgramConfirmPopup(session, ref cardPathBuffer, saveCard);

  private static void DrawLeaveProgramConfirmPopup(
    Session session,
    ref string cardPathBuffer,
    Func<string, string?> saveCard)
  {
    if (session.PendingLeaveProgramConfirm)
    {
      ImGui.OpenPopup("##leave-wprgm-confirm");
    }

    bool open = true;
    if (!ImGui.BeginPopupModal(
          "##leave-wprgm-confirm",
          ref open,
          ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
    {
      if (!open && session.PendingLeaveProgramConfirm)
      {
        session.CancelLeaveProgramConfirm();
      }

      return;
    }

    ImGui.TextUnformatted("Program has unsaved changes.");
    ImGui.TextDisabled("Save before switching to RUN?");
    ImGui.Spacing();

    if (ImGui.Button("Save", new Vector2(90f, 0f)))
    {
      if (TryStudioSaveCard(session, ref cardPathBuffer, saveCard, out string status))
      {
        ShowStatusBalloon(status);
        session.CancelLeaveProgramConfirm();
        session.ToggleProgramModeTo(false);
        ImGui.CloseCurrentPopup();
      }
      else
      {
        ShowStatusBalloon(status);
      }
    }

    ImGui.SameLine();
    if (ImGui.Button("Don't Save", new Vector2(100f, 0f)))
    {
      session.ConfirmDiscardProgramEditsAndRun();
      ImGui.CloseCurrentPopup();
    }

    ImGui.SameLine();
    if (ImGui.Button("Cancel", new Vector2(90f, 0f)))
    {
      session.CancelLeaveProgramConfirm();
      ImGui.CloseCurrentPopup();
    }

    ImGui.EndPopup();
  }

  private static void DrawCompactDebugTransport(Session session)
  {
    if (ImGui.SmallButton("−##speed-down"))
    {
      session.NudgeExecutionSpeed(-1);
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Slower free-run ([)");
    }

    ImGui.SameLine();
    ImGui.TextUnformatted(session.ExecutionSpeedLabel);
    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Execution speed (free-run Tick multiplier)");
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("+##speed-up"))
    {
      session.NudgeExecutionSpeed(1);
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Faster free-run (])");
    }

    ImGui.SameLine();
    ImGui.TextDisabled("|");
    ImGui.SameLine();

    bool powered = session.PowerOn;
    if (!powered)
    {
      ImGui.BeginDisabled();
    }

    if (ImGui.SmallButton("Break"))
    {
      session.BreakExecution();
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Break / pause (F6)");
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("Cont"))
    {
      session.ContinueExecution();
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("Continue (F5)");
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("Step"))
    {
      session.StepStudioKey();
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip(
        "F11: one keystroke / one FC element (AdvancePointer). RTN’den sonra → LBL.");
    }

    ImGui.SameLine();
    if (ImGui.SmallButton("Over"))
    {
      session.StepStudioLine();
    }

    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip("F10: one Code row / one FC box.");
    }

    if (!powered)
    {
      ImGui.EndDisabled();
    }

    ImGui.SameLine();
    FirmwareBatchSnapshot batch = session.LastBatch;
    ImGui.TextDisabled(
      $"PC={batch.ProgramCounter:X4}  steps={batch.StepCount}  {(session.ExecutionPaused ? "PAUSED" : "RUN")}");
    if (ImGui.IsItemHovered())
    {
      ImGui.SetTooltip(
        "F5 Cont  Shift+F5 Stop  F6 Break  F9 Breakpoint  F10 row/box  F11 key  |  [ ] speed  |  Microcode: title-bar Debug");
    }
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
    CalcStudioChromeStyle.PushToolbar();
    DrawClipboardAndStartButtons(session);
    DrawStatusBalloon();
    CalcStudioChromeStyle.PopToolbar();
  }

  private static void DrawCodePane(Session session, CodeTableWidths? widths)
  {
    ImGui.PushStyleColor(ImGuiCol.ChildBg, StudioMnemonicPaint.CodePaneBg);
    ImGui.PushStyleColor(ImGuiCol.Text, StudioMnemonicPaint.DefaultInk);
    ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, 0xFF3A3E42u);
    ImGui.PushStyleColor(ImGuiCol.TableRowBg, 0x00FFFFFF);
    ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, 0x14FFFFFF);
    ImGui.PushStyleColor(ImGuiCol.Header, StudioMnemonicPaint.SelectionRowBg);
    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, StudioMnemonicPaint.SelectionRowHoveredBg);
    ImGui.PushStyleColor(ImGuiCol.HeaderActive, StudioMnemonicPaint.SelectionRowActiveBg);
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

    IReadOnlyList<StudioListingView.Row> rows = StudioListingView.Build(
      lines,
      session.IsProgramDirty ? null : session.LoadedTeoCard?.Program.Steps);
    int pointerHighlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
    string modelId = session.EngineModelId;
    IReadOnlyList<string>? stripCaptions = session.CardStripLabels;

    CodeTableWidths col = widths ?? MeasureCodeTableWidths(rows, modelId, stripCaptions);
    float tableOuterW = MeasureTableOuterWidth(col);
    float rowHitW = col.ColumnsSum + ImGui.GetStyle().CellPadding.X * 2f * 4;
    float dataFooterH = 0f;
    float tableH = MathF.Max(1f, ImGui.GetContentRegionAvail().Y - dataFooterH);
    float rowH = StudioMnemonicPaint.ListingRowContentHeight();

    // Sticky header outside the scroll child.
    if (ImGui.BeginTable(
          "##studio-code-hdr",
          4,
          ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV,
          new Vector2(tableOuterW, 0f)))
    {
      const ImGuiTableColumnFlags fixedCol =
        ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;
      ImGui.TableSetupColumn("#", fixedCol, col.Index);
      ImGui.TableSetupColumn("Machine", fixedCol, col.Machine);
      ImGui.TableSetupColumn("Keys", fixedCol, col.Keys);
      ImGui.TableSetupColumn("Legend", fixedCol, col.Legend);
      DrawCodeTableHeaders();
      ImGui.EndTable();
    }

    float headerH = ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y;
    float bodyH = MathF.Max(40f, tableH - headerH);

    // NoScrollWithMouse: NewFrame must NOT apply FontSize×5. Wheel is owned below.
    if (ImGui.BeginChild(
          "##studio-code-scroll",
          new Vector2(tableOuterW, bodyH),
          ImGuiChildFlags.None,
          ImGuiWindowFlags.NoScrollWithMouse))
    {
      CalcFaceplatePointer.ApplyManualWheelScrollOnCurrentWindow(rowH);
      UpdateCodeTableDragScroll(rowH);

      if (ImGui.BeginTable(
            "##studio-code-table",
            4,
            ImGuiTableFlags.RowBg
              | ImGuiTableFlags.SizingFixedFit
              | ImGuiTableFlags.BordersInnerV,
            new Vector2(tableOuterW, 0f)))
      {
        const ImGuiTableColumnFlags fixedCol =
          ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize;
        ImGui.TableSetupColumn("#", fixedCol, col.Index);
        ImGui.TableSetupColumn("Machine", fixedCol, col.Machine);
        ImGui.TableSetupColumn("Keys", fixedCol, col.Keys);
        ImGui.TableSetupColumn("Legend", fixedCol, col.Legend);

        for (int i = 0; i < rows.Count; i++)
        {
          StudioListingView.Row row = rows[i];
          StudioListingView.Paint paint = StudioListingView.ResolvePaint(row, modelId, stripCaptions);
          ImGui.TableNextRow();
          bool selected = row.ContainsIndex(session.SelectedProgramStep);
          bool atPtr = row.Index == pointerHighlight;
          if (selected)
          {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, StudioMnemonicPaint.SelectionRowBg);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, StudioMnemonicPaint.SelectionRowBg);
          }

          ImGui.TableSetColumnIndex(0);
          Vector2 indexCellPos = ImGui.GetCursorScreenPos();
          float indexCellW = ImGui.GetContentRegionAvail().X;
          float indexLineH = rowH;
          Vector2 rowMin = indexCellPos;
          _ = ImGui.IsRectVisible(rowMin, rowMin + new Vector2(MathF.Max(1f, indexCellW), indexLineH));

          if (atPtr)
          {
            DrawPointerMarker(indexCellPos, indexLineH);
          }

          if (session.HasStudioBreakpointOnRow(row))
          {
            DrawBreakpointMarker(indexCellPos, indexLineH, atPtr);
          }

          int displayIndex = StudioListingView.DisplayStepNumber(rows, i);
          string indexText = displayIndex.ToString();
          Vector2 indexSize = ImGui.CalcTextSize(indexText);
          float markerReserve = 0f;
          if (atPtr)
          {
            markerReserve = PointerMarkerWidth(indexLineH) + PointerMarkerDigitGap;
          }

          if (session.HasStudioBreakpointOnRow(row))
          {
            markerReserve = MathF.Max(
              markerReserve,
              BreakpointMarkerReserve(indexLineH) + PointerMarkerDigitGap);
          }
          ImGui.GetWindowDrawList().AddText(
            new Vector2(
              indexCellPos.X + MathF.Max(markerReserve, indexCellW - indexSize.X),
              indexCellPos.Y + MathF.Max(0f, (indexLineH - indexSize.Y) * 0.5f)),
            StudioMnemonicPaint.DefaultInk,
            indexText);

          ImGui.TableSetColumnIndex(1);
          StudioMnemonicPaint.DrawMachineLedCell(
            StudioMuseumKeycodes.FormatMachineDisplay(row, modelId),
            align: 0f);

          ImGui.TableSetColumnIndex(2);
          StudioMnemonicPaint.DrawMnemonicKeycaps(
            paint.KeysMnemonic,
            modelId,
            align: 0f,
            rowKind: row.Kind);

          ImGui.TableSetColumnIndex(3);
          StudioMnemonicPaint.DrawLegend(paint.Legend, paint.LegendKind, align: 0.5f);

          Vector2 click = CalcImGuiTouchInput.GetPointerPos();
          bool overRow = click.X >= rowMin.X
            && click.X < rowMin.X + rowHitW
            && click.Y >= rowMin.Y
            && click.Y < rowMin.Y + indexLineH;
          if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && overRow)
          {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !s_codeDragMoved)
            {
              session.SelectedProgramStep = row.Index;
              StudioPaneSync.OnCodeSelected(row.Index);
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && !s_codeDragMoved)
            {
              if (session.TrySetProgramStartStep(row.Index))
              {
                ShowStatusBalloon($"PTR → step {session.SelectedProgramStep}");
                StudioPaneSync.FollowPointer(session.SelectedProgramStep);
              }
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
              session.SelectedProgramStep = row.Index;
              StudioPaneSync.OnCodeSelected(row.Index);
              s_codeContextStep = row.Index;
              ImGui.OpenPopup("##studio-code-row-ctx");
            }
          }
        }

        if (ImGui.BeginPopup("##studio-code-row-ctx"))
        {
          if (ImGui.MenuItem("Set start point")
              && s_codeContextStep >= 0
              && session.TrySetProgramStartStep(s_codeContextStep))
          {
            ShowStatusBalloon($"Start → step {s_codeContextStep}");
            StudioPaneSync.FollowPointer(s_codeContextStep);
          }

          if (s_codeContextStep >= 0)
          {
            bool hasBp = session.HasStudioBreakpoint(s_codeContextStep);
            if (ImGui.MenuItem(hasBp ? "Remove breakpoint" : "Toggle breakpoint (F9)"))
            {
              bool added = session.ToggleStudioBreakpoint(s_codeContextStep);
              ShowStatusBalloon(
                added
                  ? $"Breakpoint + step {s_codeContextStep}"
                  : $"Breakpoint − step {s_codeContextStep}");
            }
          }

          ImGui.EndPopup();
        }

        if (StudioPaneSync.TryConsumeCodeFollow(out int followStep))
        {
          ScrollCodeToStep(rows, followStep);
        }

        ImGui.EndTable();
      }
    }

    ImGui.EndChild();

    StudioMnemonicPaint.PopListingScale();
    ImGui.PopStyleColor(8);
  }

  private static float MeasureProgramStatusFooterHeight(
    Session session,
    IReadOnlyList<StudioListingView.Row>? rows,
    float maxWidth)
  {
    ImGuiStylePtr style = ImGui.GetStyle();
    float lineH = ImGui.GetTextLineHeight();
    float chipH = MeasureRegisterChipHeight();
    float registerRows = MeasureRegisterStripRows(session, maxWidth);
    // Separator + Lines row + gap + Data/register band.
    return style.ItemSpacing.Y * 3f
      + 1f
      + lineH
      + style.ItemSpacing.Y
      + MathF.Max(lineH, registerRows * (chipH + style.ItemSpacing.Y));
  }

  private static float MeasureRegisterChipHeight()
  {
    Vector2 pad = new(7f, 3f);
    return ImGui.CalcTextSize("R8=0").Y + pad.Y * 2f;
  }

  private static float MeasureRegisterStripRows(Session session, float maxWidth)
  {
    _ = session;
    float labelW = ImGui.CalcTextSize("Data ").X;
    float avail = MathF.Max(40f, maxWidth - labelW);
    float gap = 8f;
    float x = 0f;
    int rows = 1;
    for (int r = 1; r <= 9; r++)
    {
      // Worst-case chip width for wrapping estimate.
      float chipW = ImGui.CalcTextSize($"R{r}=-1.23456e+10").X + 14f;
      if (r > 1 && x + gap + chipW > avail)
      {
        rows++;
        x = 0f;
      }

      x += (r == 1 || x <= 0f ? 0f : gap) + chipW;
    }

    return rows;
  }

  private static void DrawProgramStatusFooter(
    Session session,
    IReadOnlyList<StudioListingView.Row>? rows,
    float maxWidth)
  {
    ImGui.Spacing();
    ImGui.Separator();

    int totalLines = rows is { Count: > 0 }
      ? StudioListingView.MaxDisplayStepNumber(rows)
      : 0;
    ImGui.TextDisabled("Lines");
    ImGui.SameLine();
    ImGui.TextUnformatted(totalLines > 0 ? totalLines.ToString() : "—");

    ImGui.Spacing();
    ImGui.TextDisabled("Data");
    ImGui.SameLine();
    ImGui.PushFont(CalcFaceplateFonts.Arial);
    if (ImGui.SmallButton("Edit\u2026"))
    {
      if (session.TryGetLiveRegisters(out IReadOnlyList<double> regs))
      {
        CalcRegisterEditor.Open(regs);
      }
    }

    ImGui.PopFont();
    ImGui.SameLine();
    DrawRegisterStrip(session, maxWidth);

    if (CalcRegisterEditor.Draw(out double[]? committed) && committed is not null)
    {
      if (session.TrySetLiveRegisters(committed))
      {
        ShowStatusBalloon("Registers updated.");
      }
    }
  }

  private static void DrawRegisterStrip(Session session, float maxWidth)
  {
    // Live firmware registers (RUN STO updates), not the card file snapshot.
    IReadOnlyList<double> regs = session.TryGetLiveRegisters(out IReadOnlyList<double> live)
      ? live
      : [];

    Vector4 ink = new(1f, 1f, 1f, 1f);
    Vector4 chip = new(0.06f, 0.06f, 0.07f, 1f);
    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    float gap = 8f;
    Vector2 pad = new(7f, 3f);
    float rowStartX = ImGui.GetCursorScreenPos().X;
    float availW = MathF.Max(40f, MathF.Min(maxWidth, ImGui.GetContentRegionAvail().X));

    for (int r = 1; r <= 9; r++)
    {
      double v = r < regs.Count ? regs[r] : 0d;
      string text = $"R{r}={FormatCardRegisterValue(v)}";
      Vector2 textSize = ImGui.CalcTextSize(text);
      Vector2 chipSize = textSize + pad * 2f;
      Vector2 p0 = ImGui.GetCursorScreenPos();
      if (r > 1 && (p0.X - rowStartX) + chipSize.X > availW)
      {
        ImGui.NewLine();
        p0 = ImGui.GetCursorScreenPos();
        rowStartX = p0.X;
      }
      else if (r > 1)
      {
        ImGui.SameLine(0f, gap);
        p0 = ImGui.GetCursorScreenPos();
      }

      draw.AddRectFilled(p0, p0 + chipSize, ImGui.ColorConvertFloat4ToU32(chip), 3f);
      ImGui.SetCursorScreenPos(p0);
      _ = ImGui.InvisibleButton($"##reg-chip-{r}", chipSize);
      if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
      {
        CalcRegisterEditor.Open(regs, focusRegister: r);
      }

      if (ImGui.IsItemHovered())
      {
        ImGui.SetTooltip($"Double-click to edit R{r}");
      }

      ImGui.SetCursorScreenPos(p0 + pad);
      ImGui.PushStyleColor(ImGuiCol.Text, ink);
      ImGui.TextUnformatted(text);
      ImGui.PopStyleColor();
      ImGui.SetCursorScreenPos(new Vector2(p0.X + chipSize.X, p0.Y));
      ImGui.Dummy(new Vector2(0f, chipSize.Y));
    }
  }

  private static float MeasureCardDataFooterHeight(Session session) => 0f;

  private static string FormatCardRegisterValue(double value) =>
    Math.Abs(value - Math.Round(value)) < 1e-9
      ? Math.Round(value).ToString("0", System.Globalization.CultureInfo.InvariantCulture)
      : value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture);

  /// <summary>
  /// Fixed <see cref="ImGuiTableColumnFlags.WidthFixed"/> request widths under listing font scale.
  /// WidthRequest is the cell work area (ImGui adds CellPadding outside it). Legend is at least
  /// full <c>CalcTextSize("Legend")</c> plus FramePadding (TableHeader clip) and breath, and at
  /// least the widest legend glyph content — verified under <see cref="StudioMnemonicPaint.StudioListingScale"/>.
  /// </summary>
  private static CodeTableWidths MeasureCodeTableWidths(
    IReadOnlyList<StudioListingView.Row>? rows,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions = null)
  {
    float s = StudioMnemonicPaint.StudioListingScale;
    ImGuiStylePtr style = ImGui.GetStyle();
    // Breath inside WidthRequest so TableHeader / centered glyphs don't clip to "Lege...".
    float breath = style.CellPadding.X * 2f;
    // TableHeader Selectable uses FramePadding; without it the header ellipsizes first.
    float headerChrome = style.FramePadding.X * 2f;

    // # column: PC ▶ + gap + right-aligned display index. Numbers advance by StepSpan
    // (merged pairs skip by 2), so width must fit at least 2 digits and grow for 3+.
    float lineH = StudioMnemonicPaint.ListingRowContentHeight();
    int maxDisplayIndex = Math.Max(StudioListingView.MaxDisplayStepNumber(rows ?? []), 99);
    float digitsW = ImGui.CalcTextSize(maxDisplayIndex.ToString()).X;
    float indexContent =
      PointerMarkerWidth(lineH) + PointerMarkerDigitGap + digitsW;
    float index = MathF.Max(
      indexContent,
      ImGui.CalcTextSize("#").X + breath + headerChrome);
    float machine = MathF.Max(MachineColumnWidthRef * s, ImGui.CalcTextSize("Machine").X + breath + headerChrome);
    float keys = MathF.Max(KeysColumnWidthRef * s, ImGui.CalcTextSize("Keys").X + breath + headerChrome);
    // Min width must keep the full "Legend" label visible when centered at StudioListingScale.
    float legend = ImGui.CalcTextSize("Legend").X + breath + headerChrome;

    if (rows is { Count: > 0 })
    {
      for (int i = 0; i < rows.Count; i++)
      {
        StudioListingView.Paint paint = StudioListingView.ResolvePaint(
          rows[i],
          modelId,
          cardStripCaptions);
        float content = StudioMnemonicPaint.MeasureLegendContentWidth(
          paint.Legend,
          paint.LegendKind);
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

  private static int FindRowForStep(IReadOnlyList<StudioListingView.Row> rows, int step)
  {
    for (int i = 0; i < rows.Count; i++)
    {
      if (rows[i].ContainsIndex(step))
      {
        return i;
      }
    }

    return -1;
  }

  /// <summary>Align the code table so <paramref name="step"/> is visible (no yank if already on screen).</summary>
  private static void ScrollCodeToStep(IReadOnlyList<StudioListingView.Row> rows, int step)
  {
    int rowIndex = FindRowForStep(rows, step);
    if (rowIndex < 0)
    {
      return;
    }

    float rowH = StudioMnemonicPaint.ListingRowContentHeight();
    float target = rowIndex * rowH;
    float viewH = MathF.Max(1f, ImGui.GetWindowSize().Y);
    float cur = ImGui.GetScrollY();
    // Already in view — do not snap (Code scrollbar release / soft follow).
    if (target >= cur + rowH * 0.25f && target + rowH <= cur + viewH - rowH * 0.25f)
    {
      return;
    }

    ImGui.SetScrollY(Math.Clamp(target - rowH, 0f, ImGui.GetScrollMaxY()));
    CalcFaceplatePointer.SyncOwnedScrollYFromWindow();
  }

  /// <summary>Drag empty Code listing area to scroll (same feel as FC canvas pan).</summary>
  private static void UpdateCodeTableDragScroll(float rowH)
  {
    _ = rowH;
    Vector2 mouse = CalcImGuiTouchInput.GetPointerPos();
    bool hovered = ImGui.IsWindowHovered(
      ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
      | ImGuiHoveredFlags.ChildWindows);
    if (!hovered)
    {
      s_codeDragArmed = false;
      return;
    }

    CalcFaceplatePointer.MarkScrollableUiHovered();
    float sb = ImGui.GetStyle().ScrollbarSize;
    Vector2 winPos = ImGui.GetWindowPos();
    Vector2 winSize = ImGui.GetWindowSize();
    bool overScrollbar = mouse.X >= winPos.X + winSize.X - sb - 1f;

    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !overScrollbar)
    {
      s_codeDragArmed = true;
      s_codeDragOrigin = mouse;
      s_codeDragLast = mouse;
      s_codeDragMoved = false;
    }

    if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      s_codeDragArmed = false;
      s_codeDragMoved = false;
      CalcFaceplatePointer.SyncOwnedScrollYFromWindow();
      return;
    }

    if (!s_codeDragArmed || overScrollbar)
    {
      return;
    }

    Vector2 delta = ImGui.GetIO().MouseDelta;
    if (MathF.Abs(delta.X) < 1e-6f && MathF.Abs(delta.Y) < 1e-6f)
    {
      delta = mouse - s_codeDragLast;
    }

    float dist = Vector2.Distance(mouse, s_codeDragOrigin);
    // Ignore tiny motion so row click-select still works.
    if (!s_codeDragMoved && dist < 5f)
    {
      return;
    }

    s_codeDragMoved = true;
    if (MathF.Abs(delta.Y) > 0.25f)
    {
      float next = Math.Clamp(
        ImGui.GetScrollY() - delta.Y,
        0f,
        MathF.Max(0f, ImGui.GetScrollMaxY()));
      ImGui.SetScrollY(next);
      CalcFaceplatePointer.SyncOwnedScrollYFromWindow();
    }

    s_codeDragLast = mouse;
  }

  /// <summary>Code child width so the table is content-sized (padding + border around table).</summary>
  private static float MeasureCodePaneWidth(in CodeTableWidths widths)
  {
    ImGuiStylePtr style = ImGui.GetStyle();
    float chrome = style.WindowPadding.X * 2f + style.ChildBorderSize * 2f;
    return MeasureTableOuterWidth(widths) + chrome;
  }

  /// <summary>Horizontal span of the PC ▶ (left pad + tip), for layout / digit reserve.</summary>
  private static float PointerMarkerWidth(float lineHeight) =>
    PointerMarkerLeftPad + lineHeight * PointerMarkerHeightFrac * PointerMarkerTipFrac;

  private static float BreakpointMarkerReserve(float lineHeight) =>
    PointerMarkerLeftPad + lineHeight * 0.42f;

  /// <summary>
  /// Small right-pointing triangle in the # column for the live Classic PTR / program step.
  /// </summary>
  private static void DrawPointerMarker(Vector2 cellMin, float lineHeight)
  {
    float h = lineHeight * PointerMarkerHeightFrac;
    float cy = cellMin.Y + lineHeight * 0.5f;
    float left = cellMin.X + PointerMarkerLeftPad;
    float tipX = left + h * PointerMarkerTipFrac;
    ImGui.GetWindowDrawList().AddTriangleFilled(
      new Vector2(left, cy - h * 0.5f),
      new Vector2(tipX, cy),
      new Vector2(left, cy + h * 0.5f),
      StudioMnemonicPaint.PointerMarkerInk);
  }

  /// <summary>Filled disc in the # column for a Studio breakpoint (left of digits / under ▶).</summary>
  private static void DrawBreakpointMarker(Vector2 cellMin, float lineHeight, bool atPtr)
  {
    float r = lineHeight * 0.18f;
    float cx = cellMin.X + PointerMarkerLeftPad + r;
    // Sit under the ▶ tip when both markers share the cell.
    float cy = atPtr
      ? cellMin.Y + lineHeight * 0.72f
      : cellMin.Y + lineHeight * 0.5f;
    ImGui.GetWindowDrawList().AddCircleFilled(
      new Vector2(cx, cy),
      r,
      StudioMnemonicPaint.BreakpointMarkerInk);
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

  private static void DrawFlowchartPane(
    Session session,
    IReadOnlyList<StudioListingView.Row>? rows)
  {
    if (rows is null || rows.Count == 0)
    {
      ImGui.TextUnformatted("Flowchart");
      ImGui.Spacing();
      ImGui.TextDisabled(
        session.SupportsCardProgram
          ? "Load a card / enter steps to see control flow."
          : "Not available for this model.");
      return;
    }

    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      ImGui.TextUnformatted("Flowchart");
      ImGui.TextDisabled("No listing.");
      return;
    }

    int pointerHighlight = StudioListingView.ResolvePointerHighlightIndex(lines, rows);
    StudioMnemonicPaint.PushListingScale();
    ImGui.PushStyleColor(ImGuiCol.ChildBg, StudioMnemonicPaint.CodePaneBg);
    ImGui.PushStyleColor(ImGuiCol.Text, StudioMnemonicPaint.DefaultInk);
    StudioFlowchartView.Draw(session, rows, pointerHighlight);
    ImGui.PopStyleColor(2);
    StudioMnemonicPaint.PopListingScale();
  }
}
