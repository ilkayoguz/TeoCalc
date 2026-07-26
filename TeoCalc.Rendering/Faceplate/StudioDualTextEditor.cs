using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// W/PRGM dual free-text editor: Machine | Keys. Apply commits via
/// <see cref="StudioProgramEditorText.TryParseDual"/> → RAM.
/// Keys/Machine panes offer prefix completions from the model vocabulary.
/// </summary>
public static class StudioDualTextEditor
{
  private enum Pane
  {
    None,
    Machine,
    Keys,
  }

  private static string s_machine = string.Empty;
  private static string s_keys = string.Empty;
  private static string s_fingerprint = string.Empty;
  private static bool s_dirty;
  private static bool s_ramDrift;
  private static string s_lastError = string.Empty;

  private static Pane s_activePane;
  private static int s_cursorPos;
  private static int s_completionIndex;
  private static bool s_completionOpen;
  private static IReadOnlyList<string> s_completionMatches = [];
  private static StudioProgramCompletions.TokenSpan s_completionToken;
  private static IReadOnlyList<string>? s_mnemonicCache;
  private static IReadOnlyList<string>? s_machineCache;
  private static string s_candidateCacheKey = string.Empty;

  public static bool IsDirty => s_dirty;

  public static void Draw(Session session)
  {
    if (!session.SupportsCardProgram)
    {
      ImGui.TextDisabled("Program memory not available.");
      return;
    }

    MaybeHydrateFromRam(session);
    EnsureCandidateCache(session);

    ImGui.TextUnformatted("Text");
    ImGui.SameLine();
    ImGui.TextDisabled("Machine | Keys | completions | Apply (Ctrl+Enter)");
    if (s_dirty)
    {
      ImGui.SameLine();
      ImGui.TextColored(new Vector4(0.95f, 0.75f, 0.35f, 1f), "unsaved text");
    }

    if (s_ramDrift && s_dirty)
    {
      ImGui.Spacing();
      ImGui.TextWrapped("RAM changed (faceplate / Ins / Del). Revert to RAM or Keep editing, then Apply.");
      if (ImGui.Button("Revert to RAM"))
      {
        ForceHydrate(session);
        CalcStudioPanelComponent.ShowKeyboardStatus("Text reverted to RAM.");
      }

      ImGui.SameLine();
      if (ImGui.Button("Keep editing"))
      {
        if (session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> live))
        {
          s_fingerprint = StudioProgramEditorText.Fingerprint(live);
        }

        s_ramDrift = false;
      }

      ImGui.Spacing();
    }

    float toolbarH = ImGui.GetFrameHeightWithSpacing();
    float errH = s_lastError.Length > 0 ? ImGui.GetTextLineHeightWithSpacing() * 2f : 0f;
    float listReserve = s_completionOpen ? ImGui.GetTextLineHeightWithSpacing() * 8f : 0f;
    float availY = MathF.Max(80f, ImGui.GetContentRegionAvail().Y - toolbarH - errH - listReserve - 4f);
    float gap = ImGui.GetStyle().ItemSpacing.X;
    float half = MathF.Max(120f, (ImGui.GetContentRegionAvail().X - gap) * 0.5f);

    ImGui.BeginChild("##dual-machine-col", new Vector2(half, availY), ImGuiChildFlags.Border);
    ImGui.TextDisabled("Machine");
    float editH = MathF.Max(40f, ImGui.GetContentRegionAvail().Y);
    DrawPaneEditor(
      session,
      Pane.Machine,
      "##dual-machine",
      ref s_machine,
      editH,
      s_machineCache ?? []);
    ImGui.EndChild();

    ImGui.SameLine();
    ImGui.BeginChild("##dual-keys-col", new Vector2(half, availY), ImGuiChildFlags.Border);
    ImGui.TextDisabled("Keys");
    editH = MathF.Max(40f, ImGui.GetContentRegionAvail().Y);
    DrawPaneEditor(
      session,
      Pane.Keys,
      "##dual-keys",
      ref s_keys,
      editH,
      s_mnemonicCache ?? []);
    ImGui.EndChild();

    if (s_completionOpen)
    {
      DrawCompletionList(session);
    }

    bool ctrl = ImGui.GetIO().KeyCtrl;
    bool ctrlEnter = ctrl && ImGui.IsKeyPressed(ImGuiKey.Enter, repeat: false);
    if (ImGui.Button("Apply") || (ctrlEnter && s_dirty && !s_completionOpen))
    {
      TryApply(session);
    }

    ImGui.SameLine();
    if (ImGui.Button("Reload from RAM"))
    {
      ForceHydrate(session);
      CalcStudioPanelComponent.ShowKeyboardStatus("Text reloaded from RAM.");
    }
    else if (ImGui.IsItemHovered())
    {
      CalcAppTooltip.Set(
        s_dirty
          ? "Discard local edits and reload from program RAM."
          : "Reload text from program RAM.");
    }

