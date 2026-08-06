using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Teo.Locale;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;
using Teo.Theme;
using TeoCalc.Core.Catalog;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>About as BeginPopupModal owned by a fullscreen capture host.</summary>
public static class CalcAboutModal
{
  private static bool s_pendingOpen;
  private static bool s_open;
  private static bool s_openPopup;

  public static void RequestOpen() => s_pendingOpen = true;

  public static bool IsOpen => s_open || s_pendingOpen;

  public static void PrepareOpen()
  {
    if (!s_pendingOpen || s_open)
      return;

    s_pendingOpen = false;
    s_open = true;
    s_openPopup = true;
  }

  public static void Draw(Session session, CalcModelDefinition faceplateModel)
  {
    PrepareOpen();
    if (!s_open && !s_openPopup && !ImGui.IsPopupOpen("##teo-about"))
      return;

    CalcAppTheme.EnsureInitialized();
    CalcLocalization.EnsureInitialized();
    LanguagePreference uiLang = CalcLocalization.Preference;
    Vector2 display = ImGui.GetIO().DisplaySize;

    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(display);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.45f));
    ImGui.Begin(
      "##teo-about-modal-host",
      ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoSavedSettings
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoNavInputs
        | ImGuiWindowFlags.NoBringToFrontOnFocus);
    ImGui.InvisibleButton("##teo-about-scrim", display);

    if (s_openPopup)
    {
      ImGui.OpenPopup("##teo-about");
      s_openPopup = false;
    }

    ImGui.PushFont(CalcFaceplateFonts.Ui);
    bool open = s_open || ImGui.IsPopupOpen("##teo-about");
    if (!ImGuiModalHost.Begin(
          "##teo-about",
          DialogStyles.AboutTitle,
          CalcAppTheme.Current,
          ref open,
          minContentWidth: 260f))
    {
      ImGui.PopFont();
      ImGui.End();
      ImGui.PopStyleColor();
      ImGui.PopStyleVar(2);
      s_open = open;
      return;
    }

    ImGui.TextUnformatted("TeoCalc");
    ImGui.TextDisabled(faceplateModel.LogoCaption);
    ImGui.Separator();
    ImGui.TextUnformatted(faceplateModel.ProductLabel);
    ImGui.TextDisabled($"{CalcUiText.Family(uiLang)}: {session.Model.Family}");
    if (session.Model.Hardware?.RomWordCount is int romWords and > 0)
      ImGui.TextDisabled($"{CalcUiText.RomWords(uiLang)}: {romWords}");

    Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
    if (ver is not null)
      ImGui.TextDisabled($"{CalcUiText.Build(uiLang)}: {ver}");

    ImGui.Spacing();
    if (ImGuiModalHost.Button(
          DialogButtonRole.Affirmative,
          CalcUiText.Close(uiLang),
          new Vector2(120f, 0f)))
    {
      open = false;
      ImGui.CloseCurrentPopup();
    }

    ImGuiModalHost.End();
    ImGui.PopFont();
    ImGui.End();
    ImGui.PopStyleColor();
    ImGui.PopStyleVar(2);
    s_open = open;
  }

  public static void HandleMarkInteraction(RectF markHit)
  {
    ImGui.SetCursorScreenPos(markHit.Min);
    _ = ImGui.InvisibleButton("##teo-mark", new Vector2(markHit.Width, markHit.Height));
    if (ImGui.IsItemHovered())
    {
      ImGuiPointerStyle.MarkLastItemClickable();
      CalcAppTooltip.Set(CalcUiText.About(CalcLocalization.Preference));
    }

    if (ImGui.IsItemClicked())
      RequestOpen();
  }
}
