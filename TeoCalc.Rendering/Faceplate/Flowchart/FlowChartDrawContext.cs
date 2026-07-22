namespace TeoCalc.Rendering.Faceplate.Flowchart;

/// <summary>Shared paint inputs for flowchart symbol components.</summary>
public readonly record struct FlowChartDrawContext(
  uint Border,
  float BorderThickness,
  string? ModelId,
  IReadOnlyList<string>? CardStripCaptions,
  bool Selected,
  bool Pointer,
  bool Hovered);
