using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>
/// Win32 touch → ImGui pointer bridge. Silk’s <c>ImGuiController</c> only copies
/// <c>IMouse</c> state and never tags <see cref="ImGuiMouseSource.TouchScreen"/>, so
/// finger taps often arrive with a stale <c>MousePos</c>. This hooks the host HWND,
/// records touch client coordinates, and patches ImGui IO after each Update.
/// </summary>
public static class CalcImGuiTouchInput
{
  private static nint s_hwnd;
  private static nint s_prevWndProc;
  private static bool s_attached;

  private static Vector2 s_pointerPos;
  private static bool s_hasPointerPos;
  private static bool s_primaryDown;
  private static bool s_primaryDownPrev;
  private static bool s_wasPrimaryClicked;
  private static bool s_touchSourceActive;
  private static int s_frameSerial;
  private static int s_lastTouchFrame = -1;

  /// <summary>True when the primary button transitioned down this frame (mouse or touch).</summary>
  public static bool WasPrimaryClicked => s_wasPrimaryClicked;

  /// <summary>True when the last pointer sample came from a touch source.</summary>
  public static bool IsTouchSource => s_touchSourceActive;

  public static void Attach(nint hwnd)
  {
    if (!OperatingSystem.IsWindows() || hwnd == 0 || s_attached)
    {
      return;
    }

    s_hwnd = hwnd;
    nint hook = Marshal.GetFunctionPointerForDelegate(s_wndProc);
    s_prevWndProc = SetWindowLongPtrW(hwnd, GwlpWndProc, hook);
    s_attached = s_prevWndProc != 0;
  }

  public static void Detach()
  {
    if (!s_attached || s_hwnd == 0)
    {
      return;
    }

    SetWindowLongPtrW(s_hwnd, GwlpWndProc, s_prevWndProc);
    s_attached = false;
    s_hwnd = 0;
    s_prevWndProc = 0;
  }

  /// <summary>
  /// Call immediately after <c>ImGuiController.Update</c> so draw/hit-test see the tap point.
  /// </summary>
  public static void ApplyAfterImGuiUpdate()
  {
    s_frameSerial++;
    s_wasPrimaryClicked = false;

    ImGuiIOPtr io = ImGui.GetIO();
    if (s_hasPointerPos && (s_touchSourceActive || s_lastTouchFrame == s_frameSerial - 1))
    {
      io.AddMouseSourceEvent(ImGuiMouseSource.TouchScreen);
      io.AddMousePosEvent(s_pointerPos.X, s_pointerPos.Y);
      io.MousePos = s_pointerPos;
    }

    bool down = s_primaryDown || io.MouseDown[0];
    if (down && !s_primaryDownPrev)
    {
      s_wasPrimaryClicked = true;
      if (s_touchSourceActive && s_hasPointerPos)
      {
        // Ensure ImGui also sees a press at the corrected position this frame.
        io.AddMouseButtonEvent(0, true);
        io.MouseDown[0] = true;
      }
    }

    if (!down && s_primaryDownPrev && s_touchSourceActive)
    {
      io.AddMouseButtonEvent(0, false);
      io.MouseDown[0] = false;
    }

    s_primaryDownPrev = down;
    if (!s_primaryDown && s_lastTouchFrame < s_frameSerial - 1)
    {
      s_touchSourceActive = false;
    }
  }

  /// <summary>Touch-corrected pointer when available; otherwise ImGui <c>MousePos</c>.</summary>
  public static Vector2 GetPointerPos()
  {
    if (s_hasPointerPos && (s_touchSourceActive || s_wasPrimaryClicked))
    {
      return s_pointerPos;
    }

    return ImGui.GetMousePos();
  }

  private static readonly WndProc s_wndProc = HookProc;

