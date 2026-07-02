using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>Layout from Body.svg companion <c>faceplate-d03-layout.json</c> (409×861).</summary>
public static class BodyFaceplateLayout
{
  public const float ReferenceWidth = 409f;

  public const float ReferenceHeight = 861f;

  /// <summary>Standard HP-65 key cap height in Body.svg viewBox units.</summary>
  public const float StandardKeyHeight = 38f;

  /// <summary>Runtime switch widgets sit ~1 key height below the painted switch-track band.</summary>
  public const float SwitchRowLift = StandardKeyHeight;

  private static readonly object Gate = new();

  private static bool _loaded;

  private static RectF _display;

  private static RectF _switchTrack;

  private static RectF _brandPlate;

  private static RectF _cardSlotBand;

  private static RectF _keypadPanel;

  private static Vector2 _onOffSwitchKnob;

  private static Vector2 _prgmRunSwitchKnob;

  private static float _switchLabelY;

  private static readonly Dictionary<int, RectF> KeyRects = new();

  private static readonly Dictionary<int, float> GoldBandAboveKey = new();

  public static void EnsureLoaded()
  {
    if (_loaded)
    {
      return;
    }

    lock (Gate)
    {
      if (_loaded)
      {
        return;
      }

      Load();
      _loaded = true;
    }
  }

  public static RectF DisplayWindow
  {
    get { EnsureLoaded(); return _display; }
  }

  public static RectF SwitchTrack
  {
    get { EnsureLoaded(); return _switchTrack; }
  }

  public static RectF BrandPlate
  {
    get { EnsureLoaded(); return _brandPlate; }
  }

  public static RectF CardSlotBand
  {
    get { EnsureLoaded(); return _cardSlotBand; }
  }

  public static RectF KeypadPanel
  {
    get { EnsureLoaded(); return _keypadPanel; }
  }

  public static int KeyCount
  {
    get { EnsureLoaded(); return KeyRects.Count; }
  }

  public static bool TryGetKeyRect(int keyChartIndex, out RectF rect)
  {
    EnsureLoaded();
    return KeyRects.TryGetValue(keyChartIndex, out rect);
  }

  public static float GoldBandHeight(int keyChartIndex)
  {
    EnsureLoaded();
    return GoldBandAboveKey.TryGetValue(keyChartIndex, out float h) ? h : 14f;
  }

  public static Vector2 OnOffSwitchKnob
  {
    get { EnsureLoaded(); return _onOffSwitchKnob; }
  }

  public static Vector2 PrgmRunSwitchKnob
  {
    get { EnsureLoaded(); return _prgmRunSwitchKnob; }
  }

  public static float SwitchLabelY
  {
    get { EnsureLoaded(); return _switchLabelY; }
  }

  // Legacy names for norms/tests.
  public static Vector2 OnOffSwitchCenter => OnOffSwitchKnob;

  public static Vector2 PrgmRunSwitchCenter => PrgmRunSwitchKnob;

  private static T EnsureAndGet<T>(Func<T> value)
  {
    EnsureLoaded();
    return value();
  }

  private static void Load()
  {
    string path = TeoCalcPaths.ResourcePath("Engine/HP-65/Assets/faceplate-d03-layout.json");
    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Body faceplate layout missing.", path);
    }

    string json = File.ReadAllText(path);
    LayoutDocument? doc = JsonSerializer.Deserialize<LayoutDocument>(json, JsonOptions);
    if (doc?.Keys is null || doc.Keys.Count == 0)
    {
      throw new InvalidDataException("Body faceplate layout has no keys.");
    }

    LayoutBand? lcd = doc.Bands?.FirstOrDefault(b => b.Id == "lcd-window");
    if (lcd is not null)
    {
      _display = new RectF(23f, lcd.Y, 361f, lcd.H);
    }

