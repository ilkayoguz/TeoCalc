using ImGuiNET;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class CalcFaceplateView
{
  public static void Draw(CalcExplorerSession session)
  {
    if (!session.SupportsCpu || session.Vocabulary is null || session.Cpu is null)
    {
      ImGui.TextDisabled("Calculator faceplate requires a Classic CPU model.");
      return;
    }

    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = ImGui.GetContentRegionAvail();
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available);
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

    CalcChassisRenderer.DrawShell(draw, origin, metrics);
    RectF display = metrics.DisplayRect(origin);
    FirmwareDisplaySnapshot displaySnapshot = session.DisplaySnapshot;
    CalcChassisRenderer.DrawDisplayDigits(
      draw,
      display,
      session.Cpu,
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

    bool anyKeyHovered = false;
    foreach (FaceplateCell cell in cells)
    {
      if (cell.KeyChartIndex >= session.Vocabulary.KeyChart.Count)
      {
        continue;
      }

      ProgramKeyEntry key = session.Vocabulary.KeyChart[cell.KeyChartIndex];
      if (key.KeyCode == 0)
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
      if (string.IsNullOrEmpty(visual.Primary))
      {
        continue;
      }

      RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
      if (keyRect.Width <= 0f || keyRect.Height <= 0f)
      {
        continue;
      }

      System.Numerics.Vector2 cellMin = keyRect.Min;
      System.Numerics.Vector2 cellMax = keyRect.Max;
      System.Numerics.Vector2 cellSize = keyRect.Size;
      CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell);

      if (!string.IsNullOrEmpty(preview.GoldOnBody)
        && (!CalcEnterRowLabels.IsEnterRowKey(cell.KeyChartIndex)
          || shiftPreview is ShiftPreviewMode.Gold or ShiftPreviewMode.GoldInverse))
      {
        DrawGoldBodyLabel(draw, preview.GoldOnBody, cellMin, cellMax, metrics, preview.GoldBodyInk);
      }

      bool leftAlign = kind != CalcButtonKind.EnterWide && cell.ColSpan >= 2;
      bool keyboardPressed = calcInputActive && powerOn && !switchClickHandled && cell.KeyChartIndex == keyboardHeldKey;
      if (CalcButton.Draw(
            draw,
            $"##hpkey{cell.KeyChartIndex}",
            cellMin,
            cellMax,
            style,
            kind,
            preview.Face,
            goldOnBody: null,
            blueOnBody: preview.BlueOnSkirt,
            metrics.Scale,
            leftAlign,
            forcePressed: keyboardPressed,
            interactive: calcInputActive && powerOn && !switchClickHandled,
            primaryInkOverride: preview.FaceInk,
            skirtInkOverride: preview.SkirtInk))
      {
        session.PressKey(cell.KeyChartIndex, (byte)key.KeyCode);
      }

      DrawShiftPreviewIndicator(draw, keyRect, cell.KeyChartIndex, shiftPreview, metrics.Scale);

      if (ImGui.IsItemHovered() && calcInputActive && powerOn)
      {
        anyKeyHovered = true;
        ImGui.SetTooltip($"{visual.Primary}  (code {key.KeyCode})");
      }
    }

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

  private readonly record struct PreviewVisual(
    string Face,
    string? GoldOnBody,
    string? BlueOnSkirt,
    uint? FaceInk,
    uint? SkirtInk,
    uint? GoldBodyInk);

  private static void DrawGoldBodyLabel(
    ImDrawListPtr draw,
    string text,
    System.Numerics.Vector2 cellMin,
    System.Numerics.Vector2 cellMax,
    CalcChassisMetrics metrics,
    uint? inkOverride = null)
  {
    float fontSize = CalcFaceplateTypography.GoldShift(metrics.Scale);
    float centerY = cellMin.Y - CalcEnterRowLabels.ShiftLabelGapAboveKey(metrics);
    float centerX = (cellMin.X + cellMax.X) * 0.5f;
    System.Numerics.Vector2 bandHalf = new((cellMax.X - cellMin.X) * 0.5f, fontSize * 0.58f);
    System.Numerics.Vector2 bandCenter = new(centerX, centerY);
    HpClassicFaceplateGlyphs.DrawBodyLabelInRect(
      draw,
      bandCenter - bandHalf,
      bandCenter + bandHalf,
      text,
      fontSize,
      inkOverride ?? CalcKeyLabelPalette.GoldOnBody,
      metrics.Scale);
  }
}
