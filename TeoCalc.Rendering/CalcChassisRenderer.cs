using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

public static class CalcChassisRenderer
{
  public static void DrawShell(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcModelDefinition model,
    bool skipText = false) =>
    CalcBodyComponent.DrawChrome(draw, origin, metrics, model, skipText: skipText);

  public static void DrawBrandPlateText(
    ImDrawListPtr draw,
    float textLeftArg,
    Vector2 plateMin,
    Vector2 plateMax,
    string brandLine,
    uint? color = null,
    float textRightMargin = 0f)
  {
    float plateHeight = plateMax.Y - plateMin.Y;
    float plateWidth = plateMax.X - plateMin.X;
    float zoneLeft = textLeftArg;
    float zoneRight = plateMax.X - MathF.Max(textRightMargin, plateWidth * 0.03f);
    uint ink = color ?? CalcChassisPalette.FooterText;

    float fontSize = plateHeight * 0.42f;
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.FontInkBounds inkBounds = CalcFaceplateFonts.MeasureArialBoldInk(brandLine, fontSize);
      float width = CalcFaceplateFonts.MeasureArialBold(brandLine, fontSize).X;
      float x = (zoneLeft + zoneRight - width) * 0.5f;
      float plateMidY = (plateMin.Y + plateMax.Y) * 0.5f;
      float y = plateMidY - inkBounds.InkMidY;
      CalcFaceplateFonts.DrawArialBoldTop(draw, brandLine, x, y, fontSize, ink);
      return;
    }

