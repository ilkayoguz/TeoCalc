using System.Numerics;
using System.Text.Json;
using TeoCalc.Core;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Body slot geometry aligned to calc-body-shell.png; keypad height scales with row count.</summary>
public static class CalcPrototypeBodyLayout
{
  public const string LayoutId = "prototype";

  private static readonly Dictionary<string, CalcBodyLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

  public static void InvalidateCache() => Cache.Clear();

  public static CalcBodyLayout Instance => Resolve("Classic", "HP-65", new CalcModelDefinition
  {
    Id = "65",
    DisplayName = "HP-65",
  });

  public static CalcBodyLayout Resolve(string family, string modelId, CalcModelDefinition model)
  {
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(family, modelId);
    ShellLayoutSpec spec = ShellLayoutSpec.Load();
    int rowCount = RowCount(cells, spec);
    string cacheKey = $"{spec.ReferenceWidth}x{spec.ReferenceHeight}|{family}|{modelId}|{rowCount}";
    if (Cache.TryGetValue(cacheKey, out CalcBodyLayout? cached))
    {
      return cached;
    }

    CalcBodyLayout layout = Create(cells, rowCount, model, spec);
    Cache[cacheKey] = layout;
    return layout;
  }

  private static int RowCount(IReadOnlyList<FaceplateCell> cells, ShellLayoutSpec spec) =>
    spec.ReferenceKeypadRows > 0
      ? (int)spec.ReferenceKeypadRows
      : cells.Count == 0
        ? 7
        : cells.Max(cell => cell.Row + cell.RowSpan - 1) + 1;

  private static CalcBodyLayout Create(
    IReadOnlyList<FaceplateCell> cells,
    int rowCount,
    CalcModelDefinition model,
    ShellLayoutSpec spec)
  {
    float refRows = Math.Max(1f, spec.ReferenceKeypadRows);

    RectF display = spec.DisplaySlot.ToRectF();
    RectF switches = spec.SwitchSlot.ToRectF();
    RectF baseKeypad = spec.KeypadSlot.ToRectF();
    RectF baseLogo = spec.LogoSlot.ToRectF();

    float keypadHeight = baseKeypad.Height * (rowCount / refRows);
    float keypadY = baseKeypad.Y;
    float logoY = keypadY + keypadHeight + (baseLogo.Y - (baseKeypad.Y + baseKeypad.Height));
    float refHeight = logoY + baseLogo.Height + (spec.ReferenceHeight - (baseLogo.Y + baseLogo.Height));

    RectF keypad = new(baseKeypad.X, keypadY, baseKeypad.Width, keypadHeight);
    RectF logo = new(baseLogo.X, logoY, baseLogo.Width, baseLogo.Height);

    float switchRowY = switches.Y + switches.Height * 0.5f;
    Vector2 onOff = new(switches.X + switches.Width * spec.SwitchLeftNorm, switchRowY);
    Vector2 angle = new(switches.X + switches.Width * spec.SwitchRightNorm, switchRowY);

    Dictionary<int, RectF> keySlots = new();
    AddKeySlots(keySlots, cells, keypad, rowCount, spec.KeyGrid);

    return new CalcBodyLayout
    {
      Id = LayoutId,
      ReferenceWidth = spec.ReferenceWidth,
      ReferenceHeight = refHeight,
      DisplaySlot = display,
      SwitchSlot = switches,
      KeypadSlot = keypad,
      LogoSlot = logo,
      CardSlotBand = null,
      SwitchRowLift = 0f,
      SwitchLabelY = switchRowY,
      OnOffSwitchCenter = onOff,
      PrgmRunSwitchCenter = angle,
      Switches = CalcSwitchCatalog.ForModel(model),
      KeySlots = keySlots,
    };
  }

