namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Label slots on one key. Modifiers (F/G/H/…) bind to these via model AnnotationStyles.
/// </summary>
public enum CalcLabelAnchor
{
  /// <summary>Outside, above the cap.</summary>
  CapAbove,

  /// <summary>Primary face band.</summary>
  CapFace,

  /// <summary>On the key skirt / etek.</summary>
  CapSkirt,

  /// <summary>Outside, below the cap.</summary>
  CapBelow,
}
