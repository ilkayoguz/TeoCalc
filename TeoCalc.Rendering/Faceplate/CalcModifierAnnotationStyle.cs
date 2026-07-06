namespace TeoCalc.Rendering.Faceplate;

public readonly record struct CalcModifierAnnotationStyle(
  CalcModifierKey Modifier,
  CalcLabelAnchor Anchor,
  CalcColorToken Ink);
