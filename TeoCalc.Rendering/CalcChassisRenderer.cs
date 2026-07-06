using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

public static class CalcChassisRenderer
{
  public static void DrawShell(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    if (Hp65FaceplateSvgAssets.UseBodyChrome && Hp65FaceplateSvgAssets.IsReady)
    {
      Hp65FaceplateSvgAssets.DrawBody(draw, origin, metrics);
      DrawCardSlotLabels(draw, origin, metrics);
      Hp65FaceplateSvgAssets.DrawLogo(draw, origin, metrics);
      return;
    }

    Vector2 size = new(metrics.Width, metrics.Height);
    Vector2 max = origin + size;
    float r = 10f * metrics.Scale;

    draw.AddRectFilled(origin, max, CalcChassisPalette.FrameEdge, r + metrics.Scale);
    Vector2 inner = origin + new Vector2(5f * metrics.Scale, 5f * metrics.Scale);
    Vector2 innerMax = max - new Vector2(5f * metrics.Scale, 5f * metrics.Scale);
    draw.AddRectFilled(inner, innerMax, CalcChassisPalette.Frame, r);

    Vector2 faceMin = inner + new Vector2(8f * metrics.Scale, 8f * metrics.Scale);
    Vector2 faceMax = innerMax - new Vector2(8f * metrics.Scale, metrics.FooterHeight + 6f * metrics.Scale);
    DrawFaceplateGrain(draw, faceMin, faceMax, metrics.Scale);

    RectF display = metrics.DisplayRect(origin);
    DrawDisplayBezel(draw, display, metrics.Scale);

    DrawSliderBand(draw, origin, faceMin, faceMax, metrics);

    RectF keypad = metrics.KeypadPanelRect(origin);
    DrawKeypadFace(draw, keypad, metrics);
    DrawCardSlots(draw, origin, metrics, paintChrome: true);

    float footerY = innerMax.Y - metrics.FooterHeight;
    draw.AddRectFilled(
      new Vector2(faceMin.X, footerY),
      new Vector2(faceMax.X, innerMax.Y),
      CalcChassisPalette.Footer,
      2f * metrics.Scale);
    DrawFooterText(draw, new Vector2(faceMin.X, footerY), faceMax.X - faceMin.X, metrics.FooterHeight, metrics.Scale);
    Hp65FaceplateSvgAssets.DrawLogo(draw, origin, metrics);
  }

