using System.Diagnostics;
using ImGuiNET;
using TeoCalc.Core;
using TeoCalc.Core.Engine.Classic;
using TeoTheme;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Studio Docs tab — in-place authoring reference + openable external links.
/// </summary>
public static class StudioDocsPane
{
  private readonly record struct DocLink(string Title, string Url, string Blurb);

  private static readonly DocLink[] Links =
  [
    new(
      "HP Museum - Classic software",
      "https://www.hpmuseum.org/software.htm",
      "Historical card programs and pac notes (external)."),
    new(
      "HP Museum - HP-65",
      "https://www.hpmuseum.org/hp65.htm",
      "Model overview and links for the T-65 lineage."),
  ];

  public static void Draw(Session session)
  {
    ImGui.TextUnformatted("Docs");
    ImGui.TextDisabled("In-place reference while authoring | links open in the browser.");
    ImGui.Spacing();

    DrawLoadedCard(session);
    ImGui.Separator();
    DrawAdvisories(session);
    ImGui.Separator();
    DrawStudioCheatsheet();
    ImGui.Separator();
    DrawEncodingNotes();
    ImGui.Separator();
    DrawLinks();
    ImGui.Separator();
    DrawSelectionHint(session);
  }

  private static void DrawAdvisories(Session session)
  {
    CalcDebugChrome.SectionHeader("Advisories");
    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) || lines.Count == 0)
    {
      CalcDebugChrome.Muted("No program loaded - advisories appear when Code has steps.");
      return;
    }

    IReadOnlyList<StudioListingView.Row> rows = session.BuildStudioListingRows(lines);
    IReadOnlyList<StudioProgramAdvisories.Advisory> advisories = StudioProgramAdvisories.Analyze(rows);
    if (advisories.Count == 0)
    {
      CalcDebugChrome.Muted(
        "No advisories (loops, missing LBL, open branch, NOP streaks, duplicate RTN/R/S). Heuristic only.");
      return;
    }

    CalcAppTheme.EnsureInitialized();
    ImGui.PushStyleColor(
      ImGuiCol.Text,
      CalcAppThemeColors.ToVector4(CalcAppTheme.Current, ThemeTokens.TextWarningColor));
    foreach (StudioProgramAdvisories.Advisory a in advisories)
    {
      ImGui.Bullet();
      ImGui.SameLine();
      ImGui.TextWrapped($"step {a.FirstStep}: {a.Message}");
    }

    ImGui.PopStyleColor();
    CalcDebugChrome.Muted("Hints only - not auto-fixes. Review before deleting steps.");
  }

  private static void DrawLoadedCard(Session session)
  {
    ImGui.TextUnformatted("Loaded card");
    if (!session.CardInserted || session.LoadedTeoCard is null)
    {
      ImGui.TextDisabled("No card inserted - load a .t65/.t67 to see Usage / RunHint here.");
      return;
    }

    string title = session.CardTitle ?? Path.GetFileName(session.LoadedCardPath) ?? "(untitled)";
    ImGui.TextWrapped(title);
    if (!string.IsNullOrWhiteSpace(session.CardCategory) || !string.IsNullOrWhiteSpace(session.CardAuthor))
    {
      ImGui.TextDisabled(
        string.Join(
          " | ",
          new[] { session.CardCategory, session.CardAuthor }.Where(static s => !string.IsNullOrWhiteSpace(s))));
    }

    if (!string.IsNullOrWhiteSpace(session.CardUsage))
    {
      ImGui.Spacing();
      ImGui.TextDisabled("Usage");
      ImGui.TextWrapped(session.CardUsage);
    }

    if (!string.IsNullOrWhiteSpace(session.CardRunHint))
    {
      ImGui.Spacing();
      ImGui.TextDisabled("Run hint");
      ImGui.TextWrapped(session.CardRunHint);
    }

    ImGui.Spacing();
    ImGui.TextDisabled("Edit Title / Labels / Usage on the Card tab.");
  }

  private static void DrawStudioCheatsheet()
  {
    ImGui.TextUnformatted("Studio shortcuts");
    Bullet("F2 power | F4 W/PRGM <-> RUN | F5 continue | F6 break");
    Bullet("F9 breakpoint | F10 step over (Code) | F11 step into");
    Bullet("Up/Down current line (W/PRGM) | Ins NOP | Del line | Ctrl+Z/Y undo/redo");
    Bullet("Ctrl+S save | Ctrl+R revert | Ctrl+C/V/X clipboard | Ctrl+F find");
    Bullet("Alt+hover faceplate key -> help balloon");
    Bullet("Text tab: Machine | Keys free text | completions | Apply / Ctrl+Enter");
    Bullet("ROM tab: microcode watch (same as Debug Follow ROM)");
  }

  private static void DrawEncodingNotes()
  {
    ImGui.TextUnformatted("Card / encoding");
    Bullet(".t65 / .t67 - TeoCalc card text ([General], [Label], [Code], [Data])");
    Bullet("CodeEncoding mnemonic (default) or machine (one internal byte per line)");
    Bullet("Museum LED pairs (e.g. 34 01) are display codes - not always identical to RAM bytes");
    Bullet("Labels A-E drive the faceplate strip and FC legends");
    Bullet("Legacy .t6x still loads; prefer model-matched .t65 / .t67 on save");
  }

  private static void DrawLinks()
  {
    ImGui.TextUnformatted("Links");
    ImGui.BulletText("TeoCalc README (local)");
    ImGui.SameLine();
    if (ImGui.SmallButton("Open##local-readme"))
    {
      TryOpenLocalReadme();
    }

    ImGui.TextDisabled("Solution layout / build notes from the repo root.");

    foreach (DocLink link in Links)
    {
      ImGui.BulletText(link.Title);
      ImGui.SameLine();
      if (ImGui.SmallButton($"Open##{link.Url}"))
      {
        if (TryOpenUrl(link.Url))
        {
          CalcStudioPanelComponent.ShowKeyboardStatus($"Opening {link.Title}...");
        }
        else
        {
          CalcStudioPanelComponent.ShowKeyboardStatus("Could not open link.");
        }
      }

      ImGui.TextDisabled(link.Blurb);
    }
  }

  private static void DrawSelectionHint(Session session)
  {
    ImGui.TextUnformatted("Selection");
    if (session.SelectedProgramStep < 0)
    {
      ImGui.TextDisabled("Select a Code row to see its step index here.");
      return;
    }

    ImGui.TextDisabled($"Selected RAM step: {session.SelectedProgramStep}");
    if (!session.TryGetProgramListing(out IReadOnlyList<ClassicProgramLine> lines) || lines.Count == 0)
    {
      ImGui.TextDisabled("No listing row for selection.");
      return;
    }

    IReadOnlyList<StudioListingView.Row> rows = session.BuildStudioListingRows(lines);
    for (int i = 0; i < rows.Count; i++)
    {
      if (!rows[i].ContainsIndex(session.SelectedProgramStep))
      {
        continue;
      }

      StudioListingView.Paint paint = StudioListingView.ResolvePaint(
        rows[i],
        session.EngineModelId,
        session.CardStripLabels);
      ImGui.TextWrapped($"Keys: {paint.KeysMnemonic}");
      string museum = StudioMuseumKeycodes.FormatMachineDisplay(rows[i], session.EngineModelId);
      if (!string.IsNullOrWhiteSpace(museum))
      {
        ImGui.TextDisabled($"Machine: {museum}");
      }

      return;
    }

    ImGui.TextDisabled("No listing row for selection.");
  }

  private static void Bullet(string text)
  {
    ImGui.Bullet();
    ImGui.SameLine();
    ImGui.TextWrapped(text);
  }

  private static bool TryOpenUrl(string url)
  {
    try
    {
      Process.Start(new ProcessStartInfo
      {
        FileName = url,
        UseShellExecute = true,
      });
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static void TryOpenLocalReadme()
  {
    try
    {
      string path = Path.Combine(TeoCalcPaths.FindRepositoryRoot(), "README.md");
      if (!File.Exists(path))
      {
        CalcStudioPanelComponent.ShowKeyboardStatus("README.md not found.");
        return;
      }

      Process.Start(new ProcessStartInfo
      {
        FileName = path,
        UseShellExecute = true,
      });
      CalcStudioPanelComponent.ShowKeyboardStatus("Opened README.md");
    }
    catch
    {
      CalcStudioPanelComponent.ShowKeyboardStatus("Could not open README.md.");
    }
  }
}
