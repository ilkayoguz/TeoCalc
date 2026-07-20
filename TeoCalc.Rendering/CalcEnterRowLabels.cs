using System.Numerics;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>HP-65 ENTER-row CapAbove legend names (PREFIX / STK / REG / PRGM).</summary>
public static class CalcEnterRowLabels
{
  public static bool IsEnterRowKey(int keyChartIndex) => keyChartIndex is 15 or 17 or 18 or 19;

  /// <summary>Gap from key well top to gold shift label center (matches ENTER row).</summary>
  public static float ShiftLabelGapAboveKey(CalcChassisMetrics metrics) => metrics.Scale * 9f;

  /// <summary>Vertical center for PREFIX / STK / REG / PRGM.</summary>
  public static float ShiftLabelRowCenterY(Vector2 origin, CalcChassisMetrics metrics)
  {
    RectF enterRect = metrics.KeyRect(origin, 15);
    return enterRect.Y - ShiftLabelGapAboveKey(metrics);
  }

  public static string? GoldLabelForKey(int keyChartIndex) =>
    keyChartIndex switch
    {
      15 => "PREFIX",
      17 => "STK",
      18 => "REG",
      19 => "PRGM",
      _ => null,
    };
}
