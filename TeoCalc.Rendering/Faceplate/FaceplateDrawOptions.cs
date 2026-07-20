namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Faceplate draw knobs shared by live windows and launcher thumbnails.
/// </summary>
public readonly record struct FaceplateDrawOptions
{
  /// <summary>Live calculator windows — full legends, digits, and chrome text.</summary>
  public static FaceplateDrawOptions Live { get; } = new(SkipText: false);

  /// <summary>Launcher / static preview — shapes and colors only; no textual ink.</summary>
  public static FaceplateDrawOptions Thumbnail { get; } = new(SkipText: true);

  /// <summary>Primitive launcher thumb — black frame + body + key faces only.</summary>
  public static FaceplateDrawOptions PrimitiveThumbnail { get; } = new(SkipText: true, Primitive: true);

  public FaceplateDrawOptions(bool SkipText, bool Primitive = false)
  {
    this.SkipText = SkipText;
    this.Primitive = Primitive;
  }

  /// <summary>
  /// When true, skip CapFace / CapAbove / skirts / switch labels / logo caption /
  /// CLEAR·COMPUTE words / LED digits — keep keys, chrome, and colors.
  /// </summary>
  public bool SkipText { get; }

  /// <summary>Ultra-simple silhouette for small icons (no fitils / key frames / text).</summary>
  public bool Primitive { get; }
}
