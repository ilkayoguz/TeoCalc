namespace TeoCalc.Rendering.Faceplate;

/// <summary>Named palette entry; resolved at render time via <see cref="CalcFaceplateTheme"/>.</summary>
public readonly record struct CalcColorToken(string Name)
{
  public static CalcColorToken From(string name) => new(name);

  public override string ToString() => Name;
}
