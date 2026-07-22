using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Classic flowchart CFG from Studio listing rows: each LBL (or unlabeled Entry)
/// is an independent routine with START / PROCESS / DECISION / END symbols.
/// Cross-routine GTO/GSB edges target the destination routine's START.
/// Classic two-step LBL pairing follows <see cref="StudioListingView"/> merges;
/// unmerged GTO/GSB + target are paired from adjacent Single rows.
/// </summary>
public static class StudioFlowchartGraph
{
  public enum NodeKind : byte
  {
    Start = 0,
    Process = 1,
    Decision = 2,
    End = 3,
    Branch = 4,
  }

  public enum EdgeKind : byte
  {
    FallThrough = 0,
    /// <summary>Condition TRUE (skip-next rejoin).</summary>
    DecisionYes = 1,
    /// <summary>Condition FALSE (execute next step).</summary>
    DecisionNo = 2,
    Goto = 3,
    Gosub = 4,
  }

  /// <summary>One flowchart symbol inside a routine.</summary>
  public readonly record struct Node(
    int Id,
    int RoutineId,
    NodeKind Kind,
    string Caption,
    int FirstRow,
    int LastRow,
    int FirstStep,
    int LastStep);

  public readonly record struct Edge(
    int FromId,
    int? ToId,
    EdgeKind Kind,
    string Caption,
    string? TargetKey,
    int? TargetRoutineId);

  /// <summary>One independent LBL (or Entry) flowchart.</summary>
  public readonly record struct Routine(
    int Id,
    string Title,
    string? LabelKey,
    int FirstRow,
    int LastRow,
    int StartNodeId);

  public readonly record struct Graph(
    IReadOnlyList<Routine> Routines,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges,
    IReadOnlySet<int> SideLayoutNodeIds,
    IReadOnlyDictionary<int, IReadOnlyList<int>> SideChainsByDecision);

  /// <summary>
  /// Build classic-symbol flowcharts (one routine per LBL / Entry).
  /// <paramref name="modelId"/> / <paramref name="cardStripCaptions"/> feed Studio Legend
  /// text for PROCESS captions (same CapAbove / CapSkirt / strip strings as the listing).
  /// </summary>
  public static Graph Build(
    IReadOnlyList<StudioListingView.Row> rows,
    string? modelId = null,
    IReadOnlyList<string>? cardStripCaptions = null)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (rows.Count == 0)
    {
      return new Graph([], [], [], new HashSet<int>(), new Dictionary<int, IReadOnlyList<int>>());
    }

    List<(int StartRow, string? LabelKey, string Title)> starts = [];
    for (int i = 0; i < rows.Count; i++)
    {
      if (TryGetLabelKey(rows[i], out string key))
      {
        starts.Add((i, key, FormatLabelRoutineTitle(key)));
      }
    }

    List<(int First, int Last, string? LabelKey, string Title)> spans = [];
    if (starts.Count == 0)
    {
      spans.Add((0, rows.Count - 1, null, "Entry"));
    }
    else
    {
      if (starts[0].StartRow > 0)
      {
        spans.Add((0, starts[0].StartRow - 1, null, "Entry"));
      }

      for (int s = 0; s < starts.Count; s++)
      {
        int first = starts[s].StartRow;
        int last = s + 1 < starts.Count ? starts[s + 1].StartRow - 1 : rows.Count - 1;
        spans.Add((first, last, starts[s].LabelKey, starts[s].Title));
      }
    }

    // Drop Classic fall-through stubs (LBL B–E + RTN only, optional NOP fillers) —
    // they are not useful as separate START–END graphs.
    spans.RemoveAll(span => IsEmptyStubRoutine(rows, span.First, span.Last));
    spans.RemoveAll(span => ShouldOmitFirmwareBuiltinStripRoutine(rows, span.First, span.Last));

    List<Routine> routines = [];
    List<Node> nodes = [];
    List<Edge> edges = [];
    int nextNodeId = 0;

    // First pass: emit symbols per routine (edges within / stubs collected after).
    List<(int FromNodeId, EdgeKind Kind, string Caption, string? TargetKey, int? FallThroughTo)> pendingCross = [];

