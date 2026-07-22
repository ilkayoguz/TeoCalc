using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

/// <summary>OS / ImGui cursor hints for faceplate chrome and interactive regions.</summary>
public static class CalcFaceplatePointer
{
  private static StandardCursor _cursor = StandardCursor.Default;

  /// <summary>True when the mouse is over a key or slider switch (set by the faceplate view).</summary>
  public static bool IsOverInteractive { get; private set; }

  /// <summary>
  /// True when the mouse is over a scrollable side-panel / Studio ImGui region.
  /// Host window drag must not steal wheel or drag-to-scroll from these children.
  /// </summary>
  public static bool IsOverScrollableUi { get; private set; }

  /// <summary>MouseWheel captured right after <c>NewFrame</c> (before EndFrame zeros it).</summary>
  private static float s_frameWheelY;

  private static float s_frameWheelX;

  /// <summary>
  /// Raw Silk <see cref="IMouse.Scroll"/> deltas accumulated between captures.
  /// Survives cases where <c>CaptureState().GetScrollWheels()</c> is already cleared
  /// by the time ImGuiController writes <c>io.MouseWheel</c>.
  /// </summary>
  private static float s_rawWheelY;

  private static float s_rawWheelX;

  private static IMouse? s_wheelMouse;

  /// <summary>Vertical wheel notches for this frame (positive = wheel up).</summary>
  public static float FrameWheelY => s_frameWheelY;

  /// <summary>Horizontal wheel notches for this frame.</summary>
  public static float FrameWheelX => s_frameWheelX;

  /// <summary>Fraction unused — Studio now owns scroll via <see cref="ApplyOwnedWheelScrollY"/>.</summary>
  private const float ImGuiWheelFontMul = 5f;

  private static float s_notchAccY;
  private static float s_notchAccX;
  private static float s_ownedScrollY = float.NaN;
  private static float s_ownedScrollX = float.NaN;

  /// <summary>Previous frame hovered a Studio scroll region — used to ignore ImGui default wheel.</summary>
  private static bool s_wasOverScrollableUi;

  /// <summary>
  /// Map raw OS/Silk wheel deltas into fractional notch units (±1 ≈ one click).
  /// </summary>
  public static float WheelToNotches(float raw)
  {
    if (MathF.Abs(raw) < 1e-8f)
    {
      return 0f;
    }

    // Classic WHEEL_DELTA bursts (and some drivers) arrive as ±120 / ±240 / …
    if (MathF.Abs(raw) >= 40f)
    {
      return raw / 120f;
    }

    return raw;
  }

  /// <summary>
  /// Accumulate high-res / multi-event deltas and emit whole notches (max ±1 per call).
  /// One physical mouse-wheel click → exactly one Studio row.
  /// </summary>
  public static float ConsumeDiscreteNotchesY()
  {
    s_notchAccY += WheelToNotches(s_frameWheelY);
    float whole = 0f;
    if (s_notchAccY >= 1f)
    {
      whole = 1f;
      s_notchAccY -= 1f;
    }
    else if (s_notchAccY <= -1f)
    {
      whole = -1f;
      s_notchAccY += 1f;
    }

    return whole;
  }

  public static float ConsumeDiscreteNotchesX()
  {
    s_notchAccX += WheelToNotches(s_frameWheelX);
    float whole = 0f;
    if (s_notchAccX >= 1f)
    {
      whole = 1f;
      s_notchAccX -= 1f;
    }
    else if (s_notchAccX <= -1f)
    {
      whole = -1f;
      s_notchAccX += 1f;
    }

    return whole;
  }

  public static void BeginFrame()
  {
    // Remember last frame’s scrollable hover before clearing (NewFrame already ran).
    s_wasOverScrollableUi = IsOverScrollableUi;
    _cursor = StandardCursor.Default;
    IsOverInteractive = false;
    IsOverScrollableUi = false;
  }

  /// <summary>Mark the current hover as a scrollable Studio / side-panel child.</summary>
  public static void MarkScrollableUiHovered() => IsOverScrollableUi = true;

