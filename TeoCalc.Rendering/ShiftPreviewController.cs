namespace TeoCalc.Rendering;

public sealed class ShiftPreviewController
{
  public ShiftPreviewMode Mode { get; private set; }

  public void Clear() => Mode = ShiftPreviewMode.None;

  public void Reset() => Clear();

  public void HandleKeyPress(int keyChartIndex) =>
    HandleKeyPress(keyChartIndex, family: null);

  public void HandleKeyPress(int keyChartIndex, string? family)
  {
    ShiftPreviewMode requested = ResolveRequested(keyChartIndex, family);
    if (requested == ShiftPreviewMode.None)
    {
      // Non-shift key clears preview (second function consumed / normal entry).
      if (!IsShiftPrefixKey(keyChartIndex, family))
      {
        Mode = ShiftPreviewMode.None;
      }

      return;
    }

    Mode = requested == Mode ? ShiftPreviewMode.None : requested;
  }

  /// <summary>Key that should show the active-modifier frame for <paramref name="mode"/>.</summary>
  public static int IndicatorKeyIndex(ShiftPreviewMode mode, string? family)
  {
    if (mode == ShiftPreviewMode.None)
    {
      return -1;
    }

    if (IsHp01(family))
    {
      return mode == ShiftPreviewMode.Gold ? 24 : -1; // Δ
    }

    if (IsHp19C(family))
    {
      return mode switch
      {
        ShiftPreviewMode.Gold => 5,  // f
        ShiftPreviewMode.Blue => 11, // g
        _ => -1,
      };
    }

    if (IsWoodstock(family))
    {
      // HP-21: single blue prefix at chart index 4 (blank CapFace).
      return mode == ShiftPreviewMode.Blue ? 4 : -1;
    }

    return mode switch
    {
      ShiftPreviewMode.Gold => 10,
      ShiftPreviewMode.GoldInverse => 11,
      ShiftPreviewMode.Blue => 14,
      _ => -1,
    };
  }

  public static bool IsShiftPrefixKey(int keyChartIndex, string? family)
  {
    if (IsHp01(family))
    {
      return keyChartIndex == 24; // Δ
    }

    if (IsHp19C(family))
    {
      return keyChartIndex is 5 or 11;
    }

    if (IsWoodstock(family))
    {
      return keyChartIndex == 4;
    }

    return keyChartIndex is 10 or 11 or 14;
  }

  private static ShiftPreviewMode ResolveRequested(int keyChartIndex, string? family)
  {
    if (IsHp01(family))
    {
      // Owner's Guide: press Δ then the key whose yellow legend you want.
      return keyChartIndex == 24 ? ShiftPreviewMode.Gold : ShiftPreviewMode.None;
    }

    if (IsHp19C(family))
    {
      return keyChartIndex switch
      {
        5 => ShiftPreviewMode.Gold,
        11 => ShiftPreviewMode.Blue,
        _ => ShiftPreviewMode.None,
      };
    }

    if (IsWoodstock(family))
    {
      return keyChartIndex == 4 ? ShiftPreviewMode.Blue : ShiftPreviewMode.None;
    }

    return keyChartIndex switch
    {
      10 => ShiftPreviewMode.Gold,
      11 => ShiftPreviewMode.GoldInverse,
      14 => ShiftPreviewMode.Blue,
      _ => ShiftPreviewMode.None,
    };
  }

  private static bool IsHp01(string? family) =>
    string.Equals(family, "HP01", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp19C(string? family) =>
    string.Equals(family, "HP19C", StringComparison.OrdinalIgnoreCase);

  private static bool IsWoodstock(string? family) =>
    string.Equals(family, "Woodstock", StringComparison.OrdinalIgnoreCase)
    || string.Equals(family, "Spice", StringComparison.OrdinalIgnoreCase);
}