    for (int ri = 0; ri < spans.Count; ri++)
    {
      (int first, int last, string? labelKey, string title) = spans[ri];
      int routineId = ri;
      int startNodeId = -1;

      int r = first;
      int? prevNodeId = null;

      // START: LBL row, or synthetic Entry start covering first process row.
      if (TryGetLabelKey(rows[r], out _))
      {
        int nodeId = nextNodeId++;
        nodes.Add(MakeSymbol(
          nodeId,
          routineId,
          NodeKind.Start,
          title,
          r,
          r,
          rows));
        startNodeId = nodeId;
        prevNodeId = nodeId;
        r++;
      }
      else
      {
        // Synthetic Entry START — no listing rows of its own (body still at first).
        int nodeId = nextNodeId++;
        nodes.Add(new Node(
          nodeId,
          routineId,
          NodeKind.Start,
          title,
          first,
          first,
          FirstStep: -1,
          LastStep: -1));
        startNodeId = nodeId;
        prevNodeId = nodeId;
      }

      while (r <= last)
      {
        StudioListingView.Row row = rows[r];

        // NOP fillers are not useful in the flowchart mental model.
        if (IsNopRow(row))
        {
          r++;
          continue;
        }

        if (TryGetExitKind(row, out string exitKind))
        {
          int nodeId = nextNodeId++;
          nodes.Add(MakeSymbol(nodeId, routineId, NodeKind.End, exitKind, r, r, rows));
          LinkFallThrough(edges, prevNodeId, nodeId);
          prevNodeId = null; // hard stop
          r++;
          continue;
        }

        if (TryGetSkipNote(row, modelId, cardStripCaptions, out string skipCaption))
        {
          int decisionId = nextNodeId++;
          nodes.Add(MakeSymbol(
            decisionId,
            routineId,
            NodeKind.Decision,
            skipCaption,
            r,
            r,
            rows));
          LinkFallThrough(edges, prevNodeId, decisionId);

          int nextRow = r + 1;
          if (nextRow > last)
          {
            edges.Add(new Edge(decisionId, null, EdgeKind.DecisionYes, string.Empty, null, null));
            edges.Add(new Edge(decisionId, null, EdgeKind.DecisionNo, string.Empty, null, null));
            prevNodeId = null;
            r++;
            continue;
          }

          // Chebyshev-style layout: TRUE block runs through first RTN; FALSE block follows it.
          if (TryParseDualBlockDecisionArms(
                rows,
                r,
                last,
                modelId,
                cardStripCaptions,
                out int trueFirst,
                out int trueLast,
                out int falseFirst,
                out int falseLast))
          {
            int trueHead = EmitPathRange(
              ref nextNodeId,
              nodes,
              edges,
              rows,
              routineId,
              trueFirst,
              trueLast,
              modelId,
              cardStripCaptions,
              pendingCross,
              out _);
            int falseHead = EmitPathRange(
              ref nextNodeId,
              nodes,
              edges,
              rows,
              routineId,
              falseFirst,
              falseLast,
              modelId,
              cardStripCaptions,
              pendingCross,
              out int falseTail);

            edges.Add(new Edge(
              decisionId,
              trueHead >= 0 ? trueHead : null,
              EdgeKind.DecisionYes,
              string.Empty,
              null,
              null));
            edges.Add(new Edge(
              decisionId,
              falseHead >= 0 ? falseHead : null,
              EdgeKind.DecisionNo,
              string.Empty,
              null,
              null));

            prevNodeId = falseTail >= 0 ? falseTail : null;
            r = falseLast + 1;
            continue;
          }

          // Simple skip-next: FALSE executes the next symbol; TRUE skips and rejoins after it.
          int falseNodeId = EmitSingleRowSymbol(
            ref nextNodeId,
            nodes,
            rows,
            routineId,
            nextRow,
            last,
            modelId,
            cardStripCaptions,
            pendingCross,
            out int falseRows,
            out bool falseContinues);
          edges.Add(new Edge(
            decisionId,
            falseNodeId,
            EdgeKind.DecisionNo,
            string.Empty,
            null,
            null));

          int trueRow = rows[nextRow].Kind == StudioListingView.MergeKind.BranchPair
            ? nextRow + 1
            : nextRow + falseRows;
          if (falseContinues && trueRow <= last)
          {
            pendingCross.Add((
              falseNodeId,
              EdgeKind.FallThrough,
              string.Empty,
              null,
              trueRow));
          }

          prevNodeId = falseContinues ? falseNodeId : null;
          pendingCross.Add((
            decisionId,
            EdgeKind.DecisionYes,
            string.Empty,
            null,
            trueRow <= last ? trueRow : null));
          r = trueRow;
          continue;
        }

        if (TryParseBranch(rows, r, out EdgeKind branchKind, out string? target, out int consumed))
        {
          int lastBranchRow = Math.Min(r + consumed - 1, last);
          int nodeId = nextNodeId++;
          string caption = branchKind == EdgeKind.Gosub
            ? "GSB " + (target ?? "?")
            : "GTO " + (target ?? "?");
          nodes.Add(MakeSymbol(
            nodeId,
            routineId,
            NodeKind.Branch,
            caption,
            r,
            lastBranchRow,
            rows));
          LinkFallThrough(edges, prevNodeId, nodeId);

          // Edge: arrow tip only — GTO/GSB text lives on the branch node.
          pendingCross.Add((nodeId, branchKind, string.Empty, target, null));

          if (branchKind == EdgeKind.Gosub)
          {
            prevNodeId = nodeId; // continue after return
          }
          else
          {
            prevNodeId = null; // GTO leaves the routine
          }

          r = lastBranchRow + 1;
          continue;
        }

        // PROCESS chunk: consecutive pure-compute ops until a control boundary.
        // TODO(flowchart): Keep consecutive compute (e.g. √x then n!) as one PROCESS
        // with legends joined ("√x · n!") until DECISION / GTO / GSB / RTN / R/S / LBL;
        // split into finer click targets only if users need that later.
        int chunkFirst = r;
        int chunkLast = r;
        while (chunkLast + 1 <= last
               && !IsControlRow(rows, chunkLast + 1, modelId, cardStripCaptions))
        {
          chunkLast++;
        }

        bool preferStrip = IsSoleBodyProcessChunk(rows, first, last, chunkFirst, chunkLast);
        string processCaption = StudioProcessCaption.Build(
          rows,
          chunkFirst,
          chunkLast,
          modelId,
          cardStripCaptions,
          labelKey,
          preferStripCaption: preferStrip);
        if (processCaption.Length == 0)
        {
          // Chunk was only NOP fillers (already skipped at loop head for singles).
          r = chunkLast + 1;
          continue;
        }

        int processId = nextNodeId++;
        nodes.Add(MakeSymbol(
          processId,
          routineId,
          NodeKind.Process,
          processCaption,
          chunkFirst,
          chunkLast,
          rows));
        LinkFallThrough(edges, prevNodeId, processId);
        prevNodeId = processId;
        r = chunkLast + 1;
      }

      routines.Add(new Routine(routineId, title, labelKey, first, last, startNodeId));
    }

