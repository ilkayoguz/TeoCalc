using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// Key colors: prefer key.faceplate.json <c>Style</c> via <see cref="CalcKeyStyleResolver"/>.
/// </summary>
public static class CalcFaceplateKeyStyles
{
  public static CalcButtonStyle StyleForKey(string family, string? modelId, int keyChartIndex) =>
    CalcKeyStyleResolver.Resolve(family, modelId, keyChartIndex);
}