  /// <summary>
  /// Subscribe to the faceplate window’s mouse scroll so Studio can pan even when
  /// Silk→ImGui <c>io.MouseWheel</c> is zero for the frame.
  /// </summary>
  public static void BindMouseWheelSource(IInputContext? input)
  {
    IMouse? mouse = input is { Mice.Count: > 0 } ? input.Mice[0] : null;
    if (ReferenceEquals(s_wheelMouse, mouse))
    {
      return;
    }

    if (s_wheelMouse is not null)
    {
      s_wheelMouse.Scroll -= OnRawMouseScroll;
    }

    s_wheelMouse = mouse;
    if (s_wheelMouse is not null)
    {
      s_wheelMouse.Scroll += OnRawMouseScroll;
    }
  }

  /// <summary>Detach scroll subscription (window teardown).</summary>
  public static void UnbindMouseWheelSource()
  {
    if (s_wheelMouse is not null)
    {
      s_wheelMouse.Scroll -= OnRawMouseScroll;
      s_wheelMouse = null;
    }

    s_rawWheelY = 0f;
    s_rawWheelX = 0f;
  }

  private static void OnRawMouseScroll(IMouse _, ScrollWheel wheel)
  {
    s_rawWheelY += wheel.Y;
    s_rawWheelX += wheel.X;
  }

  /// <summary>
  /// Call immediately after <c>ImGuiController.Update</c> (<c>NewFrame</c>) to
  /// remember this frame’s wheel deltas for Studio slow-scroll / manual pan helpers.
  /// Prefers non-zero Silk raw scroll when ImGui IO wheel is already cleared.
  /// </summary>
  public static void CaptureFrameMouseWheel()
  {
    ImGuiIOPtr io = ImGui.GetIO();
    // Same physical notch must not be double-counted: raw and io are alternatives.
    if (MathF.Abs(s_rawWheelY) > 1e-6f || MathF.Abs(s_rawWheelX) > 1e-6f)
    {
      s_frameWheelY = s_rawWheelY;
      s_frameWheelX = s_rawWheelX;
    }
    else
    {
      s_frameWheelY = io.MouseWheel;
      s_frameWheelX = io.MouseWheelH;
    }

    s_rawWheelY = 0f;
    s_rawWheelX = 0f;

    // Prevent any late ImGui default scroll consumers from seeing the raw burst again.
    io.MouseWheel = 0f;
    io.MouseWheelH = 0f;
  }

  /// <summary>
  /// Own the current scrolling window’s wheel. Always restores from a stable baseline so
  /// ImGui’s NewFrame FontSize×5 jump cannot stick — even when undo math would be wrong.
  /// </summary>
  public static void ApplyOwnedWheelScrollY(float pixelsPerNotch)
  {
    float maxY = MathF.Max(0f, ImGui.GetScrollMaxY());
    if (float.IsNaN(s_ownedScrollY))
    {
      s_ownedScrollY = ImGui.GetScrollY();
    }

    bool hovered = ImGui.IsWindowHovered(
      ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
      | ImGuiHoveredFlags.ChildWindows);
    float sb = ImGui.GetStyle().ScrollbarSize;
    Vector2 mouse = ImGui.GetMousePos();
    Vector2 winPos = ImGui.GetWindowPos();
    Vector2 winSize = ImGui.GetWindowSize();
    bool overScrollbar = mouse.X >= winPos.X + winSize.X - sb - 1f;
    bool scrollbarDrag = overScrollbar && ImGui.IsMouseDown(ImGuiMouseButton.Left);

    if (scrollbarDrag)
    {
      // Native thumb drag — adopt ImGui scroll.
      s_ownedScrollY = ImGui.GetScrollY();
      return;
    }

    if (!hovered)
    {
      // Do not adopt jumped scroll when a wheel event targeted another pane.
      if (MathF.Abs(s_frameWheelY) < 1e-6f)
      {
        s_ownedScrollY = ImGui.GetScrollY();
      }
      else
      {
        ImGui.SetScrollY(Math.Clamp(s_ownedScrollY, 0f, maxY));
      }

      return;
    }

    MarkScrollableUiHovered();
    float notches = ConsumeDiscreteNotchesY();
    if (MathF.Abs(notches) > 1e-6f)
    {
      s_ownedScrollY = Math.Clamp(
        s_ownedScrollY - notches * MathF.Max(8f, pixelsPerNotch),
        0f,
        maxY);
    }
    else if (MathF.Abs(s_frameWheelY) > 1e-6f)
    {
      // Fractional high-res fragment: hold baseline (reject ImGui page jump).
    }
    else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      // Idle — resync from ImGui only when not mid drag-pan.
      s_ownedScrollY = ImGui.GetScrollY();
    }

