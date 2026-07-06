using ImGuiNET;
using System.Numerics;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcFaceplateView
{
  public static void Draw(CalcExplorerSession session)
  {
    if (!session.SupportsFaceplate || session.Vocabulary is null)
    {
      PanamatikDisplayOnlyView.Draw(session);
      return;
    }

    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(session.Model.Model);
    CalcBodyLayout bodyLayout = CalcBodyLayoutCatalog.Resolve(faceplateModel);
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available, bodyLayout);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(session.Model.Family, session.Model.Model);

    System.Numerics.Vector2 origin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();

    ImGui.Dummy(new System.Numerics.Vector2(metrics.Width, metrics.Height));
    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
    {
      ImGui.SetWindowFocus();
    }

    bool powerOn = session.PowerOn;
    bool calcHovered = ImGui.IsWindowHovered(
      ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.ChildWindows);
    bool calcFocused = ImGui.IsWindowFocused();
    bool calcInputActive = (calcFocused || calcHovered) && !ImGui.GetIO().WantTextInput;

    CalcFaceplateKeyboard.Update(session, session.Vocabulary, calcInputActive);
    int keyboardHeldKey = CalcFaceplateKeyboard.HeldKeyChartIndex;

    CalcChassisRenderer.DrawShell(draw, origin, metrics, faceplateModel);
    RectF display = metrics.DisplayRect(origin);
    FirmwareDisplaySnapshot displaySnapshot = session.DisplaySnapshot;
    CalcChassisRenderer.DrawPanamatikDisplay(
      draw,
      display,
      session.ProgramMode,
      metrics.Scale,
      displaySnapshot.Visible,
      displaySnapshot.Text);

    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, powerOn, session.ProgramMode);
    CalcChassisRenderer.SwitchPointerState switchPointer =
      CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session, powerOn);
    bool anySwitchHovered = switchPointer.Hovered;
    bool switchClickHandled = switchPointer.ClickHandled;
    ShiftPreviewMode shiftPreview = session.ShiftPreview.Mode;
    CalcEnterRowLabels.Draw(draw, origin, metrics, shiftPreview);

    bool anyKeyHovered = DrawKeypadRows(
      draw,
      origin,
      metrics,
      cells,
      session,
      faceplateModel,
      shiftPreview,
      calcInputActive,
      powerOn,
      switchClickHandled,
      keyboardHeldKey);

    if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
    {
      session.ReleaseMouseKey();
    }

    CalcFaceplatePointer.ApplyHandCursorIfHovering(
      origin,
      metrics,
      cells,
      session.Vocabulary.KeyChart,
      anyKeyHovered,
      anySwitchHovered,
      powerOn,
      session.ProgramMode);

    session.EndDisplayFrame();

    ImGui.PopStyleVar(2);
  }

  private static bool DrawKeypadRows(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    IReadOnlyList<FaceplateCell> cells,
    CalcExplorerSession session,
    CalcModelDefinition faceplateModel,
    ShiftPreviewMode shiftPreview,
    bool calcInputActive,
    bool powerOn,
    bool switchClickHandled,
    int keyboardHeldKey)
  {
    bool anyKeyHovered = false;
    foreach (IGrouping<int, FaceplateCell> row in cells.GroupBy(cell => cell.Row).OrderBy(group => group.Key))
    {
      List<KeypadDrawItem> rowItems = [];
      foreach (FaceplateCell cell in row)
      {
        if (cell.KeyChartIndex >= session.Vocabulary!.KeyChart.Count)
        {
          continue;
        }

        ProgramKeyEntry key = session.Vocabulary.KeyChart[cell.KeyChartIndex];
        if (key.KeyCode == 0 || string.IsNullOrEmpty(ClassicKeyFaceplateLegend.Resolve(
              session.Model.Model,
              key,
              session.Vocabulary,
              cell.LabelStyle).Primary))
        {
          continue;
        }

        RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
        if (keyRect.Width <= 0f || keyRect.Height <= 0f)
        {
          continue;
        }

        HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
          session.Model.Model,
          key,
          session.Vocabulary,
          cell.LabelStyle);
        if (CalcEnterRowLabels.GoldLabelForKey(cell.KeyChartIndex) is { } enterRowGold)
        {
          visual = visual with { GoldShift = enterRowGold, GoldInverseShift = enterRowGold };
        }

        CalcButtonStyle style = CalcButton.StyleForKeyIndex(cell.KeyChartIndex);
        PreviewVisual preview = ApplyShiftPreview(visual, shiftPreview, style);
        CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell);
        CalcKeyVisual keyVisual = BuildKeyVisual(preview, style, kind, cell.KeyChartIndex, shiftPreview);

        rowItems.Add(new KeypadDrawItem(
          cell,
          key,
          visual,
          keyRect,
          keyVisual,
          kind,
          style,
          preview));
      }

      if (rowItems.Count == 0)
      {
        continue;
      }

      int count = rowItems.Count;
      Vector2[] slotMins = new Vector2[count];
      Vector2[] slotMaxs = new Vector2[count];
      Vector2[] capMins = new Vector2[count];
      Vector2[] capMaxs = new Vector2[count];
      CalcKeyVisual[] visuals = new CalcKeyVisual[count];
      for (int i = 0; i < count; i++)
      {
        slotMins[i] = rowItems[i].KeyRect.Min;
        slotMaxs[i] = rowItems[i].KeyRect.Max;
        visuals[i] = rowItems[i].KeyVisual;
      }

      CalcKeyRowLayout.ApplyRowBands(visuals, slotMins, slotMaxs, capMins, capMaxs, metrics.Scale);

      for (int i = 0; i < count; i++)
      {
        KeypadDrawItem item = rowItems[i];
        bool leftAlign = item.Kind != CalcButtonKind.EnterWide && item.Cell.ColSpan >= 2;
        bool keyboardPressed = calcInputActive && powerOn && !switchClickHandled
          && item.Cell.KeyChartIndex == keyboardHeldKey;
        if (CalcKeyComponent.DrawAtCapBounds(
              draw,
              $"##hpkey{item.Cell.KeyChartIndex}",
              slotMins[i],
              slotMaxs[i],
              capMins[i],
              capMaxs[i],
              item.KeyVisual,
              faceplateModel,
              metrics.Scale,
              leftAlign,
              forcePressed: keyboardPressed,
              interactive: calcInputActive && powerOn && !switchClickHandled))
        {
          session.PressKey(item.Cell.KeyChartIndex, (byte)item.Key.KeyCode);
        }

        DrawShiftPreviewIndicator(draw, item.KeyRect, item.Cell.KeyChartIndex, shiftPreview, metrics.Scale);

        if (ImGui.IsItemHovered() && calcInputActive && powerOn)
        {
          anyKeyHovered = true;
          ImGui.SetTooltip($"{item.Visual.Primary}  (code {item.Key.KeyCode})");
        }
      }
    }

    return anyKeyHovered;
  }

  private readonly record struct KeypadDrawItem(
    FaceplateCell Cell,
    ProgramKeyEntry Key,
    HpCalcKeyVisual Visual,
    RectF KeyRect,
    CalcKeyVisual KeyVisual,
    CalcButtonKind Kind,
    CalcButtonStyle Style,
    PreviewVisual Preview);

  private static void DrawShiftPreviewIndicator(
    ImDrawListPtr draw,
    RectF keyRect,
    int keyChartIndex,
    ShiftPreviewMode mode,
    float scale)
  {
    if (mode == ShiftPreviewMode.None || keyChartIndex != ShiftPreviewKeyIndex(mode))
    {
      return;
    }

    uint color = mode == ShiftPreviewMode.Blue
      ? CalcChassisPalette.BlueLabel
      : CalcChassisPalette.GoldLabel;
    float inset = scale * 2.2f;
    float rounding = scale * 7f;
    float thickness = MathF.Max(scale * 1.6f, 2.2f);
    System.Numerics.Vector2 min = keyRect.Min + new System.Numerics.Vector2(inset);
    System.Numerics.Vector2 max = keyRect.Max - new System.Numerics.Vector2(inset);

    draw.AddRect(min, max, WithAlpha(color, 48), rounding, ImDrawFlags.RoundCornersAll, thickness + scale * 5.5f);
    draw.AddRect(min, max, WithAlpha(color, 92), rounding, ImDrawFlags.RoundCornersAll, thickness + scale * 2.6f);
    draw.AddRect(min, max, WithAlpha(color, 245), rounding, ImDrawFlags.RoundCornersAll, thickness);

    if (mode == ShiftPreviewMode.GoldInverse)
    {
      float inner = scale * 3.6f;
      draw.AddRect(
        min + new System.Numerics.Vector2(inner),
        max - new System.Numerics.Vector2(inner),
        WithAlpha(CalcChassisPalette.KeyText, 190),
        MathF.Max(scale * 4f, rounding - inner),
        ImDrawFlags.RoundCornersAll,
        MathF.Max(scale * 0.9f, 1.2f));
    }
  }

  private static int ShiftPreviewKeyIndex(ShiftPreviewMode mode) =>
    mode switch
    {
      ShiftPreviewMode.Gold => 10,
      ShiftPreviewMode.GoldInverse => 11,
      ShiftPreviewMode.Blue => 14,
      _ => -1,
    };

  private static uint WithAlpha(uint color, byte alpha) =>
    (color & 0x00FFFFFFu) | ((uint)alpha << 24);

  private static PreviewVisual ApplyShiftPreview(HpCalcKeyVisual visual, ShiftPreviewMode mode, CalcButtonStyle style)
  {
    return mode switch
    {
      ShiftPreviewMode.Blue when !string.IsNullOrEmpty(visual.BlueShift) => new(
        visual.BlueShift,
        visual.GoldShift,
        visual.Primary,
        CalcKeyLabelPalette.BlueOnCap(style),
        CalcKeyLabelPalette.SkirtOnCap(style),
        null),
      ShiftPreviewMode.Gold when !string.IsNullOrEmpty(visual.GoldShift) => new(
        visual.GoldShift,
        visual.Primary,
        visual.BlueShift,
        CalcKeyLabelPalette.GoldOnCap(style),
        null,
        CalcKeyLabelPalette.PrimaryOnCap(style)),
      ShiftPreviewMode.GoldInverse when !string.IsNullOrEmpty(visual.GoldInverseShift) => new(
        visual.GoldInverseShift,
        visual.Primary,
        visual.BlueShift,
        CalcKeyLabelPalette.GoldOnCap(style),
        null,
        CalcKeyLabelPalette.PrimaryOnCap(style)),
      _ => new(visual.Primary, visual.GoldShift, visual.BlueShift, null, null, null),
    };
  }

  private static CalcKeyVisual BuildKeyVisual(
    PreviewVisual preview,
    CalcButtonStyle style,
    CalcButtonKind kind,
    int keyChartIndex,
    ShiftPreviewMode shiftPreview)
  {
    List<CalcKeyAnnotation> annotations = [];
    if (!string.IsNullOrEmpty(preview.GoldOnBody)
      && (!CalcEnterRowLabels.IsEnterRowKey(keyChartIndex)
        || shiftPreview is ShiftPreviewMode.Gold or ShiftPreviewMode.GoldInverse))
    {
      annotations.Add(new CalcKeyAnnotation(CalcModifierKey.F, CalcLabelAnchor.CapAbove, preview.GoldOnBody));
    }

    if (!string.IsNullOrEmpty(preview.BlueOnSkirt))
    {
      annotations.Add(new CalcKeyAnnotation(CalcModifierKey.G, CalcLabelAnchor.CapSkirt, preview.BlueOnSkirt));
    }

    return new CalcKeyVisual
    {
      CapFace = preview.Face,
      CapStyle = style,
      Kind = kind,
      Annotations = annotations,
      CapFaceInkOverride = preview.FaceInk,
      CapSkirtInkOverride = preview.SkirtInk,
    };
  }

  private readonly record struct PreviewVisual(
    string Face,
    string? GoldOnBody,
    string? BlueOnSkirt,
    uint? FaceInk,
    uint? SkirtInk,
    uint? GoldBodyInk);

}
