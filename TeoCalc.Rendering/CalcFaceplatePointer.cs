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

  public static void BeginFrame()
  {
    _cursor = StandardCursor.Default;
    IsOverInteractive = false;
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
