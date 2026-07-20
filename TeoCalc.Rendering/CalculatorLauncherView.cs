using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering.Faceplate;
using TeoGame.Presentation.Components;
using TeoGame.Presentation.Navigation;

namespace TeoCalc.Rendering;

/// <summary>
/// Launcher icon grid — selection/nav via TeoGame <see cref="IconGridComponent"/>;
/// hand cursor via <see cref="NavPointerStyle"/> (same policy as TeoCave IconGrid).
/// Resize picks an a×b grid for the entry count, then maximizes the co-scaled
/// icon+label tile unit to fill width and height (no leftover-width-only bumps).
/// </summary>
public static class CalculatorLauncherView
{
  private const float RefCellW = 100f;
  private const float RefIconSize = 94f;
  private const float RefNameFont = 16f;
  private const float RefCellGap = 3f;
  private const float RefPadX = 8f;
  private const float RefPadY = 6f;
  private const float InitialScale = 1.35f;
  private const int DefaultColumns = 5;
  private const int DefaultRows = 4;
  private const float RefNameGap = 4f;
  /// <summary>Top inset above the icon inside a tile (scales with the icon+label unit).</summary>
  private const float RefIconPad = 1f;
  /// <summary>
  /// Prefer another a×b only when it grows icons by at least this factor
  /// (avoids jittery column flips on tiny resizes).
  /// </summary>
  private const float GridSwitchScaleGain = 1.04f;

  /// <summary>
  /// Reference row height: icon pad + icon + name gap + name font + inter-row cell gap.
  /// Icon and label are one vertical unit; cell gap is reserved so labels are not clipped
  /// by <c>tileMax = rowH - cellGap</c> or covered by the next row.
  /// </summary>
  private const float RefRowH = RefIconPad + RefIconSize + RefNameGap + RefNameFont + RefCellGap;

  private readonly record struct GridMetrics(
    int Columns,
    float Scale,
    float CellW,
    float IconSize,
    float NameFont,
    float NameGap,
    float IconPad,
    float CellGap,
    float PadX,
    float PadY,
    float RowH,
    float GridW,
    float GridH);

  public static bool DrawContent(CalculatorLauncherModel launcher, CalcFramelessShell.RectF content)
  {
    NavPointerStyle.BeginFrame();

    GridMetrics metrics = ComputeMetrics(launcher.Entries.Count, content.Width, content.Height);
    launcher.EnsureColumnCount(metrics.Columns);

    bool openSelected = UpdateKeyboard(launcher);

    ImGui.SetCursorScreenPos(content.Min + new Vector2(metrics.PadX, metrics.PadY));
    ImGui.PushClipRect(content.Min, content.Max, intersect_with_current_clip_rect: true);
    openSelected |= DrawIconGrid(launcher, content.Width - metrics.PadX * 2f, metrics);
    ImGui.PopClipRect();

    if (NavPointerStyle.WantsHandCursor)
    {
      CalcFaceplatePointer.RequestHandCursor();
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }

    return openSelected;
  }

  /// <summary>Initial client size for a comfortable 5×4 grid (not cramped).</summary>
  public static Vector2 PreferredWindowSize(int entryCount)
  {
    int columns = DefaultColumns;
    int rows = DefaultRows;
    if (entryCount > 0)
    {
      rows = Math.Max(DefaultRows, (entryCount + columns - 1) / columns);
    }

    float scale = InitialScale;
    float cellW = RefCellW * scale;
    float padX = RefPadX * scale;
    float padY = RefPadY * scale;
    float rowH = RefRowH * scale;
    float contentW = columns * cellW + padX * 2f;
    float contentH = rows * rowH + padY * 2f;
    return new Vector2(
      contentW + CalcFramelessShell.BeadInset * 2f,
      contentH + CalcFramelessShell.BandTop + CalcFramelessShell.BeadInset);
  }

