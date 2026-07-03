namespace TeoCalc.Rendering;

public sealed class ShiftPreviewController
{
  public ShiftPreviewMode Mode { get; private set; }

  public void Clear() => Mode = ShiftPreviewMode.None;

  public void Reset() => Clear();

  public void HandleKeyPress(int keyChartIndex)
  {
    ShiftPreviewMode requested = keyChartIndex switch
    {
      10 => ShiftPreviewMode.Gold,
      11 => ShiftPreviewMode.GoldInverse,
      14 => ShiftPreviewMode.Blue,
      _ => ShiftPreviewMode.None,
    };

    Mode = requested == Mode ? ShiftPreviewMode.None : requested;
  }
}
