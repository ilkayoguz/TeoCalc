using System.Numerics;
using ImGuiNET;
using TeoCalc.Formats;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

public enum CalcCapabilitySidePanelMode
{
  None,
  Card,
  Printer,
  Debug,
  Studio,
}

/// <summary>Left side strip for card / printer / debug / studio tools inside the calc window chrome.</summary>
public static class CalcCapabilitySidePanelComponent
{
  public static float PreferredWidthRef =>
    MathF.Max(
      MathF.Max(
        MathF.Max(CalcCardPanelComponent.PreferredWidthRef, CalcPrinterPanelComponent.PreferredWidthRef),
        CalcDebugPanelComponent.PreferredWidthRef),
      CalcStudioPanelComponent.PreferredWidthRef);

  public static float WidthFor(CalcCapabilitySidePanelMode mode) =>
    mode switch
    {
      CalcCapabilitySidePanelMode.Card => CalcCardPanelComponent.PreferredWidthRef,
      CalcCapabilitySidePanelMode.Printer => CalcPrinterPanelComponent.PreferredWidthRef,
      CalcCapabilitySidePanelMode.Debug => CalcDebugPanelComponent.PreferredWidthRef,
      CalcCapabilitySidePanelMode.Studio => CalcStudioPanelComponent.PreferredWidthRef,
      _ => 0f,
    };

  public static void DrawChrome(ImDrawListPtr draw, float x, float y, float width, float height)
  {
    RectF bounds = new(x, y, width, height);
    draw.AddRectFilled(bounds.Min, bounds.Max, Calc00dWireStyle.SwitchPanelFill);
    uint divider = Calc00dWireStyle.GrayFitilFill;
    draw.AddLine(
      new Vector2(bounds.Max.X, bounds.Min.Y),
      new Vector2(bounds.Max.X, bounds.Max.Y),
      divider,
      Calc00dWireStyle.FitilWidthRef);
  }

  public static void DrawContent(
    CalcCapabilitySidePanelMode mode,
    Session session,
    ref string cardPathBuffer,
    ref string cardStatusMessage,
    ref string debugDumpStatus,
    bool canLoadSaveCard,
    Func<string, string?> loadCard,
    Func<string, string?> saveCard,
    string cardFileExtension,
    int cardProgramCapacity,
    IReadOnlyList<string> printLines,
    Action? onTestPrint,
    Action? onClearPrint,
    bool cardInserted = false,
    string? loadedCardPath = null,
    TeoCardDocument? loadedTeoCard = null,
    Action? onEjectCard = null)
  {
    switch (mode)
    {
      case CalcCapabilitySidePanelMode.Card:
        goto case CalcCapabilitySidePanelMode.Studio;
      case CalcCapabilitySidePanelMode.Studio:
        CalcStudioPanelComponent.DrawInline(
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
        break;
      case CalcCapabilitySidePanelMode.Printer:
        CalcPrinterPanelComponent.DrawInline(printLines, onTestPrint, onClearPrint);
        break;
      case CalcCapabilitySidePanelMode.Debug:
        CalcDebugPanelComponent.DrawInline(session, ref debugDumpStatus);
        break;
    }
  }
}