  /// <summary>
  /// Pick columns×rows that maximize the uniform icon+label scale for the content box.
  /// Prefer ~5 columns when nearly as large; switch a×b only when another grid grows icons.
  /// </summary>
  private static GridMetrics ComputeMetrics(int entryCount, float contentW, float contentH)
  {
    entryCount = Math.Max(1, entryCount);
    contentW = MathF.Max(1f, contentW);
    contentH = MathF.Max(1f, contentH);

    float contentAR = contentW / contentH;
    float cellAR = RefCellW / RefRowH;

    // Pass 1: largest fill scale any a×b can achieve.
    float maxScale = 0f;
    for (int columns = 1; columns <= entryCount; columns++)
    {
      int rows = Math.Max(1, (entryCount + columns - 1) / columns);
      maxScale = MathF.Max(maxScale, MaxFillScale(columns, rows, contentW, contentH));
    }

    // Pass 2: among grids within the switch band of maxScale, prefer ~5×N then aspect fit.
    float acceptFloor = maxScale / GridSwitchScaleGain;
    int bestColumns = 1;
    int bestRows = entryCount;
    float bestScale = 0f;
    float bestAspectPenalty = float.MaxValue;
    int bestPreferDelta = int.MaxValue;

    for (int columns = 1; columns <= entryCount; columns++)
    {
      int rows = Math.Max(1, (entryCount + columns - 1) / columns);
      float candidateScale = MaxFillScale(columns, rows, contentW, contentH);
      if (candidateScale < acceptFloor)
      {
        continue;
      }

      float gridAR = (columns * cellAR) / MathF.Max(1, rows);
      float aspectPenalty = MathF.Abs(MathF.Log(MathF.Max(0.01f, gridAR / contentAR)));
      int preferDelta = Math.Abs(columns - DefaultColumns);

      bool take =
        preferDelta < bestPreferDelta
        || (preferDelta == bestPreferDelta && aspectPenalty < bestAspectPenalty - 1e-4f)
        || (preferDelta == bestPreferDelta
            && MathF.Abs(aspectPenalty - bestAspectPenalty) <= 1e-4f
            && candidateScale > bestScale);

      if (!take)
      {
        continue;
      }

      bestColumns = columns;
      bestRows = rows;
      bestScale = candidateScale;
      bestAspectPenalty = aspectPenalty;
      bestPreferDelta = preferDelta;
    }

    float cellW = RefCellW * bestScale;
    float cellGap = RefCellGap * bestScale;
    float iconSize = RefIconSize * bestScale;
    float nameFont = RefNameFont * bestScale;
    float nameGap = RefNameGap * bestScale;
    float iconPad = RefIconPad * bestScale;
    float rowH = iconPad + iconSize + nameGap + nameFont + cellGap;
    float gridW = bestColumns * cellW;
    float gridH = bestRows * rowH;
    // Center leftover on the non-limiting axis (pads absorb unused space).
    float padX = MathF.Max(RefPadX * bestScale, (contentW - gridW) * 0.5f);
    float padY = MathF.Max(RefPadY * bestScale, (contentH - gridH) * 0.5f);

    return new GridMetrics(
      bestColumns,
      bestScale,
      cellW,
      iconSize,
      nameFont,
      nameGap,
      iconPad,
      cellGap,
      padX,
      padY,
      rowH,
      gridW,
      gridH);
  }

  /// <summary>
  /// Largest uniform scale where columns×rows of reference tiles plus scaled pads fit.
  /// </summary>
  private static float MaxFillScale(int columns, int rows, float contentW, float contentH)
  {
    float denW = columns * RefCellW + 2f * RefPadX;
    float denH = rows * RefRowH + 2f * RefPadY;
    if (denW <= 0.01f || denH <= 0.01f)
    {
      return 0f;
    }

    return MathF.Min(contentW / denW, contentH / denH);
  }

  private static bool UpdateKeyboard(CalculatorLauncherModel launcher)
  {
    if (ImGui.GetIO().WantTextInput)
    {
      return false;
    }

    IconGridComponent grid = launcher.IconGrid;
    bool confirm = ImGui.IsKeyPressed(ImGuiKey.Enter, repeat: false);
    ComponentNavInput input = new(
      moveLeft: ImGui.IsKeyPressed(ImGuiKey.LeftArrow, repeat: true),
      moveRight: ImGui.IsKeyPressed(ImGuiKey.RightArrow, repeat: true),
      moveUp: ImGui.IsKeyPressed(ImGuiKey.UpArrow, repeat: true),
      moveDown: ImGui.IsKeyPressed(ImGuiKey.DownArrow, repeat: true),
      confirm: confirm);

    ComponentNavResult result = grid.ProcessLocalNav(input);
    if (result == ComponentNavResult.Handled || result == ComponentNavResult.ConfirmActivated)
    {
      launcher.Select(grid.FocusedIndex);
    }

    return confirm && launcher.TryOpenSelectedTeoCalc(out _);
  }

