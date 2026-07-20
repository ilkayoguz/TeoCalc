using ImGuiNET;
using System.Numerics;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcFaceplateView
{
  public static void Draw(CalcExplorerSession session, Vector2? availableOverride = null)
  {
    if (!session.SupportsFaceplate || session.Vocabulary is null)
    {
      LedDisplayOnlyView.Draw(session);
      return;
    }

    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, System.Numerics.Vector2.Zero);

    System.Numerics.Vector2 available = availableOverride ?? ImGui.GetContentRegionAvail();
    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(session.Model);
    CalcBodyLayout bodyLayout = CalcBodyLayoutCatalog.ResolveForFaceplate(
      faceplateModel,
      session.Model.Family,
      session.Model.Model);
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available, bodyLayout);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(session.Model.Family, session.Model.Model);

    Vector2 bodySize = new(metrics.Width, metrics.Height);
    if (CalcModernBody.IsActive)
    {
      Vector2 cursor = ImGui.GetCursorPos();
      ImGui.SetCursorPos(cursor + new Vector2(
        MathF.Max(0f, (available.X - bodySize.X) * 0.5f),
        MathF.Max(0f, (available.Y - bodySize.Y) * 0.5f)));
    }

    ImGui.Dummy(bodySize);
    System.Numerics.Vector2 origin = ImGui.GetItemRectMin();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
    {
      ImGui.SetWindowFocus();
    }

    bool calcHovered = ImGui.IsWindowHovered(
      ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.ChildWindows);
    bool calcFocused = ImGui.IsWindowFocused();
    bool calcInputActive = (calcFocused || calcHovered) && !ImGui.GetIO().WantTextInput;

    CalcFaceplateKeyboard.Update(session, session.Vocabulary, calcInputActive);
    int keyboardHeldKey = CalcFaceplateKeyboard.HeldKeyChartIndex;

    CalcChassisRenderer.DrawShell(draw, origin, metrics, faceplateModel);

    RectF display = ResolveDisplayRect(origin, metrics);
    HandleDisplayPowerDoubleClick(display, session);
    bool powerOn = session.PowerOn;

    CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, session);
    if (metrics.Layout.HasCardSlots && CalcModernBody.IsActive)
    {
      CalcChassisRenderer.DrawCardSlots(draw, origin, metrics, paintChrome: true);
    }

    CalcChassisRenderer.SwitchPointerState switchPointer =
      CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session, powerOn);
    bool anySwitchHovered = switchPointer.Hovered;
    bool switchClickHandled = switchPointer.ClickHandled;

    FirmwareDisplaySnapshot displaySnapshot = session.DisplaySnapshot;
    CalcChassisRenderer.DrawLedDisplay(
      draw,
      display,
      session.ProgramMode,
      metrics.Scale,
      displaySnapshot.Visible,
      displaySnapshot.Text);

    ShiftPreviewMode shiftPreview = session.ShiftPreview.Mode;
    if (string.Equals(session.Model.Family, "Classic", StringComparison.OrdinalIgnoreCase)
        && !CalcModernBody.IsActive)
    {
      CalcEnterRowLabels.Draw(draw, origin, metrics, shiftPreview);
    }

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
      session.ProgramMode,
      session);

    session.EndDisplayFrame();

    ImGui.PopStyleVar(2);
  }

  private static void HandleDisplayPowerDoubleClick(RectF display, CalcExplorerSession session)
  {
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool overDisplay = mouse.X >= display.Min.X && mouse.X <= display.Max.X
      && mouse.Y >= display.Min.Y && mouse.Y <= display.Max.Y;
    if (!overDisplay)
    {
      return;
    }

    if (!session.PowerOn)
    {
      CalcFaceplatePointer.RequestHandCursor();
    }

    if (!session.PowerOn && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
    {
      session.PowerOnResume();
    }
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
        // KeyCode 0 = blank chart slot, except HP-35 CLR (index 4) which is CapAbove-only faceplate.
        if (key.KeyCode == 0 && !IsHp35ClrFaceplateSlot(session.Model.Model, cell.KeyChartIndex))
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
          session.Model.Family,
          key,
          session.Vocabulary,
          cell.LabelStyle);
        if (IsHp65Or67EnterRowGold(session.Model)
            && CalcEnterRowLabels.GoldLabelForKey(cell.KeyChartIndex) is { } enterRowGold)
        {
          visual = visual with { GoldShift = enterRowGold, GoldInverseShift = enterRowGold };
        }

        CalcButtonStyle style = CalcFaceplateKeyStyles.StyleForKey(
          session.Model.Family,
          session.Model.Model,
          cell.KeyChartIndex);
        PreviewVisual preview = ApplyShiftPreview(visual, shiftPreview, style, session.Model.Model);
        CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell, session.Model.Family);
        CalcKeyVisual keyVisual = BuildKeyVisual(
          preview,
          style,
          kind,
          cell.KeyChartIndex,
          shiftPreview,
          faceplateModel);

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
              drawWell: !CalcModernBody.IsActive,
              forcePressed: keyboardPressed,
              interactive: calcInputActive && powerOn && !switchClickHandled))
        {
          session.PressKey(item.Cell.KeyChartIndex, (byte)item.Key.KeyCode);
        }

        DrawShiftPreviewIndicator(
          draw,
          item.KeyRect,
          item.Cell.KeyChartIndex,
          shiftPreview,
          session.Model.Family,
          session.Model.Model,
          metrics.Scale);

        if (ImGui.IsItemHovered() && calcInputActive && powerOn)
        {
          anyKeyHovered = true;
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
    string? family,
    string? modelId,
    float scale)
  {
    if (mode == ShiftPreviewMode.None
        || keyChartIndex != ShiftPreviewController.IndicatorKeyIndex(mode, family, modelId))
    {
      return;
    }

    uint color = mode switch
    {
      ShiftPreviewMode.Blue => CalcChassisPalette.BlueLabel,
      ShiftPreviewMode.Black => CalcChassisPalette.KeyCapDarkText,
      _ => CalcChassisPalette.GoldLabel,
    };
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

  private static uint WithAlpha(uint color, byte alpha) =>
    (color & 0x00FFFFFFu) | ((uint)alpha << 24);

  private static PreviewVisual ApplyShiftPreview(
    HpCalcKeyVisual visual,
    ShiftPreviewMode mode,
    CalcButtonStyle style,
    string? modelId)
  {
    return mode switch
    {
      ShiftPreviewMode.Blue when !string.IsNullOrEmpty(visual.BlueShift) => new(
        ComposeBluePreviewFace(visual),
        visual.GoldShift,
        visual.GoldShiftRight,
        visual.Primary,
        visual.BlackShift,
        CalcKeyLabelPalette.GShiftPreviewFaceInk(style, modelId),
        CalcKeyLabelPalette.SkirtOnCap(style),
        null),
      ShiftPreviewMode.Black when !string.IsNullOrEmpty(visual.BlackShift) => new(
        visual.BlackShift,
        visual.GoldShift,
        visual.GoldShiftRight,
        visual.BlueShift,
        visual.Primary,
        CalcKeyLabelPalette.PrimaryOnCap(style),
        CalcKeyLabelPalette.SkirtOnCap(style),
        null),
      // Dual CapAbove (GoldRight) collapses during gold preview — face shows left gold legend.
      ShiftPreviewMode.Gold when !string.IsNullOrEmpty(visual.GoldShift) => new(
        visual.GoldShift,
        visual.Primary,
        null,
        visual.BlueShift,
        visual.BlackShift,
        CalcKeyLabelPalette.GoldOnCap(style),
        null,
        CalcKeyLabelPalette.PrimaryOnCap(style)),
      ShiftPreviewMode.GoldInverse when !string.IsNullOrEmpty(visual.GoldInverseShift) => new(
        visual.GoldInverseShift,
        visual.Primary,
        null,
        visual.BlueShift,
        visual.BlackShift,
        CalcKeyLabelPalette.GoldOnCap(style),
        null,
        CalcKeyLabelPalette.PrimaryOnCap(style)),
      _ => new(
        visual.Primary,
        visual.GoldShift,
        visual.GoldShiftRight,
        visual.BlueShift,
        visual.BlackShift,
        null,
        null,
        null),
    };
  }

  private static string ComposeBluePreviewFace(HpCalcKeyVisual visual)
  {
    if (!string.IsNullOrEmpty(visual.GoldShift)
        && CalcCapAboveComposite.IsSpaceSavingInverse(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeInversePreviewFace(visual.GoldShift, visual.BlueShift!);
    }

    return visual.BlueShift!;
  }

  private static CalcKeyVisual BuildKeyVisual(
    PreviewVisual preview,
    CalcButtonStyle style,
    CalcButtonKind kind,
    int keyChartIndex,
    ShiftPreviewMode shiftPreview,
    CalcModelDefinition model)
  {
    List<CalcKeyAnnotation> annotations = [];
    bool classicEnterRow =
      string.Equals(model.DisplayName, "HP-65", StringComparison.OrdinalIgnoreCase)
      || string.Equals(model.DisplayName, "HP-67", StringComparison.OrdinalIgnoreCase)
      || model.Id is "65" or "67";
    bool gOnCapAbove = CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G) == CalcLabelAnchor.CapAbove;
    bool dualCapAbove = gOnCapAbove
      && !string.IsNullOrEmpty(preview.GoldOnBody)
      && !string.IsNullOrEmpty(preview.BlueOnSkirt);
    bool spaceSavingInverse = dualCapAbove
      && CalcCapAboveComposite.IsSpaceSavingInverse(preview.GoldOnBody, preview.BlueOnSkirt);
    bool splitDualCapAbove = dualCapAbove && !spaceSavingInverse;
    if (!string.IsNullOrEmpty(preview.GoldOnBody)
      && (!classicEnterRow
        || !CalcEnterRowLabels.IsEnterRowKey(keyChartIndex)
        || shiftPreview is ShiftPreviewMode.Gold or ShiftPreviewMode.GoldInverse))
    {
      CalcLabelAlign goldAlign = splitDualCapAbove || !string.IsNullOrEmpty(preview.GoldOnBodyRight)
        ? CalcLabelAlign.Left
        : CalcLabelAlign.Center;
      annotations.Add(CalcModifierPlacement.Annotate(
        model,
        CalcModifierKey.F,
        preview.GoldOnBody,
        align: goldAlign));
    }

    if (!string.IsNullOrEmpty(preview.GoldOnBodyRight)
      && (!classicEnterRow
        || !CalcEnterRowLabels.IsEnterRowKey(keyChartIndex)
        || shiftPreview is ShiftPreviewMode.Gold or ShiftPreviewMode.GoldInverse))
    {
      annotations.Add(CalcModifierPlacement.Annotate(
        model,
        CalcModifierKey.F,
        preview.GoldOnBodyRight,
        align: CalcLabelAlign.Right));
    }

    if (!string.IsNullOrEmpty(preview.BlueOnSkirt))
    {
      CalcLabelAlign blueAlign = splitDualCapAbove ? CalcLabelAlign.Right : CalcLabelAlign.Center;
      annotations.Add(CalcModifierPlacement.Annotate(
        model,
        CalcModifierKey.G,
        preview.BlueOnSkirt,
        align: blueAlign));
    }

    if (!string.IsNullOrEmpty(preview.BlackOnSkirt))
    {
      annotations.Add(CalcModifierPlacement.Annotate(model, CalcModifierKey.H, preview.BlackOnSkirt));
    }

    return new CalcKeyVisual
    {
      CapFace = preview.Face,
      CapStyle = style,
      Kind = kind,
      Annotations = annotations,
      CapFaceInkOverride = preview.FaceInk,
      CapSkirtInkOverride = preview.SkirtInk
        ?? (!string.IsNullOrEmpty(preview.BlackOnSkirt)
          ? CalcKeyLabelPalette.HShiftSkirtInk(style)
          : null),
      CapAboveInkOverride = preview.GoldBodyInk,
    };
  }

  private readonly record struct PreviewVisual(
    string Face,
    string? GoldOnBody,
    string? GoldOnBodyRight,
    string? BlueOnSkirt,
    string? BlackOnSkirt,
    uint? FaceInk,
    uint? SkirtInk,
    uint? GoldBodyInk);

  /// <summary>HP-35 CLR chart slot (KeyCode 0) still has CapAbove faceplate art.</summary>
  private static bool IsHp35ClrFaceplateSlot(string? modelId, int keyChartIndex) =>
    keyChartIndex == 4
    && (string.Equals(modelId, "HP-35", StringComparison.OrdinalIgnoreCase)
      || string.Equals(modelId, "35", StringComparison.OrdinalIgnoreCase));

  private static bool IsHp65Or67EnterRowGold(TeoCalcModelDefinition model) =>
    string.Equals(model.Model, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(model.Model, "HP-67", StringComparison.OrdinalIgnoreCase)
    || string.Equals(model.Model, "HP-67BE", StringComparison.OrdinalIgnoreCase)
    || string.Equals(model.DisplayName, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(model.DisplayName, "HP-67", StringComparison.OrdinalIgnoreCase);

  private static RectF ResolveDisplayRect(Vector2 origin, CalcChassisMetrics metrics)
  {
    if (!CalcModernBody.IsActive)
    {
      return metrics.DisplayRect(origin);
    }

    RectF glass = Calc00dBodyLayout.GlassFromBezel(metrics.Layout.DisplaySlot);
    float scale = metrics.Scale;
    return new RectF(
      origin.X + glass.X * scale,
      origin.Y + glass.Y * scale,
      glass.Width * scale,
      glass.Height * scale);
  }
}
