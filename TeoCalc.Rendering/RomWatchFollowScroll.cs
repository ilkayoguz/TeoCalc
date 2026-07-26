namespace TeoCalc.Rendering;

/// <summary>
/// Keeps ROM-watch PC in view without snapping every microcode step.
/// Only moves the window when PC leaves a margin band inside the visible rows.
/// </summary>
public static class RomWatchFollowScroll
{
  public const int DefaultWindowRows = 64;

  public const int DefaultMargin = 4;

  /// <summary>
  /// Returns the next scroll-top address for a windowed ROM list.
  /// </summary>
  public static int Adjust(
    int scroll,
    int programCounter,
    int wordCount,
    int windowRows = DefaultWindowRows,
    int margin = DefaultMargin)
  {
    if (wordCount <= 0)
    {
      return 0;
    }

    int maxIndex = wordCount - 1;
    int pc = Math.Clamp(programCounter, 0, maxIndex);
    int rows = Math.Max(1, windowRows);
    int pad = Math.Clamp(margin, 0, Math.Max(0, rows / 2 - 1));
    int next = Math.Clamp(scroll, 0, maxIndex);
    int last = next + rows - 1;

    if (pc >= next + pad && pc <= last - pad)
    {
      return next;
    }

    // Soft re-center: PC about one-third down the window.
    int target = pc - rows / 3;
    return Math.Clamp(target, 0, maxIndex);
  }

  /// <summary>Force PC near the upper third (Follow toggled on / model load).</summary>
  public static int CenterOn(int programCounter, int wordCount, int windowRows = DefaultWindowRows) =>
    Adjust(scroll: int.MinValue, programCounter, wordCount, windowRows, margin: 0);
}