    Dictionary<string, int> labelToStartNode = new(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, int> labelToRoutine = new(StringComparer.OrdinalIgnoreCase);
    foreach (Routine routine in routines)
    {
      if (!string.IsNullOrEmpty(routine.LabelKey) && routine.StartNodeId >= 0)
      {
        labelToStartNode[routine.LabelKey] = routine.StartNodeId;
        labelToRoutine[routine.LabelKey] = routine.Id;
      }
    }

    // Map row → first node whose FirstRow == row (for decision Yes/No targets).
    Dictionary<int, int> rowToNode = [];
    foreach (Node node in nodes)
    {
      if (!rowToNode.ContainsKey(node.FirstRow))
      {
        rowToNode[node.FirstRow] = node.Id;
      }
    }

    foreach ((int fromId, EdgeKind kind, string caption, string? targetKey, int? fallRow) in pendingCross)
    {
      if (kind == EdgeKind.FallThrough)
      {
        int? toId = null;
        if (fallRow is int rr && rowToNode.TryGetValue(rr, out int dest))
        {
          toId = dest;
        }

        edges.Add(new Edge(fromId, toId, EdgeKind.FallThrough, caption, TargetKey: null, TargetRoutineId: null));
        continue;
      }

      if (kind is EdgeKind.DecisionYes or EdgeKind.DecisionNo)
      {
        int? toId = null;
        if (fallRow is int rr && rowToNode.TryGetValue(rr, out int dest))
        {
          toId = dest;
        }

        edges.Add(new Edge(fromId, toId, kind, caption, TargetKey: null, TargetRoutineId: null));
        continue;
      }

      // Goto / Gosub cross-routine (or unresolved stub).
      int? toNode = null;
      int? toRoutine = null;
      if (!string.IsNullOrEmpty(targetKey)
          && labelToStartNode.TryGetValue(targetKey, out int startId))
      {
        toNode = startId;
        toRoutine = labelToRoutine[targetKey];
      }

      edges.Add(new Edge(fromId, toNode, kind, caption, targetKey, toRoutine));
    }

    PopulateSideLayout(nodes, edges, out HashSet<int> sideIds, out Dictionary<int, IReadOnlyList<int>> sideChains);
    return new Graph(routines, nodes, edges, sideIds, sideChains);
  }

  /// <summary>
  /// TRUE-arm nodes for dual-block decisions (e.g. Chebyshev <c>1·RTN</c>) layout to the
  /// right of the diamond; FALSE stays on the main spine below.
  /// </summary>
  private static void PopulateSideLayout(
    IReadOnlyList<Node> nodes,
    IReadOnlyList<Edge> edges,
    out HashSet<int> sideIds,
    out Dictionary<int, IReadOnlyList<int>> chainsByDecision)
  {
    sideIds = [];
    chainsByDecision = new Dictionary<int, IReadOnlyList<int>>();
    foreach (Node node in nodes)
    {
      if (node.Kind != NodeKind.Decision)
      {
        continue;
      }

      int decisionId = node.Id;
      int? yesId = null;
      int? noId = null;
      foreach (Edge edge in edges)
      {
        if (edge.FromId != decisionId)
        {
          continue;
        }

        if (edge.Kind == EdgeKind.DecisionYes)
        {
          yesId = edge.ToId;
        }
        else if (edge.Kind == EdgeKind.DecisionNo)
        {
          noId = edge.ToId;
        }
      }

      if (yesId is not int y0 || noId is not int n0)
      {
        continue;
      }

      List<int> yesChain = CollectFallThroughChain(y0, edges);
      if (yesChain.Count == 0)
      {
        continue;
      }

      bool hasRtn = false;
      int maxYesRow = -1;
      foreach (int id in yesChain)
      {
        Node n = nodes[id];
        maxYesRow = Math.Max(maxYesRow, n.LastRow);
        if (n.Kind == NodeKind.End)
        {
          hasRtn = true;
        }
      }

      if (!hasRtn || nodes[n0].FirstRow <= maxYesRow)
      {
        continue;
      }

      chainsByDecision[decisionId] = yesChain;
      foreach (int id in yesChain)
      {
        sideIds.Add(id);
      }
    }
  }

  private static List<int> CollectFallThroughChain(int startId, IReadOnlyList<Edge> edges)
  {
    List<int> chain = [];
    int? cur = startId;
    while (cur is int id)
    {
      if (chain.Contains(id))
      {
        break;
      }

      chain.Add(id);
      int? next = null;
      foreach (Edge edge in edges)
      {
        if (edge.FromId == id && edge.Kind == EdgeKind.FallThrough)
        {
          next = edge.ToId;
          break;
        }
      }

      cur = next;
    }

    return chain;
  }

  /// <summary>Node id whose step range contains <paramref name="stepIndex"/>, or -1.</summary>
  public static int FindNodeIdForStep(Graph graph, int stepIndex)
  {
    if (stepIndex < 0)
    {
      return -1;
    }

    // Prefer the most specific (narrowest) match so Entry START (-1) never steals body steps.
    int bestId = -1;
    int bestSpan = int.MaxValue;
    foreach (Node node in graph.Nodes)
    {
      if (node.FirstStep < 0 || stepIndex < node.FirstStep || stepIndex > node.LastStep)
      {
        continue;
      }

      int span = node.LastStep - node.FirstStep;
      if (span < bestSpan)
      {
        bestSpan = span;
        bestId = node.Id;
      }
    }

    return bestId;
  }