    // Always force owned value so NewFrame’s jump never remains visible.
    ImGui.SetScrollY(Math.Clamp(s_ownedScrollY, 0f, maxY));
  }

  /// <summary>Keep owned scroll baseline in sync after drag / programmatic SetScrollY.</summary>
  public static void SyncOwnedScrollYFromWindow() =>
    s_ownedScrollY = ImGui.GetScrollY();

  /// <summary>Obsolete name — redirects to owned scroll (one row ≈ font×1.2).</summary>
  public static void SoftenDefaultWheelScrollOnCurrentWindow()
  {
    float row = MathF.Max(12f, ImGui.GetFontSize() * 1.15f);
    ApplyOwnedWheelScrollY(row);
  }

  /// <summary>
  /// Apply wheel on a <see cref="ImGuiWindowFlags.NoScrollWithMouse"/> window (Code listing child).
  /// ImGui never applies FontSize×5 — we only SetScrollY by discrete notches.
  /// </summary>
  public static void ApplyManualWheelScrollOnCurrentWindow(float pixelsPerNotch)
  {
    if (!ImGui.IsWindowHovered(
          ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
          | ImGuiHoveredFlags.ChildWindows))
    {
      if (MathF.Abs(s_frameWheelY) < 1e-6f)
      {
        s_ownedScrollY = ImGui.GetScrollY();
      }

      return;
    }

    MarkScrollableUiHovered();
    float maxY = MathF.Max(0f, ImGui.GetScrollMaxY());
    if (float.IsNaN(s_ownedScrollY))
    {
      s_ownedScrollY = ImGui.GetScrollY();
    }

    float sb = ImGui.GetStyle().ScrollbarSize;
    Vector2 mouse = ImGui.GetMousePos();
    Vector2 winPos = ImGui.GetWindowPos();
    Vector2 winSize = ImGui.GetWindowSize();
    bool overScrollbar = mouse.X >= winPos.X + winSize.X - sb - 1f;
    if (overScrollbar && ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      s_ownedScrollY = ImGui.GetScrollY();
      return;
    }

    float notches = ConsumeDiscreteNotchesY();
    if (MathF.Abs(notches) > 1e-6f)
    {
      s_ownedScrollY = Math.Clamp(
        s_ownedScrollY - notches * MathF.Max(8f, pixelsPerNotch),
        0f,
        maxY);
      ImGui.SetScrollY(s_ownedScrollY);
      return;
    }

    // No wheel — keep baseline in sync with native scrollbar position.
    s_ownedScrollY = ImGui.GetScrollY();
  }

  public static void RequestHandCursor() => RequestCursor(StandardCursor.Hand);

  public static void RequestCursor(StandardCursor cursor) => _cursor = cursor;

  public static void ApplyHandCursorIfHovering(
    Vector2 origin,
    CalcChassisMetrics metrics,
    IReadOnlyList<FaceplateCell> cells,
    IReadOnlyList<ProgramKeyEntry> keyChart,
    bool anyKeyHoveredFromImGui,
    bool anySwitchHoveredFromImGui,
    bool powerOn,
    bool programMode,
    CalcExplorerSession? session = null)
  {
    _ = programMode;

    if (!ImGui.IsWindowHovered(
          ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
          | ImGuiHoveredFlags.AllowWhenBlockedByPopup
          | ImGuiHoveredFlags.ChildWindows))
    {
      return;
    }

    Vector2 mouse = ImGui.GetIO().MousePos;

    bool overSwitch = anySwitchHoveredFromImGui
      || (session is not null && CalcChassisRenderer.IsMouseOverSwitch(mouse, origin, metrics, session, powerOn));
    if (overSwitch)
    {
      IsOverInteractive = true;
      RequestHandCursor();
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
      return;
    }

    if (anyKeyHoveredFromImGui || (powerOn && IsMouseOverAnyKey(mouse, origin, metrics, cells, keyChart)))
    {
      IsOverInteractive = true;
      RequestHandCursor();
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }
  }

  /// <summary>Apply OS cursor after ImGuiController.Render (Silk may overwrite in-frame cursor).</summary>
  public static void ApplyPendingCursor(IInputContext? input)
  {
    ImGui.SetMouseCursor(ToImGuiCursor(_cursor));

    if (TryApplySilkCursor(input, _cursor))
    {
      return;
    }

    if (OperatingSystem.IsWindows())
    {
      TryApplyWin32Cursor(_cursor);
    }
  }

  private static ImGuiMouseCursor ToImGuiCursor(StandardCursor cursor) => cursor switch
  {
    StandardCursor.Hand => ImGuiMouseCursor.Hand,
    StandardCursor.HResize => ImGuiMouseCursor.ResizeEW,
    StandardCursor.VResize => ImGuiMouseCursor.ResizeNS,
    StandardCursor.NwseResize => ImGuiMouseCursor.ResizeNWSE,
    StandardCursor.NeswResize => ImGuiMouseCursor.ResizeNESW,
    StandardCursor.ResizeAll => ImGuiMouseCursor.ResizeAll,
    StandardCursor.NotAllowed => ImGuiMouseCursor.NotAllowed,
    _ => ImGuiMouseCursor.Arrow,
  };

  private static bool TryApplySilkCursor(IInputContext? input, StandardCursor standard)
  {
    if (input is null || input.Mice.Count == 0)
    {
      return false;
    }

    ICursor cursor = input.Mice[0].Cursor;
    if (!cursor.IsSupported(standard))
    {
      return false;
    }

    cursor.Type = CursorType.Standard;
    cursor.StandardCursor = standard;
    return true;
  }

  private static void TryApplyWin32Cursor(StandardCursor standard)
  {
    int id = standard switch
    {
      StandardCursor.Hand => 32649,
      StandardCursor.HResize => 32644,
      StandardCursor.VResize => 32645,
      StandardCursor.NwseResize => 32642,
      StandardCursor.NeswResize => 32643,
      StandardCursor.ResizeAll => 32646,
      StandardCursor.NotAllowed => 32648,
      _ => 32512,
    };
    nint handle = LoadCursorW(0, new nint(id));
    if (handle != 0)
    {
      SetCursor(handle);
    }
  }

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  private static extern nint LoadCursorW(nint hInstance, nint lpCursorName);

  [DllImport("user32.dll")]
  private static extern nint SetCursor(nint hCursor);

  private static bool IsMouseOverAnyKey(
    Vector2 mouse,
    Vector2 origin,
    CalcChassisMetrics metrics,
    IReadOnlyList<FaceplateCell> cells,
    IReadOnlyList<ProgramKeyEntry> keyChart)
  {
    foreach (FaceplateCell cell in cells)
    {
      if (cell.KeyChartIndex >= keyChart.Count)
      {
        continue;
      }

      if (keyChart[cell.KeyChartIndex].KeyCode == 0)
      {
        continue;
      }

      RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
      if (keyRect.Width <= 0f || keyRect.Height <= 0f)
      {
        continue;
      }

      if (Contains(mouse, keyRect))
      {
        return true;
      }
    }

    return false;
  }

  private static bool Contains(Vector2 point, RectF rect) =>
    point.X >= rect.X
    && point.X <= rect.Max.X
    && point.Y >= rect.Y
    && point.Y <= rect.Max.Y;
}