    if (doc.SwitchTrack is not null)
    {
      _switchTrack = new RectF(doc.SwitchTrack.X, doc.SwitchTrack.Y, doc.SwitchTrack.W, doc.SwitchTrack.H);
    }
    else if (BodySvgRects.TryGetRect("switch-track", out RectF switchPanel))
    {
      _switchTrack = switchPanel;
    }
    else
    {
      _switchTrack = new RectF(23f, 154f, 361f, 41f);
    }

    float onOffX = doc.Switches?.OnOff?.X ?? _switchTrack.X + _switchTrack.Width * 0.249f;
    float prgmX = doc.Switches?.PrgmRun?.X ?? _switchTrack.X + _switchTrack.Width * 0.751f;
    float rowY = _switchTrack.Y + _switchTrack.Height * 0.5f - SwitchRowLift;
    _onOffSwitchKnob = new Vector2(onOffX, rowY);
    _prgmRunSwitchKnob = new Vector2(prgmX, rowY);

    LayoutBand? aboveSwitch = doc.Bands?.FirstOrDefault(b => b.Id == "face-above-switch");
    _switchLabelY = aboveSwitch is not null
      ? aboveSwitch.Y + aboveSwitch.H * 0.5f
      : rowY;

    LayoutBand? brand = doc.Bands?.FirstOrDefault(b => b.Id == "brand-plate");
    if (brand is not null)
    {
      _brandPlate = new RectF(23f, brand.Y, 361f, brand.H);
    }

    _cardSlotBand = doc.Bands?.FirstOrDefault(b => b.Id == "face-below-switch") is { } belowSwitch
      ? new RectF(23f, belowSwitch.Y, 361f, belowSwitch.H)
      : new RectF(23f, 195f, 361f, 23f);

    LayoutBand? keypad = doc.Bands?.FirstOrDefault(b => b.Id == "keypad-panel");
    if (keypad is not null)
    {
      _keypadPanel = new RectF(23f, keypad.Y, 361f, keypad.H);
    }

    List<LayoutKey> slots = doc.Keys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
    List<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells("Classic")
      .OrderBy(c => c.Row)
      .ThenBy(c => c.Column)
      .ToList();

    if (slots.Count != cells.Count)
    {
      throw new InvalidDataException(
        $"Body layout key count ({slots.Count}) does not match faceplate cells ({cells.Count}).");
    }

    List<float> rowTops = slots.GroupBy(k => k.Y).OrderBy(g => g.Key).Select(g => g.Key).ToList();
    Dictionary<float, int> rowIndexByY = rowTops.Select((y, i) => (y, i)).ToDictionary(t => t.y, t => t.i);

    for (int i = 0; i < cells.Count; i++)
    {
      LayoutKey slot = slots[i];
      FaceplateCell cell = cells[i];
      KeyRects[cell.KeyChartIndex] = new RectF(slot.X, slot.Y, slot.W, slot.H);

      int rowIndex = rowIndexByY[slot.Y];
      float band = rowIndex == 0
        ? slot.Y - _cardSlotBand.Y
        : slot.Y - rowTops[rowIndex - 1] - 38f;
      GoldBandAboveKey[cell.KeyChartIndex] = Math.Max(8f, band);
    }
  }

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private sealed class LayoutDocument
  {
    public List<LayoutBand>? Bands { get; init; }

    public LayoutSwitchTrack? SwitchTrack { get; init; }

    public LayoutSwitches? Switches { get; init; }

    public List<LayoutKey>? Keys { get; init; }
  }

  private sealed class LayoutSwitches
  {
    public LayoutSwitchPoint? OnOff { get; init; }

    public LayoutSwitchPoint? PrgmRun { get; init; }
  }

  private sealed class LayoutSwitchPoint
  {
    public float X { get; init; }

    public float Y { get; init; }
  }

  private sealed class LayoutBand
  {
    public string? Id { get; init; }

    public float Y { get; init; }

    public float H { get; init; }
  }

  private sealed class LayoutSwitchTrack
  {
    public float X { get; init; }

    public float Y { get; init; }

    public float W { get; init; }

    public float H { get; init; }
  }

  private sealed class LayoutKey
  {
    public string? Id { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public float W { get; init; }

    public float H { get; init; }
  }
}
