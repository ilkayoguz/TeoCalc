namespace TeoCalc.Rendering.Faceplate;

/// <summary>Horizontal placement of a band label within its key slot.</summary>
public enum CalcLabelAlign
{
  Center,
  Left,
  Right,
}

public static class CalcLabelAlignMetrics
{
  /// <summary>
  /// Extra horizontal inset for dual band legends (Gold left / Blue or GoldRight right)
  /// on CapAbove or CapBelow.
  /// </summary>
  public static float DualCapAboveInset(float scale) => DualBandInset(scale);

  /// <summary>Shared dual L/R inset for CapAbove and CapBelow bands.</summary>
  public static float DualBandInset(float scale) => scale * 5f;
}
