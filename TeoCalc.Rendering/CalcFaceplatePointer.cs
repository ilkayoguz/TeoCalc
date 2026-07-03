using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering;

/// <summary>Hand cursor for interactive faceplate regions (keys and slider switches).</summary>
public static class CalcFaceplatePointer
{
  private static bool _wantHand;

  public static void ApplyHandCursorIfHovering(
    Vector2 origin,
    CalcChassisMetrics metrics,
    IReadOnlyList<FaceplateCell> cells,
    IReadOnlyList<ProgramKeyEntry> keyChart,
    bool anyKeyHoveredFromImGui,
    bool anySwitchHoveredFromImGui,
    bool powerOn,
    bool programMode)
  {
    _wantHand = false;

    if (!ImGui.IsWindowHovered(
          ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
          | ImGuiHoveredFlags.AllowWhenBlockedByPopup
          | ImGuiHoveredFlags.ChildWindows))
    {
      return;
    }

    Vector2 mouse = ImGui.GetIO().MousePos;

    if (anySwitchHoveredFromImGui || CalcChassisRenderer.IsMouseOverSwitch(mouse, origin, metrics, powerOn, programMode))
    {
      _wantHand = true;
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
      return;
    }

    if (anyKeyHoveredFromImGui || (powerOn && IsMouseOverAnyKey(mouse, origin, metrics, cells, keyChart)))
    {
      _wantHand = true;
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }
  }

  /// <summary>Apply OS cursor after ImGuiController.Render (Silk may overwrite in-frame cursor).</summary>
  public static void ApplyPendingCursor(IInputContext? input)
  {
    if (_wantHand)
    {
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }

    if (TryApplySilkCursor(input, _wantHand))
    {
      return;
    }

    if (OperatingSystem.IsWindows())
    {
      TryApplyWin32Cursor(_wantHand);
    }
  }

  private static bool TryApplySilkCursor(IInputContext? input, bool hand)
  {
    if (input is null || input.Mice.Count == 0)
    {
      return false;
    }

    ICursor cursor = input.Mice[0].Cursor;
    cursor.Type = CursorType.Standard;
    cursor.StandardCursor = hand ? StandardCursor.Hand : StandardCursor.Default;
    return true;
  }

  private static void TryApplyWin32Cursor(bool hand)
  {
    nint handle = LoadCursorW(0, new nint(hand ? 32649 : 32512));
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