  /// <summary>Routine id containing <paramref name="stepIndex"/>, or -1.</summary>
  public static int FindRoutineIdForStep(Graph graph, int stepIndex)
  {
    int nodeId = FindNodeIdForStep(graph, stepIndex);
    if (nodeId < 0)
    {
      return -1;
    }

    return graph.Nodes[nodeId].RoutineId;
  }

  private static bool IsControlRow(
    IReadOnlyList<StudioListingView.Row> rows,
    int rowIndex,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    if (TryGetLabelKey(rows[rowIndex], out _))
    {
      return true;
    }

    if (TryGetExitKind(rows[rowIndex], out _))
    {
      return true;
    }

    if (TryGetSkipNote(rows[rowIndex], modelId, cardStripCaptions, out _))
    {
      return true;
    }

    return TryParseBranch(rows, rowIndex, out _, out _, out _);
  }

  /// <summary>
  /// Emit one control/process symbol starting at <paramref name="rowIndex"/> (no PROCESS chunking).
  /// Used for the skip-next FALSE arm so DecisionYes (TRUE/skip) can rejoin on the following row.
  /// </summary>
  private static int EmitSingleRowSymbol(
    ref int nextNodeId,
    List<Node> nodes,
    IReadOnlyList<StudioListingView.Row> rows,
    int routineId,
    int rowIndex,
    int lastRow,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    List<(int FromNodeId, EdgeKind Kind, string Caption, string? TargetKey, int? FallThroughTo)> pendingCross,
    out int rowsConsumed,
    out bool continuesAfter)
  {
    rowsConsumed = 1;
    continuesAfter = true;
    StudioListingView.Row row = rows[rowIndex];

    // Skip leading NOPs so the yes-arm lands on a meaningful symbol when possible.
    int emitRow = rowIndex;
    while (emitRow <= lastRow && IsNopRow(rows[emitRow]))
    {
      emitRow++;
    }

    if (emitRow > lastRow)
    {
      // Yes-arm was only NOP fillers — synthesize an empty process so edges still resolve.
      int nopId = nextNodeId++;
      nodes.Add(MakeSymbol(
        nopId,
        routineId,
        NodeKind.Process,
        string.Empty,
        rowIndex,
        lastRow,
        rows));
      rowsConsumed = lastRow - rowIndex + 1;
      return nopId;
    }

    rowsConsumed = emitRow - rowIndex + 1;
    row = rows[emitRow];

    if (TryGetExitKind(row, out string exitKind))
    {
      int id = nextNodeId++;
      nodes.Add(MakeSymbol(id, routineId, NodeKind.End, exitKind, emitRow, emitRow, rows));
      continuesAfter = false;
      return id;
    }

    if (TryGetSkipNote(row, modelId, cardStripCaptions, out string nestedDecision))
    {
      int id = nextNodeId++;
      nodes.Add(MakeSymbol(
        id,
        routineId,
        NodeKind.Decision,
        nestedDecision,
        emitRow,
        emitRow,
        rows));
      return id;
    }

    if (TryParseBranch(rows, emitRow, out EdgeKind branchKind, out string? target, out int consumed))
    {
      int lastBranchRow = Math.Min(emitRow + consumed - 1, lastRow);
      rowsConsumed = lastBranchRow - rowIndex + 1;
      int id = nextNodeId++;
      string caption = branchKind == EdgeKind.Gosub
        ? "GSB " + (target ?? "?")
        : "GTO " + (target ?? "?");
      nodes.Add(MakeSymbol(
        id,
        routineId,
        NodeKind.Branch,
        caption,
        emitRow,
        lastBranchRow,
        rows));
      pendingCross.Add((id, branchKind, string.Empty, target, null));
      continuesAfter = branchKind == EdgeKind.Gosub;
      return id;
    }

    int processId = nextNodeId++;
    nodes.Add(MakeSymbol(
      processId,
      routineId,
      NodeKind.Process,
      ResolveRowCaption(row, modelId, cardStripCaptions),
      emitRow,
      emitRow,
      rows));
    return processId;
  }

  private static void LinkFallThrough(List<Edge> edges, int? fromId, int toId)
  {
    if (fromId is int from)
    {
      edges.Add(new Edge(from, toId, EdgeKind.FallThrough, string.Empty, null, null));
    }
  }

  private static Node MakeSymbol(
    int id,
    int routineId,
    NodeKind kind,
    string caption,
    int firstRow,
    int lastRow,
    IReadOnlyList<StudioListingView.Row> rows)
  {
    return new Node(
      id,
      routineId,
      kind,
      caption,
      firstRow,
      lastRow,
      rows[firstRow].Index,
      LastStepIndex(rows[lastRow]));
  }

