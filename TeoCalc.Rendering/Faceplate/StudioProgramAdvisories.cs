using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Static heuristics on Studio listing / flowchart CFG.
/// Advisories are hints - not auto-fixes (see TODO algorithm assists).
/// </summary>
public static class StudioProgramAdvisories
{
  public enum Kind : byte
  {
    /// <summary>Self-GTO with no RTN/R/S and no outward GTO/GSB.</summary>
    InfiniteLoopSuspect = 1,

    /// <summary>GTO/GSB target has no matching LBL in the listing.</summary>
    MissingLabelTarget = 2,

    /// <summary>GTO/GSB opcode without a following target token.</summary>
    IncompleteBranch = 3,

    /// <summary>Two or more consecutive NOP fillers (bytes that can often be dropped).</summary>
    ConsecutiveNops = 4,

    /// <summary>Adjacent duplicate exit (RTN/RTN or R/S/R/S).</summary>
    DuplicateExit = 5,
  }

  public readonly record struct Advisory(
    Kind Kind,
    string Message,
    string? LabelKey,
    int FirstStep);

  /// <summary>
  /// Analyze listing rows. Safe on empty input. Does not mutate rows.
  /// </summary>
  public static IReadOnlyList<Advisory> Analyze(
    IReadOnlyList<StudioListingView.Row> rows,
    bool omitStripFilters = false)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (rows.Count == 0)
    {
      return [];
    }

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      modelId: null,
      cardStripCaptions: null,
      omitStripFilters: omitStripFilters);

    List<Advisory> advisories = [];
    foreach (StudioFlowchartGraph.Routine routine in graph.Routines)
    {
      if (TryFindSelfGotoInfiniteLoop(rows, graph, routine, out Advisory loop))
      {
        advisories.Add(loop);
      }
    }

    CollectMissingLabelTargets(graph, rows, advisories);
    CollectIncompleteBranches(rows, advisories);
    CollectOptimizeHints(rows, advisories);

    return advisories
      .OrderBy(a => a.FirstStep)
      .ThenBy(a => (int)a.Kind)
      .ToList();
  }

  private static void CollectOptimizeHints(
    IReadOnlyList<StudioListingView.Row> rows,
    List<Advisory> advisories)
  {
    int i = 0;
    while (i < rows.Count)
    {
      if (!StudioFlowchartGraph.IsNopRow(rows[i]))
      {
        i++;
        continue;
      }

      int start = i;
      while (i < rows.Count && StudioFlowchartGraph.IsNopRow(rows[i]))
      {
        i++;
      }

      int count = i - start;
      if (count < 2)
      {
        continue;
      }

      advisories.Add(new Advisory(
        Kind.ConsecutiveNops,
        $"{count} consecutive NOPs from step {rows[start].Index} - candidates to delete (hint only).",
        LabelKey: null,
        rows[start].Index));
    }

    for (int r = 0; r + 1 < rows.Count; r++)
    {
      if (!StudioFlowchartGraph.TryGetExitKind(rows[r], out string exit)
          || !StudioFlowchartGraph.TryGetExitKind(rows[r + 1], out string exit2)
          || !string.Equals(exit, exit2, StringComparison.Ordinal))
      {
        continue;
      }

      advisories.Add(new Advisory(
        Kind.DuplicateExit,
        $"Duplicate {exit} at steps {rows[r].Index} and {rows[r + 1].Index} - second may be redundant.",
        LabelKey: null,
        rows[r].Index));
    }
  }

  private static void CollectMissingLabelTargets(
    StudioFlowchartGraph.Graph graph,
    IReadOnlyList<StudioListingView.Row> rows,
    List<Advisory> advisories)
  {
    HashSet<string> knownLabels = new(StringComparer.OrdinalIgnoreCase);
    foreach (StudioFlowchartGraph.Routine routine in graph.Routines)
    {
      if (!string.IsNullOrEmpty(routine.LabelKey))
      {
        knownLabels.Add(routine.LabelKey);
      }
    }

    HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);
    foreach (StudioFlowchartGraph.Edge edge in graph.Edges)
    {
      if (edge.Kind is not (StudioFlowchartGraph.EdgeKind.Goto or StudioFlowchartGraph.EdgeKind.Gosub))
      {
        continue;
      }

      if (string.IsNullOrEmpty(edge.TargetKey) || knownLabels.Contains(edge.TargetKey))
      {
        continue;
      }

      if (!reported.Add(edge.TargetKey))
      {
        continue;
      }

      int step = StepForNode(graph, rows, edge.FromId);
      string op = edge.Kind == StudioFlowchartGraph.EdgeKind.Gosub ? "GSB" : "GTO";
      advisories.Add(new Advisory(
        Kind.MissingLabelTarget,
        $"{op} {edge.TargetKey} has no matching LBL {edge.TargetKey}.",
        edge.TargetKey,
        step));
    }
  }

  private static void CollectIncompleteBranches(
    IReadOnlyList<StudioListingView.Row> rows,
    List<Advisory> advisories)
  {
    HashSet<int> reportedSteps = [];
    for (int i = 0; i < rows.Count; i++)
    {
      if (!StudioFlowchartGraph.TryParseBranch(
            rows,
            i,
            out StudioFlowchartGraph.EdgeKind kind,
            out string? target,
            out int consumed))
      {
        continue;
      }

      if (!string.IsNullOrEmpty(target))
      {
        i += Math.Max(0, consumed - 1);
        continue;
      }

      if (kind is not (StudioFlowchartGraph.EdgeKind.Goto or StudioFlowchartGraph.EdgeKind.Gosub))
      {
        continue;
      }

      int step = rows[i].Index;
      if (!reportedSteps.Add(step))
      {
        continue;
      }

      string op = kind == StudioFlowchartGraph.EdgeKind.Gosub ? "GSB" : "GTO";
      advisories.Add(new Advisory(
        Kind.IncompleteBranch,
        $"{op} at step {step} has no target (open branch).",
        LabelKey: null,
        step));
    }
  }

  private static int StepForNode(
    StudioFlowchartGraph.Graph graph,
    IReadOnlyList<StudioListingView.Row> rows,
    int nodeId)
  {
    if (nodeId < 0 || nodeId >= graph.Nodes.Count)
    {
      return 0;
    }

    int row = graph.Nodes[nodeId].FirstRow;
    if (row >= 0 && row < rows.Count)
    {
      return rows[row].Index;
    }

    return graph.Nodes[nodeId].FirstStep;
  }

  private static bool TryFindSelfGotoInfiniteLoop(
    IReadOnlyList<StudioListingView.Row> rows,
    StudioFlowchartGraph.Graph graph,
    StudioFlowchartGraph.Routine routine,
    out Advisory advisory)
  {
    advisory = default;
    string? labelKey = routine.LabelKey;
    if (string.IsNullOrEmpty(labelKey))
    {
      return false;
    }

    HashSet<int> routineNodeIds = [];
    foreach (StudioFlowchartGraph.Node node in graph.Nodes)
    {
      if (node.RoutineId == routine.Id)
      {
        routineNodeIds.Add(node.Id);
      }
    }

    bool hasSelfGoto = false;
    bool hasOutwardBranch = false;
    foreach (StudioFlowchartGraph.Edge edge in graph.Edges)
    {
      if (!routineNodeIds.Contains(edge.FromId))
      {
        continue;
      }

      if (edge.Kind is not (StudioFlowchartGraph.EdgeKind.Goto or StudioFlowchartGraph.EdgeKind.Gosub))
      {
        continue;
      }

      if (string.IsNullOrEmpty(edge.TargetKey))
      {
        continue;
      }

      if (edge.Kind == StudioFlowchartGraph.EdgeKind.Goto
          && string.Equals(edge.TargetKey, labelKey, StringComparison.OrdinalIgnoreCase))
      {
        hasSelfGoto = true;
        continue;
      }

      if (!string.Equals(edge.TargetKey, labelKey, StringComparison.OrdinalIgnoreCase))
      {
        hasOutwardBranch = true;
      }
    }

    if (!hasSelfGoto || hasOutwardBranch)
    {
      return false;
    }

    for (int i = routine.FirstRow; i <= routine.LastRow && i < rows.Count; i++)
    {
      if (StudioFlowchartGraph.TryGetExitKind(rows[i], out _))
      {
        return false;
      }
    }

    int firstStep = routine.FirstRow >= 0 && routine.FirstRow < rows.Count
      ? rows[routine.FirstRow].Index
      : 0;
    advisory = new Advisory(
      Kind.InfiniteLoopSuspect,
      $"LBL {labelKey} -> GTO {labelKey} with no RTN/R/S or outward branch - may spin forever.",
      labelKey,
      firstStep);
    return true;
  }
}
