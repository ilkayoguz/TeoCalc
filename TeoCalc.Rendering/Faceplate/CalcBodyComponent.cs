using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Procedural calculator body chrome with Display → Switches → Keypad → Logo slots.</summary>
public static class CalcBodyComponent
{
  public static CalcBodySlots MeasureSlots(Vector2 origin, CalcChassisMetrics metrics) =>
    new(
      metrics.DisplayRect(origin),
      metrics.SwitchTrackRect(origin),
      metrics.KeypadPanelRect(origin),
      metrics.LogoRect(origin));

  public static void DrawChrome(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcModelDefinition model,
    bool skipText = false)
  {
    if (CalcModernBody.IsActive)
    {
      CalcModernBodyChrome.Draw(draw, origin, metrics, model, skipText: skipText);
      return;
    }

    if (ClassicFaceplateSvgAssets.UseBodyChrome && ClassicFaceplateSvgAssets.IsReady)
    {
      ClassicFaceplateSvgAssets.DrawBody(draw, origin, metrics);
      if (!skipText)
      {
        CalcChassisRenderer.DrawCardSlotLabels(draw, origin, metrics);
      }

      ClassicFaceplateSvgAssets.DrawLogo(draw, origin, metrics, skipText: skipText);
      return;
    }

    DrawProceduralChrome(draw, origin, metrics, model, skipText);
  }

  private static void DrawProceduralChrome(
    ImDrawListPtr draw,
    Vector2 origin,
    CalcChassisMetrics metrics,
    CalcModelDefinition model,
    bool skipText)
  {
    Vector2 size = new(metrics.Width, metrics.Height);
    Vector2 max = origin + size;
    float scale = metrics.Scale;
    float r = 14f * scale;
    uint shellShadow = CalcChassisPalette.KeyWellEdge;

    draw.AddRectFilled(origin + new Vector2(scale * 2.5f, scale * 3.5f), max + new Vector2(scale * 2.5f, scale * 3.5f), shellShadow, r);
    draw.AddRectFilled(origin, max, CalcChassisPalette.FrameEdge, r + scale);
    Vector2 inner = origin + new Vector2(7f * scale, 7f * scale);
    Vector2 innerMax = max - new Vector2(7f * scale, 7f * scale);
    draw.AddRectFilled(inner, innerMax, CalcChassisPalette.Frame, r - scale * 0.5f);

    Vector2 faceMin = inner + new Vector2(11f * scale, 11f * scale);
    Vector2 faceMax = innerMax - new Vector2(11f * scale, metrics.FooterHeight + 9f * scale);
    DrawFaceplateGrain(draw, faceMin, faceMax, scale);

    CalcBodySlots slots = MeasureSlots(origin, metrics);
    DrawDisplayBezel(draw, slots.Display, scale);
    if (!CalcModernBody.IsActive)
    {
      DrawSwitchBand(draw, slots.Switches, scale);
      DrawKeypadChrome(draw, slots.Keypad, metrics);
    }

    if (metrics.Layout.HasCardSlots)
    {
      CalcChassisRenderer.DrawCardSlots(draw, origin, metrics, paintChrome: true, skipText: skipText);
    }

    CalcLogoComponent.Draw(draw, slots.Logo.Min, slots.Logo.Max, model, scale, skipText: skipText);
  }

  private static void DrawDisplayBezel(ImDrawListPtr draw, RectF display, float scale)
  {
    draw.AddRectFilled(display.Min, display.Max, CalcChassisPalette.DisplayBezel, 5f * scale);
    if (CalcModernBody.IsActive)
    {
      return;
    }

    Vector2 inset = new(4f * scale, 4f * scale);
    draw.AddRectFilled(display.Min + inset, display.Max - inset, CalcChassisPalette.DisplayGlass, 4f * scale);
  }

  private static void DrawSwitchBand(ImDrawListPtr draw, RectF switchSlot, float scale)
  {
    CalcSwitchPanelComponent.Draw(
      draw,
      switchSlot,
      scale,
      Array.Empty<CalcSwitchSpec>(),
      static (_, _) => 0f,
      modernChrome: false);
  }

  private static void DrawKeypadChrome(ImDrawListPtr draw, RectF keypad, CalcChassisMetrics metrics)
  {
    draw.AddRectFilled(keypad.Min, keypad.Max, CalcChassisPalette.KeyWell, 5f * metrics.Scale);
  }

  private static void DrawFaceplateGrain(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale)
  {
    draw.AddRectFilled(min, max, CalcChassisPalette.Faceplate, 8f * scale);
    if (CalcModernBody.IsActive)
    {
      DrawVerticalBrush(draw, min, max, scale);
      return;
    }

    int seed = (int)(min.X * 13 + min.Y * 29);
    int grains = (int)Math.Clamp(220 * scale, 140, 320);
    for (int grain = 0; grain < grains; grain++)
    {
      seed = seed * 1664525 + 1013904223;
      float u = (seed & 0xFFFF) / 65535f;
      seed = seed * 1664525 + 1013904223;
      float v = (seed & 0xFFFF) / 65535f;
      Vector2 point = new(
        min.X + 8f + u * MathF.Max(10f, max.X - min.X - 16f),
        min.Y + 8f + v * MathF.Max(10f, max.Y - min.Y - 16f));
      draw.AddCircleFilled(point, (0.4f + (grain % 3) * 0.15f) * scale, CalcChassisPalette.FaceplateGrain);
    }
  }

  private static void DrawVerticalBrush(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale)
  {
    float height = max.Y - min.Y;
    int lines = (int)Math.Clamp(height / (1.2f * scale), 24, 72);
    for (int line = 0; line < lines; line++)
    {
      float y = min.Y + line / (float)Math.Max(1, lines - 1) * height;
      uint color = line % 2 == 0 ? 0x30FFFFFFu : 0x24000000u;
      draw.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), color, scale * 0.35f);
    }
  }
}