  /// <summary>
  /// True when <paramref name="chunkFirst"/>…<paramref name="chunkLast"/> is the only
  /// non-NOP / non-exit body under a labeled routine (sole PROCESS → strip caption OK).
  /// </summary>
  public static bool IsSoleBodyProcessChunk(
    IReadOnlyList<StudioListingView.Row> rows,
    int routineFirst,
    int routineLast,
    int chunkFirst,
    int chunkLast)
  {
    if (routineFirst < 0 || routineLast < routineFirst || chunkFirst < routineFirst || chunkLast > routineLast)
    {
      return false;
    }

    // Body starts after optional LBL row.
    int bodyStart = routineFirst;
    if (TryGetLabelKey(rows[routineFirst], out _))
    {
      bodyStart = routineFirst + 1;
    }

    for (int i = bodyStart; i < chunkFirst; i++)
    {
      if (!IsNopRow(rows[i]))
      {
        return false;
      }
    }

    for (int i = chunkLast + 1; i <= routineLast; i++)
    {
      if (IsNopRow(rows[i]))
      {
        continue;
      }

      if (TryGetExitKind(rows[i], out _))
      {
        continue;
      }

      return false;
    }

    return true;
  }

  private static string ResolveRowCaption(
    StudioListingView.Row row,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions) =>
    StudioProcessCaption.ResolvePart(row, modelId, cardStripCaptions);

  private static int LastStepIndex(StudioListingView.Row row) =>
    row.Index + row.StepSpan - 1;

