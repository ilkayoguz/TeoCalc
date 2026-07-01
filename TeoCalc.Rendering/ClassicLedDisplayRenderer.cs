using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Rendering;

/// <summary>Draws HP-65 Classic 15-position segmented LED display (3×5 modules).</summary>
public static class ClassicLedDisplayRenderer
{
  private const int ModuleSize = 5;

  private const float ModuleGapFactor = 0.35f;

  private const float ExponentScale = 0.76f;

  [Flags]
  private enum SevenSegment : byte
  {
    None = 0,
    A = 1 << 0,
    B = 1 << 1,
    C = 1 << 2,
    D = 1 << 3,
    E = 1 << 4,
    F = 1 << 5,
    G = 1 << 6,
  }

  public static void Draw(
    ImDrawListPtr draw,
    RectF display,
    ClassicRegisterFile registers,
    bool displayOn,
    bool programMode,
    byte programEndState,
    float scale)
  {
    Vector2 min = display.Min + new Vector2(5f * scale, 4f * scale);
    Vector2 max = display.Max - new Vector2(5f * scale, 4f * scale);
    draw.AddRectFilled(min, max, CalcChassisPalette.DisplayGlass, 2f * scale);

    if (!displayOn)
    {
      return;
    }

    ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(
      registers,
      displayOn: true,
      programMode,
      programEndState);

    float innerWidth = max.X - min.X - 8f * scale;
    float innerHeight = max.Y - min.Y - 6f * scale;
    float moduleGap = innerWidth * ModuleGapFactor / (ClassicLedDisplayMapper.LogicalSlotCount / (float)ModuleSize);
    float cellWidth = (innerWidth - moduleGap * 2f) / ClassicLedDisplayMapper.LogicalSlotCount;
    float digitHeight = innerHeight * 0.84f;
    float digitY = min.Y + (max.Y - min.Y - digitHeight) * 0.5f;
    float startX = min.X + 4f * scale;

    for (int slot = 0; slot < ClassicLedDisplayMapper.LogicalSlotCount; slot++)
    {
      float cellX = startX + slot * cellWidth + ModuleGapBefore(slot) * moduleGap;
      bool exponent = slot >= 13;
      float widthScale = exponent ? ExponentScale : 1f;
      float heightScale = exponent ? ExponentScale : 1f;
      float cellW = cellWidth * widthScale;
      float cellH = digitHeight * heightScale;
      float y = digitY + (digitHeight - cellH) * 0.5f;
      float x = cellX + (cellWidth - cellW) * 0.5f;
      DrawSlot(draw, new Vector2(x, y), new Vector2(cellW - scale * 0.35f, cellH), slots[slot], scale);
    }

    if (programMode)
    {
      float badge = Math.Clamp(digitHeight * 0.28f, 8f * scale, 12f * scale);
      draw.AddText(
        ImGui.GetFont(),
        badge,
        new Vector2(max.X - badge * 4.2f, min.Y + scale),
        CalcChassisPalette.DisplayDigit,
        "PRGM");
    }
  }

  private static int ModuleGapBefore(int slotIndex) =>
    slotIndex switch
    {
      >= 10 => 2,
      >= 5 => 1,
      _ => 0,
    };

  private static void DrawSlot(ImDrawListPtr draw, Vector2 origin, Vector2 size, ClassicLedDisplaySlot slot, float scale)
  {
    switch (slot.Kind)
    {
      case ClassicLedSlotKind.Blank:
        return;
      case ClassicLedSlotKind.DecimalPoint:
        DrawDecimalPoint(draw, origin, size, scale);
        return;
      case ClassicLedSlotKind.Minus:
        DrawSegments(draw, origin, size, SevenSegment.G, scale);
        return;
      case ClassicLedSlotKind.Digit:
        DrawSegments(draw, origin, size, SegmentsForDigit(slot.Digit), scale);
        return;
    }
  }

