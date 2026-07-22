using System.Numerics;
using ImGuiNET;
using TeoCalc.Formats;
using TeoCalc.Rendering.Faceplate.Flowchart;
using Session = TeoCalc.Rendering.CalcExplorerSession;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Read-only Studio flowchart pane: classic START / PROCESS / DECISION / END
/// symbols with polyline connectors from <see cref="StudioFlowchartGraph"/>.
/// Each LBL routine is its own flowchart (selectable or stacked).
/// </summary>
public static class StudioFlowchartView
{
  private const float NodeMinWidth = 108f;
  /// <summary>Wide enough for captions like <c>STO R1 · +1 · RCL R2</c> without ellipsis.</summary>
  private const float NodeMaxWidth = 360f;
  private const float DecisionMinSize = 72f;
  private const float DecisionMaxSize = 280f;
  private const float NodePadX = 10f;
  private const float NodePadY = 8f;
  private const float NodeGapY = 22f;
  private const float RoutineGapY = 36f;
  private const float CanvasPad = 10f;
  private const float CrossBump = 28f;
  private const float SideColumnGap = 28f;
  /// <summary>Separate TRUE/FALSE side-lane X offsets so arrow tips do not stack.</summary>
  private const float DecisionLaneOffsetTrue = 0f;
  private static readonly uint Border = 0xFF6A7078u;
  private static readonly uint BorderSelected = 0xFFC8B090u;
  private static readonly uint BorderPointer = StudioMnemonicPaint.PointerMarkerInk;
  private static readonly uint TitleInk = StudioMnemonicPaint.DefaultInk;
  private static readonly uint EdgeInk = 0xFF8A929Au;
  /// <summary>DECISION TRUE path (ImGui ABGR: R in low byte).</summary>
  private static readonly uint EdgeYesInk = 0xFF6AC83Cu;
  /// <summary>DECISION FALSE path (ImGui ABGR).</summary>
  private static readonly uint EdgeNoInk = 0xFF4444E0u;
  private static readonly uint EdgeGotoInk = 0xFFD8B06Au;
  private static readonly uint EdgeGosubInk = 0xFF80D05Au;
  /// <summary>STO word (store into Rn) — red.</summary>
  private static readonly uint StoreInk = 0xFF4444E0u;
  /// <summary>RCL word (recall from Rn) — green.</summary>
  private static readonly uint RecallInk = 0xFF6AC83Cu;

  private static int s_selectedRoutine = -1; // -1 = All stacked

  /// <summary>Manual canvas pan (not ImGui ScrollY — that never moved draw origin reliably).</summary>
  private static float s_panY;

  private static float s_panX;
  private static bool s_scrollbarDragging;
  private static bool s_scrollbarGesture;
  private static float s_scrollbarGrabOffsetY;
  private static int s_suppressFollowFrames;
  private static bool s_didPanThisGesture;
  private static bool s_canvasPanArmed;
  private static int s_pressNodeId = -1;
  private static int s_contextNodeId = -1;
  private static Vector2 s_pressPos;
  private static Vector2 s_panDragLast;

  /// <summary>One normalized wheel notch ≈ one FC node row.</summary>
  private const float CanvasWheelPixels = 40f;
  private const float ScrollbarW = 10f;
  private const float EdgeThickness = 2.35f;
  private const float ArrowHeadSize = 7f;

  public static void Draw(
    Session session,
    IReadOnlyList<StudioListingView.Row> rows,
    int pointerHighlightStep)
  {
    ArgumentNullException.ThrowIfNull(session);
    ArgumentNullException.ThrowIfNull(rows);

    ImGui.TextUnformatted("Flowchart");
    if (rows.Count == 0)
    {
      ImGui.TextDisabled("Empty program.");
      return;
    }

    StudioFlowchartGraph.Graph graph = StudioFlowchartGraph.Build(
      rows,
      session.EngineModelId,
      session.CardStripLabels);
    if (graph.Routines.Count == 0 || graph.Nodes.Count == 0)
    {
      ImGui.TextDisabled("No flowchart symbols.");
      return;
    }

    DrawRoutineSelector(graph);

    int selectedNode = StudioFlowchartGraph.FindNodeIdForStep(graph, session.SelectedProgramStep);
    int pointerNode = pointerHighlightStep >= 0
      ? StudioFlowchartGraph.FindNodeIdForStep(graph, pointerHighlightStep)
      : -1;

    float lineH = ImGui.GetTextLineHeight();
    bool showAll = s_selectedRoutine < 0;
    List<int> visibleRoutines = [];
    if (showAll)
    {
      for (int i = 0; i < graph.Routines.Count; i++)
      {
        visibleRoutines.Add(i);
      }
    }
    else if (s_selectedRoutine < graph.Routines.Count)
    {
      visibleRoutines.Add(s_selectedRoutine);
    }

    // Layout: main spine (FALSE / fall-through) vertical; dual-block TRUE arms on the right.
    Dictionary<int, Vector2> nodeMin = [];
    Dictionary<int, Vector2> nodeMax = [];
    Dictionary<int, Vector2> nodeSize = [];
    IReadOnlySet<int> sideNodes = graph.SideLayoutNodeIds;
    float columnW = NodeMinWidth;
    float sideColW = 0f;

    foreach (StudioFlowchartGraph.Node node in graph.Nodes)
    {
      Vector2 size = MeasureNodeSize(
        node,
        lineH,
        session.EngineModelId,
        session.CardStripLabels);
      nodeSize[node.Id] = size;
      columnW = MathF.Max(columnW, size.X);
    }

    float leftCorridor = CrossBump + 48f;
    float rightCorridor = CrossBump + 48f;
    float layoutX = CanvasPad + leftCorridor;
    float layoutY = CanvasPad;

    for (int vi = 0; vi < visibleRoutines.Count; vi++)
    {
      int rid = visibleRoutines[vi];
      foreach (StudioFlowchartGraph.Node node in graph.Nodes)
      {
        if (node.RoutineId != rid || sideNodes.Contains(node.Id))
        {
          continue;
        }

        Vector2 size = nodeSize[node.Id];
        float nx = layoutX + (columnW - size.X) * 0.5f;
        nodeMin[node.Id] = new Vector2(nx, layoutY);
        nodeMax[node.Id] = new Vector2(nx + size.X, layoutY + size.Y);
        layoutY += size.Y + NodeGapY;
      }

      foreach ((int decisionId, IReadOnlyList<int> chain) in graph.SideChainsByDecision)
      {
        if (!nodeMin.TryGetValue(decisionId, out Vector2 anchorMin)
            || !nodeMax.TryGetValue(decisionId, out Vector2 anchorMax))
        {
          continue;
        }

        StudioFlowchartGraph.Node decisionNode = graph.Nodes[decisionId];
        if (decisionNode.RoutineId != rid)
        {
          continue;
        }

        float sideX = layoutX + columnW + SideColumnGap;
        float decisionCy = (anchorMin.Y + anchorMax.Y) * 0.5f;
        float sideY = 0f;
        for (int ci = 0; ci < chain.Count; ci++)
        {
          int nodeId = chain[ci];
          Vector2 size = nodeSize[nodeId];
          if (ci == 0)
          {
            // First TRUE-arm box: vertical center lines up with decision right-mid (arrow anchor).
            sideY = decisionCy - size.Y * 0.5f;
          }

          nodeMin[nodeId] = new Vector2(sideX, sideY);
          nodeMax[nodeId] = new Vector2(sideX + size.X, sideY + size.Y);
          sideColW = MathF.Max(sideColW, size.X);
          sideY += size.Y + NodeGapY;
        }
      }

      layoutY += RoutineGapY - NodeGapY;
    }

    float contentH = CanvasPad;
    foreach ((int id, Vector2 max) in nodeMax)
    {
      contentH = MathF.Max(contentH, max.Y + CanvasPad);
    }

    float contentW = CanvasPad + leftCorridor + columnW
      + (sideColW > 0f ? SideColumnGap + sideColW : 0f)
      + rightCorridor + CanvasPad;

    Vector2 avail = ImGui.GetContentRegionAvail();
    float viewH = MathF.Max(80f, avail.Y - 4f);
    float viewW = MathF.Max(40f, avail.X);

    // Manual pan — do NOT use ImGui ScrollY / SetNextWindowContentSize for FC movement.
    float maxPanY = MathF.Max(0f, contentH - viewH);
    float maxPanX = MathF.Max(0f, contentW - MathF.Max(1f, viewW - ScrollbarW));
    s_panY = Math.Clamp(s_panY, 0f, maxPanY);
    s_panX = Math.Clamp(s_panX, 0f, maxPanX);

    if (s_suppressFollowFrames > 0)
    {
      s_suppressFollowFrames--;
      _ = StudioPaneSync.TryConsumeFlowchartFollow(out _);
    }
    else if (StudioPaneSync.TryConsumeFlowchartFollow(out int followStep))
    {
      // Soft follow: only pan when the target is off-screen (avoids scrollbar release snap).
      TryPanToStepIfNeeded(followStep, graph, nodeMin, nodeMax, maxPanY, viewH);
    }

    ImGuiWindowFlags canvasFlags =
      ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    if (!ImGui.BeginChild(
          "##studio-fc-canvas",
          new Vector2(0f, viewH),
          ImGuiChildFlags.None,
          canvasFlags))
    {
      ImGui.EndChild();
      return;
    }

    // Hit pane sized to content region (not GetWindowSize) so clip matches draw clipping
    // and partially visible boxes at the top/bottom edge remain selectable.
    Vector2 canvasPos = ImGui.GetWindowPos();
    Vector2 canvasSize = ImGui.GetWindowSize();
    Vector2 paneOrigin = ImGui.GetCursorScreenPos();
    Vector2 contentAvail = ImGui.GetContentRegionAvail();
    float scrollGutterForHit = maxPanY >= 1f ? ScrollbarW : 0f;
    float hitW = MathF.Max(1f, contentAvail.X - scrollGutterForHit);
    float hitH = MathF.Max(1f, contentAvail.Y);
    ImGui.InvisibleButton("##fc-canvas-hit", new Vector2(hitW, hitH));
    bool canvasItemActive = ImGui.IsItemActive();
    bool canvasItemClicked = ImGui.IsItemClicked();
    Vector2 paneClipMin = paneOrigin;
    Vector2 paneClipMax = paneOrigin + new Vector2(hitW, hitH);

    // Prefer touch-corrected pointer when Silk left MousePos stale on finger tap.
    Vector2 mouse = CalcImGuiTouchInput.GetPointerPos();
    bool overCanvasRect = mouse.X >= paneClipMin.X
      && mouse.X < paneClipMax.X + scrollGutterForHit
      && mouse.Y >= paneClipMin.Y
      && mouse.Y < paneClipMax.Y;
    bool canvasHovered = overCanvasRect
      || ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem)
      || ImGui.IsWindowHovered(
           ImGuiHoveredFlags.AllowWhenBlockedByActiveItem
           | ImGuiHoveredFlags.ChildWindows);

