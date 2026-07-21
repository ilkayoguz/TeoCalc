using System.Numerics;
using ImGuiNET;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

public enum CalcCapabilitySidePanelMode
{
  None,
  Card,
  Printer,
}

/// <summary>Left side strip for card / printer tools inside the calc window chrome.</summary>
public static class CalcCapabilitySidePanelComponent
{
  public static float PreferredWidthRef =>
    MathF.Max(CalcCardPanelComponent.PreferredWidthRef, CalcPrinterPanelComponent.PreferredWidthRef);

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
    ref string cardPathBuffer,
    ref string cardStatusMessage,
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
        CalcCardPanelComponent.DrawInline(
          ref cardPathBuffer,
          ref cardStatusMessage,
          canLoadSaveCard,
          loadCard,
          saveCard,
          cardFileExtension,
          cardProgramCapacity,
          cardInserted,
          loadedCardPath,
          loadedTeoCard,
          onEjectCard);
        break;
      case CalcCapabilitySidePanelMode.Printer:
        CalcPrinterPanelComponent.DrawInline(printLines, onTestPrint, onClearPrint);
        break;
    }
  }
}
