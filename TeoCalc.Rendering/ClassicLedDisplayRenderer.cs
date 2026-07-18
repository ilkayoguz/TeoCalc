using System.Numerics;
using ImGuiNET;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>Draws HP-65 Classic LED display via Panamatik <c>LEDcharset_class.TTF</c> or procedural 7-segment fallback.</summary>
public static class ClassicLedDisplayRenderer
{
  /// <summary>Panamatik KML <c>hp65_470</c>: font 20 in display height 42.</summary>
  private const float LedFontHeightRatio = 20f / 42f;

  /// <summary>Modern glass is shorter — keep digits inside the bezel, then letter-space to full width.</summary>
  private const float ModernLedFontHeightRatio = 0.52f;

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
    float scale,
    string? ledText = null)
  {
    bool modern = CalcModernBody.IsActive;
    Vector2 min;
    Vector2 max;
    if (modern)
    {
      // Caller passes the glass rect; chrome already owns the surround.
      min = display.Min;
      max = display.Max;
    }
    else
    {
      min = display.Min + new Vector2(5f * scale, 4f * scale);
      max = display.Max - new Vector2(5f * scale, 4f * scale);
      draw.AddRectFilled(min, max, CalcChassisPalette.DisplayGlass, 2f * scale);
    }

    if (!displayOn)
    {
      return;
    }

    if (CalcFaceplateFonts.IsLedDisplayReady)
    {
      string text = ledText ?? ClassicDisplayFormatter.ToLedFontText(
        registers,
        displayOn: true,
        programMode,
        programEndState);
      DrawLedFontText(draw, min, max, text, scale, programMode);
    }
    else
    {
      ClassicLedDisplaySlot[] slots = ClassicLedDisplayMapper.Map(
        registers,
        displayOn: true,
        programMode,
        programEndState);
      DrawProcedural(draw, min, max, slots, scale, programMode);
    }
  }

  private static void DrawLedFontText(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string text,
    float scale,
    bool programMode)
  {
    ImFontPtr font = CalcFaceplateFonts.LedDisplay;
    float padX = 5f * scale;
    float padY = 4f * scale;
    float innerWidth = MathF.Max(1f, max.X - min.X - padX * 2f);
    float innerHeight = MathF.Max(1f, max.Y - min.Y - padY * 2f);
    float ratio = CalcModernBody.IsActive ? ModernLedFontHeightRatio : LedFontHeightRatio;
    float fontSize = innerHeight * ratio;

    // Center the painted ink (not the font's em/line box) so left/right and top/bottom gaps match.
    CalcFaceplateFonts.FontInkBounds ink = CalcFaceplateFonts.MeasureLedInk(text, fontSize);
    if (ink.Width > innerWidth && ink.Width > 1f)
    {
      fontSize *= innerWidth / ink.Width;
      ink = CalcFaceplateFonts.MeasureLedInk(text, fontSize);
    }

    Vector2 center = new((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);
    float y = center.Y - ink.InkMidY;
    Vector2 glowOffset = new(scale * 0.45f, scale * 0.45f);
    Vector2 boldOffset = new(scale * 0.28f, 0f);

    draw.PushClipRect(min, max, true);
    bool stretch = CalcModernBody.IsActive && text.Length > 1 && ink.Width + 1f < innerWidth;
    if (stretch)
    {
      DrawLedStretched(draw, font, text, min.X + padX, max.X - padX, y, fontSize, glowOffset, boldOffset);
    }
    else
    {
      Vector2 pos = new(center.X - ink.InkMidX, y);
      draw.AddText(font, fontSize, pos + glowOffset, CalcChassisPalette.DisplayDigitGlow, text);
      draw.AddText(font, fontSize, pos + boldOffset, CalcChassisPalette.DisplayDigit, text);
      draw.AddText(font, fontSize, pos, CalcChassisPalette.DisplayDigit, text);
    }

    if (programMode)
    {
      float badge = Math.Clamp(fontSize * 0.28f, 7f * scale, 11f * scale);
      draw.AddText(
        ImGui.GetFont(),
        badge,
        new Vector2(max.X - badge * 4.2f, min.Y + scale),
        CalcChassisPalette.DisplayDigit,
        "PRGM");
    }

    draw.PopClipRect();
  }

  private static void DrawLedStretched(
    ImDrawListPtr draw,
    ImFontPtr font,
    string text,
    float leftX,
    float rightX,
    float topY,
    float fontSize,
    Vector2 glowOffset,
    Vector2 boldOffset)
  {
    int n = text.Length;
    Span<float> advance = stackalloc float[n];
    float totalAdvance = 0f;
    for (int i = 0; i < n; i++)
    {
      advance[i] = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, text.AsSpan(i, 1)).X;
      totalAdvance += advance[i];
    }

    // Pin the first glyph's ink-left to leftX and the last glyph's ink-right to rightX so the
    // outer margins are symmetric (leftX / rightX are equidistant from the glass edges); the
    // leftover space is shared as equal inter-glyph gaps.
    CalcFaceplateFonts.FontInkBounds firstInk = CalcFaceplateFonts.MeasureLedInk(text.Substring(0, 1), fontSize);
    CalcFaceplateFonts.FontInkBounds lastInk = CalcFaceplateFonts.MeasureLedInk(text.Substring(n - 1, 1), fontSize);
    float penStart = leftX - firstInk.Left;
    float penEnd = rightX - (lastInk.Left + lastInk.Width);
    float spacing = (penEnd - penStart - (totalAdvance - advance[n - 1])) / (n - 1);

    float x = penStart;
    for (int i = 0; i < n; i++)
    {
      ReadOnlySpan<char> glyph = text.AsSpan(i, 1);
      Vector2 pos = new(x, topY);
      draw.AddText(font, fontSize, pos + glowOffset, CalcChassisPalette.DisplayDigitGlow, glyph);
      draw.AddText(font, fontSize, pos + boldOffset, CalcChassisPalette.DisplayDigit, glyph);
      draw.AddText(font, fontSize, pos, CalcChassisPalette.DisplayDigit, glyph);
      x += advance[i] + spacing;
    }
  }

  private static void DrawProcedural(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    ClassicLedDisplaySlot[] slots,
    float scale,
    bool programMode)
  {
    const int moduleSize = 5;
    const float moduleGapFactor = 0.35f;
    float innerWidth = max.X - min.X - 8f * scale;
    float innerHeight = max.Y - min.Y - 6f * scale;
    float moduleGap = innerWidth * moduleGapFactor / (ClassicLedDisplayMapper.LogicalSlotCount / (float)moduleSize);
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