  public static void DrawBrandPlateText(
    ImDrawListPtr draw,
    float textLeftArg,
    Vector2 plateMin,
    Vector2 plateMax,
    uint? color = null,
    float textRightMargin = 0f)
  {
    const string brandLine = "HEWLETT-PACKARD 65";
    float plateHeight = plateMax.Y - plateMin.Y;
    float plateWidth = plateMax.X - plateMin.X;
    float textLeft = textLeftArg;
    float textRight = plateMax.X - MathF.Max(textRightMargin, plateWidth * 0.04f);
    uint ink = color ?? CalcChassisPalette.FooterBrandText;

    float fontSize = plateHeight * 0.5f;
    Vector2 measure = MeasureBrandArial(brandLine, fontSize);
    float y = plateMin.Y + (plateHeight - measure.Y) * 0.5f;

    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.DrawArialBoldStretchedToWidth(draw, brandLine, textLeft, textRight, y, fontSize, ink);
      return;
    }

    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(textLeft, y), ink, brandLine);
  }

  private static Vector2 MeasureBrandArial(string text, float fontSize) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

  private static void DrawFooterText(ImDrawListPtr draw, Vector2 origin, float width, float height, float scale)
  {
    DrawBrandPlateText(
      draw,
      origin.X + width * 0.08f,
      origin,
      origin + new Vector2(width, height),
      CalcChassisPalette.FooterText);
  }

  public static void DrawPanamatikDisplay(
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

  public static void DrawDisplayDigits(
    ImDrawListPtr draw,
    RectF display,
    ClassicCpu cpu,
    bool programMode,
    float scale,
    bool displayLit,
    string? ledText = null)
  {
    DrawPanamatikDisplay(draw, display, programMode, scale, displayLit, ledText);
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

  public static void DrawCardSlots(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics, bool paintChrome)
  {
    RectF band = metrics.CardSlotBandRect(origin);
    float bandInsetY = metrics.Scale * 0.8f;
    float baseFont = (band.Height - bandInsetY * 2f) / 1.15f;
    float fontSize = baseFont * 2f * 0.7f;
    float slotHeight = band.Height * 0.38f;
    float slotY = band.Y + band.Height * 0.52f;

    float maxLabelHeight = 0f;
    for (int column = 0; column < CalcFaceplateLayout.Columns; column++)
    {
      maxLabelHeight = MathF.Max(
        maxLabelHeight,
        HpClassicFaceplateGlyphs.MeasureCardSlotLabel(column, fontSize).Height);
    }

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

      float slotInsetX = metrics.Scale * 1.5f;
      float slotLeft = cellX + slotInsetX;
      float slotRight = cellX + cellWidth - slotInsetX;
      float centerX = (slotLeft + slotRight) * 0.5f;
      float centerY = labelCenterY;
      Vector2 drawCenter = HpClassicFaceplateGlyphs.CardSlotLabelDrawCenter(
        column,
        new Vector2(centerX, centerY),
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

  public static void DrawSliderSwitches(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics, bool powerOn, bool programMode)
  {
    RectF panel = metrics.SwitchTrackRect(origin);
    float lift = BodyFaceplateLayout.SwitchRowLift * metrics.Scale;
    float rowY = panel.Y + panel.Height * 0.5f - lift;
    float onOffX = panel.X + panel.Width * 0.249f;
    float prgmX = panel.X + panel.Width * 0.751f;

    float labelY = origin.Y + BodyFaceplateLayout.SwitchLabelY * metrics.Scale;

    DrawSwitch(
      draw,
      new Vector2(onOffX, rowY),
      labelY,
      metrics.Scale,
      powerOn ? 1f : 0f,
      "OFF",
      "ON");

    DrawSwitch(
      draw,
      new Vector2(prgmX, rowY),
      labelY,
      metrics.Scale,
      programMode ? 0f : 1f,
      "W/PRGM",
      "RUN");
  }

  public readonly record struct SwitchPointerState(bool Hovered, bool ClickHandled);

  public static SwitchPointerState HandleSwitchPointers(
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcExplorerSession session,
    bool powerOn)
  {
    RectF panel = metrics.SwitchTrackRect(origin);
    float lift = BodyFaceplateLayout.SwitchRowLift * metrics.Scale;
    float rowY = panel.Y + panel.Height * 0.5f - lift;
    float onOffX = panel.X + panel.Width * 0.249f;
    float prgmX = panel.X + panel.Width * 0.751f;
    float scale = metrics.Scale;
    Vector2 mouse = ImGui.GetIO().MousePos;
    bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

    bool anyHovered = false;
    bool clickHandled = false;
    anyHovered |= PollSwitchPointer(
      mouse,
      clicked,
      panel,
      new Vector2(onOffX, rowY),
      scale,
      powerOn ? 1f : 0f,
      leftHalf =>
      {
        if (leftHalf)
        {
          session.PowerOff();
        }
        else
        {
          session.PowerOnResume();
        }
      },
      ref clickHandled);

    if (powerOn)
    {
      anyHovered |= PollSwitchPointer(
        mouse,
        clicked,
        panel,
        new Vector2(prgmX, rowY),
        scale,
        session.ProgramMode ? 0f : 1f,
        leftHalf => session.ToggleProgramModeTo(leftHalf),
        ref clickHandled);
    }

    return new SwitchPointerState(anyHovered, clickHandled);
  }

  public static bool IsMouseOverSwitch(Vector2 mouse, Vector2 origin, CalcChassisMetrics metrics, bool powerOn, bool programMode)
  {
    RectF panel = metrics.SwitchTrackRect(origin);
    float lift = BodyFaceplateLayout.SwitchRowLift * metrics.Scale;
    float rowY = panel.Y + panel.Height * 0.5f - lift;
    float onOffX = panel.X + panel.Width * 0.249f;
    float prgmX = panel.X + panel.Width * 0.751f;
    float scale = metrics.Scale;

    if (ContainsSwitchHit(mouse, panel, new Vector2(onOffX, rowY), scale, powerOn ? 1f : 0f))
    {
      return true;
    }

    return powerOn && ContainsSwitchHit(mouse, panel, new Vector2(prgmX, rowY), scale, programMode ? 0f : 1f);
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

  private static void DrawDisplayBezel(ImDrawListPtr draw, RectF display, float scale)
  {
    draw.AddRectFilled(display.Min, display.Max, CalcChassisPalette.DisplayBezel, 4f * scale);
    draw.AddRect(display.Min, display.Max, CalcChassisPalette.KeyWellEdge, 4f * scale, ImDrawFlags.None, scale);
  }

  private static void DrawSliderBand(ImDrawListPtr draw, Vector2 origin, Vector2 faceMin, Vector2 faceMax, CalcChassisMetrics metrics)
  {
    RectF track = BodyFaceplateLayout.SwitchTrack;
    float top = origin.Y + track.Y * metrics.Scale;
    float bottom = top + track.Height * metrics.Scale;
    draw.AddRectFilled(
      new Vector2(faceMin.X, top),
      new Vector2(faceMax.X, bottom),
      CalcChassisPalette.SliderTrack,
      3f * metrics.Scale);
  }

  private static void DrawSwitch(
    ImDrawListPtr draw,
    Vector2 knobCenter,
    float labelBaselineY,
    float scale,
    float position,
    string leftLabel,
    string rightLabel)
  {
    float trackWidth = 58f * scale;
    float trackHeight = 8f * scale;
    float knobWidth = 22f * scale;
    float knobHeight = 14f * scale;
    Vector2 trackMin = new(knobCenter.X - trackWidth * 0.5f, knobCenter.Y - trackHeight * 0.5f);
    Vector2 trackMax = new(knobCenter.X + trackWidth * 0.5f, knobCenter.Y + trackHeight * 0.5f);
    draw.AddRectFilled(trackMin, trackMax, CalcChassisPalette.SwitchTrack, trackHeight * 0.5f);

    float knobX = trackMin.X + position * (trackWidth - knobWidth);
    Vector2 knobMin = new(knobX, knobCenter.Y - knobHeight * 0.5f);
    Vector2 knobMax = new(knobX + knobWidth, knobCenter.Y + knobHeight * 0.5f);
    draw.AddRectFilled(knobMin, knobMax, CalcChassisPalette.SwitchKnob, 2f * scale);

    float labelSize = CalcFaceplateTypography.SwitchLabel(scale);
    float labelGap = scale * 10f;
    DrawSwitchLabel(draw, leftLabel, new Vector2(trackMin.X - labelGap, labelBaselineY), labelSize, rightAligned: true);
    DrawSwitchLabel(draw, rightLabel, new Vector2(trackMax.X + labelGap, labelBaselineY), labelSize, rightAligned: false);
  }

  private static void DrawSwitchLabel(ImDrawListPtr draw, string text, Vector2 anchor, float fontSize, bool rightAligned)
  {
    ImFontPtr font = CalcFaceplateFonts.IsReady ? CalcFaceplateFonts.SansBold : ImGui.GetFont();
    Vector2 size = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float x = rightAligned ? anchor.X - size.X : anchor.X;
    float y = anchor.Y - size.Y * 0.5f;
    draw.AddText(font, fontSize, new Vector2(x, y), CalcChassisPalette.SwitchLabel, text);
  }

  private static void DrawFaceplateGrain(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale)
  {
    draw.AddRectFilled(min, max, CalcChassisPalette.Faceplate, 6f * scale);
    int seed = (int)(min.X * 13 + min.Y * 29);
    int grains = (int)Math.Clamp(180 * scale, 120, 280);
    for (int grain = 0; grain < grains; grain++)
    {
      seed = seed * 1664525 + 1013904223;
      float u = (seed & 0xFFFF) / 65535f;
      seed = seed * 1664525 + 1013904223;
      float v = (seed & 0xFFFF) / 65535f;
      Vector2 point = new(
        min.X + 6f + u * MathF.Max(10f, max.X - min.X - 12f),
        min.Y + 6f + v * MathF.Max(10f, max.Y - min.Y - 12f));
      draw.AddCircleFilled(point, (0.45f + (grain % 3) * 0.18f) * scale, CalcChassisPalette.FaceplateGrain);
    }
  }

  private static void DrawKeypadFace(ImDrawListPtr draw, RectF keypad, CalcChassisMetrics metrics)
  {
    draw.AddRectFilled(keypad.Min, keypad.Max, CalcChassisPalette.Faceplate, 4f * metrics.Scale);

    if (BodyFaceplateLayout.TryGetKeyRect(15, out RectF enterKey))
    {
      RectF panel = BodyFaceplateLayout.KeypadPanel;
      float enterRuleY = keypad.Y + (enterKey.Y + enterKey.Height * 0.5f - panel.Y) * metrics.Scale;
      draw.AddLine(
        new Vector2(keypad.X + metrics.Scale * 4f, enterRuleY),
        new Vector2(keypad.Max.X - metrics.Scale * 4f, enterRuleY),
        CalcChassisPalette.GoldRule,
        metrics.Scale);
    }
  }
}