    if (s_dirty && !s_ramDrift)
    {
      ImGui.SameLine();
      if (ImGui.Button("Discard"))
      {
        ForceHydrate(session);
        CalcStudioPanelComponent.ShowKeyboardStatus("Text discarded.");
      }
    }

    if (s_lastError.Length > 0)
    {
      ImGui.TextColored(new Vector4(0.95f, 0.45f, 0.4f, 1f), s_lastError);
    }
  }

  private static void DrawPaneEditor(
    Session session,
    Pane pane,
    string id,
    ref string buffer,
    float editH,
    IReadOnlyList<string> candidates)
  {
    string before = buffer;
    if (ImGui.InputTextMultiline(
          id,
          ref buffer,
          64 * 1024,
          new Vector2(-1f, editH)))
    {
      s_dirty = true;
      s_lastError = string.Empty;
      s_activePane = pane;
      s_cursorPos = EstimateCursor(before, buffer);
      SyncOpposite(session, pane);
      RefreshCompletions(buffer, candidates);
    }

    if (ImGui.IsItemActive() || ImGui.IsItemFocused())
    {
      s_activePane = pane;
      if (s_cursorPos < 0 || s_cursorPos > buffer.Length)
      {
        s_cursorPos = buffer.Length;
      }

      RefreshCompletions(buffer, candidates);
      HandleCompletionKeys(session, ref buffer, candidates);
    }
  }

  private static void SyncOpposite(Session session, Pane pane)
  {
    if (pane == Pane.Machine)
    {
      s_keys = StudioProgramEditorText.SyncDocumentFromMachine(
        s_machine,
        session.EngineModelId,
        session.FormatProgramCodeForEditor);
    }
    else if (pane == Pane.Keys)
    {
      s_machine = StudioProgramEditorText.SyncDocumentFromKeys(
        s_keys,
        session.EngineModelId,
        session.ResolveProgramMnemonicForEditor,
        session.FormatProgramCodeForEditor);
    }
  }

  private static void RefreshCompletions(string buffer, IReadOnlyList<string> candidates)
  {
    if (!StudioProgramCompletions.TryExtractToken(buffer, s_cursorPos, out StudioProgramCompletions.TokenSpan token)
        || token.Text.Length < 1)
    {
      CloseCompletions();
      return;
    }

    IReadOnlyList<string> matches = StudioProgramCompletions.Filter(candidates, token.Text);
    if (matches.Count == 0)
    {
      CloseCompletions();
      return;
    }

    // Exact full match alone → no popup.
    if (matches.Count == 1
        && string.Equals(matches[0], token.Text, StringComparison.OrdinalIgnoreCase))
    {
      CloseCompletions();
      return;
    }

    if (!s_completionOpen
        || !string.Equals(s_completionToken.Text, token.Text, StringComparison.Ordinal)
        || s_completionToken.Start != token.Start)
    {
      s_completionIndex = 0;
    }

    s_completionOpen = true;
    s_completionMatches = matches;
    s_completionToken = token;
    s_completionIndex = Math.Clamp(s_completionIndex, 0, matches.Count - 1);
  }

  private static void HandleCompletionKeys(
    Session session,
    ref string buffer,
    IReadOnlyList<string> candidates)
  {
    if (!s_completionOpen)
    {
      return;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.Escape, repeat: false))
    {
      CloseCompletions();
      return;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, repeat: true))
    {
      s_completionIndex = Math.Max(0, s_completionIndex - 1);
      return;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, repeat: true))
    {
      s_completionIndex = Math.Min(s_completionMatches.Count - 1, s_completionIndex + 1);
      return;
    }

    bool accept = ImGui.IsKeyPressed(ImGuiKey.Tab, repeat: false)
      || (ImGui.IsKeyPressed(ImGuiKey.Enter, repeat: false) && !ImGui.GetIO().KeyCtrl);
    if (!accept || s_completionMatches.Count == 0)
    {
      return;
    }

    AcceptCompletion(session, ref buffer, s_completionMatches[s_completionIndex], candidates);
  }

  private static void AcceptCompletion(
    Session session,
    ref string buffer,
    string replacement,
    IReadOnlyList<string> candidates)
  {
    buffer = StudioProgramCompletions.ReplaceToken(buffer, s_completionToken, replacement);
    s_cursorPos = s_completionToken.Start + replacement.Length;
    s_dirty = true;
    s_lastError = string.Empty;
    if (s_activePane == Pane.Machine)
    {
      s_machine = buffer;
    }
    else if (s_activePane == Pane.Keys)
    {
      s_keys = buffer;
    }

    SyncOpposite(session, s_activePane);
    CloseCompletions();
    RefreshCompletions(buffer, candidates);
  }

  private static void DrawCompletionList(Session session)
  {
    ImGui.Spacing();
    ImGui.TextDisabled(
      s_activePane == Pane.Machine ? "Machine completions" : "Keys completions");
    float rowH = ImGui.GetTextLineHeightWithSpacing();
    float height = MathF.Min(rowH * 8f, rowH * (s_completionMatches.Count + 0.5f));
    if (!ImGui.BeginChild("##dual-completions", new Vector2(-1f, height), ImGuiChildFlags.Border))
    {
      ImGui.EndChild();
      return;
    }

    for (int i = 0; i < s_completionMatches.Count; i++)
    {
      string item = s_completionMatches[i];
      bool selected = i == s_completionIndex;
      if (ImGui.Selectable(item, selected))
      {
        if (s_activePane == Pane.Machine)
        {
          AcceptCompletion(session, ref s_machine, item, s_machineCache ?? []);
        }
        else if (s_activePane == Pane.Keys)
        {
          AcceptCompletion(session, ref s_keys, item, s_mnemonicCache ?? []);
        }
      }

      if (selected)
      {
        ImGui.SetItemDefaultFocus();
      }
    }

    ImGui.EndChild();
  }

  private static void CloseCompletions()
  {
    s_completionOpen = false;
    s_completionMatches = [];
    s_completionIndex = 0;
  }

  private static void EnsureCandidateCache(Session session)
  {
    string key = $"{session.EngineModelId}|{session.UsesActCardProgram}";
    if (string.Equals(key, s_candidateCacheKey, StringComparison.Ordinal)
        && s_mnemonicCache is not null
        && s_machineCache is not null)
    {
      return;
    }

    s_candidateCacheKey = key;
    s_mnemonicCache = session.EnumerateProgramMnemonics();
    s_machineCache = session.EnumerateMachineCompletionTokens();
  }

  private static int EstimateCursor(string before, string after)
  {
    int i = 0;
    int min = Math.Min(before.Length, after.Length);
    while (i < min && before[i] == after[i])
    {
      i++;
    }

    if (after.Length >= before.Length)
    {
      return Math.Min(after.Length, i + (after.Length - before.Length));
    }

    return i;
  }

  private static void MaybeHydrateFromRam(Session session)
  {
    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      return;
    }

    string fp = StudioProgramEditorText.Fingerprint(lines);
    if (string.Equals(fp, s_fingerprint, StringComparison.Ordinal))
    {
      s_ramDrift = false;
      return;
    }

    if (s_dirty)
    {
      s_ramDrift = true;
      return;
    }

    ApplyHydrate(lines, session.EngineModelId, fp);
  }

  private static void ForceHydrate(Session session)
  {
    CloseCompletions();
    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      s_machine = string.Empty;
      s_keys = string.Empty;
      s_fingerprint = string.Empty;
      s_dirty = false;
      s_ramDrift = false;
      s_lastError = string.Empty;
      return;
    }

    ApplyHydrate(lines, session.EngineModelId, StudioProgramEditorText.Fingerprint(lines));
  }

  private static void ApplyHydrate(
    IReadOnlyList<ClassicProgramLine> lines,
    string modelId,
    string fingerprint)
  {
    StudioProgramEditorText.Hydrate(lines, modelId, out s_machine, out s_keys);
    s_fingerprint = fingerprint;
    s_dirty = false;
    s_ramDrift = false;
    s_lastError = string.Empty;
    CloseCompletions();
  }

  private static void TryApply(Session session)
  {
    CloseCompletions();
    if (!StudioProgramEditorText.TryParseDual(
          s_machine,
          s_keys,
          session.EngineModelId,
          session.ResolveProgramMnemonicForEditor,
          session.FormatProgramCodeForEditor,
          out List<byte> codes,
          out string? parseError))
    {
      s_lastError = parseError ?? "Parse failed.";
      CalcStudioPanelComponent.ShowKeyboardStatus(s_lastError);
      return;
    }

    if (!session.TryApplyProgramCodes(codes, out string? applyError))
    {
      s_lastError = applyError ?? "Apply failed.";
      CalcStudioPanelComponent.ShowKeyboardStatus(s_lastError);
      return;
    }

    s_lastError = string.Empty;
    s_dirty = false;
    s_ramDrift = false;
    if (session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines))
    {
      ApplyHydrate(lines, session.EngineModelId, StudioProgramEditorText.Fingerprint(lines));
    }

    CalcStudioPanelComponent.ShowKeyboardStatus(
      session.StudioStatusMessage.Length > 0 ? session.StudioStatusMessage : "Applied.");
    session.StudioStatusMessage = string.Empty;
  }
}
