using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Modern display band (outer bezel). Switch panel aligns to this horizontally.</summary>
public static class CalcDisplayComponent
{
  /// <summary>Outer display bezel in 00d reference units — left/right authority for face bands.</summary>
  public static RectF OuterSlot => Calc00dBodyLayout.DisplayBezelSlot;

  public static RectF GlassSlot => Calc00dBodyLayout.DisplayGlassSlot;

  public static float BandLeft => OuterSlot.X;

  public static float BandWidth => OuterSlot.Width;

  public static float BandBottom => OuterSlot.Y + OuterSlot.Height;
}
