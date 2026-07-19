namespace TeoCalc.Rendering.Faceplate;

public readonly record struct CalcKeyAnnotation(
  CalcModifierKey Modifier,
  CalcLabelAnchor Anchor,
  string Text,
  CalcLabelAlign Align = CalcLabelAlign.Center);