  private static nint HookProc(nint hwnd, uint msg, nint wParam, nint lParam)
  {
    switch (msg)
    {
      case WmLButtonDown:
      case WmLButtonUp:
      case WmMouseMove:
        if (IsTouchExtraInfo(GetMessageExtraInfo()))
        {
          float x = (short)(lParam.ToInt64() & 0xFFFF);
          float y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
          RecordTouch(x, y, msg == WmLButtonDown, msg == WmLButtonUp);
        }
        else if (msg == WmMouseMove && !s_primaryDown)
        {
          s_touchSourceActive = false;
        }

        break;

      case WmPointerDown:
      case WmPointerUpdate:
      case WmPointerUp:
        if (TryReadPointerClient(hwnd, wParam, out float px, out float py, out bool isTouch))
        {
          if (isTouch)
          {
            RecordTouch(px, py, msg == WmPointerDown, msg == WmPointerUp);
          }
        }

        break;
    }

    return CallWindowProcW(s_prevWndProc, hwnd, msg, wParam, lParam);
  }

  private static void RecordTouch(float x, float y, bool down, bool up)
  {
    s_pointerPos = new Vector2(x, y);
    s_hasPointerPos = true;
    s_touchSourceActive = true;
    s_lastTouchFrame = s_frameSerial;
    if (down)
    {
      s_primaryDown = true;
    }

    if (up)
    {
      s_primaryDown = false;
    }
  }

  private static bool IsTouchExtraInfo(nint extraInfo)
  {
    long v = extraInfo.ToInt64();
    // imgui_impl_win32 / glfw: pen 0xFF515700, touch 0xFF515780 (mask 0xFFFFFF80).
    return (v & 0xFFFFFF80L) == 0xFF515780L;
  }

  private static bool TryReadPointerClient(
    nint hwnd,
    nint wParam,
    out float x,
    out float y,
    out bool isTouch)
  {
    x = 0f;
    y = 0f;
    isTouch = false;
    uint pointerId = (uint)(wParam.ToInt64() & 0xFFFF);
    if (!GetPointerInfo(pointerId, out PointerInfo info))
    {
      return false;
    }

    isTouch = info.pointerType == PtTouch || info.pointerType == PtPen;
    POINT pt = info.ptPixelLocation;
    if (!ScreenToClient(hwnd, ref pt))
    {
      return false;
    }

    x = pt.X;
    y = pt.Y;
    return true;
  }

  private const int GwlpWndProc = -4;
  private const uint WmMouseMove = 0x0200;
  private const uint WmLButtonDown = 0x0201;
  private const uint WmLButtonUp = 0x0202;
  private const uint WmPointerUpdate = 0x0245;
  private const uint WmPointerDown = 0x0246;
  private const uint WmPointerUp = 0x0247;
  private const uint PtPen = 3;
  private const uint PtTouch = 2;

  private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

  [StructLayout(LayoutKind.Sequential)]
  private struct POINT
  {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct PointerInfo
  {
    public uint pointerType;
    public uint pointerId;
    public uint frameId;
    public uint pointerFlags;
    public nint sourceDevice;
    public nint hwndTarget;
    public POINT ptPixelLocation;
    public POINT ptHimetricLocation;
    public POINT ptPixelLocationRaw;
    public POINT ptHimetricLocationRaw;
    public uint dwTime;
    public uint historyCount;
    public int inputData;
    public uint dwKeyStates;
    public ulong PerformanceCount;
    public uint ButtonChangeType;
  }

  [DllImport("user32.dll")]
  private static extern nint CallWindowProcW(nint prev, nint hwnd, uint msg, nint wParam, nint lParam);

  [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
  private static extern nint SetWindowLongPtrW(nint hwnd, int index, nint value);

  [DllImport("user32.dll")]
  private static extern nint GetMessageExtraInfo();

  [DllImport("user32.dll")]
  private static extern bool ScreenToClient(nint hwnd, ref POINT lpPoint);

  [DllImport("user32.dll")]
  private static extern bool GetPointerInfo(uint pointerId, out PointerInfo pointerInfo);
}
