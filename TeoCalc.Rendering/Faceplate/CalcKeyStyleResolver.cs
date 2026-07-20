namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Resolves per-key <see cref="CalcButtonStyle"/> from key.faceplate.json <c>Style</c>.
/// </summary>
public static class CalcKeyStyleResolver
{
  public static CalcButtonStyle Resolve(string family, string? modelId, int keyChartIndex)
  {
    _ = family;
    if (!string.IsNullOrWhiteSpace(modelId)
        && ClassicKeyFaceplateLegend.TryGetStyle(modelId, keyChartIndex, out CalcButtonStyle fromJson))
    {
      return fromJson;
    }

    // Style is expected in key.faceplate.json after M1; White only if a key entry is incomplete.
    return CalcButtonStyle.White;
  }

  public static bool TryParse(string? styleName, out CalcButtonStyle style)
  {
    style = default;
    if (string.IsNullOrWhiteSpace(styleName))
    {
      return false;
    }

    return Enum.TryParse(styleName.Trim(), ignoreCase: true, out style);
  }

  public static string Format(CalcButtonStyle style) =>
    style.ToString();
}
