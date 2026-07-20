using System.Numerics;
using TeoCalc.Core;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// HP Modern body geometry. Band width follows the key grid (keys flush with display/switch);
/// card-reader models insert <see cref="CalcCardSlotComponent"/> under the switch panel;
/// logo band height from <see cref="CalcLogoPanelComponent.HeightRef"/> (window chrome).
/// </summary>
public static class Calc00dBodyLayout
{
  public const string LayoutId = "00d";

  /// <summary>Legacy 00d canvas — used as fallback defaults before resolve.</summary>
  public const float ReferenceWidth = 687f;

  public const float ReferenceHeight = 1176f;

  public static readonly RectF DisplayBezelSlot = new(63f, 69f, 557f, 126f);

  public static readonly RectF DisplayGlassSlot = new(73f, 79f, 537f, 106f);

  public static readonly RectF SwitchSlot = new(63f, 203f, 557f, 100f);

  public static readonly RectF KeypadSlot = new(63f, 311f, 557f, 724f);

  public static readonly RectF LogoSlot = new(63f, 1043f, 557f, CalcLogoPanelComponent.HeightRef);

  public static readonly RectF FaceplateSlot = new(39f, 39f, 609f, 1098f);

  public const float SwitchRowY = 253f;

  /// <summary>Uniform faceplate padding: equal space left / right / top / bottom around the content.</summary>
  private const float FacePadRef = 9f;

  private const float FacePadXRef = FacePadRef;

  private const float FaceTopPadRef = FacePadRef;

  private const float FaceBottomPadRef = FacePadRef;

  /// <summary>Slightly shorter than classic 00d bezel for balance with the key grid.</summary>
  private const float DisplayHeightRef = 96f;

  private const float DisplayGlassInsetRef = 8f;

  private const float SwitchLeftNorm = 0.358f;

  private const float SwitchRightNorm = 0.816f;

  private static readonly Dictionary<string, CalcBodyLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

  public static readonly Vector2 OnOffSwitchCenter = new(
    SwitchSlot.X + SwitchSlot.Width * SwitchLeftNorm,
    SwitchRowY);

  public static readonly Vector2 PrgmRunSwitchCenter = new(
    SwitchSlot.X + SwitchSlot.Width * SwitchRightNorm,
    SwitchRowY);

  public static CalcBodyLayout Resolve(string family, string modelId, CalcModelDefinition model)
  {
    string cacheKey = $"{family}|{modelId}";
    if (Cache.TryGetValue(cacheKey, out CalcBodyLayout? cached))
    {
      return cached;
    }

    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(family, modelId);
    IReadOnlyList<CalcSwitchSpec> switches = CalcSwitchCatalog.ForModel(model);
    bool hasCardSlot = CalcCardSlotComponent.ModelHasCardSlot(model.Id)
      || CalcCardSlotComponent.ModelHasCardSlot(modelId);

    CalcKeyPanelComponent.PanelMetrics keyMetrics = CalcKeyPanelComponent.Measure(cells, model: model);

    float bandLeft = FacePadXRef;
    float bandWidth = keyMetrics.Width;
    float refWidth = bandLeft * 2f + bandWidth;

    float displayTop = FaceTopPadRef;
    RectF display = new(bandLeft, displayTop, bandWidth, DisplayHeightRef);

    RectF switchSlot = CalcSwitchPanelComponent.ResolveSlotRef(
      switches,
      bandLeft,
      bandWidth,
      display.Y + display.Height);

    RectF? cardSlot = null;
    float keypadTop;
    if (hasCardSlot)
    {
      cardSlot = CalcCardSlotComponent.ResolveSlotRef(
        bandLeft,
        bandWidth,
        switchSlot.Y + switchSlot.Height);
      // Pull keypad up by its top gutter so A–E sit close under the legend frame
      // (same trick as GapBelowSwitchRef when there is no card strip).
      keypadTop = cardSlot.Value.Y + cardSlot.Value.Height
        + CalcCardSlotComponent.GapAboveKeypadRef
        - CalcKeyPanelComponent.GutterRef;
    }
    else
    {
      keypadTop = switchSlot.Y + switchSlot.Height + CalcKeyPanelComponent.GapBelowSwitchRef;
    }

    RectF keypad = CalcKeyPanelComponent.ResolveSlotRef(bandLeft, keypadTop, keyMetrics);
    Dictionary<int, RectF> keySlots = CalcKeyPanelComponent.BuildKeySlots(keypad, cells, keyMetrics);

    // Logo is drawn as a fixed-height window-level bottom band, not inside the scaled
    // faceplate. The content ends after the keypad; the logo slot collapses to zero
    // height so CalcChassisGeometry.FooterHeight is 0 and no in-faceplate logo is drawn.
    float refHeight = keypad.Y + keypad.Height + FaceBottomPadRef;
    RectF logo = new(bandLeft, refHeight, bandWidth, 0f);

    float switchRowY = switchSlot.Y + switchSlot.Height * 0.5f;
    Vector2 onOff = new(switchSlot.X + switchSlot.Width * SwitchLeftNorm, switchRowY);
    Vector2 prgm = new(switchSlot.X + switchSlot.Width * SwitchRightNorm, switchRowY);

    CalcBodyLayout layout = new()
    {
      Id = LayoutId,
      ReferenceWidth = refWidth,
      ReferenceHeight = refHeight,
      DisplaySlot = display,
      SwitchSlot = switchSlot,
      KeypadSlot = keypad,
      LogoSlot = logo,
      CardSlotBand = cardSlot,
      SwitchRowLift = 0f,
      SwitchLabelY = switchRowY,
      OnOffSwitchCenter = onOff,
      PrgmRunSwitchCenter = prgm,
      Switches = switches,
      KeySlots = keySlots,
    };

    Cache[cacheKey] = layout;
    return layout;
  }

  /// <summary>Glass rect matching a resolved display bezel (10px inset on classic 00d).</summary>
  public static RectF GlassFromBezel(RectF bezel)
  {
    float inset = DisplayGlassInsetRef;
    return new RectF(
      bezel.X + inset,
      bezel.Y + inset,
      MathF.Max(0f, bezel.Width - inset * 2f),
      MathF.Max(0f, bezel.Height - inset * 2f));
  }
}
