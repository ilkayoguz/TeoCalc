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

    DrawFaceplateCore(
      draw,
      origin,
      metrics,
      cells,
      session.Model,
      faceplateModel,
      session.Vocabulary,
      session,
      interactive: true,
      calcInputActive,
      keyboardHeldKey);

    session.EndDisplayFrame();

    ImGui.PopStyleVar(2);
  }

  /// <summary>
  /// Non-interactive faceplate draw for launcher thumbnails — same chrome/keys path as live windows.
  /// Defaults to <paramref name="skipText"/> = true so CapFace / legends / LED / switch / logo text are omitted.
  /// </summary>
  public static void DrawStaticPreview(
    ImDrawListPtr draw,
    Vector2 origin,
    Vector2 available,
    TeoCalcModelDefinition model,
    CalcModelDefinition faceplateModel,
    ProgramVocabulary? vocabulary,
    CalcBodyLayout bodyLayout,
    bool skipText = true)
  {
    CalcChassisMetrics metrics = CalcChassisGeometry.Fit(available, bodyLayout);
    IReadOnlyList<FaceplateCell> cells = CalcFaceplateLayout.GetPhysicalCells(model.Family, model.Model);
    DrawFaceplateCore(
      draw,
      origin,
      metrics,
      cells,
      model,
      faceplateModel,
      vocabulary,
      session: null,
      interactive: false,
      calcInputActive: false,
      keyboardHeldKey: -1,
      skipText: skipText);
  }

  private static void DrawFaceplateCore(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    IReadOnlyList<FaceplateCell> cells,
    TeoCalcModelDefinition model,
    CalcModelDefinition faceplateModel,
    ProgramVocabulary? vocabulary,
    CalcExplorerSession? session,
    bool interactive,
    bool calcInputActive,
    int keyboardHeldKey,
    bool skipText = false)
  {
    CalcChassisRenderer.DrawShell(draw, origin, metrics, faceplateModel, skipText: skipText);

    RectF display = ResolveDisplayRect(origin, metrics);
    if (interactive && session is not null)
    {
      HandleDisplayPowerDoubleClick(display, session);
    }

    bool powerOn = session?.PowerOn ?? true;
    if (session is not null)
    {
      CalcChassisRenderer.DrawSliderSwitches(draw, origin, metrics, session, skipText: skipText);
    }
    else
    {
      DrawStaticSwitches(draw, origin, metrics, skipText);
    }

    if (metrics.Layout.HasCardSlots && CalcModernBody.IsActive)
    {
      CalcChassisRenderer.DrawCardSlots(draw, origin, metrics, paintChrome: true, skipText: skipText);
    }

    CalcChassisRenderer.SwitchPointerState switchPointer = interactive && session is not null
      ? CalcChassisRenderer.HandleSwitchPointers(origin, metrics, session, powerOn)
      : default;
    bool anySwitchHovered = switchPointer.Hovered;
    bool switchClickHandled = switchPointer.ClickHandled;

    if (!skipText)
    {
      FirmwareDisplaySnapshot displaySnapshot = session?.DisplaySnapshot
        ?? new FirmwareDisplaySnapshot("0.00", Visible: true, BlankPulse: false, Revision: 0, StepCount: 0, ProgramCounter: 0);
      CalcChassisRenderer.DrawLedDisplay(
        draw,
        display,
        session?.ProgramMode ?? false,
        metrics.Scale,
        displaySnapshot.Visible,
        displaySnapshot.Text);
    }

    if (vocabulary is null)
    {
      return;
    }

    ShiftPreviewMode shiftPreview = skipText
      ? ShiftPreviewMode.None
      : session?.ShiftPreview.Mode ?? ShiftPreviewMode.None;
    bool anyKeyHovered = DrawKeypadRows(
      draw,
      origin,
      metrics,
      cells,
      model,
      faceplateModel,
      vocabulary,
      session,
      shiftPreview,
      calcInputActive,
      powerOn,
      switchClickHandled,
      keyboardHeldKey,
      interactive,
      skipText);

    if (interactive && session is not null)
    {
      if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
      {
        session.ReleaseMouseKey();
      }

      CalcFaceplatePointer.ApplyHandCursorIfHovering(
        origin,
        metrics,
        cells,
        vocabulary.KeyChart,
        anyKeyHovered,
        anySwitchHovered,
        powerOn,
        session.ProgramMode,
        session);
    }
  }

  private static void DrawStaticSwitches(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    bool skipText)
  {
    float scale = metrics.Scale;
    RectF slot = metrics.SwitchTrackRect(origin);
    IReadOnlyList<CalcSwitchSpec> specs = metrics.Layout.Switches;
    CalcSwitchPanelComponent.Draw(
      draw,
      slot,
      scale,
      specs,
      static (_, _) => 1f,
      modernChrome: CalcModernBody.IsActive,
      skipText: skipText);
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
    TeoCalcModelDefinition model,
    CalcModelDefinition faceplateModel,
    ProgramVocabulary vocabulary,
    CalcExplorerSession? session,
    ShiftPreviewMode shiftPreview,
    bool calcInputActive,
    bool powerOn,
    bool switchClickHandled,
    int keyboardHeldKey,
    bool interactive,
    bool skipText)
  {
    bool anyKeyHovered = false;

    foreach (IGrouping<int, FaceplateCell> row in cells.GroupBy(cell => cell.Row).OrderBy(group => group.Key))
    {
      List<KeypadDrawItem> rowItems = [];
      foreach (FaceplateCell cell in row)
      {
        if (cell.KeyChartIndex >= vocabulary.KeyChart.Count)
        {
          continue;
        }

        ProgramKeyEntry key = vocabulary.KeyChart[cell.KeyChartIndex];
        // KeyCode 0 = blank chart slot, except Classic faceplate keys that share scancode 0
        // (HP-35 CLR, HP-45 gold, HP-55 BST).
        if (key.KeyCode == 0 && !IsClassicKeyCodeZeroFaceplateSlot(model.Model, cell.KeyChartIndex))
        {
          continue;
        }

        RectF keyRect = metrics.KeyRect(origin, cell.KeyChartIndex);
        if (keyRect.Width <= 0f || keyRect.Height <= 0f)
        {
          continue;
        }

        HpCalcKeyVisual visual = ClassicKeyFaceplateLegend.Resolve(
          model.Model,
          model.Family,
          key,
          vocabulary,
          cell.LabelStyle);
        // HP-65 enter-row CapAbove (PREFIX/STK/REG/PRGM) comes from faceplate JSON Gold.
        // Fallback if JSON omits Gold so Retro/Modern both keep authentic legends.
        if (IsHp65EnterRowGold(model)
            && string.IsNullOrEmpty(visual.GoldShift)
            && CalcEnterRowLabels.GoldLabelForKey(cell.KeyChartIndex) is { } enterRowGold)
        {
          visual = visual with { GoldShift = enterRowGold };
        }

        CalcButtonStyle style = CalcFaceplateKeyStyles.StyleForKey(
          model.Family,
          model.Model,
          cell.KeyChartIndex);
        PreviewVisual preview = ApplyShiftPreview(visual, shiftPreview, style, model.Model);
        CalcButtonKind kind = CalcFaceplateLayout.ButtonKindForKey(key, cell, model.Family);
        CalcKeyVisual keyVisual = BuildKeyVisual(
          preview,
          style,
          kind,
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

      CalcKeyRowLayout.ApplyRowBands(
        visuals,
        slotMins,
        slotMaxs,
        capMins,
        capMaxs,
        metrics.Scale,
        faceplateModel);

      (int KeyChartIndex, RectF KeyRect)[] rowSpanKeys = new (int, RectF)[count];
      for (int i = 0; i < count; i++)
      {
        rowSpanKeys[i] = (rowItems[i].Cell.KeyChartIndex, rowItems[i].KeyRect);
      }

      if (!skipText)
      {
        CalcBracketLegendComponent.TryDrawForRow(
          draw,
          model.Model,
          rowSpanKeys,
          metrics.Scale);
      }

      for (int i = 0; i < count; i++)
      {
        KeypadDrawItem item = rowItems[i];
        bool leftAlign = item.Kind != CalcButtonKind.EnterWide && item.Cell.ColSpan >= 2;
        bool keyboardPressed = interactive && calcInputActive && powerOn && !switchClickHandled
          && item.Cell.KeyChartIndex == keyboardHeldKey;
        if (CalcKeyComponent.DrawAtCapBounds(
              draw,
              interactive ? $"##hpkey{item.Cell.KeyChartIndex}" : $"##thumbkey{item.Cell.KeyChartIndex}",
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
              interactive: interactive && calcInputActive && powerOn && !switchClickHandled,
              skipText: skipText))
        {
          session?.PressKey(item.Cell.KeyChartIndex, (byte)item.Key.KeyCode);
        }

        if (interactive && !skipText)
        {
          DrawShiftPreviewIndicator(
            draw,
            item.KeyRect,
            item.Cell.KeyChartIndex,
            shiftPreview,
            model.Family,
            model.Model,
            metrics.Scale);
        }

        if (interactive && ImGui.IsItemHovered() && calcInputActive && powerOn)
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
        ComposeGoldPreviewFace(visual),
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

  private static string ComposeGoldPreviewFace(HpCalcKeyVisual visual)
  {
    if (CalcCapAboveComposite.IsSpaceSavingHmsPlusMinus(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeHmsPlusMinusPreviewFace(
        visual.GoldShift, visual.BlueShift, blueShift: false);
    }

    if (CalcCapAboveComposite.IsSpaceSavingUnitConversion(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeUnitConversionPreviewFace(
        visual.GoldShift, visual.BlueShift, blueShift: false);
    }

    return visual.GoldShift!;
  }

  private static string ComposeBluePreviewFace(HpCalcKeyVisual visual)
  {
    if (!string.IsNullOrEmpty(visual.GoldShift)
        && CalcCapAboveComposite.IsSpaceSavingInverse(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeInversePreviewFace(visual.GoldShift, visual.BlueShift!);
    }

    if (CalcCapAboveComposite.IsSpaceSavingHmsPlusMinus(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeHmsPlusMinusPreviewFace(
        visual.GoldShift, visual.BlueShift, blueShift: true);
    }

    if (CalcCapAboveComposite.IsSpaceSavingUnitConversion(visual.GoldShift, visual.BlueShift))
    {
      return CalcCapAboveComposite.ComposeUnitConversionPreviewFace(
        visual.GoldShift, visual.BlueShift, blueShift: true);
    }

    return visual.BlueShift!;
  }

  private static CalcKeyVisual BuildKeyVisual(
    PreviewVisual preview,
    CalcButtonStyle style,
    CalcButtonKind kind,
    CalcModelDefinition model)
  {
    List<CalcKeyAnnotation> annotations = [];
    bool gOnCapAbove = CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G) == CalcLabelAnchor.CapAbove;
    bool fOnCapBelow = CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.F) == CalcLabelAnchor.CapBelow;
    bool gOnCapBelow = CalcModifierPlacement.PrimaryAnchor(model, CalcModifierKey.G) == CalcLabelAnchor.CapBelow;
    bool dualCapAbove = gOnCapAbove
      && !string.IsNullOrEmpty(preview.GoldOnBody)
      && !string.IsNullOrEmpty(preview.BlueOnSkirt);
    bool dualCapBelow = fOnCapBelow
      && gOnCapBelow
      && !string.IsNullOrEmpty(preview.GoldOnBody)
      && !string.IsNullOrEmpty(preview.BlueOnSkirt);
    bool spaceSavingCapAbove = dualCapAbove
      && CalcCapAboveComposite.IsSpaceSavingDualInk(preview.GoldOnBody, preview.BlueOnSkirt);
    bool spaceSavingCapBelow = dualCapBelow
      && CalcCapAboveComposite.IsSpaceSavingDualInk(preview.GoldOnBody, preview.BlueOnSkirt);
    // CapAbove/CapBelow space-saving composites stay centered; other duals split L/R.
    bool splitDualBand = (dualCapAbove && !spaceSavingCapAbove)
      || (dualCapBelow && !spaceSavingCapBelow);
    if (!string.IsNullOrEmpty(preview.GoldOnBody))
    {
      CalcLabelAlign goldAlign = splitDualBand || !string.IsNullOrEmpty(preview.GoldOnBodyRight)
        ? CalcLabelAlign.Left
        : CalcLabelAlign.Center;
      annotations.Add(CalcModifierPlacement.Annotate(
        model,
        CalcModifierKey.F,
        preview.GoldOnBody,
        align: goldAlign));
    }

    if (!string.IsNullOrEmpty(preview.GoldOnBodyRight))
    {
      annotations.Add(CalcModifierPlacement.Annotate(
        model,
        CalcModifierKey.F,
        preview.GoldOnBodyRight,
        align: CalcLabelAlign.Right));
    }

    if (!string.IsNullOrEmpty(preview.BlueOnSkirt))
    {
      CalcLabelAlign blueAlign = splitDualBand ? CalcLabelAlign.Right : CalcLabelAlign.Center;
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
          ? CalcKeyLabelPalette.HShiftSkirtInk(style, model.Id)
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

  /// <summary>
  /// Classic KeyCode 0 chart slots that still have physical faceplate keys:
  /// HP-35 CLR, HP-45 gold prefix, HP-55 BST, HP-70 FV (Finseth / scancode 0).
  /// </summary>
  private static bool IsClassicKeyCodeZeroFaceplateSlot(string? modelId, int keyChartIndex) =>
    keyChartIndex == 4
    && modelId is not null
    && modelId.ToUpperInvariant() is "HP-35" or "35" or "HP-45" or "45" or "HP-55" or "55"
      or "HP-70" or "70";

  private static bool IsHp65EnterRowGold(TeoCalcModelDefinition model) =>
    string.Equals(model.Model, "HP-65", StringComparison.OrdinalIgnoreCase)
    || string.Equals(model.DisplayName, "HP-65", StringComparison.OrdinalIgnoreCase);

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