    if (canvasHovered)
    {
      CalcFaceplatePointer.MarkScrollableUiHovered();
      float wheel = CalcFaceplatePointer.FrameWheelY;
      if (MathF.Abs(wheel) < 1e-6f)
      {
        wheel = ImGui.GetIO().MouseWheel;
      }

      if (MathF.Abs(wheel) > 1e-6f || MathF.Abs(CalcFaceplatePointer.FrameWheelY) > 1e-6f)
      {
        float notches = CalcFaceplatePointer.ConsumeDiscreteNotchesY();
        if (MathF.Abs(notches) > 1e-6f)
        {
          s_panY = Math.Clamp(s_panY - notches * CanvasWheelPixels, 0f, maxPanY);
        }
      }

      float wheelH = CalcFaceplatePointer.FrameWheelX;
      if (MathF.Abs(wheelH) < 1e-6f)
      {
        wheelH = ImGui.GetIO().MouseWheelH;
      }

      if (MathF.Abs(wheelH) > 1e-6f || MathF.Abs(CalcFaceplatePointer.FrameWheelX) > 1e-6f)
      {
        float notches = CalcFaceplatePointer.ConsumeDiscreteNotchesX();
        if (MathF.Abs(notches) > 1e-6f)
        {
          s_panX = Math.Clamp(s_panX - notches * CanvasWheelPixels, 0f, maxPanX);
        }
      }
    }

    UpdateManualScrollbarPan(viewH, contentH, maxPanY);

    // Pan must not rewrite selection — that caused scrollbar snap / sticky focus bugs.
    // Code↔FC selection sync happens only on explicit click (OnFlowchartSelected).

    Dictionary<int, Vector2> screenMin = [];
    Dictionary<int, Vector2> screenMax = [];
    Dictionary<int, IFlowChartComponent> components = [];
    foreach ((int id, Vector2 localMin) in nodeMin)
    {
      screenMin[id] = LocalToScreen(localMin, paneOrigin, s_panX, s_panY);
      screenMax[id] = LocalToScreen(nodeMax[id], paneOrigin, s_panX, s_panY);
    }

    foreach (StudioFlowchartGraph.Node node in graph.Nodes)
    {
      if (screenMin.ContainsKey(node.Id))
      {
        components[node.Id] = FlowChartComponentFactory.Create(node);
      }
    }

    int hoveredNode = canvasHovered
      ? HitTestTopComponent(
          mouse,
          graph,
          components,
          screenMin,
          screenMax,
          paneClipMin,
          paneClipMax)
      : -1;

    float scrollGutter = maxPanY >= 1f ? ScrollbarW : 0f;
    bool overScrollbar = mouse.X >= canvasPos.X + canvasSize.X - scrollGutter;
    UpdateCanvasDragPan(
      canvasItemActive,
      canvasItemClicked,
      overScrollbar,
      mouse,
      maxPanX,
      maxPanY,
      hoveredNode);

    // Select on press (component click). Release / touch keep the same node if we never panned.
    bool pressSelect = (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                        || CalcImGuiTouchInput.WasPrimaryClicked)
      && !s_scrollbarGesture
      && !overScrollbar
      && hoveredNode >= 0;
    bool releaseSelect = ImGui.IsMouseReleased(ImGuiMouseButton.Left)
      && !s_didPanThisGesture
      && !s_scrollbarGesture
      && s_pressNodeId >= 0;
    int selectNodeId = pressSelect
      ? hoveredNode
      : releaseSelect
        ? s_pressNodeId
        : -1;
    if (selectNodeId >= 0
        && TryResolveNode(graph, components, selectNodeId, out StudioFlowchartGraph.Node pressed))
    {
      int step = ResolveNodeClickStep(pressed, graph);
      if (step >= 0)
      {
        session.SelectedProgramStep = step;
        StudioPaneSync.OnFlowchartSelected(step);
      }
    }

    // Context: Set start point
    if (hoveredNode >= 0
        && ImGui.IsMouseClicked(ImGuiMouseButton.Right)
        && TryResolveNode(graph, components, hoveredNode, out _))
    {
      s_contextNodeId = hoveredNode;
      ImGui.OpenPopup("##fc-node-ctx");
    }

    if (ImGui.BeginPopup("##fc-node-ctx"))
    {
      if (ImGui.MenuItem("Set start point")
          && TryResolveNode(graph, components, s_contextNodeId, out StudioFlowchartGraph.Node startNode))
      {
        int step = ResolveNodeClickStep(startNode, graph);
        if (step >= 0)
        {
          _ = session.TrySetProgramStartStep(step);
          session.SelectedProgramStep = step;
          StudioPaneSync.OnFlowchartSelected(step);
        }
      }

      ImGui.EndPopup();
    }

    bool ctrlShiftF10 = ImGui.GetIO().KeyCtrl
      && ImGui.GetIO().KeyShift
      && ImGui.IsKeyPressed(ImGuiKey.F10);
    if (ctrlShiftF10 && session.SelectedProgramStep >= 0)
    {
      _ = session.TrySetProgramStartStep(session.SelectedProgramStep);
    }

    if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      s_didPanThisGesture = false;
      s_pressNodeId = -1;
      s_canvasPanArmed = false;
      if (s_scrollbarGesture)
      {
        // Drop any follow queued during the gesture so release does not yank pan.
        s_suppressFollowFrames = 2;
        _ = StudioPaneSync.TryConsumeFlowchartFollow(out _);
      }

