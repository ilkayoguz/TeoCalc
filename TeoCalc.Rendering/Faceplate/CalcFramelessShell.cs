using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Shared frameless window chrome: bead frame, top title band, DWM rounded corners.
/// Used by calculator hosts and the TeoCalc launcher.
/// </summary>
public static class CalcFramelessShell
{
  /// <summary>Outer bead frame thickness: 2q black + 1q gray + 1q light-gray.</summary>
  public const float BeadInset = Calc00dWireStyle.FitilWidthRef * 4f;

  /// <summary>Fixed dark-gray top band inside the bead (caption buttons + drag strip).</summary>
  public static float TopBandHeight => CalcWindowTitlePanelComponent.Height + 6f;

  public static float BandTop => BeadInset + TopBandHeight;

  public readonly record struct RectF(float X, float Y, float Width, float Height)
  {
    public Vector2 Min => new(X, Y);

    public Vector2 Max => new(X + Width, Y + Height);
  }

  /// <summary>Draw bead + title band; returns the content rect below the title (inside the bead).</summary>
  public static RectF DrawChrome(ImDrawListPtr draw, Vector2 display, string? title = null)
  {
    float q = Calc00dWireStyle.FitilWidthRef;
    RectF frame = new(0f, 0f, display.X, display.Y);
    float radius = Calc00dWireStyle.OuterRadiusRef;

    RectF cursor = DrawBead(draw, frame, radius, q * 2f, Calc00dWireStyle.BlackFitilFill, Calc00dWireStyle.BlackFitilShine);
    radius = MathF.Max(0f, radius - q * 2f);

    cursor = DrawBead(draw, cursor, radius, q, Calc00dWireStyle.GrayFitilFill, Calc00dWireStyle.GrayFitilShine);
    radius = MathF.Max(0f, radius - q);

    cursor = DrawBead(draw, cursor, radius, q, Calc00dWireStyle.LightGrayFitilFill, Calc00dWireStyle.LightGrayFitilShine);
    radius = MathF.Max(0f, radius - q);

    CalcAppTheme.EnsureInitialized();
    FillRoundedRect(draw, cursor, radius, CalcAppTheme.WindowBack);

    RectF band = new(cursor.X, cursor.Y, cursor.Width, TopBandHeight);
    draw.AddRectFilled(band.Min, band.Max, CalcAppTheme.TitleBarBack, radius, ImDrawFlags.RoundCornersTop);

    if (!string.IsNullOrWhiteSpace(title))
    {
      ImFontPtr font = ImGui.GetFont();
      float fontSize = 15f;
      Vector2 size = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, title);
      float textX = band.X + 14f;
      float textY = band.Y + MathF.Max(0f, (band.Height - size.Y) * 0.5f);
      draw.AddText(font, fontSize, new Vector2(textX, textY), CalcAppTheme.TitleBarInk, title);
    }

    return new RectF(
      cursor.X,
      cursor.Y + TopBandHeight,
      cursor.Width,
      MathF.Max(0f, cursor.Height - TopBandHeight));
  }

  public static void ApplyRoundedCorners(nint hwnd)
  {
    if (!OperatingSystem.IsWindows() || hwnd == 0)
    {
      return;
    }

    int preference = DwmWcpRound;
    _ = DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
  }

  public static bool TryGetCursorPos(out int x, out int y)
  {
    x = y = 0;
    if (!OperatingSystem.IsWindows() || !GetCursorPos(out POINT point))
    {
      return false;
    }

    x = point.X;
    y = point.Y;
    return true;
  }

  private static RectF DrawBead(
    ImDrawListPtr draw,
    RectF outer,
    float radius,
    float width,
    uint bodyColor,
    uint shineColor)
  {
    float shine = MathF.Max(1f, width * Calc00dWireStyle.FitilShineFraction);
    FillRoundedRect(draw, outer, radius, shineColor);
    RectF body = Inset(outer, shine);
    FillRoundedRect(draw, body, MathF.Max(0f, radius - shine), bodyColor);
    return Inset(outer, width);
  }

  private static RectF Inset(RectF rect, float amount) => new(
    rect.X + amount,
    rect.Y + amount,
    MathF.Max(0f, rect.Width - amount * 2f),
    MathF.Max(0f, rect.Height - amount * 2f));

  private static void FillRoundedRect(ImDrawListPtr draw, RectF rect, float radius, uint color) =>
    draw.AddRectFilled(rect.Min, rect.Max, color, radius, ImDrawFlags.RoundCornersAll);

  private const int DwmwaWindowCornerPreference = 33;
  private const int DwmWcpRound = 2;

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;
    public int Y;
  }

  [DllImport("user32.dll")]
  private static extern bool GetCursorPos(out POINT lpPoint);

  [DllImport("dwmapi.dll")]
  private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);
}
