using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;
using TeoCalc.Core.Catalog;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Mini About modal opened from the Teo mark on the logo band.</summary>
public static class CalcAboutModal
{
  private static bool s_openRequested;
  private static bool s_open;

  public static void RequestOpen() => s_openRequested = true;

  public static bool IsOpen => s_open || s_openRequested;

  public static void Draw(Session session, CalcModelDefinition faceplateModel)
  {
    if (s_openRequested)
    {
      ImGui.OpenPopup("##teo-about");
      s_openRequested = false;
      s_open = true;
    }

    if (!s_open && !ImGui.IsPopupOpen("##teo-about"))
    {
      return;
    }

    CalcAppTheme.EnsureInitialized();
    bool open = s_open || ImGui.IsPopupOpen("##teo-about");
    if (!ImGuiModalHost.Begin(
          "##teo-about",
          DialogStyles.AboutTitle,
          CalcAppTheme.Current,
          ref open,
          minContentWidth: 260f))
    {
      s_open = open;
      return;
    }

    ImGui.TextUnformatted("TeoCalc");
    ImGui.TextDisabled(faceplateModel.LogoCaption);
    ImGui.Separator();
    ImGui.TextUnformatted(faceplateModel.ProductLabel);
    ImGui.TextDisabled($"Family: {session.Model.Family}");
    if (session.Model.Hardware?.RomWordCount is int romWords and > 0)
    {
      ImGui.TextDisabled($"ROM words: {romWords}");
    }

    Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
    if (ver is not null)
    {
      ImGui.TextDisabled($"Build: {ver}");
    }

    ImGui.Spacing();
    if (ImGuiModalHost.CloseButton(new Vector2(120f, 0f)))
    {
      open = false;
      ImGui.CloseCurrentPopup();
    }

    ImGuiModalHost.End();
    s_open = open;
  }

  /// <summary>
  /// Hit-test the Teo mark; hover shows a tip, click opens About.
  /// </summary>
  public static void HandleMarkInteraction(RectF markHit)
  {
    ImGui.SetCursorScreenPos(markHit.Min);
    _ = ImGui.InvisibleButton("##teo-mark", new Vector2(markHit.Width, markHit.Height));
    if (ImGui.IsItemHovered())
    {
      ImGuiPointerStyle.MarkLastItemClickable();
      CalcAppTooltip.Set("About TeoCalc");
    }

    if (ImGui.IsItemClicked())
    {
      RequestOpen();
    }
  }
}