    Vector2 measure = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, brandLine);
    float fallbackX = (zoneLeft + zoneRight - measure.X) * 0.5f;
    float fallbackY = (plateMin.Y + plateMax.Y - measure.Y) * 0.5f;
    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(fallbackX, fallbackY), ink, brandLine);
  }

  private static Vector2 MeasureBrandArial(string text, float fontSize) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

  /// <summary>Classic LED / seven-segment display surface.</summary>
  public static void DrawLedDisplay(
    ImDrawListPtr draw,
    RectF display,
    bool programMode,
    float scale,
    bool displayLit,
    string? ledText = null)
  {
    ClassicLedDisplayRenderer.Draw(
      draw,
      display,
      new ClassicRegisterFile(),
      displayLit,
      programMode,
      programEndState: 0,
      scale,
      ledText);
  }

  [Obsolete("Use DrawLedDisplay.")]
  public static void DrawLegacyDisplay(
    ImDrawListPtr draw,
    RectF display,
    bool programMode,
    float scale,
    bool displayLit,
    string? ledText = null) =>
    DrawLedDisplay(draw, display, programMode, scale, displayLit, ledText);

  [Obsolete("Use DrawLedDisplay.")]
  public static void DrawPanamatikDisplay(
    ImDrawListPtr draw,
    RectF display,
    bool programMode,
    float scale,
    bool displayLit,
    string? ledText = null) =>
    DrawLedDisplay(draw, display, programMode, scale, displayLit, ledText);

  public static void DrawDisplayDigits(
    ImDrawListPtr draw,
    RectF display,
    ClassicCpu cpu,
    bool programMode,
    float scale,
    bool displayLit,
    string? ledText = null)
  {
    DrawLedDisplay(draw, display, programMode, scale, displayLit, ledText);
  }

  public static void DrawSegmentedDisplay(
    ImDrawListPtr draw,
    RectF display,
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode,
    byte programEndState,
    float scale) =>
    ClassicLedDisplayRenderer.Draw(draw, display, registers, displayOn, programMode, programEndState, scale);

  public static void DrawSegmentedDisplay(
    ImDrawListPtr draw,
    RectF display,
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode,
    byte programEndState,
    float scale,
    string? ledText) =>
    ClassicLedDisplayRenderer.Draw(draw, display, registers, displayOn, programMode, programEndState, scale, ledText);

  /// <summary>Card-slot function labels only — no chrome; Body.svg owns panel color.</summary>
  public static void DrawCardSlotLabels(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics) =>
    DrawCardSlots(draw, origin, metrics, paintChrome: false);

  public static void DrawCardSlots(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    bool paintChrome,
    IReadOnlyList<string>? labels = null,
    bool skipText = false)
  {
    if (!metrics.Layout.HasCardSlots)
    {
      return;
    }

    RectF band = metrics.CardSlotBandRect(origin);
    if (CalcModernBody.IsActive)
    {
      CalcCardSlotComponent.Draw(draw, band, metrics, origin, labels, skipText: skipText);
      return;
    }

    float bandInsetY = metrics.Scale * 0.8f;
    float baseFont = (band.Height - bandInsetY * 2f) / 1.15f;
    float fontSize = baseFont * 2f * 0.7f;
    float slotHeight = band.Height * 0.38f;
    float slotY = band.Y + band.Height * 0.52f;
    float labelCenterY = band.Y + band.Height * 0.20f;

    if (!paintChrome)
    {
      Vector2 clipMin = new(band.X, band.Y - band.Height * 0.35f);
      Vector2 clipMax = new(band.Max.X, band.Max.Y + band.Height * 0.15f);
      draw.PushClipRect(clipMin, clipMax, true);
    }

    for (int column = 0; column < CalcFaceplateLayout.Columns; column++)
    {
      if (!metrics.TryGetCardSlotColumn(origin, column, out RectF columnRect))
      {
        continue;
      }

      float cellX = columnRect.X;
      float cellWidth = columnRect.Width;

      if (paintChrome)
      {
        draw.AddRectFilled(
          new Vector2(cellX + metrics.Scale * 1.5f, slotY),
          new Vector2(cellX + cellWidth - metrics.Scale * 1.5f, slotY + slotHeight),
          CalcChassisPalette.CardSlot,
          metrics.Scale);
      }

      if (skipText)
      {
        continue;
      }

      float slotInsetX = metrics.Scale * 1.5f;
      float slotLeft = cellX + slotInsetX;
      float slotRight = cellX + cellWidth - slotInsetX;
      float centerX = (slotLeft + slotRight) * 0.5f;
      Vector2 drawCenter = HpClassicFaceplateGlyphs.CardSlotLabelDrawCenter(
        column,
        new Vector2(centerX, labelCenterY),
        fontSize,
        metrics.Scale);
      HpClassicFaceplateGlyphs.DrawCardSlotLabel(
        draw,
        column,
        drawCenter,
        fontSize,
        CalcChassisPalette.CardSlotLabel,
        metrics.Scale);
    }

    if (!paintChrome)
    {
      draw.PopClipRect();
    }
  }

  public static void DrawSliderSwitches(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcExplorerSession session,
    bool skipText = false)
  {
    float scale = metrics.Scale;
    RectF slot = metrics.SwitchTrackRect(origin);
    IReadOnlyList<CalcSwitchSpec> specs = metrics.Layout.Switches;
    session.EnsureFaceplateSwitches(specs);
    CalcSwitchPanelComponent.Draw(
      draw,
      slot,
      scale,
      specs,
      (index, spec) => session.GetFaceplateSwitchNorm(index, spec),
      modernChrome: CalcModernBody.IsActive,
      skipText: skipText);
  }

  public readonly record struct SwitchPointerState(bool Hovered, bool ClickHandled);

  public static SwitchPointerState HandleSwitchPointers(
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcExplorerSession session,
    bool powerOn)
  {
    RectF slot = metrics.SwitchTrackRect(origin);
    float scale = metrics.Scale;
    IReadOnlyList<CalcSwitchSpec> specs = metrics.Layout.Switches;
    session.EnsureFaceplateSwitches(specs);
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

    bool anyHovered = false;
    bool clickHandled = false;

    for (int i = 0; i < specs.Count; i++)
    {
      CalcSwitchSpec spec = specs[i];
      bool interactive = spec.IsPower || powerOn;
      if (!interactive)
      {
        continue;
      }

      float norm = session.GetFaceplateSwitchNorm(i, spec);
      CalcSwitchComponent.HitLayout hit = CalcSwitchPanelComponent.BuildHitLayout(slot, scale, specs, i, norm);
      SwitchLabelSlot part = CalcSwitchComponent.HitTest(mouse, hit);
      if (part == SwitchLabelSlot.None)
      {
        continue;
      }

      anyHovered = true;
      if (!clicked || clickHandled)
      {
        continue;
      }

      if (part == SwitchLabelSlot.Knob)
      {
        session.AdvanceFaceplateSwitch(i, spec);
      }
      else if (part == SwitchLabelSlot.Track)
      {
        session.SetFaceplateSwitchIndex(i, spec, CalcSwitchComponent.IndexForTrackClick(spec, hit, mouse));
      }
      else
      {
        session.SetFaceplateSwitchIndex(i, spec, spec.IndexForLabel(part));
      }

      clickHandled = true;
    }

    return new SwitchPointerState(anyHovered, clickHandled);
  }

  public static bool IsMouseOverSwitch(
    Vector2 mouse,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcExplorerSession session,
    bool powerOn)
  {
    RectF slot = metrics.SwitchTrackRect(origin);
    float scale = metrics.Scale;
    IReadOnlyList<CalcSwitchSpec> specs = metrics.Layout.Switches;
    session.EnsureFaceplateSwitches(specs);

    for (int i = 0; i < specs.Count; i++)
    {
      CalcSwitchSpec spec = specs[i];
      if (!spec.IsPower && !powerOn)
      {
        continue;
      }

      float norm = session.GetFaceplateSwitchNorm(i, spec);
      CalcSwitchComponent.HitLayout hit = CalcSwitchPanelComponent.BuildHitLayout(slot, scale, specs, i, norm);
      if (CalcSwitchComponent.HitTest(mouse, hit) != SwitchLabelSlot.None)
      {
        return true;
      }
    }

    return false;
  }

  private static (float RowY, float OnOffX, float PrgmX, RectF Panel) ResolveSwitchGeometry(
    Vector2 origin,
    CalcChassisMetrics metrics)
  {
    RectF panel = metrics.SwitchTrackRect(origin);
    if (CalcModernBody.IsActive)
    {
      return (
        origin.Y + metrics.Layout.OnOffSwitchCenter.Y * metrics.Scale,
        origin.X + metrics.Layout.OnOffSwitchCenter.X * metrics.Scale,
        origin.X + metrics.Layout.PrgmRunSwitchCenter.X * metrics.Scale,
        panel);
    }

    float lift = metrics.Layout.SwitchRowLift * metrics.Scale;
    float rowY = panel.Y + panel.Height * 0.5f - lift;
    return (
      rowY,
      panel.X + panel.Width * 0.249f,
      panel.X + panel.Width * 0.751f,
      panel);
  }

  private static (Vector2 Min, Vector2 Max) SwitchTrackBounds(Vector2 knobCenter, float scale)
  {
    float trackWidth = 58f * scale;
    float trackHeight = 8f * scale;
    float left = knobCenter.X - trackWidth * 0.5f;
    float top = knobCenter.Y - trackHeight * 0.5f - scale * 3f;
    float bottom = knobCenter.Y + trackHeight * 0.5f + scale * 3f;
    return (new Vector2(left, top), new Vector2(left + trackWidth, bottom));
  }

  private static (Vector2 Min, Vector2 Max) SwitchKnobSlideBounds(Vector2 knobCenter, float scale, float position)
  {
    float trackWidth = 58f * scale;
    float knobWidth = 22f * scale;
    float knobHeight = 14f * scale;
    float trackLeft = knobCenter.X - trackWidth * 0.5f;
    float knobLeft = trackLeft + position * (trackWidth - knobWidth);
    float top = knobCenter.Y - knobHeight * 0.5f - scale * 4f;
    float bottom = knobCenter.Y + knobHeight * 0.5f + scale * 4f;
    return (new Vector2(knobLeft - scale * 3f, top), new Vector2(knobLeft + knobWidth + scale * 3f, bottom));
  }

  private static (Vector2 Min, Vector2 Max) SwitchHoleHitBounds(RectF switchPanel, Vector2 knobCenter, float scale)
  {
    float halfW = switchPanel.Width * 0.24f;
    return (
      new Vector2(knobCenter.X - halfW, switchPanel.Y),
      new Vector2(knobCenter.X + halfW, switchPanel.Max.Y));
  }

  private static bool ContainsSwitchHit(Vector2 mouse, RectF switchPanel, Vector2 knobCenter, float scale, float knobPosition)
  {
    (Vector2 knobMin, Vector2 knobMax) = SwitchKnobSlideBounds(knobCenter, scale, knobPosition);
    (Vector2 trackMin, Vector2 trackMax) = SwitchTrackBounds(knobCenter, scale);
    (Vector2 holeMin, Vector2 holeMax) = SwitchHoleHitBounds(switchPanel, knobCenter, scale);
    return PointInRect(mouse, knobMin, knobMax)
      || PointInRect(mouse, trackMin, trackMax)
      || PointInRect(mouse, holeMin, holeMax);
  }

  private static bool PollSwitchPointer(
    Vector2 mouse,
    bool clicked,
    RectF switchPanel,
    Vector2 knobCenter,
    float scale,
    float knobPosition,
    Action<bool> onClickHalf,
    ref bool clickHandled)
  {
    if (!ContainsSwitchHit(mouse, switchPanel, knobCenter, scale, knobPosition))
    {
      return false;
    }

    if (clicked)
    {
      (Vector2 knobMin, Vector2 knobMax) = SwitchKnobSlideBounds(knobCenter, scale, knobPosition);
      bool onKnob = PointInRect(mouse, knobMin, knobMax);
      // Clicking the sliding knob should flip state; only the empty track half uses L/R split.
      bool leftHalf = onKnob ? knobPosition >= 0.5f : mouse.X < knobCenter.X;
      onClickHalf(leftHalf);
      clickHandled = true;
    }

    return true;
  }

  private static bool PointInRect(Vector2 point, Vector2 min, Vector2 max) =>
    point.X >= min.X && point.X <= max.X && point.Y >= min.Y && point.Y <= max.Y;

  private static void DrawSwitch(
    ImDrawListPtr draw,
    Vector2 knobCenter,
    float labelBaselineY,
    float scale,
    float position,
    string leftLabel,
    string rightLabel,
    bool paintTrack = true,
    bool paintLabels = true,
    bool paintKnob = true)
  {
    bool modern = CalcModernBody.IsActive;
    float trackWidth = (modern ? 76f : 58f) * scale;
    float trackHeight = (modern ? 10f : 8f) * scale;
    float knobWidth = (modern ? 26f : 22f) * scale;
    float knobHeight = (modern ? 16f : 14f) * scale;
    Vector2 trackMin = new(knobCenter.X - trackWidth * 0.5f, knobCenter.Y - trackHeight * 0.5f);
    Vector2 trackMax = new(knobCenter.X + trackWidth * 0.5f, knobCenter.Y + trackHeight * 0.5f);
    if (paintTrack)
    {
      uint track = modern ? CalcChassisPalette.SwitchTrack : CalcChassisPalette.KeyWell;
      uint edge = CalcChassisPalette.KeyWellEdge;
      draw.AddRectFilled(trackMin, trackMax, track, trackHeight * 0.5f);
      draw.AddRect(trackMin, trackMax, edge, trackHeight * 0.5f, ImDrawFlags.None, scale * 0.55f);
      if (modern)
      {
        Vector2 inset = new(scale * 0.5f, scale * 0.35f);
        draw.AddRect(trackMin + inset, trackMax - inset, 0x33000000, trackHeight * 0.5f, ImDrawFlags.None, scale * 0.35f);
      }
    }

    if (paintKnob)
    {
      float knobX = trackMin.X + position * (trackWidth - knobWidth);
      Vector2 knobMin = new(knobX, knobCenter.Y - knobHeight * 0.5f);
      Vector2 knobMax = new(knobX + knobWidth, knobCenter.Y + knobHeight * 0.5f);
      draw.AddRectFilled(knobMin, knobMax, CalcChassisPalette.SwitchKnob, modern ? 2.5f * scale : 2f * scale);
      if (modern)
      {
        float ridgeX = knobMin.X + scale * 2f;
        while (ridgeX < knobMax.X - scale * 2f)
        {
          draw.AddLine(
            new Vector2(ridgeX, knobMin.Y + scale),
            new Vector2(ridgeX, knobMax.Y - scale),
            0x44FFFFFF,
            scale * 0.35f);
          ridgeX += scale * 2.2f;
        }
      }
      else
      {
        draw.AddRect(knobMin, knobMax, 0x44FFFFFF, 2f * scale, ImDrawFlags.None, scale * 0.5f);
      }
    }

    if (!paintLabels)
    {
      return;
    }

    float labelSize = CalcFaceplateTypography.SwitchLabel(scale);
    float labelGap = (modern ? 14f : 10f) * scale;
    DrawSwitchLabel(draw, leftLabel, new Vector2(trackMin.X - labelGap, labelBaselineY), labelSize, rightAligned: true);
    DrawSwitchLabel(draw, rightLabel, new Vector2(trackMax.X + labelGap, labelBaselineY), labelSize, rightAligned: false);
  }

  private static void DrawSwitchLabel(ImDrawListPtr draw, string text, Vector2 anchor, float fontSize, bool rightAligned)
  {
    if (string.IsNullOrEmpty(text))
    {
      return;
    }

    ImFontPtr font = CalcFaceplateFonts.IsReady ? CalcFaceplateFonts.SansBold : ImGui.GetFont();
    Vector2 size = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float x = rightAligned ? anchor.X - size.X : anchor.X;
    float y = anchor.Y - size.Y * 0.5f;
    draw.AddText(font, fontSize, new Vector2(x, y), CalcChassisPalette.SwitchLabel, text);
  }
}
