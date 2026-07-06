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
    CalcModelDefinition model)
  {
    if (Hp65FaceplateSvgAssets.UseBodyChrome && Hp65FaceplateSvgAssets.IsReady)
    {
      Hp65FaceplateSvgAssets.DrawBody(draw, origin, metrics);
      CalcChassisRenderer.DrawCardSlotLabels(draw, origin, metrics);
      Hp65FaceplateSvgAssets.DrawLogo(draw, origin, metrics);
      return;
    }

    Vector2 size = new(metrics.Width, metrics.Height);
    Vector2 max = origin + size;
    float scale = metrics.Scale;
    float r = 10f * scale;

    draw.AddRectFilled(origin, max, CalcChassisPalette.FrameEdge, r + scale);
    Vector2 inner = origin + new Vector2(5f * scale, 5f * scale);
    Vector2 innerMax = max - new Vector2(5f * scale, 5f * scale);
    draw.AddRectFilled(inner, innerMax, CalcChassisPalette.Frame, r);

    Vector2 faceMin = inner + new Vector2(8f * scale, 8f * scale);
    Vector2 faceMax = innerMax - new Vector2(8f * scale, metrics.FooterHeight + 6f * scale);
    DrawFaceplateGrain(draw, faceMin, faceMax, scale);

    CalcBodySlots slots = MeasureSlots(origin, metrics);
    DrawDisplayBezel(draw, slots.Display, scale);
    DrawSwitchBand(draw, faceMin, faceMax, slots.Switches, scale);
    DrawKeypadChrome(draw, slots.Keypad, metrics);
    if (metrics.Layout.HasCardSlots)
    {
      CalcChassisRenderer.DrawCardSlots(draw, origin, metrics, paintChrome: true);
    }

    CalcLogoComponent.Draw(draw, slots.Logo.Min, slots.Logo.Max, model, scale);
  }

  private static void DrawDisplayBezel(ImDrawListPtr draw, RectF display, float scale)
  {
    draw.AddRectFilled(display.Min, display.Max, CalcChassisPalette.DisplayBezel, 4f * scale);
    draw.AddRect(display.Min, display.Max, CalcChassisPalette.KeyWellEdge, 4f * scale, ImDrawFlags.None, scale);
  }

  private static void DrawSwitchBand(
    ImDrawListPtr draw,
    Vector2 faceMin,
    Vector2 faceMax,
    RectF switchSlot,
    float scale)
  {
    draw.AddRectFilled(
      new Vector2(faceMin.X, switchSlot.Y),
      new Vector2(faceMax.X, switchSlot.Max.Y),
      CalcChassisPalette.SliderTrack,
      3f * scale);
  }

  private static void DrawKeypadChrome(ImDrawListPtr draw, RectF keypad, CalcChassisMetrics metrics)
  {
    draw.AddRectFilled(keypad.Min, keypad.Max, CalcChassisPalette.Faceplate, 4f * metrics.Scale);

    CalcBodyLayout layout = metrics.Layout;
    if (layout.TryGetKeySlot(15, out RectF enterKey))
    {
      RectF panel = layout.KeypadSlot;
      float enterRuleY = keypad.Y + (enterKey.Y + enterKey.Height * 0.5f - panel.Y) * metrics.Scale;
      draw.AddLine(
        new Vector2(keypad.X + metrics.Scale * 4f, enterRuleY),
        new Vector2(keypad.Max.X - metrics.Scale * 4f, enterRuleY),
        CalcChassisPalette.GoldRule,
        metrics.Scale);
    }
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
}
