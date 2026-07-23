namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Bidirectional scroll/selection sync between Studio Code listing and Flowchart.
/// Uses a one-frame echo guard so peer pan/scroll does not ping-pong.
/// </summary>
public static class StudioPaneSync
{
  private enum ApplyingPeer
  {
    None,
    Code,
    Flowchart,
  }

  private static ApplyingPeer s_applyingPeer;
  private static int s_followFcStep = -1;
  private static int s_followCodeStep = -1;

  public static bool IsApplyingFromFlowchart => s_applyingPeer == ApplyingPeer.Flowchart;

  public static bool IsApplyingFromCode => s_applyingPeer == ApplyingPeer.Code;

  /// <summary>Which Studio pane last received an explicit click (drives F10/F11 grain).</summary>
  public static StudioFocus Focus { get; private set; } = StudioFocus.Code;

  public enum StudioFocus : byte
  {
    Code = 0,
    Flowchart = 1,
  }

  public static void NoteCodeFocus() => Focus = StudioFocus.Code;

  public static void NoteFlowchartFocus() => Focus = StudioFocus.Flowchart;

  public static void OnCodeSelected(int step)
  {
    NoteCodeFocus();
    if (!IsApplyingFromFlowchart)
    {
      s_followFcStep = step;
    }
  }

  public static void OnFlowchartSelected(int step)
  {
    NoteFlowchartFocus();
    if (!IsApplyingFromCode)
    {
      s_followCodeStep = step;
    }
  }

  /// <summary>
  /// Follow both panes to <paramref name="step"/> without changing <see cref="Focus"/>
  /// (used when PTR moves from W/PRGM entry or Studio step).
  /// </summary>
  public static void FollowPointer(int step)
  {
    if (!IsApplyingFromFlowchart)
    {
      s_followFcStep = step;
    }

    if (!IsApplyingFromCode)
    {
      s_followCodeStep = step;
    }
  }

  public static void OnCodeScrolled(int step)
  {
    if (!IsApplyingFromFlowchart)
    {
      s_followFcStep = step;
    }
  }

  public static void OnFlowchartScrolled(int step)
  {
    if (!IsApplyingFromCode)
    {
      s_followCodeStep = step;
    }
  }

  public static bool TryConsumeFlowchartFollow(out int step)
  {
    step = s_followFcStep;
    if (step < 0)
    {
      return false;
    }

    s_followFcStep = -1;
    s_applyingPeer = ApplyingPeer.Code;
    return true;
  }

  public static bool TryConsumeCodeFollow(out int step)
  {
    step = s_followCodeStep;
    if (step < 0)
    {
      return false;
    }

    s_followCodeStep = -1;
    s_applyingPeer = ApplyingPeer.Flowchart;
    return true;
  }

  /// <summary>Call once per Studio frame after both panes have drawn.</summary>
  public static void EndFrame() => s_applyingPeer = ApplyingPeer.None;
}