  /// <summary>True for NOP filler rows (omitted from PROCESS captions and flowchart noise).</summary>
  public static bool IsNopRow(StudioListingView.Row row)
  {
    if (string.Equals(row.Mnemonic.Trim(), "NOP", StringComparison.OrdinalIgnoreCase)
        && (row.Kind == StudioListingView.MergeKind.Single
            || string.IsNullOrEmpty(row.SecondMnemonic)))
    {
      return true;
    }

    return string.Equals(row.DisplayMnemonic.Trim(), "NOP", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Classic fall-through stubs: LBL + only NOP / RTN (no real body).
  /// Omitted so they do not appear as separate START–END graphs.
  /// <c>R/S</c>-only bodies are kept (user stop entry, not an injected stub).
  /// </summary>
  public static bool IsEmptyStubRoutine(
    IReadOnlyList<StudioListingView.Row> rows,
    int first,
    int last)
  {
    if (first < 0 || last < first || last >= rows.Count)
    {
      return false;
    }

    if (!TryGetLabelKey(rows[first], out _))
    {
      return false;
    }

    for (int i = first + 1; i <= last; i++)
    {
      StudioListingView.Row row = rows[i];
      if (IsNopRow(row))
      {
        continue;
      }

      if (TryGetExitKind(row, out string exitKind)
          && string.Equals(exitKind, "RTN", StringComparison.Ordinal))
      {
        continue;
      }

      // Any other content (including R/S) means a real routine body.
      return false;
    }

    // LBL alone, or LBL + NOP* / RTN* only.
    return true;
  }

  /// <summary>
  /// HP-65 firmware leaves no-card strip defaults in RAM after injected stubs
  /// (e.g. <c>LBL E · X&lt;&gt;Y · RTN</c>, <c>LBL A · g · 4 · RTN</c> for <c>1/x</c>).
  /// Omitted even when no card authoring filter is available.
  /// </summary>
  public static bool IsFirmwareBuiltinStripRoutine(
    IReadOnlyList<StudioListingView.Row> rows,
    int first,
    int last)
  {
    if (first < 0 || last < first || last >= rows.Count)
    {
      return false;
    }

    if (!TryGetLabelKey(rows[first], out string key)
        || !ClassicCardStripLabels.TryGetStripColumn(key, out int column))
    {
      return false;
    }

    List<string> body = [];
    for (int i = first + 1; i <= last; i++)
    {
      StudioListingView.Row row = rows[i];
      if (IsNopRow(row))
      {
        continue;
      }

      if (TryGetExitKind(row, out _))
      {
        continue;
      }

      body.Add(row.DisplayMnemonic.Trim());
    }

    if (body.Count == 0)
    {
      return false;
    }

    return column switch
    {
      0 => MatchesLblA_OneOverX(body),
      1 => MatchesLblB_Sqrt(body),
      2 => MatchesLblC_YpowX(body),
      3 => MatchesLblD_Rdown(body),
      4 => MatchesLblE_XExchange(body),
      _ => false,
    };
  }

  /// <summary>
  /// Firmware strip built-ins after user program are omitted; the pure no-card
  /// A–E faceplate catalog is kept intact (prior strip letters are not "user" content).
  /// </summary>
  public static bool ShouldOmitFirmwareBuiltinStripRoutine(
    IReadOnlyList<StudioListingView.Row> rows,
    int first,
    int last)
  {
    if (!IsFirmwareBuiltinStripRoutine(rows, first, last))
    {
      return false;
    }

    // A–E builtins alone are the catalog — do not drop B–E just because A precedes them.
    if (IsNoCardFaceplateCatalogOnly(rows))
    {
      return false;
    }

    return HasSubstantiveProgramBefore(rows, first);
  }

  /// <summary>
  /// True when the listing is only the HP-65 no-card A–E faceplate catalog
  /// (firmware strip builtins and/or empty stubs) — safe to synthesize missing letters.
  /// </summary>
  public static bool IsNoCardFaceplateCatalogOnly(IReadOnlyList<StudioListingView.Row> rows)
  {
    ArgumentNullException.ThrowIfNull(rows);
    if (rows.Count == 0)
    {
      return true;
    }

    List<(int First, int Last)> spans = [];
    List<int> labelStarts = [];
    for (int i = 0; i < rows.Count; i++)
    {
      if (TryGetLabelKey(rows[i], out _))
      {
        labelStarts.Add(i);
      }
    }

    for (int s = 0; s < labelStarts.Count; s++)
    {
      int first = labelStarts[s];
      int last = s + 1 < labelStarts.Count ? labelStarts[s + 1] - 1 : rows.Count - 1;
      spans.Add((first, last));
    }
    bool[] covered = new bool[rows.Count];
    foreach ((int first, int last) in spans)
    {
      if (!TryGetLabelKey(rows[first], out string key)
          || !ClassicCardStripLabels.TryGetStripColumn(key, out _))
      {
        return false;
      }

      if (!IsFirmwareBuiltinStripRoutine(rows, first, last)
          && !IsEmptyStubRoutine(rows, first, last))
      {
        return false;
      }

      for (int i = first; i <= last; i++)
      {
        covered[i] = true;
      }
    }

    for (int i = 0; i < rows.Count; i++)
    {
      if (covered[i] || IsNopRow(rows[i]))
      {
        continue;
      }

      if (TryGetExitKind(rows[i], out _))
      {
        continue;
      }

      return false;
    }

    return true;
  }

  private static bool HasSubstantiveProgramBefore(
    IReadOnlyList<StudioListingView.Row> rows,
    int beforeIndex)
  {
    for (int i = 0; i < beforeIndex; i++)
    {
      StudioListingView.Row row = rows[i];
      if (IsNopRow(row))
      {
        continue;
      }

      if (TryGetLabelKey(row, out _))
      {
        return true;
      }

      if (!TryGetExitKind(row, out _))
      {
        return true;
      }
    }

    return false;
  }

  private static bool MatchesLblE_XExchange(List<string> body) =>
    body.Count == 1
    && (body[0].Equals("X<>Y", StringComparison.OrdinalIgnoreCase)
        || body[0].Equals("x\u2194y", StringComparison.OrdinalIgnoreCase));

  private static bool MatchesLblA_OneOverX(List<string> body)
  {
    if (body.Count == 1)
    {
      string token = body[0];
      return token.Equals("g 4", StringComparison.OrdinalIgnoreCase)
        || token.Equals("1/x", StringComparison.OrdinalIgnoreCase);
    }

    return body.Count == 2
      && body[0].Equals("g", StringComparison.OrdinalIgnoreCase)
      && body[1].Equals("4", StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesLblB_Sqrt(List<string> body)
  {
    if (body.Count == 1)
    {
      string token = body[0];
      return token.Equals("f 9", StringComparison.OrdinalIgnoreCase)
        || token.Equals("\u221ax", StringComparison.OrdinalIgnoreCase)
        || token.Equals("SQRT", StringComparison.OrdinalIgnoreCase);
    }

    return body.Count == 2
      && body[0].Equals("f", StringComparison.OrdinalIgnoreCase)
      && body[1].Equals("9", StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesLblC_YpowX(List<string> body)
  {
    if (body.Count == 1)
    {
      string token = body[0];
      return token.Equals("g 5", StringComparison.OrdinalIgnoreCase)
        || token.Equals("y^x", StringComparison.OrdinalIgnoreCase);
    }

    return body.Count == 2
      && body[0].Equals("g", StringComparison.OrdinalIgnoreCase)
      && body[1].Equals("5", StringComparison.OrdinalIgnoreCase);
  }

  private static bool MatchesLblD_Rdown(List<string> body) =>
    body.Count == 1
    && (body[0].Equals("RDOWN", StringComparison.OrdinalIgnoreCase)
        || body[0].Equals("R\u2193", StringComparison.OrdinalIgnoreCase));

  /// <summary>True when the row is an LBL definition (merged or fused).</summary>
  public static bool TryGetLabelKey(StudioListingView.Row row, out string key)
  {
    key = string.Empty;
    if (row.Kind == StudioListingView.MergeKind.LabelPair
        && string.Equals(row.Mnemonic.Trim(), "LBL", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(row.SecondMnemonic))
    {
      key = NormalizeTarget(row.SecondMnemonic);
      return key.Length > 0;
    }

    if (TrySplitOpcodeTarget(row.DisplayMnemonic, out string op, out string target)
        && string.Equals(op, "LBL", StringComparison.OrdinalIgnoreCase))
    {
      key = target;
      return key.Length > 0;
    }

    return false;
  }

  /// <summary>
  /// Parse a GTO/GSB at <paramref name="rowIndex"/> (fused mnemonic or Classic two-step).
  /// Shift+GTO conditionals are not branches (see <see cref="TryGetSkipNote"/>).
  /// </summary>
  public static bool TryParseBranch(
    IReadOnlyList<StudioListingView.Row> rows,
    int rowIndex,
    out EdgeKind kind,
    out string? target,
    out int rowsConsumed)
  {
    kind = EdgeKind.FallThrough;
    target = null;
    rowsConsumed = 1;
    if (rowIndex < 0 || rowIndex >= rows.Count)
    {
      return false;
    }

    StudioListingView.Row row = rows[rowIndex];

    if (row.Kind == StudioListingView.MergeKind.ShiftPair)
    {
      return false;
    }

    string display = row.DisplayMnemonic.Trim();
    if (TrySplitOpcodeTarget(display, out string op, out string fusedTarget)
        && IsBranchOpcode(op, out kind))
    {
      target = fusedTarget;
      return true;
    }

    if (!IsBranchOpcode(display, out kind))
    {
      return false;
    }

    if (rowIndex + 1 >= rows.Count)
    {
      target = null;
      return true;
    }

    StudioListingView.Row next = rows[rowIndex + 1];
    if (next.Kind != StudioListingView.MergeKind.Single)
    {
      target = null;
      return true;
    }

    string nextTok = next.Mnemonic.Trim();
    if (IsBranchOpcode(nextTok, out _)
        || IsExitMnemonic(nextTok)
        || string.Equals(nextTok, "LBL", StringComparison.OrdinalIgnoreCase)
        || StudioShiftLegend.IsShiftPrefix(nextTok))
    {
      target = null;
      return true;
    }

    target = NormalizeTarget(nextTok);
    rowsConsumed = 2;
    return true;
  }

  public static bool TryGetExitKind(StudioListingView.Row row, out string exitKind)
  {
    exitKind = string.Empty;
    if (IsExitMnemonic(row.Mnemonic) && row.Kind == StudioListingView.MergeKind.Single)
    {
      exitKind = NormalizeExit(row.Mnemonic);
      return true;
    }

    if (row.Kind != StudioListingView.MergeKind.Single
        && !string.IsNullOrEmpty(row.SecondMnemonic)
        && IsExitMnemonic(row.SecondMnemonic))
    {
      exitKind = NormalizeExit(row.SecondMnemonic);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Skip-next conditionals: fused <c>x=0?</c>, faceplate g-tests (<c>x≠y</c>, <c>x≤y</c>, …),
  /// and shift+GTO/GSB. Not stack exchange (<c>x↔y</c> / <c>X&lt;&gt;Y</c>).
  /// </summary>
  public static bool TryGetSkipNote(StudioListingView.Row row, out string note) =>
    TryGetSkipNote(row, modelId: null, cardStripCaptions: null, out note);

  public static bool TryGetSkipNote(
    StudioListingView.Row row,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    out string note)
  {
    note = string.Empty;

    string display = row.DisplayMnemonic.Trim();
    if (display.EndsWith('?') && IsComparisonTestToken(display))
    {
      note = FormatDecisionCaption(display);
      return true;
    }

    if (display.EndsWith('?'))
    {
      // Other ? mnemonics (flags, etc.) still skip-next on Classic.
      note = display;
      return true;
    }

    if (row.Kind == StudioListingView.MergeKind.ShiftPair
        && !string.IsNullOrEmpty(row.SecondMnemonic)
        && (string.Equals(row.SecondMnemonic.Trim(), "GTO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.SecondMnemonic.Trim(), "GSB", StringComparison.OrdinalIgnoreCase)))
    {
      note = display;
      return true;
    }

    // g + key whose CapSkirt/CapAbove is an x/y or x/0 test (legend often lacks trailing ?).
    string caption = ResolveRowCaption(row, modelId, cardStripCaptions);
    if (IsComparisonTestToken(caption) || IsComparisonTestToken(display))
    {
      note = FormatDecisionCaption(
        !string.IsNullOrWhiteSpace(caption) ? caption : display);
      return true;
    }

    if (!string.IsNullOrEmpty(row.Mnemonic) && IsComparisonTestToken(row.Mnemonic))
    {
      note = FormatDecisionCaption(row.Mnemonic);
      return true;
    }

    if (!string.IsNullOrEmpty(row.SecondMnemonic) && IsComparisonTestToken(row.SecondMnemonic))
    {
      note = FormatDecisionCaption(row.SecondMnemonic);
      return true;
    }

    return false;
  }

  /// <summary>True for skip-next x/y or x/0 tests (not x↔y stack exchange).</summary>
  public static bool IsComparisonTestToken(string? token)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return false;
    }

    string t = NormalizeComparisonToken(token);
    return t is "x!=y" or "x=y" or "x>y" or "x<y" or "x<=y" or "x>=y"
      or "x!=0" or "x=0" or "x>0" or "x<0" or "x<=0" or "x>=0";
  }

  private static string FormatDecisionCaption(string raw)
  {
    string trimmed = raw.Trim();
    if (trimmed.Length == 0)
    {
      return trimmed;
    }

    // Prefer faceplate glyphs in the diamond (x≠y) when the source used ASCII.
    // No trailing "?" — the diamond already marks a DECISION.
    string pretty = trimmed
      .Replace("x!=", "x≠", StringComparison.OrdinalIgnoreCase)
      .Replace("x<=", "x≤", StringComparison.OrdinalIgnoreCase)
      .Replace("x>=", "x≥", StringComparison.OrdinalIgnoreCase);
    while (pretty.EndsWith('?'))
    {
      pretty = pretty[..^1].TrimEnd();
    }

    return pretty;
  }

  private static string NormalizeComparisonToken(string token)
  {
    string t = token.Trim();
    if (t.EndsWith('?'))
    {
      t = t[..^1].Trim();
    }

    // Strip accidental "g " prefix from Keys display.
    if (t.StartsWith("g ", StringComparison.OrdinalIgnoreCase))
    {
      t = t[2..].Trim();
    }

    return t
      .Replace("≠", "!=", StringComparison.Ordinal)
      .Replace("≤", "<=", StringComparison.Ordinal)
      .Replace("≥", ">=", StringComparison.Ordinal)
      .Replace(" ", "", StringComparison.Ordinal)
      .ToLowerInvariant();
  }

  private static bool IsBranchOpcode(string token, out EdgeKind kind)
  {
    kind = EdgeKind.FallThrough;
    string t = token.Trim();
    if (string.Equals(t, "GTO", StringComparison.OrdinalIgnoreCase))
    {
      kind = EdgeKind.Goto;
      return true;
    }

    if (string.Equals(t, "GSB", StringComparison.OrdinalIgnoreCase))
    {
      kind = EdgeKind.Gosub;
      return true;
    }

    return false;
  }

  private static bool IsExitMnemonic(string? mnemonic) =>
    !string.IsNullOrWhiteSpace(mnemonic)
    && (string.Equals(mnemonic.Trim(), "RTN", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mnemonic.Trim(), "R/S", StringComparison.OrdinalIgnoreCase));

  private static string NormalizeExit(string mnemonic)
  {
    string t = mnemonic.Trim();
    return string.Equals(t, "R/S", StringComparison.OrdinalIgnoreCase) ? "R/S" : "RTN";
  }

  private static string NormalizeTarget(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return string.Empty;
    }

    return raw.Trim();
  }

  /// <summary>Split "GTO A" / "LBL 1" / "GSB e" into opcode + target.</summary>
  public static bool TrySplitOpcodeTarget(string mnemonic, out string opcode, out string target)
  {
    opcode = string.Empty;
    target = string.Empty;
    if (string.IsNullOrWhiteSpace(mnemonic))
    {
      return false;
    }

    string[] parts = mnemonic.Split(
      (char[]?)null,
      2,
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length < 2)
    {
      return false;
    }

    opcode = parts[0];
    target = NormalizeTarget(parts[1]);
    return target.Length > 0;
  }

  private static string FormatLabelRoutineTitle(string key) =>
    ClassicCardStripLabels.IsFaceplateLabelKey(key) ? key : "LBL " + key;

  /// <summary>
  /// TRUE arm runs through the first <c>RTN</c>; FALSE arm is the code immediately after it
  /// until the next control row (e.g. Chebyshev <c>x&lt;=y?</c> with <c>1·RTN</c> vs <c>1·STO 4</c>).
  /// </summary>
  private static bool TryParseDualBlockDecisionArms(
    IReadOnlyList<StudioListingView.Row> rows,
    int decisionRow,
    int last,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    out int trueFirst,
    out int trueLast,
    out int falseFirst,
    out int falseLast)
  {
    trueFirst = trueLast = falseFirst = falseLast = -1;
    int next = decisionRow + 1;
    if (next > last)
    {
      return false;
    }

    int rtnRow = -1;
    for (int i = next; i <= last; i++)
    {
      if (TryGetExitKind(rows[i], out string exit) && string.Equals(exit, "RTN", StringComparison.Ordinal))
      {
        rtnRow = i;
        break;
      }

      if (i > next && IsControlRow(rows, i, modelId, cardStripCaptions))
      {
        break;
      }
    }

    if (rtnRow < next)
    {
      return false;
    }

    trueFirst = next;
    trueLast = rtnRow;
    falseFirst = rtnRow + 1;
    if (falseFirst > last)
    {
      return false;
    }

    falseLast = falseFirst;
    while (falseLast + 1 <= last
           && !IsControlRow(rows, falseLast + 1, modelId, cardStripCaptions))
    {
      falseLast++;
    }

    return falseLast >= falseFirst;
  }

  /// <summary>Emit a linear path of symbols between listing rows (inclusive).</summary>
  /// <returns>Head node id (first symbol in the path).</returns>
  private static int EmitPathRange(
    ref int nextNodeId,
    List<Node> nodes,
    List<Edge> edges,
    IReadOnlyList<StudioListingView.Row> rows,
    int routineId,
    int pathFirst,
    int pathLast,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    List<(int FromNodeId, EdgeKind Kind, string Caption, string? TargetKey, int? FallThroughTo)> pendingCross,
    out int tailNodeId)
  {
    tailNodeId = -1;
    int head = -1;
    int? prev = null;
    int r = pathFirst;
    while (r <= pathLast)
    {
      if (IsNopRow(rows[r]))
      {
        r++;
        continue;
      }

      if (TryGetExitKind(rows[r], out string exitKind))
      {
        int id = nextNodeId++;
        nodes.Add(MakeSymbol(id, routineId, NodeKind.End, exitKind, r, r, rows));
        LinkFallThrough(edges, prev, id);
        if (head < 0)
        {
          head = id;
        }

        tailNodeId = id;
        prev = null;
        r++;
        continue;
      }

      if (TryParseBranch(rows, r, out EdgeKind branchKind, out string? target, out int consumed))
      {
        int lastBranchRow = Math.Min(r + consumed - 1, pathLast);
        int id = nextNodeId++;
        string caption = branchKind == EdgeKind.Gosub
          ? "GSB " + (target ?? "?")
          : "GTO " + (target ?? "?");
        nodes.Add(MakeSymbol(
          id,
          routineId,
          NodeKind.Branch,
          caption,
          r,
          lastBranchRow,
          rows));
        LinkFallThrough(edges, prev, id);
        if (head < 0)
        {
          head = id;
        }

        pendingCross.Add((id, branchKind, string.Empty, target, null));
        prev = branchKind == EdgeKind.Gosub ? id : null;
        tailNodeId = id;
        r = lastBranchRow + 1;
        continue;
      }

      int symbolId = EmitSingleRowSymbol(
        ref nextNodeId,
        nodes,
        rows,
        routineId,
        r,
        pathLast,
        modelId,
        cardStripCaptions,
        pendingCross,
        out int rowsConsumed,
        out bool continues);
      LinkFallThrough(edges, prev, symbolId);
      if (head < 0)
      {
        head = symbolId;
      }

      tailNodeId = symbolId;
      prev = continues ? symbolId : null;
      r += Math.Max(1, rowsConsumed);
    }

    return head;
  }
}