  private static void AddKeySlots(
    Dictionary<int, RectF> slots,
    IReadOnlyList<FaceplateCell> cells,
    RectF keypad,
    int rowCount,
    KeyGridSpec grid)
  {
    const int columns = 5;
    float availW = keypad.Width - grid.PadX * 2f;
    float availH = keypad.Height - grid.PadY * 2f;
    float cellW = (availW - grid.GapX * (columns - 1)) / columns;
    float cellH = (availH - grid.GapY * (rowCount - 1)) / rowCount;
    float insetX = cellW * grid.CapInset;
    float insetY = cellH * grid.CapInset;
    float originX = keypad.X + grid.PadX;
    float originY = keypad.Y + grid.PadY;

    foreach (FaceplateCell cell in cells)
    {
      float cellX = originX + cell.Column * (cellW + grid.GapX);
      float cellY = originY + cell.Row * (cellH + grid.GapY);
      float spanW = cellW * cell.ColSpan + grid.GapX * Math.Max(0, cell.ColSpan - 1);
      float spanH = cellH * cell.RowSpan + grid.GapY * Math.Max(0, cell.RowSpan - 1);
      slots[cell.KeyChartIndex] = new RectF(
        cellX + insetX * cell.ColSpan,
        cellY + insetY * cell.RowSpan,
        spanW - insetX * 2f * cell.ColSpan,
        spanH - insetY * 2f * cell.RowSpan);
    }
  }

  private sealed class ShellLayoutSpec
  {
    public float ReferenceWidth { get; init; } = 716f;

    public float ReferenceHeight { get; init; } = 979f;

    public float ReferenceKeypadRows { get; init; } = 7f;

    public float ReferenceKeypadCols { get; init; } = 5f;

    public int[]? DisplayGlassRgb { get; init; }

    public RectSpec DisplaySlot { get; init; } = new();

    public InsetSpec DisplayGlassInset { get; init; } = new();

    public RectSpec SwitchSlot { get; init; } = new();

    public float SwitchRowY { get; init; } = 162f;

    public float SwitchLeftNorm { get; init; } = 0.248f;

    public float SwitchRightNorm { get; init; } = 0.752f;

    public RectSpec KeypadSlot { get; init; } = new();

    public RectSpec LogoSlot { get; init; } = new();

    public float BrandTextLeftNorm { get; init; } = 0.36f;

    public KeyGridSpec KeyGrid { get; init; } = new();

    public static ShellLayoutSpec Load()
    {
      string path = TeoCalcPaths.ResourcePath("Engine/Shared/Assets/calc-body-shell-layout.json");
      if (!File.Exists(path))
      {
        return new ShellLayoutSpec();
      }

      string json = File.ReadAllText(path);
      ShellLayoutSpec? spec = JsonSerializer.Deserialize<ShellLayoutSpec>(json, JsonOptions);
      return spec ?? new ShellLayoutSpec();
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
      PropertyNameCaseInsensitive = true,
    };
  }

  public sealed class RectSpec
  {
    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }

    public RectF ToRectF() => new(X, Y, Width, Height);
  }

  public sealed class KeyGridSpec
  {
    public float PadX { get; init; } = 22f;

    public float PadY { get; init; } = 12f;

    public float GapX { get; init; } = 14f;

    public float GapY { get; init; } = 10f;

    public float CapInset { get; init; } = 0.16f;

    public RowCountSpec RowCounts { get; init; } = new();
  }

  public sealed class RowCountSpec
  {
    public int Classic { get; init; } = 8;

    public int Woodstock { get; init; } = 7;

    public int HP01 { get; init; } = 4;

    public int HP19C { get; init; } = 6;
  }

  public static float BrandTextLeftNorm => ShellLayoutSpec.Load().BrandTextLeftNorm;

  public static uint ShellDisplayGlassColor
  {
    get
    {
      int[]? rgb = ShellLayoutSpec.Load().DisplayGlassRgb;
      if (rgb is { Length: >= 3 })
      {
        // ImGui packs ABGR: A << 24 | B << 16 | G << 8 | R
        return 0xFF000000u | ((uint)rgb[2] << 16) | ((uint)rgb[1] << 8) | (uint)rgb[0];
      }

      return CalcChassisPalette.DisplayGlass;
    }
  }

  public static Vector4 DisplayGlassInsetNorm
  {
    get
    {
      ShellLayoutSpec spec = ShellLayoutSpec.Load();
      InsetSpec inset = spec.DisplayGlassInset;
      return new Vector4(inset.Left, inset.Top, inset.Right, inset.Bottom);
    }
  }

  public sealed class InsetSpec
  {
    public float Left { get; init; }

    public float Top { get; init; }

    public float Right { get; init; }

    public float Bottom { get; init; }
  }
}