  private static void DrawDecimalPoint(ImDrawListPtr draw, Vector2 origin, Vector2 size, float scale)
  {
    float radius = MathF.Max(1.6f * scale, size.Y * 0.09f);
    Vector2 center = new(origin.X + size.X * 0.5f, origin.Y + size.Y * 0.86f);
    draw.AddCircleFilled(center + new Vector2(scale * 0.5f, scale * 0.5f), radius, CalcChassisPalette.DisplayDigitGlow);
    draw.AddCircleFilled(center, radius, CalcChassisPalette.DisplayDigit);
  }

  private static void DrawSegments(
    ImDrawListPtr draw,
    Vector2 origin,
    Vector2 size,
    SevenSegment segments,
    float scale)
  {
    if (segments == SevenSegment.None)
    {
      return;
    }

    float padding = size.X * 0.1f;
    float thickness = MathF.Max(1.4f * scale, size.Y * 0.11f);
    float horizontalLength = size.X - padding * 2f;
    float verticalLength = (size.Y - thickness * 3f) * 0.5f;
    float left = origin.X + padding;
    float right = origin.X + size.X - padding;
    float top = origin.Y + padding * 0.5f;
    float middle = origin.Y + size.Y * 0.5f - thickness * 0.5f;
    float bottom = origin.Y + size.Y - padding * 0.5f - thickness;

    if (segments.HasFlag(SevenSegment.A))
    {
      DrawBar(draw, new Vector2(left, top), new Vector2(right, top + thickness), scale);
    }

    if (segments.HasFlag(SevenSegment.G))
    {
      DrawBar(draw, new Vector2(left, middle), new Vector2(right, middle + thickness), scale);
    }

    if (segments.HasFlag(SevenSegment.D))
    {
      DrawBar(draw, new Vector2(left, bottom), new Vector2(right, bottom + thickness), scale);
    }

    if (segments.HasFlag(SevenSegment.F))
    {
      DrawBar(draw, new Vector2(left, top), new Vector2(left + thickness, top + verticalLength), scale);
    }

    if (segments.HasFlag(SevenSegment.B))
    {
      DrawBar(draw, new Vector2(right - thickness, top), new Vector2(right, top + verticalLength), scale);
    }

    if (segments.HasFlag(SevenSegment.E))
    {
      DrawBar(draw, new Vector2(left, middle + thickness), new Vector2(left + thickness, bottom), scale);
    }

    if (segments.HasFlag(SevenSegment.C))
    {
      DrawBar(draw, new Vector2(right - thickness, middle + thickness), new Vector2(right, bottom), scale);
    }
  }

  private static void DrawBar(ImDrawListPtr draw, Vector2 min, Vector2 max, float scale)
  {
    Vector2 glowOffset = new(scale * 0.55f, scale * 0.55f);
    draw.AddRectFilled(min + glowOffset, max + glowOffset, CalcChassisPalette.DisplayDigitGlow, max.Y - min.Y);
    draw.AddRectFilled(min, max, CalcChassisPalette.DisplayDigit, max.Y - min.Y);
  }

  private static SevenSegment SegmentsForDigit(byte digit) =>
    digit switch
    {
      0 => SevenSegment.A | SevenSegment.B | SevenSegment.C | SevenSegment.D | SevenSegment.E | SevenSegment.F,
      1 => SevenSegment.B | SevenSegment.C,
      2 => SevenSegment.A | SevenSegment.B | SevenSegment.D | SevenSegment.E | SevenSegment.G,
      3 => SevenSegment.A | SevenSegment.B | SevenSegment.C | SevenSegment.D | SevenSegment.G,
      4 => SevenSegment.B | SevenSegment.C | SevenSegment.F | SevenSegment.G,
      5 => SevenSegment.A | SevenSegment.C | SevenSegment.D | SevenSegment.F | SevenSegment.G,
      6 => SevenSegment.A | SevenSegment.C | SevenSegment.D | SevenSegment.E | SevenSegment.F | SevenSegment.G,
      7 => SevenSegment.A | SevenSegment.B | SevenSegment.C,
      8 => SevenSegment.A | SevenSegment.B | SevenSegment.C | SevenSegment.D | SevenSegment.E | SevenSegment.F | SevenSegment.G,
      9 => SevenSegment.A | SevenSegment.B | SevenSegment.C | SevenSegment.D | SevenSegment.F | SevenSegment.G,
      _ => SevenSegment.None,
    };
}
