namespace TeoCalc.Rendering.Faceplate;

/// <summary>Named palette entry; resolved at render time via <see cref="CalcKeyColorPalette"/>.</summary>
public readonly record struct CalcColorToken(string Name)
{
  public override string ToString() => Name;
}
