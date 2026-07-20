namespace TeoCalc.Rendering;

public sealed class ShiftPreviewController
{
  public ShiftPreviewMode Mode { get; private set; }

  public void Clear() => Mode = ShiftPreviewMode.None;

  public void Reset() => Clear();

  public void HandleKeyPress(int keyChartIndex) =>
    HandleKeyPress(keyChartIndex, family: null, modelId: null);

  public void HandleKeyPress(int keyChartIndex, string? family) =>
    HandleKeyPress(keyChartIndex, family, modelId: null);

  public void HandleKeyPress(int keyChartIndex, string? family, string? modelId)
  {
    ShiftPreviewMode requested = ResolveRequested(keyChartIndex, family, modelId);
    if (requested == ShiftPreviewMode.None)
    {
      // Non-shift key clears preview (second function consumed / normal entry).
      if (!IsShiftPrefixKey(keyChartIndex, family, modelId))
      {
        Mode = ShiftPreviewMode.None;
      }

      return;
    }

    Mode = requested == Mode ? ShiftPreviewMode.None : requested;
  }

  /// <summary>Key that should show the active-modifier frame for <paramref name="mode"/>.</summary>
  public static int IndicatorKeyIndex(ShiftPreviewMode mode, string? family) =>
    IndicatorKeyIndex(mode, family, modelId: null);

  public static int IndicatorKeyIndex(ShiftPreviewMode mode, string? family, string? modelId)
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

    if (IsHp35(modelId))
    {
      return -1; // Classic HP-35: no shift prefix keys.
    }

    if (IsWoodstock(family))
    {
      if (IsHp22(modelId) || IsHp31(modelId) || IsHp37(modelId))
      {
        // HP-22 / HP-31E / HP-37E: single gold prefix at chart index 9.
        return mode == ShiftPreviewMode.Gold ? 9 : -1;
      }

      if (IsHp38(modelId))
      {
        // HP-38E: gold f at 8, blue g at 9.
        return mode switch
        {
          ShiftPreviewMode.Gold => 8,
          ShiftPreviewMode.Blue => 9,
          _ => -1,
        };
      }

      if (IsHp34(modelId))
      {
        // HP-34C: gold f at 3, blue g at 4, black h at 9.
        return mode switch
        {
          ShiftPreviewMode.Gold => 3,
          ShiftPreviewMode.Blue => 4,
          ShiftPreviewMode.Black => 9,
          _ => -1,
        };
      }

      if (IsHp25(modelId) || IsHp27(modelId) || IsHp29(modelId) || IsHp32(modelId) || IsHp33(modelId))
      {
        // HP-25 / HP-27 / HP-29C / HP-32E / HP-33C: gold f at 3, blue g at 4 (letters on CapFace).
        return mode switch
        {
          ShiftPreviewMode.Gold => 3,
          ShiftPreviewMode.Blue => 4,
          _ => -1,
        };
      }

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

  public static bool IsShiftPrefixKey(int keyChartIndex, string? family) =>
    IsShiftPrefixKey(keyChartIndex, family, modelId: null);

  public static bool IsShiftPrefixKey(int keyChartIndex, string? family, string? modelId)
  {
    if (IsHp35(modelId))
    {
      return false;
    }

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
      if (IsHp22(modelId) || IsHp31(modelId) || IsHp37(modelId))
      {
        return keyChartIndex == 9;
      }

      if (IsHp38(modelId))
      {
        return keyChartIndex is 8 or 9;
      }

      if (IsHp34(modelId))
      {
        return keyChartIndex is 3 or 4 or 9;
      }

      if (IsHp25(modelId) || IsHp27(modelId) || IsHp29(modelId) || IsHp32(modelId) || IsHp33(modelId))
      {
        return keyChartIndex is 3 or 4;
      }

      return keyChartIndex == 4;
    }

    return keyChartIndex is 10 or 11 or 14;
  }

  private static ShiftPreviewMode ResolveRequested(int keyChartIndex, string? family, string? modelId)
  {
    if (IsHp35(modelId))
    {
      return ShiftPreviewMode.None;
    }

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
      if (IsHp22(modelId) || IsHp31(modelId) || IsHp37(modelId))
      {
        return keyChartIndex == 9 ? ShiftPreviewMode.Gold : ShiftPreviewMode.None;
      }

      if (IsHp38(modelId))
      {
        return keyChartIndex switch
        {
          8 => ShiftPreviewMode.Gold,
          9 => ShiftPreviewMode.Blue,
          _ => ShiftPreviewMode.None,
        };
      }

      if (IsHp34(modelId))
      {
        return keyChartIndex switch
        {
          3 => ShiftPreviewMode.Gold,
          4 => ShiftPreviewMode.Blue,
          9 => ShiftPreviewMode.Black,
          _ => ShiftPreviewMode.None,
        };
      }

      if (IsHp25(modelId) || IsHp27(modelId) || IsHp29(modelId) || IsHp32(modelId) || IsHp33(modelId))
      {
        return keyChartIndex switch
        {
          3 => ShiftPreviewMode.Gold,
          4 => ShiftPreviewMode.Blue,
          _ => ShiftPreviewMode.None,
        };
      }

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

  private static bool IsHp22(string? modelId) =>
    string.Equals(modelId, "HP-22", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "22", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp25(string? modelId) =>
    string.Equals(modelId, "HP-25", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "25", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp27(string? modelId) =>
    string.Equals(modelId, "HP-27", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "27", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp29(string? modelId) =>
    string.Equals(modelId, "HP-29", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-29C", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "29", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "29C", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp31(string? modelId) =>
    string.Equals(modelId, "HP-31", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-31E", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "31", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "31E", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp32(string? modelId) =>
    string.Equals(modelId, "HP-32", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-32E", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "32", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "32E", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp33(string? modelId) =>
    string.Equals(modelId, "HP-33", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-33C", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-33E", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "33", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "33C", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "33E", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp34(string? modelId) =>
    string.Equals(modelId, "HP-34", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-34C", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "34", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "34C", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp37(string? modelId) =>
    string.Equals(modelId, "HP-37", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-37E", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "37", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "37E", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp38(string? modelId) =>
    string.Equals(modelId, "HP-38", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "HP-38E", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "38", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "38E", StringComparison.OrdinalIgnoreCase);

  private static bool IsHp35(string? modelId) =>
    string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase)
    || string.Equals(modelId, "35", StringComparison.OrdinalIgnoreCase);
}
