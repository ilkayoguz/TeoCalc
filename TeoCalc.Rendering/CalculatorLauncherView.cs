using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering.Faceplate;
using TeoGame.Presentation.Components;
using TeoGame.Presentation.Navigation;

namespace TeoCalc.Rendering;

/// <summary>
/// Launcher icon grid — selection/nav via TeoGame <see cref="IconGridComponent"/>;
/// hand cursor via <see cref="NavPointerStyle"/> (same policy as TeoCave IconGrid).
/// </summary>
public static class CalculatorLauncherView
{
  private const float CellW = 96f;
  private const float IconSize = 88f;
  private const float NameFont = 13f;
  private const float CellGap = 6f;
  private const float HeaderBlockH = 26f;
  private const float WindowPadX = 10f;
  private const float WindowPadY = 6f;

  public static bool Draw(CalculatorLauncherModel launcher)
  {
    CalcFaceplatePointer.BeginFrame();
    NavPointerStyle.BeginFrame();

    bool openSelected = UpdateKeyboard(launcher);

    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(WindowPadX, WindowPadY));
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 2f));
    ImGui.Begin(
      "TeoCalc Launcher",
      ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);

    ImGui.AlignTextToFramePadding();
    ImGui.TextUnformatted("TeoCalc");
    ImGui.SameLine();
    ImGui.TextDisabled("· pick a model");

    openSelected |= DrawIconGrid(launcher);

    ImGui.End();
    ImGui.PopStyleVar(2);

    // TeoCave IconGrid policy: hand cursor while over a clickable tile.
    if (NavPointerStyle.WantsHandCursor)
    {
      CalcFaceplatePointer.RequestHandCursor();
      ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }

    return openSelected;
  }

  /// <summary>Preferred launcher client size for the current catalog (tight to the icon grid).</summary>
  public static Vector2 PreferredWindowSize(int entryCount)
  {
    int columns = CalculatorLauncherModel.ColumnCount;
    int rows = Math.Max(1, (entryCount + columns - 1) / columns);
    float nameBlockH = NameFont + 6f;
    float rowH = IconSize + nameBlockH + 4f;
    float gridH = rows * rowH;
    float gridW = columns * CellW;
    float width = gridW + WindowPadX * 2f + 4f;
    float height = HeaderBlockH + gridH + WindowPadY * 2f + 4f;
    return new Vector2(width, height);
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

  private static bool DrawIconGrid(CalculatorLauncherModel launcher)
  {
    IReadOnlyList<CalculatorLauncherEntry> entries = launcher.Entries;
    if (entries.Count == 0)
    {
      return false;
    }

    int columns = CalculatorLauncherModel.ColumnCount;
    float nameBlockH = NameFont + 6f;
    float rowH = IconSize + nameBlockH + 4f;
    int rows = (entries.Count + columns - 1) / columns;
    float gridHeight = rows * rowH;
    float gridWidth = columns * CellW;
    float availW = ImGui.GetContentRegionAvail().X;
    Vector2 origin = ImGui.GetCursorScreenPos();
    float offsetX = MathF.Max(0f, (availW - gridWidth) * 0.5f);
    origin = new Vector2(origin.X + offsetX, origin.Y);

    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    ImFontPtr font = ImGui.GetFont();
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool open = false;

    for (int index = 0; index < entries.Count; index++)
    {
      CalculatorLauncherEntry entry = entries[index];
      int col = index % columns;
      int row = index / columns;
      float cellX = origin.X + col * CellW;
      float cellY = origin.Y + row * rowH;
      Vector2 tileMin = new(cellX, cellY);
      Vector2 tileMax = tileMin + new Vector2(CellW - CellGap, rowH - CellGap);
      Vector2 iconMin = new(cellX + ((CellW - CellGap) - IconSize) * 0.5f, cellY + 2f);
      Vector2 iconMax = iconMin + new Vector2(IconSize, IconSize);

      bool selected = index == launcher.SelectedIndex;
      bool focused = index == launcher.IconGrid.FocusedIndex;
      bool hovered = Contains(mouse, tileMin, tileMax);

      // TeoCave IconGrid policy: hand cursor on clickable tiles.
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

      // Hover chrome only — do not Select on hover (that drove the status-line "hover text").
      DrawTileChrome(draw, tileMin, tileMax, selected, focused, hovered || ImGui.IsItemHovered());
      CalculatorLauncherThumbnail.Draw(draw, iconMin, iconMax, entry);
      DrawTileLabel(draw, font, entry.LauncherLabel, tileMin.X, iconMax.Y + 2f, CellW - CellGap, selected);
      ImGui.PopID();
    }

    ImGui.SetCursorScreenPos(new Vector2(origin.X - offsetX, origin.Y));
    ImGui.Dummy(new Vector2(availW, gridHeight));

    // Thumbs apply per-model themes while drawing; restore the shell default.
    CalcFaceplateThemeState.ApplyForModel(CalcModelCatalog.Hp65);
    return open;
  }

  private static void DrawTileChrome(
    ImDrawListPtr draw,
    Vector2 tileMin,
    Vector2 tileMax,
    bool selected,
    bool focused,
    bool hovered)
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
    draw.AddRectFilled(tileMin, tileMax, fill, 5f);
    draw.AddRect(tileMin, tileMax, ring, 5f, ImDrawFlags.RoundCornersAll, selected ? 2f : 1.25f);
  }

  private static void DrawTileLabel(
    ImDrawListPtr draw,
    ImFontPtr font,
    string label,
    float x,
    float y,
    float width,
    bool selected)
  {
    Vector2 size = font.CalcTextSizeA(NameFont, float.MaxValue, 0f, label);
    float textX = x + MathF.Max(0f, (width - size.X) * 0.5f);
    uint color = selected ? 0xFFE8EEF4u : 0xFFB0B8C0u;
    draw.AddText(font, NameFont, new Vector2(textX, y), color, label);
  }

  private static bool Contains(Vector2 point, Vector2 min, Vector2 max) =>
    point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;
}
