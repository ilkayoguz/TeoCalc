namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// One faceplate slide switch with up to four legend slots:
/// <code>
///              TopLabel
/// LeftLabel [ KNOB ] RightLabel
///            BottomLabel
/// </code>
/// A non-empty Top or Bottom label marks a 3-position switch (mid state).
/// </summary>
public readonly record struct CalcSwitchSpec(
  string LeftLabel,
  string RightLabel,
  int InitialIndex = 0,
  string TopLabel = "",
  string BottomLabel = "")
{
  public int PositionCount =>
    string.IsNullOrEmpty(TopLabel) && string.IsNullOrEmpty(BottomLabel) ? 2 : 3;

  public bool IsPower =>
    string.Equals(LeftLabel, "OFF", StringComparison.OrdinalIgnoreCase)
    && (string.Equals(RightLabel, "ON", StringComparison.OrdinalIgnoreCase)
      || string.Equals(RightLabel, "RUN", StringComparison.OrdinalIgnoreCase));

  /// <summary>Knob norm in [0,1]: left=0, right=1, 3-pos mid=0.5.</summary>
  public float InitialNorm => NormForIndex(InitialIndex);

  public float NormForIndex(int index)
  {
    if (PositionCount == 2)
    {
      return index <= 0 ? 0f : 1f;
    }

    return index switch
    {
      <= 0 => 0f,
      1 => 0.5f,
      _ => 1f,
    };
  }

  public int ClampIndex(int index) =>
    Math.Clamp(index, 0, PositionCount - 1);

  public int NextIndex(int index) =>
    (ClampIndex(index) + 1) % PositionCount;

  public int IndexForLabel(SwitchLabelSlot slot) =>
    slot switch
    {
      SwitchLabelSlot.Left => 0,
      SwitchLabelSlot.Top or SwitchLabelSlot.Bottom => PositionCount == 3 ? 1 : 0,
      SwitchLabelSlot.Right => PositionCount - 1,
      _ => 0,
    };

  public static CalcSwitchSpec Power(int initialIndex = 0) =>
    new("OFF", "ON", InitialIndex: initialIndex);

  public static CalcSwitchSpec ClassicPrgmRun(int initialIndex = 1) =>
    new("W/PRGM", "RUN", InitialIndex: initialIndex);

  public static CalcSwitchSpec WoodstockPrgmRun(int initialIndex = 1) =>
    new("PRGM", "RUN", InitialIndex: initialIndex);

  public static CalcSwitchSpec WoodstockAngle(int initialIndex = 0) =>
    new("DEG", "RAD", InitialIndex: initialIndex);

  public static CalcSwitchSpec BeginEnd(int initialIndex = 0) =>
    new("BEGIN", "END", InitialIndex: initialIndex);

  /// <summary>HP-38E/38C: one payment/date switch with dual-row side legends (Finseth).</summary>
  public static CalcSwitchSpec DateBeginEnd(int initialIndex = 0) =>
    new("D.MY\nBEGIN", "M.DY\nEND", InitialIndex: initialIndex);

  /// <summary>HP-55: TIMER · RUN with PRGM on top (3-position).</summary>
  public static CalcSwitchSpec TimerPrgmRun(int initialIndex = 2) =>
    new("TIMER", "RUN", InitialIndex: initialIndex, TopLabel: "PRGM");

  /// <summary>HP-19C: OFF · RUN with PRGM below.</summary>
  public static CalcSwitchSpec OffRunPrgm(int initialIndex = 0) =>
    new("OFF", "RUN", InitialIndex: initialIndex, BottomLabel: "PRGM");

  /// <summary>HP-19C: MAN · NORM with TRACE below.</summary>
  public static CalcSwitchSpec ManNormTrace(int initialIndex = 0) =>
    new("MAN", "NORM", InitialIndex: initialIndex, BottomLabel: "TRACE");
}

public enum SwitchLabelSlot
{
  None,
  Left,
  Right,
  Top,
  Bottom,
  Knob,
  Track,
}
