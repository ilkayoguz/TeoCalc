using System.Numerics;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Model-independent body slot geometry in reference units (Display → Switches → Keypad → Logo).</summary>
public sealed class CalcBodyLayout
{
  public required string Id { get; init; }

  public float ReferenceWidth { get; init; }

  public float ReferenceHeight { get; init; }

  public required RectF DisplaySlot { get; init; }

  public required RectF SwitchSlot { get; init; }

  public required RectF KeypadSlot { get; init; }

  public required RectF LogoSlot { get; init; }

  public RectF? CardSlotBand { get; init; }

  public float SwitchRowLift { get; init; }

  public float SwitchLabelY { get; init; }

  public Vector2 OnOffSwitchCenter { get; init; }

  public Vector2 PrgmRunSwitchCenter { get; init; }

  /// <summary>Ordered faceplate switches (labels + initial positions). Power is always first when present.</summary>
  public IReadOnlyList<CalcSwitchSpec> Switches { get; init; } = CalcSwitchCatalog.Classic65;

  /// <summary>Legacy mode-switch pair (second switch). Prefer <see cref="Switches"/>.</summary>
  public CalcSwitchLabels SwitchLabels =>
    Switches.Count > 1
      ? new CalcSwitchLabels(Switches[1].LeftLabel, Switches[1].RightLabel)
      : CalcSwitchLabels.PowerOnly;

  public bool HasModeSwitch => Switches.Count > 1;

  public bool HasCardSlots => CardSlotBand is not null;

  public IReadOnlyDictionary<int, RectF> KeySlots { get; init; }
    = new Dictionary<int, RectF>();

  public bool TryGetKeySlot(int keyChartIndex, out RectF slot) =>
    KeySlots.TryGetValue(keyChartIndex, out slot);

  public CalcBodyLayout WithSwitches(IReadOnlyList<CalcSwitchSpec> switches) => new()
  {
    Id = Id,
    ReferenceWidth = ReferenceWidth,
    ReferenceHeight = ReferenceHeight,
    DisplaySlot = DisplaySlot,
    SwitchSlot = SwitchSlot,
    KeypadSlot = KeypadSlot,
    LogoSlot = LogoSlot,
    CardSlotBand = CardSlotBand,
    SwitchRowLift = SwitchRowLift,
    SwitchLabelY = SwitchLabelY,
    OnOffSwitchCenter = OnOffSwitchCenter,
    PrgmRunSwitchCenter = PrgmRunSwitchCenter,
    Switches = switches,
    KeySlots = KeySlots,
  };
}

public readonly record struct CalcBodySlots(
  RectF Display,
  RectF Switches,
  RectF Keypad,
  RectF Logo);