      s_scrollbarGesture = false;
    }

    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    draw.PushClipRect(paneClipMin, paneClipMax, true);

    // Edges under symbols; arrow heads redrawn after fills (tips were covered on GTO/GSB).
    List<(Vector2 From, Vector2 To, uint Col)> arrowHeads = [];
    foreach (StudioFlowchartGraph.Edge edge in graph.Edges)
    {
      if (!screenMin.ContainsKey(edge.FromId))
      {
        continue;
      }

      uint baseCol = edge.Kind switch
      {
        StudioFlowchartGraph.EdgeKind.Goto => EdgeGotoInk,
        StudioFlowchartGraph.EdgeKind.Gosub => EdgeGosubInk,
        StudioFlowchartGraph.EdgeKind.DecisionYes => EdgeYesInk,
        StudioFlowchartGraph.EdgeKind.DecisionNo => EdgeNoInk,
        _ => EdgeInk,
      };

      bool activeEdge = hoveredNode >= 0
        && (edge.FromId == hoveredNode || edge.ToId == hoveredNode);
      (uint col, float thick) = StyleEdge(baseCol, activeEdge);

      Vector2 fromMin = screenMin[edge.FromId];
      Vector2 fromMax = screenMax[edge.FromId];

      if (edge.Kind is StudioFlowchartGraph.EdgeKind.Goto or StudioFlowchartGraph.EdgeKind.Gosub)
      {
        if (edge.ToId is int toId && screenMin.ContainsKey(toId))
        {
          // Entire polyline on the LEFT corridor: exit left, run left, enter left center.
          Vector2 start = SideMid(fromMin, fromMax, right: false);
          Vector2 destMin = screenMin[toId];
          Vector2 destMax = screenMax[toId];
          Vector2 end = SideMid(destMin, destMax, right: false);
          end.X -= 1f;
          DrawLeftBranchPolyline(draw, start, end, col, bumpScale: 1.2f, arrowHeads, thick);
        }
        else
        {
          Vector2 stub = SideMid(fromMin, fromMax, right: false);
          Vector2 end = stub + new Vector2(-36f, 0f);
          Vector2 lineEnd = ShortenToward(stub, end, ArrowHeadSize * 0.9f);
          draw.AddLine(stub, lineEnd, col, thick);
          arrowHeads.Add((stub, end, col));
        }

        continue;
      }

      if (edge.ToId is not int dest || !screenMin.ContainsKey(dest))
      {
        if (edge.Kind is StudioFlowchartGraph.EdgeKind.DecisionYes
            or StudioFlowchartGraph.EdgeKind.DecisionNo)
        {
          // TRUE (Yes) skips around to the side; FALSE (No) would have gone down.
          Vector2 stub = edge.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes
            ? SideMid(fromMin, fromMax, right: true)
            : CenterBottom(fromMin, fromMax);
          Vector2 end = stub + (edge.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes
            ? new Vector2(28f, 0f)
            : new Vector2(0f, 18f));
          Vector2 lineEnd = ShortenToward(stub, end, ArrowHeadSize * 0.9f);
          draw.AddLine(stub, lineEnd, col, thick);
          arrowHeads.Add((stub, end, col));
        }

        continue;
      }

      Vector2 toMin = screenMin[dest];
      Vector2 toMax = screenMax[dest];

      if (edge.Kind == StudioFlowchartGraph.EdgeKind.DecisionYes)
      {
        Vector2 start = SideMid(fromMin, fromMax, right: true);
        start.X += DecisionLaneOffsetTrue;
        if (graph.SideLayoutNodeIds.Contains(dest))
        {
          // TRUE arm on the right column — mostly horizontal from the diamond.
          Vector2 end = new(toMin.X - 1f, MidY(toMin, toMax));
          DrawSideEntryArrow(draw, start, end, col, arrowHeads, thick);
        }
        else
        {
          // Skip-next rejoin further down the spine.
          Vector2 end = SideMid(toMin, toMax, right: true);
          end.X += DecisionLaneOffsetTrue + 1f;
          DrawBranchPolyline(draw, start, end, col, bumpScale: 0.85f, arrowHeads, thick);
        }
      }
      else if (edge.Kind == StudioFlowchartGraph.EdgeKind.DecisionNo)
      {
        // FALSE — straight down on the main spine, red.
        Vector2 start = CenterBottom(fromMin, fromMax);
        Vector2 end = CenterTop(toMin, toMax);
        end.Y -= 1f;
        DrawArrow(draw, start, end, col, arrowHeads, thick);
      }
      else
      {
        Vector2 start = CenterBottom(fromMin, fromMax);
        Vector2 end = CenterTop(toMin, toMax);
        end.Y -= 1f;
        DrawArrow(draw, start, end, col, arrowHeads, thick);
      }
    }

    // Symbols via typed components (no per-node InvisibleButton — canvas button would steal clicks).
    foreach (StudioFlowchartGraph.Node node in graph.Nodes)
    {
      if (!screenMin.TryGetValue(node.Id, out Vector2 min)
          || !screenMax.TryGetValue(node.Id, out Vector2 max)
          || !components.TryGetValue(node.Id, out IFlowChartComponent? component))
      {
        continue;
      }

      if (max.Y < paneClipMin.Y || min.Y > paneClipMax.Y
          || max.X < paneClipMin.X || min.X > paneClipMax.X)
      {
        continue;
      }

      bool selected = node.Id == selectedNode;
      bool atPtr = node.Id == pointerNode;
      bool hovered = node.Id == hoveredNode;
      uint border = atPtr
        ? BorderPointer
        : selected
          ? BorderSelected
          : hovered
            ? BorderSelected
            : Border;
      float thickness = atPtr || selected ? 2.25f : hovered ? 2f : 1.25f;

      component.Draw(
        draw,
        min,
        max,
        new FlowChartDrawContext(
          border,
          thickness,
          session.EngineModelId,
          session.CardStripLabels,
          selected,
          atPtr,
          hovered));

      if (atPtr)
      {
        float h = lineH * 0.55f;
        float cy = min.Y + NodePadY + lineH * 0.5f;
        float tip = max.X - 5f;
        float left = tip - h * 0.9f;
        draw.AddTriangleFilled(
          new Vector2(left, cy - h * 0.5f),
          new Vector2(tip, cy),
          new Vector2(left, cy + h * 0.5f),
          BorderPointer);
      }

      if (hovered)
      {
        ImGui.SetTooltip(FormatNodeTooltip(node, session.CardStripLabels));
      }
    }

    // Arrow tips above fills so GTO/GSB/Decision heads stay visible.
    foreach ((Vector2 from, Vector2 to, uint col) in arrowHeads)
    {
      DrawArrowHead(draw, from, to, col);
    }

    draw.PopClipRect();

    DrawManualScrollbar(draw, contentH, viewH, maxPanY);

    ImGui.EndChild();
  }

  private static bool TryResolveNode(
    StudioFlowchartGraph.Graph graph,
    IReadOnlyDictionary<int, IFlowChartComponent> components,
    int nodeId,
    out StudioFlowchartGraph.Node node)
  {
    if (components.TryGetValue(nodeId, out IFlowChartComponent? component))
    {
      node = component.Node;
      return true;
    }

    for (int i = 0; i < graph.Nodes.Count; i++)
    {
      if (graph.Nodes[i].Id == nodeId)
      {
        node = graph.Nodes[i];
        return true;
      }
    }

    node = default;
    return false;
  }

  private static int ResolveNodeClickStep(
    StudioFlowchartGraph.Node node,
    StudioFlowchartGraph.Graph graph)
  {
    if (node.FirstStep >= 0)
    {
      return node.FirstStep;
    }

    foreach (StudioFlowchartGraph.Node other in graph.Nodes)
    {
      if (other.RoutineId == node.RoutineId && other.FirstStep >= 0)
      {
        return other.FirstStep;
      }
    }

    return -1;
  }

  private static void DrawRoutineSelector(StudioFlowchartGraph.Graph graph)
  {
    if (s_selectedRoutine >= graph.Routines.Count)
    {
      s_selectedRoutine = -1;
    }

    string preview = s_selectedRoutine < 0
      ? "All routines"
      : graph.Routines[s_selectedRoutine].Title;
    ImGui.SetNextItemWidth(MathF.Min(180f, ImGui.GetContentRegionAvail().X));
    if (ImGui.BeginCombo("##fc-routine", preview))
    {
      if (ImGui.Selectable("All routines", s_selectedRoutine < 0))
      {
        s_selectedRoutine = -1;
      }

      for (int i = 0; i < graph.Routines.Count; i++)
      {
        if (ImGui.Selectable(graph.Routines[i].Title, s_selectedRoutine == i))
        {
          s_selectedRoutine = i;
        }
      }

      ImGui.EndCombo();
    }
  }

  private static void DrawSymbol(
    ImDrawListPtr draw,
    StudioFlowchartGraph.Node node,
    Vector2 min,
    Vector2 max,
    uint border,
    float thickness,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    IFlowChartComponent component = FlowChartComponentFactory.Create(node);
    component.Draw(
      draw,
      min,
      max,
      new FlowChartDrawContext(
        border,
        thickness,
        modelId,
        cardStripCaptions,
        Selected: false,
        Pointer: false,
        Hovered: false));
  }

  /// <summary>Caption paint shared by <see cref="FlowChartComponentBase"/> subclasses.</summary>
  internal static void DrawNodeCaption(
    ImDrawListPtr draw,
    StudioFlowchartGraph.Node node,
    Vector2 min,
    Vector2 max,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    float w = max.X - min.X;
    Vector2 center = (min + max) * 0.5f;

    if (node.Kind == StudioFlowchartGraph.NodeKind.Start
        && TryDrawStartStripChrome(draw, node, min, max, modelId, cardStripCaptions))
    {
      return;
    }

    DrawFaceplateCaption(
      draw,
      center,
      w,
      node.Caption,
      diamond: node.Kind == StudioFlowchartGraph.NodeKind.Decision);
  }

  /// <summary>
  /// Centered multi-line caption via <see cref="ClassicFaceplateGlyphs.DrawBodyLabel"/>
  /// (same path as Studio Legend). <c>STO R1</c> / <c>RCL R1</c> render as red / green <c>R1</c> keycaps.
  /// </summary>
  private static void DrawFaceplateCaption(
    ImDrawListPtr draw,
    Vector2 center,
    float boxW,
    string caption,
    bool diamond = false)
  {
    if (caption.Length == 0)
    {
      caption = " ";
    }

    float maxTextW = MathF.Max(8f, boxW - NodePadX * 2f);
    if (diamond)
    {
      // Match MeasureDecisionSize: keep label inside diamond facets.
      maxTextW = MathF.Min(maxTextW, boxW * 0.42f);
    }

    List<(string Text, float Width, float Height)> lines = WrapCaptionLines(caption, maxTextW);
    float totalH = 0f;
    for (int i = 0; i < lines.Count; i++)
    {
      totalH += lines[i].Height;
    }

    float y = center.Y - totalH * 0.5f;
    for (int i = 0; i < lines.Count; i++)
    {
      (string text, float width, float height) = lines[i];
      Vector2 topLeft = new(center.X - width * 0.5f, y);
      DrawColoredCaptionLine(draw, topLeft, text);
      y += height;
    }
  }

  private static ClassicFaceplateGlyphs.LabelSize MeasureColoredCaptionLine(string line)
  {
    float w = 0f;
    float h = 0f;
    // CapAbove measure uses fontSize*0.92; draw ink (√x, R↓, …) often needs the full em.
    float minLineH = StudioMnemonicPaint.LegendFontSize();
    float keyW = StudioMnemonicPaint.KeycapWidthRef * StudioMnemonicPaint.StudioListingScale;
    float keyH = StudioMnemonicPaint.ListingRowContentHeight();
    foreach (string part in SplitCaptionParts(line))
    {
      if (part.Length > 0 && part[0] == '\u0003')
      {
        w += keyW;
        h = MathF.Max(h, keyH);
        continue;
      }

      string glyph = part.Length > 0 && (part[0] == '\u0001' || part[0] == '\u0002')
        ? part[1..]
        : part;
      ClassicFaceplateGlyphs.LabelSize size = StudioMnemonicPaint.MeasureDrawableLegend(glyph);
      w += size.Width;
      h = MathF.Max(h, size.Height);
    }

    return new ClassicFaceplateGlyphs.LabelSize(w, MathF.Max(h, minLineH));
  }

  private static void DrawColoredCaptionLine(ImDrawListPtr draw, Vector2 topLeft, string line)
  {
    float x = topLeft.X;
    float keyW = StudioMnemonicPaint.KeycapWidthRef * StudioMnemonicPaint.StudioListingScale;
    float keyH = StudioMnemonicPaint.ListingRowContentHeight();
    float lineH = MeasureColoredCaptionLine(line).Height;
    float keyY = topLeft.Y + MathF.Max(0f, (lineH - keyH) * 0.5f);
    foreach (string part in SplitCaptionParts(line))
    {
      if (part.StartsWith("\u0001", StringComparison.Ordinal))
      {
        string glyph = part[1..];
        StudioMnemonicPaint.DrawDrawableLegendAt(
          draw,
          new Vector2(x, topLeft.Y),
          glyph,
          StoreInk);
        x += StudioMnemonicPaint.MeasureDrawableLegend(glyph).Width;
        continue;
      }

      if (part.StartsWith("\u0002", StringComparison.Ordinal))
      {
        string glyph = part[1..];
        StudioMnemonicPaint.DrawDrawableLegendAt(
          draw,
          new Vector2(x, topLeft.Y),
          glyph,
          RecallInk);
        x += StudioMnemonicPaint.MeasureDrawableLegend(glyph).Width;
        continue;
      }

      if (part.StartsWith("\u0003", StringComparison.Ordinal))
      {
        string label = part[1..];
        StudioMnemonicPaint.ChromeForLabelKey(out uint face, out uint ink);
        StudioMnemonicPaint.DrawKeycapAt(draw, new Vector2(x, keyY), label, face, ink);
        x += keyW;
        continue;
      }

      StudioMnemonicPaint.DrawDrawableLegendAt(draw, new Vector2(x, topLeft.Y), part, TitleInk);
      x += StudioMnemonicPaint.MeasureDrawableLegend(part).Width;
    }
  }

  /// <summary>
  /// Split a PROCESS line into drawable parts; mark STO with SOH, RCL with STX,
  /// and GTO/GSB/LBL targets with ETX (black label keycap).
  /// </summary>
  internal static List<string> SplitCaptionParts(string line)
  {
    List<string> parts = [];
    string[] groups = line.Split(" · ", StringSplitOptions.None);
    for (int gi = 0; gi < groups.Length; gi++)
    {
      if (gi > 0)
      {
        parts.Add(" · ");
      }

      AppendStoRclTaggedParts(parts, groups[gi]);
    }

    return parts;
  }

  private static void AppendStoRclTaggedParts(List<string> parts, string expr)
  {
    string s = expr.Trim();
    if (s.Length == 0)
    {
      return;
    }

    if (TryAppendBranchLabelTaggedParts(parts, s))
    {
      return;
    }

    // STO4 / RCL1 / STO 4 / RCL 1 → red/green Rn (no STO/RCL word).
    if (StudioMuseumKeycodes.TryParseFusedStoRcl(s, out string fusedOp, out int fusedDigit))
    {
      char tag = string.Equals(fusedOp, "STO", StringComparison.OrdinalIgnoreCase)
        ? '\u0001'
        : '\u0002';
      parts.Add(tag + "R" + fusedDigit.ToString());
      return;
    }

    foreach (string head in new[] { "STO", "RCL" })
    {
      if (!s.StartsWith(head, StringComparison.OrdinalIgnoreCase)
          || (s.Length > head.Length && char.IsLetter(s[head.Length])))
      {
        continue;
      }

      char tag = head[0] == 'S' ? '\u0001' : '\u0002';
      string rest = s[head.Length..].TrimStart();

      // Plain store/recall: STO R1 / STO 1 / STO4 → colored Rn only.
      if (TryParseRegisterToken(rest, out string rn)
          || TryParsePlainRegisterDigit(rest, out rn))
      {
        parts.Add(tag + rn);
        return;
      }

      // Register arithmetic: STO + 1 — red STO, register digit as red Rn.
      if (rest.Length >= 2
          && rest[0] is '+' or '-' or '*' or '/'
          && TryParseRegisterOperand(rest[1..].TrimStart(), out rn))
      {
        parts.Add(tag + head);
        parts.Add(" " + rest[0] + " ");
        parts.Add(tag + rn);
        return;
      }

      parts.Add(tag + head);
      if (rest.Length > 0)
      {
        parts.Add(rest);
      }

      return;
    }

    AppendRegisterRefParts(parts, s);
  }

  /// <summary>
  /// <c>GTO 1</c> / <c>GSB A</c> / <c>LBL E</c> (and fused <c>GTO1</c>): paint the target
  /// with the same black label keycap chrome as Studio listing A–E / 0–9.
  /// </summary>
  private static bool TryAppendBranchLabelTaggedParts(List<string> parts, string expr)
  {
    if (StudioFlowchartGraph.TrySplitOpcodeTarget(expr, out string opcode, out string target)
        && IsBranchLabelOpcode(opcode)
        && ClassicCardStripLabels.IsFaceplateLabelKey(target))
    {
      parts.Add(opcode);
      parts.Add(" ");
      parts.Add('\u0003' + target);
      return true;
    }

    // Fused card forms without space: GTO1, GSBA, LBL0.
    foreach (string head in new[] { "GTO", "GSB", "LBL" })
    {
      if (!expr.StartsWith(head, StringComparison.OrdinalIgnoreCase)
          || expr.Length <= head.Length)
      {
        continue;
      }

      string rest = expr[head.Length..].Trim();
      if (!ClassicCardStripLabels.IsFaceplateLabelKey(rest))
      {
        continue;
      }

      parts.Add(expr[..head.Length]);
      parts.Add(" ");
      parts.Add('\u0003' + rest.Trim());
      return true;
    }

    return false;
  }

  private static bool IsBranchLabelOpcode(string opcode) =>
    string.Equals(opcode, "GTO", StringComparison.OrdinalIgnoreCase)
    || string.Equals(opcode, "GSB", StringComparison.OrdinalIgnoreCase)
    || string.Equals(opcode, "LBL", StringComparison.OrdinalIgnoreCase);

  private static bool TryParsePlainRegisterDigit(string text, out string register)
  {
    register = string.Empty;
    string t = text.Trim();
    if (t.Length == 1 && t[0] is >= '0' and <= '9')
    {
      register = "R" + t;
      return true;
    }

    return false;
  }

  private static bool TryParseRegisterOperand(string text, out string register)
    => TryParseRegisterToken(text, out register) || TryParsePlainRegisterDigit(text, out register);

  private static bool TryParseRegisterToken(string text, out string register)
  {
    register = string.Empty;
    if (text.Length < 2)
    {
      return false;
    }

    if (text[0] is not ('R' or 'r') || !char.IsDigit(text[1]))
    {
      return false;
    }

    int end = 1;
    while (end < text.Length && char.IsDigit(text[end]))
    {
      end++;
    }

    register = "R" + text[1..end];
    return true;
  }

  private static void AppendRegisterRefParts(List<string> parts, string expr)
  {
    int start = 0;
    for (int i = 0; i <= expr.Length; i++)
    {
      if (i < expr.Length
          && (expr[i] is 'R' or 'r')
          && i + 1 < expr.Length
          && char.IsDigit(expr[i + 1]))
      {
        if (i > start)
        {
          parts.Add(expr[start..i]);
        }

        int j = i + 1;
        while (j < expr.Length && char.IsDigit(expr[j]))
        {
          j++;
        }

        parts.Add("\u0002" + expr[i..j]);
        start = j;
        i = j - 1;
        continue;
      }

      if (i == expr.Length && start < expr.Length)
      {
        parts.Add(expr[start..]);
      }
    }
  }

  /// <summary>
  /// Edge captions (incl. <c>→ LBL</c> / <c>GTO 1</c>) via CapAbove glyphs + label keycaps —
  /// never default-font AddText.
  /// </summary>
  private static void DrawEdgeCaption(ImDrawListPtr draw, Vector2 topLeft, uint color, string caption)
  {
    if (string.IsNullOrEmpty(caption))
    {
      return;
    }

    // Branch/label targets need keycap chrome; fall back to flat legend otherwise.
    if (caption.Contains("GTO", StringComparison.OrdinalIgnoreCase)
        || caption.Contains("GSB", StringComparison.OrdinalIgnoreCase)
        || caption.Contains("LBL", StringComparison.OrdinalIgnoreCase))
    {
      DrawColoredCaptionLine(draw, topLeft, caption);
      return;
    }

    StudioMnemonicPaint.DrawDrawableLegendAt(draw, topLeft, caption, color);
  }

  private static string TruncateFaceplateLine(string line, float maxTextW)
  {
    if (line.Length == 0)
    {
      return " ";
    }

    ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(line);
    if (size.Width <= maxTextW || line.Length <= 4)
    {
      return line;
    }

    // ASCII "..." — U+2026 would force PrepareDrawableLegend → ToAsciiLegend on the whole line.
    const string ellipsis = "...";
    string truncated = line;
    while (truncated.Length > 4)
    {
      truncated = truncated[..^1];
      ClassicFaceplateGlyphs.LabelSize ellipsized = MeasureColoredCaptionLine(truncated + ellipsis);
      if (ellipsized.Width <= maxTextW)
      {
        return truncated + ellipsis;
      }
    }

    return truncated + ellipsis;
  }

  /// <summary>
  /// Wrap at explicit newlines and middot groups; only ellipsize a single unbreakable token.
  /// </summary>
  private static List<(string Text, float Width, float Height)> WrapCaptionLines(
    string caption,
    float maxTextW)
  {
    List<(string Text, float Width, float Height)> lines = [];
    foreach (string raw in caption.Split('\n'))
    {
      string[] groups = raw.Split(" · ", StringSplitOptions.None);
      if (groups.Length <= 1)
      {
        string line = TruncateFaceplateLine(raw, maxTextW);
        ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(line);
        lines.Add((line, size.Width, size.Height));
        continue;
      }

      string current = groups[0];
      for (int g = 1; g < groups.Length; g++)
      {
        string candidate = current + " · " + groups[g];
        if (MeasureColoredCaptionLine(candidate).Width <= maxTextW)
        {
          current = candidate;
          continue;
        }

        if (current.Length > 0)
        {
          ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(current);
          lines.Add((current, size.Width, size.Height));
        }

        current = groups[g];
        if (MeasureColoredCaptionLine(current).Width > maxTextW)
        {
          current = TruncateFaceplateLine(current, maxTextW);
        }
      }

      if (current.Length > 0)
      {
        ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(current);
        lines.Add((current, size.Width, size.Height));
      }
    }

    if (lines.Count == 0)
    {
      ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(" ");
      lines.Add((" ", size.Width, size.Height));
    }

    return lines;
  }

  /// <summary>
  /// START for <c>LBL A</c>–<c>E</c>: faceplate letter keycap + strip/legend caption
  /// drawn on the draw list only (no ImGui Dummy — keeps FC ContentSize stable).
  /// </summary>
  private static bool TryDrawStartStripChrome(
    ImDrawListPtr draw,
    StudioFlowchartGraph.Node node,
    Vector2 min,
    Vector2 max,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    if (!TryGetStartStripLetter(node.Caption, out string letter))
    {
      return false;
    }

    string legend = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, letter);
    StudioShiftLegend.ShiftKind kind = string.IsNullOrEmpty(legend)
      ? StudioShiftLegend.ShiftKind.None
      : ClassicCardStripLabels.UsesNoCardStripChrome(cardStripCaptions)
        ? StudioShiftLegend.ShiftKind.NoCardStrip
        : StudioShiftLegend.ShiftKind.CardStrip;

    float s = StudioMnemonicPaint.StudioListingScale;
    float keyW = StudioMnemonicPaint.KeycapWidthRef * s;
    float gap = StudioMnemonicPaint.KeycapGapRef * s;
    float legendW = string.IsNullOrEmpty(legend)
      ? 0f
      : StudioMnemonicPaint.MeasureLegendContentWidth(legend, kind);
    float contentW = keyW + (legendW > 0f ? gap + legendW : 0f);
    float contentH = StudioMnemonicPaint.ListingRowContentHeight();
    float maxW = (max.X - min.X) - NodePadX * 2f;
    if (contentW > maxW && legendW > 0f)
    {
      legendW = MathF.Max(0f, maxW - keyW - gap);
      contentW = keyW + (legendW > 0f ? gap + legendW : 0f);
    }

    Vector2 center = (min + max) * 0.5f;
    Vector2 topLeft = new(center.X - contentW * 0.5f, center.Y - contentH * 0.5f);
    StudioMnemonicPaint.ChromeForLabelKey(out uint face, out uint ink);
    StudioMnemonicPaint.DrawKeycapAt(draw, topLeft, letter, face, ink);
    if (!string.IsNullOrEmpty(legend) && legendW > 0f)
    {
      uint legendInk = kind == StudioShiftLegend.ShiftKind.NoCardStrip
        ? CalcCardSlotComponent.LabelInk
        : kind == StudioShiftLegend.ShiftKind.CardStrip
          ? CalcCardSlotComponent.LabelInk
          : TitleInk;
      if (kind == StudioShiftLegend.ShiftKind.CardStrip)
      {
        StudioMnemonicPaint.DrawCardStripLegendAt(
          draw,
          new Vector2(topLeft.X + keyW + gap, topLeft.Y),
          legend,
          legendW,
          contentH);
      }
      else
      {
        ClassicFaceplateGlyphs.LabelSize legSize =
          StudioMnemonicPaint.MeasureDrawableLegend(legend);
        float ly = topLeft.Y + MathF.Max(0f, (contentH - legSize.Height) * 0.5f);
        StudioMnemonicPaint.DrawDrawableLegendAt(
          draw,
          new Vector2(topLeft.X + keyW + gap, ly),
          legend,
          legendInk);
      }
    }

    return true;
  }

  private static bool TryGetStartStripLetter(string caption, out string letter)
  {
    letter = string.Empty;
    List<string> tokens = StudioMnemonicPaint.Tokenize(caption);
    if (tokens.Count == 1 && ClassicCardStripLabels.IsFaceplateLabelKey(tokens[0]))
    {
      letter = tokens[0].Trim();
      return letter.Length > 0;
    }

    if (tokens.Count >= 2
        && string.Equals(tokens[0], "LBL", StringComparison.OrdinalIgnoreCase)
        && ClassicCardStripLabels.IsFaceplateLabelKey(tokens[1]))
    {
      letter = tokens[1].Trim();
      return letter.Length > 0;
    }

    return false;
  }

  private static bool TryFormatStartTooltip(
    StudioFlowchartGraph.Node node,
    IReadOnlyList<string>? cardStripCaptions,
    out string tip)
  {
    tip = string.Empty;
    if (node.Kind != StudioFlowchartGraph.NodeKind.Start
        || !TryGetStartStripLetter(node.Caption, out string letter))
    {
      return false;
    }

    string legend = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, letter);
    tip = string.IsNullOrEmpty(legend) ? letter : $"{letter} {legend}";
    return true;
  }

  private static string FormatNodeTooltip(
    StudioFlowchartGraph.Node node,
    IReadOnlyList<string>? cardStripCaptions)
  {
    string label = TryFormatStartTooltip(node, cardStripCaptions, out string startTip)
      ? startTip
      : node.Caption;
    label = StudioShiftLegend.ToAsciiLegend(label);
    int lineCount = Math.Max(1, node.LastRow - node.FirstRow + 1);
    string steps = node.FirstStep >= 0
      ? $"steps {node.FirstStep}-{node.LastStep}"
      : "entry";
    return $"{node.Kind}: {label}\n{lineCount} lines - {steps}";
  }

  private static Vector2 MeasureNodeSize(
    StudioFlowchartGraph.Node node,
    float lineH,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions)
  {
    if (node.Kind == StudioFlowchartGraph.NodeKind.Start
        && TryMeasureStartStripContent(node, modelId, cardStripCaptions, out Vector2 startSize))
    {
      float w = Math.Clamp(startSize.X + NodePadX * 2f, NodeMinWidth, NodeMaxWidth);
      float h = MathF.Max(startSize.Y + NodePadY * 2f, NodePadY * 2f + lineH + 4f);
      return new Vector2(w, h);
    }

    if (node.Kind == StudioFlowchartGraph.NodeKind.Decision)
    {
      return MeasureDecisionSize(node.Caption);
    }

    // Prefer full caption width up to NodeMaxWidth; wrap leftover into extra height.
    MeasureCaptionExtents(node.Caption, out float textW, out _, lineH);
    float boxW = Math.Clamp(textW + NodePadX * 2f, NodeMinWidth, NodeMaxWidth);
    float maxTextW = boxW - NodePadX * 2f;
    List<(string Text, float Width, float Height)> wrapped = WrapCaptionLines(node.Caption, maxTextW);
    float wrapH = 0f;
    float wrapW = 0f;
    for (int i = 0; i < wrapped.Count; i++)
    {
      wrapH += wrapped[i].Height;
      wrapW = MathF.Max(wrapW, wrapped[i].Width);
    }

    boxW = Math.Clamp(wrapW + NodePadX * 2f, NodeMinWidth, NodeMaxWidth);
    // Height follows wrapped glyph lines + pad (never shorter than content).
    float boxH = wrapH + NodePadY * 2f + 4f;
    return new Vector2(boxW, MathF.Max(boxH, NodePadY * 2f + lineH + 4f));
  }

  /// <summary>
  /// Diamond must contain an axis-aligned caption rect: at text height H the usable
  /// mid-width is <c>side − H</c>, so <c>side ≥ wrapW + wrapH + pads</c>.
  /// </summary>
  private static Vector2 MeasureDecisionSize(string caption)
  {
    // Force early wrap so labels fit inside the diamond facets.
    float maxTextW = DecisionMaxSize * 0.42f;
    List<(string Text, float Width, float Height)> wrapped = WrapCaptionLines(
      string.IsNullOrEmpty(caption) ? " " : caption,
      maxTextW);
    float wrapW = 0f;
    float wrapH = 0f;
    for (int i = 0; i < wrapped.Count; i++)
    {
      wrapW = MathF.Max(wrapW, wrapped[i].Width);
      wrapH += wrapped[i].Height;
    }

    // Inscribed text rect of size (wrapW × wrapH) needs side ≥ wrapW + wrapH.
    float side = wrapW + wrapH + NodePadX + NodePadY;
    side = Math.Clamp(side, DecisionMinSize, DecisionMaxSize);

    // If max size still clips width, re-wrap tighter and grow height budget.
    float usableW = MathF.Max(8f, side - wrapH - NodePadX);
    if (wrapW > usableW + 0.5f)
    {
      wrapped = WrapCaptionLines(string.IsNullOrEmpty(caption) ? " " : caption, usableW);
      wrapW = 0f;
      wrapH = 0f;
      for (int i = 0; i < wrapped.Count; i++)
      {
        wrapW = MathF.Max(wrapW, wrapped[i].Width);
        wrapH += wrapped[i].Height;
      }

      side = Math.Clamp(wrapW + wrapH + NodePadX + NodePadY, DecisionMinSize, DecisionMaxSize);
    }

    return new Vector2(side, side);
  }

  private static void MeasureCaptionExtents(
    string caption,
    out float width,
    out float height,
    float lineH)
  {
    if (caption.Length == 0)
    {
      caption = " ";
    }

    float minLine = MathF.Max(lineH, StudioMnemonicPaint.LegendFontSize());
    width = 0f;
    height = 0f;
    foreach (string raw in caption.Split('\n'))
    {
      ClassicFaceplateGlyphs.LabelSize size = MeasureColoredCaptionLine(raw);
      width = MathF.Max(width, size.Width);
      height += MathF.Max(minLine, size.Height);
    }

    if (height <= 0f)
    {
      height = minLine;
    }
  }

  private static bool TryMeasureStartStripContent(
    StudioFlowchartGraph.Node node,
    string? modelId,
    IReadOnlyList<string>? cardStripCaptions,
    out Vector2 size)
  {
    _ = modelId;
    size = default;
    if (!TryGetStartStripLetter(node.Caption, out string letter))
    {
      return false;
    }

    string legend = ClassicCardStripLabels.CaptionForLetter(cardStripCaptions, letter);
    StudioShiftLegend.ShiftKind kind = string.IsNullOrEmpty(legend)
      ? StudioShiftLegend.ShiftKind.None
      : ClassicCardStripLabels.UsesNoCardStripChrome(cardStripCaptions)
        ? StudioShiftLegend.ShiftKind.NoCardStrip
        : StudioShiftLegend.ShiftKind.CardStrip;

    float s = StudioMnemonicPaint.StudioListingScale;
    float keyW = StudioMnemonicPaint.KeycapWidthRef * s;
    float gap = StudioMnemonicPaint.KeycapGapRef * s;
    float legendW = string.IsNullOrEmpty(legend)
      ? 0f
      : StudioMnemonicPaint.MeasureLegendContentWidth(legend, kind);
    float contentW = keyW + (legendW > 0f ? gap + legendW : 0f);
    float contentH = StudioMnemonicPaint.ListingRowContentHeight();
    if (!string.IsNullOrEmpty(legend))
    {
      // Legend CapAbove can exceed keycap em-box — size START to the taller of the two.
      float legendH = StudioMnemonicPaint.MeasureDrawableLegend(legend).Height;
      contentH = MathF.Max(contentH, legendH);
    }

    size = new Vector2(contentW, contentH);
    return true;
  }

  private static void DrawSideEntryArrow(
    ImDrawListPtr draw,
    Vector2 from,
    Vector2 to,
    uint col,
    List<(Vector2 From, Vector2 To, uint Col)>? arrowHeads,
    float thickness = EdgeThickness)
  {
    if (MathF.Abs(from.Y - to.Y) < 3f)
    {
      DrawArrow(draw, from, to, col, arrowHeads, thickness);
      return;
    }

    Vector2 corner = new(to.X, from.Y);
    draw.AddLine(from, corner, col, thickness);
    Vector2 lineEnd = ShortenToward(corner, to, ArrowHeadSize * 0.9f);
    draw.AddLine(corner, lineEnd, col, thickness);
    if (arrowHeads is null)
    {
      DrawArrowHead(draw, corner, to, col);
    }
    else
    {
      arrowHeads.Add((corner, to, col));
    }
  }

  private static Vector2 CenterTop(Vector2 min, Vector2 max) =>
    new((min.X + max.X) * 0.5f, min.Y);

  private static Vector2 CenterBottom(Vector2 min, Vector2 max) =>
    new((min.X + max.X) * 0.5f, max.Y);

  private static float MidY(Vector2 min, Vector2 max) => (min.Y + max.Y) * 0.5f;

  private static Vector2 SideMid(Vector2 min, Vector2 max, bool right) =>
    new(right ? max.X : min.X, MidY(min, max));

  private static void DrawArrow(
    ImDrawListPtr draw,
    Vector2 from,
    Vector2 to,
    uint col,
    List<(Vector2 From, Vector2 To, uint Col)>? arrowHeads = null,
    float thickness = EdgeThickness)
  {
    Vector2 delta = to - from;
    float len = delta.Length();
    if (len < 1f)
    {
      return;
    }

    Vector2 dir = delta / len;
    float inset = MathF.Min(ArrowHeadSize * 0.9f, len * 0.42f);
    Vector2 lineEnd = to - dir * inset;
    draw.AddLine(from, lineEnd, col, thickness);
    if (arrowHeads is null)
    {
      DrawArrowHead(draw, from, to, col);
    }
    else
    {
      arrowHeads.Add((from, to, col));
    }
  }

  private static void DrawBranchPolyline(
    ImDrawListPtr draw,
    Vector2 from,
    Vector2 to,
    uint col,
    float bumpScale,
    List<(Vector2 From, Vector2 To, uint Col)>? arrowHeads = null,
    float thickness = EdgeThickness)
  {
    float bump = CrossBump * bumpScale;
    float midX = MathF.Max(from.X, to.X) + bump;
    Vector2 a = new(midX, from.Y);
    Vector2 b = new(midX, to.Y);
    draw.AddLine(from, a, col, thickness);
    draw.AddLine(a, b, col, thickness);
    Vector2 lineEnd = ShortenToward(b, to, ArrowHeadSize * 0.9f);
    draw.AddLine(b, lineEnd, col, thickness);
    if (arrowHeads is null)
    {
      DrawArrowHead(draw, b, to, col);
    }
    else
    {
      arrowHeads.Add((b, to, col));
    }
  }

  /// <summary>GTO/GSB: exit left → vertical left corridor → enter dest left center; tip on last segment.</summary>
  private static void DrawLeftBranchPolyline(
    ImDrawListPtr draw,
    Vector2 from,
    Vector2 to,
    uint col,
    float bumpScale,
    List<(Vector2 From, Vector2 To, uint Col)>? arrowHeads = null,
    float thickness = EdgeThickness)
  {
    float bump = CrossBump * bumpScale;
    float midX = MathF.Min(from.X, to.X) - bump;
    Vector2 a = new(midX, from.Y);
    Vector2 b = new(midX, to.Y);
    draw.AddLine(from, a, col, thickness);
    draw.AddLine(a, b, col, thickness);
    Vector2 lineEnd = ShortenToward(b, to, ArrowHeadSize * 0.9f);
    draw.AddLine(b, lineEnd, col, thickness);
    if (arrowHeads is null)
    {
      DrawArrowHead(draw, b, to, col);
    }
    else
    {
      arrowHeads.Add((b, to, col));
    }
  }

  private static void UpdateManualScrollbarPan(float viewH, float contentH, float maxPanY)
  {
    if (maxPanY < 1f)
    {
      s_scrollbarDragging = false;
      return;
    }

    Vector2 winMin = ImGui.GetWindowPos();
    Vector2 winSize = ImGui.GetWindowSize();
    float trackX = winMin.X + winSize.X - ScrollbarW;
    float trackTop = winMin.Y;
    float trackH = MathF.Max(1f, winSize.Y);
    float thumbH = Math.Clamp(trackH * (viewH / MathF.Max(viewH, contentH)), 18f, trackH);
    float thumbTravel = MathF.Max(0f, trackH - thumbH);
    float thumbY = trackTop + (maxPanY > 0f ? thumbTravel * (s_panY / maxPanY) : 0f);

    // ImGui-owned thumb hit — ActiveId keeps drag stable across release frames (no snap).
    ImGui.SetCursorScreenPos(new Vector2(trackX, thumbY));
    ImGui.InvisibleButton("##fc-sb-thumb", new Vector2(ScrollbarW, thumbH));
    if (ImGui.IsItemActivated())
    {
      s_scrollbarGrabOffsetY = CalcImGuiTouchInput.GetPointerPos().Y - thumbY;
      s_scrollbarDragging = true;
      s_scrollbarGesture = true;
      s_pressNodeId = -1;
      s_suppressFollowFrames = 3;
      _ = StudioPaneSync.TryConsumeFlowchartFollow(out _);
    }

    if (ImGui.IsItemActive() && thumbTravel > 0f)
    {
      float mouseY = CalcImGuiTouchInput.GetPointerPos().Y;
      float thumbTop = Math.Clamp(
        mouseY - s_scrollbarGrabOffsetY,
        trackTop,
        trackTop + thumbTravel);
      s_panY = Math.Clamp(
        maxPanY * ((thumbTop - trackTop) / MathF.Max(1f, thumbTravel)),
        0f,
        maxPanY);
      s_scrollbarDragging = true;
      s_scrollbarGesture = true;
      s_suppressFollowFrames = 3;
    }
    else if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      s_scrollbarDragging = false;
    }

    // Track page click (above/below thumb).
    Vector2 mouse = CalcImGuiTouchInput.GetPointerPos();
    bool overTrack = mouse.X >= trackX
      && mouse.X <= trackX + ScrollbarW
      && mouse.Y >= trackTop
      && mouse.Y <= trackTop + trackH;
    bool overThumb = ImGui.IsItemHovered() || ImGui.IsItemActive();
    if (overTrack)
    {
      CalcFaceplatePointer.MarkScrollableUiHovered();
    }

    if (overTrack
        && !overThumb
        && ImGui.IsMouseClicked(ImGuiMouseButton.Left)
        && !s_scrollbarDragging)
    {
      float localY = Math.Clamp(mouse.Y - trackTop - thumbH * 0.5f, 0f, thumbTravel);
      s_panY = Math.Clamp(maxPanY * (localY / MathF.Max(1f, thumbTravel)), 0f, maxPanY);
      s_scrollbarGrabOffsetY = thumbH * 0.5f;
      s_scrollbarGesture = true;
      s_pressNodeId = -1;
      s_suppressFollowFrames = 3;
      _ = StudioPaneSync.TryConsumeFlowchartFollow(out _);
    }
  }

  private static void UpdateCanvasDragPan(
    bool canvasItemActive,
    bool canvasItemClicked,
    bool overScrollbar,
    Vector2 mouse,
    float maxPanX,
    float maxPanY,
    int hoveredNode)
  {
    if (overScrollbar || s_scrollbarDragging || s_scrollbarGesture)
    {
      return;
    }

    if (canvasItemClicked)
    {
      s_panDragLast = mouse;
      s_pressPos = mouse;
      s_didPanThisGesture = false;
      s_canvasPanArmed = true;
      s_pressNodeId = hoveredNode;
    }

    if (!canvasItemActive || !s_canvasPanArmed)
    {
      return;
    }

    // Prefer ImGui MouseDelta while the canvas InvisibleButton is ActiveId.
    Vector2 delta = ImGui.GetIO().MouseDelta;
    if (MathF.Abs(delta.X) < 1e-6f && MathF.Abs(delta.Y) < 1e-6f)
    {
      delta = mouse - s_panDragLast;
    }

    float dist = Vector2.Distance(mouse, s_pressPos);
    const float dragThreshold = 3f;
    if (!s_didPanThisGesture && dist < dragThreshold)
    {
      s_panDragLast = mouse;
      return;
    }

    s_didPanThisGesture = true;
    if (MathF.Abs(delta.X) > 1e-4f || MathF.Abs(delta.Y) > 1e-4f)
    {
      s_panY = Math.Clamp(s_panY - delta.Y, 0f, maxPanY);
      s_panX = Math.Clamp(s_panX - delta.X, 0f, maxPanX);
    }

    s_panDragLast = mouse;
  }

  private static int HitTestTopComponent(
    Vector2 mouse,
    StudioFlowchartGraph.Graph graph,
    IReadOnlyDictionary<int, IFlowChartComponent> components,
    IReadOnlyDictionary<int, Vector2> screenMin,
    IReadOnlyDictionary<int, Vector2> screenMax,
    Vector2 clipMin,
    Vector2 clipMax)
  {
    for (int ni = graph.Nodes.Count - 1; ni >= 0; ni--)
    {
      StudioFlowchartGraph.Node node = graph.Nodes[ni];
      if (!components.TryGetValue(node.Id, out IFlowChartComponent? component)
          || !screenMin.TryGetValue(node.Id, out Vector2 min)
          || !screenMax.TryGetValue(node.Id, out Vector2 max))
      {
        continue;
      }

      if (component.HitTest(mouse, min, max, clipMin, clipMax))
      {
        return node.Id;
      }
    }

    return -1;
  }

  private static bool TryPanToStepIfNeeded(
    int step,
    StudioFlowchartGraph.Graph graph,
    Dictionary<int, Vector2> nodeMinLocal,
    Dictionary<int, Vector2> nodeMaxLocal,
    float maxPanY,
    float viewH)
  {
    int nodeId = StudioFlowchartGraph.FindNodeIdForStep(graph, step);
    if (nodeId < 0
        || !nodeMinLocal.TryGetValue(nodeId, out Vector2 min)
        || !nodeMaxLocal.TryGetValue(nodeId, out Vector2 max))
    {
      return false;
    }

    float clipTop = s_panY;
    float clipBot = s_panY + viewH;
    // Already on screen — do not yank the viewport (scrollbar snap fix).
    if (max.Y > clipTop + 8f && min.Y < clipBot - 8f)
    {
      return false;
    }

    s_panY = Math.Clamp(min.Y - CanvasPad * 0.5f, 0f, maxPanY);
    return true;
  }

  private static bool TryPanToStep(
    int step,
    StudioFlowchartGraph.Graph graph,
    Dictionary<int, Vector2> nodeMinLocal,
    float maxPanY)
  {
    int nodeId = StudioFlowchartGraph.FindNodeIdForStep(graph, step);
    if (nodeId < 0 || !nodeMinLocal.TryGetValue(nodeId, out Vector2 min))
    {
      return false;
    }

    s_panY = Math.Clamp(min.Y - CanvasPad * 0.5f, 0f, maxPanY);
    return true;
  }

  private static void DrawManualScrollbar(
    ImDrawListPtr draw,
    float contentH,
    float viewH,
    float maxPanY)
  {
    if (maxPanY < 1f)
    {
      return;
    }

    Vector2 winMin = ImGui.GetWindowPos();
    Vector2 winSize = ImGui.GetWindowSize();
    float trackX = winMin.X + winSize.X - ScrollbarW;
    float trackTop = winMin.Y;
    float trackBot = winMin.Y + winSize.Y;
    float trackH = MathF.Max(1f, trackBot - trackTop);
    float thumbH = Math.Clamp(trackH * (viewH / MathF.Max(viewH, contentH)), 18f, trackH);
    float thumbTravel = MathF.Max(0f, trackH - thumbH);
    float thumbY = trackTop + (maxPanY > 0f ? thumbTravel * (s_panY / maxPanY) : 0f);

    uint trackCol = 0xFF2A2E32u;
    uint thumbCol = 0xFF6A7078u;
    uint thumbHot = 0xFF8A929Au;
    draw.AddRectFilled(
      new Vector2(trackX, trackTop),
      new Vector2(trackX + ScrollbarW, trackBot),
      trackCol);

    Vector2 mouse = ImGui.GetMousePos();
    bool overThumb = mouse.X >= trackX
      && mouse.X <= trackX + ScrollbarW
      && mouse.Y >= thumbY
      && mouse.Y <= thumbY + thumbH;

    draw.AddRectFilled(
      new Vector2(trackX + 1f, thumbY),
      new Vector2(trackX + ScrollbarW - 1f, thumbY + thumbH),
      overThumb || s_scrollbarDragging ? thumbHot : thumbCol,
      2f);
  }

  private static void DrawArrowHead(ImDrawListPtr draw, Vector2 from, Vector2 to, uint col)
  {
    Vector2 dir = to - from;
    float len = dir.Length();
    if (len < 1f)
    {
      return;
    }

    dir /= len;
    Vector2 ortho = new(-dir.Y, dir.X);
    const float size = 7f;
    Vector2 tip = to;
    Vector2 left = to - dir * size + ortho * (size * 0.55f);
    Vector2 right = to - dir * size - ortho * (size * 0.55f);
    draw.AddTriangleFilled(tip, left, right, col);
  }

  private static Vector2 ShortenToward(Vector2 from, Vector2 to, float inset)
  {
    Vector2 delta = to - from;
    float len = delta.Length();
    if (len <= inset + 0.5f)
    {
      return from;
    }

    return to - delta / len * inset;
  }

  private static Vector2 LocalToScreen(
    Vector2 local,
    Vector2 paneOrigin,
    float panX,
    float panY) =>
    paneOrigin - new Vector2(panX, panY) + local;

  /// <summary>
  /// True when <paramref name="mouse"/> is inside the node rect and the visible content clip.
  /// Partially visible boxes count when the pointer is over the on-screen strip.
  /// </summary>
  internal static bool HitVisibleNode(
    Vector2 mouse,
    Vector2 screenMin,
    Vector2 screenMax,
    Vector2 clipMin,
    Vector2 clipMax)
  {
    if (mouse.X < clipMin.X || mouse.X >= clipMax.X
        || mouse.Y < clipMin.Y || mouse.Y >= clipMax.Y)
    {
      return false;
    }

    return mouse.X >= screenMin.X
      && mouse.X <= screenMax.X
      && mouse.Y >= screenMin.Y
      && mouse.Y <= screenMax.Y;
  }

  private static (uint Col, float Thick) StyleEdge(uint baseCol, bool active) =>
    active
      ? (HighlightEdgeInk(baseCol), EdgeThickness * 1.85f)
      : (baseCol, EdgeThickness);

  private static uint HighlightEdgeInk(uint col)
  {
    byte a = (byte)(col >> 24);
    byte b = (byte)Math.Min(255, (col & 0xFF) + 55);
    byte g = (byte)Math.Min(255, ((col >> 8) & 0xFF) + 55);
    byte r = (byte)Math.Min(255, ((col >> 16) & 0xFF) + 55);
    return (uint)((a << 24) | (r << 16) | (g << 8) | b);
  }
}
