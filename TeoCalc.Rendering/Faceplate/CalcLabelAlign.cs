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
  /// <summary>Extra horizontal inset for dual CapAbove legends (Gold left / Blue or GoldRight right).</summary>
  public static float DualCapAboveInset(float scale) => scale * 5f;
}
