using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Window control buttons (Minimize, Maximize/Restore, Close) overlaid on the
/// top-right of the calculator, whose own top area acts as the drag title bar.
/// Optional model capability icons sit on the left (card / printer stubs).
/// </summary>
public static class CalcWindowTitlePanelComponent
{
  /// <summary>Button band height — the top drag strip is kept at least this tall on resize.</summary>
  public const float Height = 32f;

  public const float ButtonWidth = 46f;

  public const int ButtonCount = 3;

  private const uint IconColor = 0xFFE8E8E8;
  private const uint IconCloseHover = 0xFFFFFFFF;
  private const uint HoverFill = 0x33FFFFFF;
  private const uint HoverCloseFill = 0xFF2311E8; // Windows close red #E81123 (ImGui ABGR)

  public static float ButtonsWidth => ButtonWidth * ButtonCount;

  public enum TitleAction
  {
    None,
    Minimize,
    ToggleMaximize,
    Close,
    OpenCard,
    OpenPrinter,
  }

  public static float CapabilityIconsWidth(bool hasCardSlot, bool hasPrinter)
  {
    int count = (hasCardSlot ? 1 : 0) + (hasPrinter ? 1 : 0);
    return ButtonWidth * count;
  }

  /// <summary>True when the mouse is over caption or capability buttons within the top band.</summary>
  public static bool IsOverButtons(
    Vector2 mouse,
    float top,
    float height,
    float rightEdge,
    float leftEdge = 0f,
    bool hasCardSlot = false,
    bool hasPrinter = false)
  {
    if (mouse.Y < top || mouse.Y > top + height)
    {
      return false;
    }

    if (mouse.X >= rightEdge - ButtonsWidth && mouse.X <= rightEdge)
    {
      return true;
    }

    float capWidth = CapabilityIconsWidth(hasCardSlot, hasPrinter);
    return capWidth > 0f
      && mouse.X >= leftEdge
      && mouse.X <= leftEdge + capWidth;
  }

  /// <summary>
  /// Draws caption buttons flush to <paramref name="rightEdge"/> and optional
  /// capability icons flush to <paramref name="leftEdge"/>.
  /// </summary>
  public static TitleAction Draw(
    bool isMaximized,
    float top,
    float height,
    float rightEdge,
    float leftEdge = 0f,
    bool hasCardSlot = false,
    bool hasPrinter = false,
    bool cardPanelOpen = false,
    bool printerPanelOpen = false)
  {
    ImDrawListPtr draw = ImGui.GetForegroundDrawList();
    TitleAction action = TitleAction.None;

    float capX = leftEdge;
    if (hasCardSlot)
    {
      uint fill = cardPanelOpen ? 0x55FFFFFFu : HoverFill;
      if (Button(draw, "##cap-card", capX, top, height, fill, IconColor, DrawCardIcon))
      {
        action = TitleAction.OpenCard;
      }

      capX += ButtonWidth;
    }

    if (hasPrinter)
    {
      uint fill = printerPanelOpen ? 0x55FFFFFFu : HoverFill;
      if (Button(draw, "##cap-print", capX, top, height, fill, IconColor, DrawPrinterIcon))
      {
        action = TitleAction.OpenPrinter;
      }
    }

    float x0 = rightEdge - ButtonsWidth;

    if (Button(draw, "##win-min", x0, top, height, HoverFill, IconColor, DrawMinimizeIcon))
    {
      action = TitleAction.Minimize;
    }

    float xMax = x0 + ButtonWidth;
    if (Button(draw, "##win-max", xMax, top, height, HoverFill, IconColor, isMaximized ? DrawRestoreIcon : DrawMaximizeIcon))
    {
      action = TitleAction.ToggleMaximize;
    }

    float xClose = x0 + ButtonWidth * 2f;
    if (Button(draw, "##win-close", xClose, top, height, HoverCloseFill, IconColor, DrawCloseIcon, IconCloseHover))
    {
      action = TitleAction.Close;
    }

    return action;
  }

  private static bool Button(
    ImDrawListPtr draw,
    string id,
    float x,
    float top,
    float height,
    uint hoverFill,
    uint iconColor,
    Action<ImDrawListPtr, float, float, uint> drawIcon,
    uint? iconHoverColor = null)
  {
    ImGui.SetCursorScreenPos(new Vector2(x, top));
    bool clicked = ImGui.InvisibleButton(id, new Vector2(ButtonWidth, height));
    bool hovered = ImGui.IsItemHovered();
    if (hovered)
    {
      CalcFaceplatePointer.RequestHandCursor();
      draw.AddRectFilled(new Vector2(x, top), new Vector2(x + ButtonWidth, top + height), hoverFill);
    }

    float cy = top + height * 0.5f;
    drawIcon(draw, x, cy, hovered && iconHoverColor.HasValue ? iconHoverColor.Value : iconColor);
    return clicked;
  }

  private static void DrawMinimizeIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddLine(new Vector2(cx - 5f, cy), new Vector2(cx + 5f, cy), color, 1.2f);
  }

  private static void DrawMaximizeIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddRect(new Vector2(cx - 5f, cy - 5f), new Vector2(cx + 5f, cy + 5f), color, 0f, ImDrawFlags.None, 1.2f);
  }

  private static void DrawRestoreIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddRect(new Vector2(cx - 3f, cy - 6f), new Vector2(cx + 5f, cy + 2f), color, 0f, ImDrawFlags.None, 1.2f);
    draw.AddRect(new Vector2(cx - 5f, cy - 2f), new Vector2(cx + 3f, cy + 6f), color, 0f, ImDrawFlags.None, 1.2f);
  }

  private static void DrawCloseIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddLine(new Vector2(cx - 5f, cy - 5f), new Vector2(cx + 5f, cy + 5f), color, 1.2f);
    draw.AddLine(new Vector2(cx + 5f, cy - 5f), new Vector2(cx - 5f, cy + 5f), color, 1.2f);
  }

  private static void DrawCardIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddRect(new Vector2(cx - 7f, cy - 4.5f), new Vector2(cx + 7f, cy + 4.5f), color, 1.5f, ImDrawFlags.None, 1.2f);
    draw.AddLine(new Vector2(cx - 4f, cy - 1f), new Vector2(cx + 4f, cy - 1f), color, 1.1f);
  }

  private static void DrawPrinterIcon(ImDrawListPtr draw, float x, float cy, uint color)
  {
    float cx = x + ButtonWidth * 0.5f;
    draw.AddRect(new Vector2(cx - 6f, cy - 2f), new Vector2(cx + 6f, cy + 5f), color, 0f, ImDrawFlags.None, 1.2f);
    draw.AddRect(new Vector2(cx - 4f, cy - 6f), new Vector2(cx + 4f, cy - 2f), color, 0f, ImDrawFlags.None, 1.2f);
    draw.AddLine(new Vector2(cx - 3f, cy + 1.5f), new Vector2(cx + 3f, cy + 1.5f), color, 1.1f);
  }
}
