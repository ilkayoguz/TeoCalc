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

  public IReadOnlyDictionary<int, RectF> KeySlots { get; init; }
    = new Dictionary<int, RectF>();

  public bool TryGetKeySlot(int keyChartIndex, out RectF slot) =>
    KeySlots.TryGetValue(keyChartIndex, out slot);
}

public readonly record struct CalcBodySlots(
  RectF Display,
  RectF Switches,
  RectF Keypad,
  RectF Logo);
