using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;



namespace TeoCalc.Rendering;



public static class CalcChassisRenderer

{

  public static void DrawShell(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
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



    RectF keypad = metrics.KeypadRect(origin);

    DrawKeypadFace(draw, keypad, metrics);

    DrawCardSlots(draw, keypad, metrics);



    float footerY = innerMax.Y - metrics.FooterHeight;

    draw.AddRectFilled(

      new Vector2(faceMin.X, footerY),

      new Vector2(faceMax.X, innerMax.Y),

      CalcChassisPalette.Footer,

      2f * metrics.Scale);

    DrawFooterText(draw, new Vector2(faceMin.X, footerY), faceMax.X - faceMin.X, metrics.FooterHeight, metrics.Scale);

  }



  public static void DrawDisplayDigits(

    ImDrawListPtr draw,

    RectF display,

    ClassicCpu cpu,

    bool programMode,

    float scale)

  {

    DrawSegmentedDisplay(draw, display, cpu.State.Registers, (cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0, programMode, cpu.Program.EndState, scale);

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



  public static void DrawCardSlots(ImDrawListPtr draw, RectF keypad, CalcChassisMetrics metrics)

  {

    float slotHeight = metrics.CardSlotBand * 0.42f;

    float slotY = keypad.Y + metrics.CardSlotBand * 0.34f;

    float labelY = keypad.Y + metrics.CardSlotBand * 0.08f;

    float fontSize = ImGui.GetFontSize() * 0.52f * metrics.Scale;



    for (int column = 0; column < CalcFaceplateLayout.Columns; column++)

    {

      float cellX = keypad.X + metrics.KeypadInset + column * (metrics.KeyWidth + metrics.KeyGapH);

      draw.AddRectFilled(

        new Vector2(cellX + metrics.Scale * 1.5f, slotY),

        new Vector2(cellX + metrics.KeyWidth - metrics.Scale * 1.5f, slotY + slotHeight),

        CalcChassisPalette.CardSlot,

        metrics.Scale);



      string label = CalcFaceplateLayout.CardSlotLabels[column];

      Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, label);

      draw.AddText(

        ImGui.GetFont(),

        fontSize,

        new Vector2(cellX + (metrics.KeyWidth - size.X) * 0.5f, labelY),

        CalcChassisPalette.GoldLabel,

        label);

    }

  }



  public static void DrawSliderSwitches(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics, bool powerOn, bool programMode)

  {

    DrawSwitch(

      draw,

      NormPoint(origin, CalcFaceplateLayout.OnOffSwitchNorm, metrics.Scale),

      metrics.Scale,

      powerOn ? 1f : 0f,

      "OFF",

      "ON");

    DrawSwitch(

      draw,

      NormPoint(origin, CalcFaceplateLayout.PrgmRunSwitchNorm, metrics.Scale),

      metrics.Scale,

      programMode ? 0f : 1f,

      "W/PRGM",

      "RUN");

  }



  private static void DrawDisplayBezel(ImDrawListPtr draw, RectF display, float scale)

  {

    draw.AddRectFilled(display.Min, display.Max, CalcChassisPalette.DisplayBezel, 4f * scale);

    draw.AddRect(display.Min, display.Max, CalcChassisPalette.KeyWellEdge, 4f * scale, ImDrawFlags.None, scale);

  }



  private static void DrawSliderBand(ImDrawListPtr draw, Vector2 origin, Vector2 faceMin, Vector2 faceMax, CalcChassisMetrics metrics)

  {

    float sliderTop = origin.Y + CalcChassisGeometry.SliderBandTopPx * metrics.Scale;

    float sliderBottom = origin.Y + CalcChassisGeometry.SliderBandBottomPx * metrics.Scale;

    draw.AddRectFilled(

      new Vector2(faceMin.X, sliderTop),

      new Vector2(faceMax.X, sliderBottom),

      CalcChassisPalette.SliderTrack,

      3f * metrics.Scale);

  }



  private static void DrawSwitch(

    ImDrawListPtr draw,

    Vector2 center,

    float scale,

    float position,

    string leftLabel,

    string rightLabel)

  {

    float trackWidth = 58f * scale;

    float trackHeight = 8f * scale;

    float knobWidth = 22f * scale;

    float knobHeight = 14f * scale;

    Vector2 trackMin = new(center.X - trackWidth * 0.5f, center.Y - trackHeight * 0.5f);

    Vector2 trackMax = new(center.X + trackWidth * 0.5f, center.Y + trackHeight * 0.5f);

    draw.AddRectFilled(trackMin, trackMax, CalcChassisPalette.SwitchTrack, trackHeight * 0.5f);



    float knobX = trackMin.X + position * (trackWidth - knobWidth);

    Vector2 knobMin = new(knobX, center.Y - knobHeight * 0.5f);

    Vector2 knobMax = new(knobX + knobWidth, center.Y + knobHeight * 0.5f);

    draw.AddRectFilled(knobMin, knobMax, CalcChassisPalette.SwitchKnob, 2f * scale);



    float labelSize = ImGui.GetFontSize() * 0.48f * scale;

    DrawSwitchLabel(draw, leftLabel, new Vector2(trackMin.X - scale * 4f, center.Y), labelSize, rightAligned: true);

    DrawSwitchLabel(draw, rightLabel, new Vector2(trackMax.X + scale * 4f, center.Y), labelSize, rightAligned: false);

  }



  private static void DrawSwitchLabel(ImDrawListPtr draw, string text, Vector2 anchor, float fontSize, bool rightAligned)

  {

    Vector2 size = ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

    float x = rightAligned ? anchor.X - size.X : anchor.X;

    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, anchor.Y - size.Y * 0.5f), CalcChassisPalette.SwitchLabel, text);

  }



  private static Vector2 NormPoint(Vector2 origin, Vector2 norm, float scale) =>

    new(

      origin.X + norm.X * CalcChassisGeometry.ReferenceWidth * scale,

      origin.Y + norm.Y * CalcChassisGeometry.ReferenceHeight * scale);



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



    float enterRuleY = keypad.Y + metrics.CardSlotBand + metrics.GoldBand + 3f * metrics.RowPitch - metrics.KeyGapV * 0.35f;

    draw.AddLine(

      new Vector2(keypad.X + metrics.Scale * 4f, enterRuleY),

      new Vector2(keypad.Max.X - metrics.Scale * 4f, enterRuleY),

      CalcChassisPalette.GoldRule,

      metrics.Scale);

  }



  private static void DrawFooterText(ImDrawListPtr draw, Vector2 origin, float width, float height, float scale)

  {

    string hp = "hp";

    string brand = "HEWLETT·PACKARD";

    string model = "65";

    float hpSize = height * 0.42f;

    float brandSize = height * 0.22f;

    float modelSize = height * 0.34f;

    Vector2 hpPos = new(origin.X + width * 0.08f, origin.Y + (height - hpSize) * 0.5f);

    draw.AddText(ImGui.GetFont(), hpSize, hpPos, CalcChassisPalette.FooterText, hp);

    Vector2 brandSize2 = ImGui.GetFont().CalcTextSizeA(brandSize, float.MaxValue, 0f, brand);

    draw.AddText(ImGui.GetFont(), brandSize, new Vector2(origin.X + width * 0.5f - brandSize2.X * 0.5f, origin.Y + height * 0.18f), CalcChassisPalette.FooterText, brand);

    Vector2 modelSize2 = ImGui.GetFont().CalcTextSizeA(modelSize, float.MaxValue, 0f, model);

    draw.AddText(ImGui.GetFont(), modelSize, new Vector2(origin.X + width * 0.84f - modelSize2.X, origin.Y + (height - modelSize) * 0.5f), CalcChassisPalette.FooterText, model);

  }

}

