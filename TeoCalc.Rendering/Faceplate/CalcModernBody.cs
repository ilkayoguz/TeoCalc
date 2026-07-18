namespace TeoCalc.Rendering.Faceplate;

/// <summary>Modern theme: procedural body chrome (Display → Switches → Keypad → Logo). No bitmap/SVG shell.</summary>
public static class CalcModernBody
{
  public static bool IsActive =>
    string.Equals(CalcFaceplateTheme.Current.Id, "Modern", StringComparison.OrdinalIgnoreCase);
}
