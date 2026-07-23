using System.Numerics;
using System.Reflection;
using ImGuiNET;
using TeoCalc.Core.Catalog;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Mini About modal opened from the Teo mark on the logo band.</summary>
public static class CalcAboutModal
{
  private static bool s_openRequested;

  public static void RequestOpen() => s_openRequested = true;

  public static void Draw(Session session, CalcModelDefinition faceplateModel)
  {
    if (s_openRequested)
    {
      ImGui.OpenPopup("##teo-about");
      s_openRequested = false;
    }

    CalcAppDialogStyle.PushModal();
    bool open = true;
    if (!ImGui.BeginPopupModal(
          "##teo-about",
          ref open,
          ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
    {
      CalcAppDialogStyle.PopModal();
      return;
    }

    ImGui.Dummy(new Vector2(260f, 0f));
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
    CalcAppDialogStyle.PushAffirmative();
    if (ImGui.Button("Close", new Vector2(120f, 0f)))
    {
      ImGui.CloseCurrentPopup();
    }

    CalcAppDialogStyle.PopButton();

    ImGui.EndPopup();
    CalcAppDialogStyle.PopModal();
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
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
      CalcAppTooltip.Set("About TeoCalc");
    }

    if (ImGui.IsItemClicked())
    {
      RequestOpen();
    }
  }
}