  private static bool DrawIconGrid(CalculatorLauncherModel launcher, float availW, GridMetrics metrics)
  {
    IReadOnlyList<CalculatorLauncherEntry> entries = launcher.Entries;
    if (entries.Count == 0)
    {
      return false;
    }

    int columns = metrics.Columns;
    Vector2 origin = ImGui.GetCursorScreenPos();
    float offsetX = MathF.Max(0f, (availW - metrics.GridW) * 0.5f);
    origin = new Vector2(origin.X + offsetX, origin.Y);

    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool open = false;
    float chromeRadius = 5f * metrics.Scale;

    for (int index = 0; index < entries.Count; index++)
    {
      CalculatorLauncherEntry entry = entries[index];
      int col = index % columns;
      int row = index / columns;
      float cellX = origin.X + col * metrics.CellW;
      float cellY = origin.Y + row * metrics.RowH;
      Vector2 tileMin = new(cellX, cellY);
      Vector2 tileMax = tileMin + new Vector2(metrics.CellW - metrics.CellGap, metrics.RowH - metrics.CellGap);
      // Icon + label share one reserved vertical stack inside the tile (below CellGap).
      Vector2 iconMin = new(
        cellX + ((metrics.CellW - metrics.CellGap) - metrics.IconSize) * 0.5f,
        cellY + metrics.IconPad);
      Vector2 iconMax = iconMin + new Vector2(metrics.IconSize, metrics.IconSize);
      float labelY = iconMax.Y + metrics.NameGap * 0.5f;

      bool selected = index == launcher.SelectedIndex;
      bool focused = index == launcher.IconGrid.FocusedIndex;
      bool hovered = Contains(mouse, tileMin, tileMax);

      NavPointerStyle.MarkClickable(mouse, tileMin, tileMax);

      ImGui.SetCursorScreenPos(tileMin);
      ImGui.PushID(entry.ModelId);
      ImGui.InvisibleButton("tile", tileMax - tileMin);
      if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
      {
        launcher.Select(index);
        if (entry.CanOpenTeoCalc)
        {
          open = launcher.TryOpenSelectedTeoCalc(out _);
        }
      }

      if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && entry.CanOpenReference)
      {
        launcher.OpenReference(index);
      }

      DrawTileChrome(draw, tileMin, tileMax, selected, focused, hovered || ImGui.IsItemHovered(), chromeRadius);
      CalculatorLauncherThumbnail.Draw(draw, iconMin, iconMax, entry);
      DrawTileLabel(
        draw,
        entry.LauncherLabel,
        tileMin.X,
        labelY,
        metrics.CellW - metrics.CellGap,
        metrics.NameFont);
      ImGui.PopID();
    }

    ImGui.SetCursorScreenPos(new Vector2(origin.X - offsetX, origin.Y));
    ImGui.Dummy(new Vector2(availW, metrics.GridH));

    CalcFaceplateThemeState.ApplyForModel(CalcModelCatalog.Hp65);
    return open;
  }

  private static void DrawTileChrome(
    ImDrawListPtr draw,
    Vector2 tileMin,
    Vector2 tileMax,
    bool selected,
    bool focused,
    bool hovered,
    float radius)
  {
    if (!(selected || focused || hovered))
    {
      return;
    }

    uint fill = selected
      ? 0x66284A66u
      : hovered
        ? 0x44283848u
        : 0x33203040u;
    uint ring = selected ? 0xFF6AA0D0u : focused ? 0xFF8AB4DCu : 0xFF506878u;
    draw.AddRectFilled(tileMin, tileMax, fill, radius);
    draw.AddRect(tileMin, tileMax, ring, radius, ImDrawFlags.RoundCornersAll, selected ? 2f : 1.25f);
  }

  private static void DrawTileLabel(
    ImDrawListPtr draw,
    string label,
    float x,
    float y,
    float width,
    float nameFont)
  {
    const uint white = 0xFFFFFFFFu;
    float textW = CalcFaceplateFonts.ArialBoldWidth(label, nameFont);
    float textX = x + MathF.Max(0f, (width - textW) * 0.5f);
    CalcFaceplateFonts.DrawArialBoldTop(draw, label, textX, y, nameFont, white);
  }

  private static bool Contains(Vector2 point, Vector2 min, Vector2 max) =>
    point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;
}
