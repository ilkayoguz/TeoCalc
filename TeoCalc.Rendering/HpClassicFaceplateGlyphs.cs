using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Vector-drawn HP Classic faceplate symbols (math italic x/y, arrows, radicals).</summary>
public static class HpClassicFaceplateGlyphs
{
  public readonly record struct LabelSize(float Width, float Height);

  public static void DrawCardSlotLabel(
    ImDrawListPtr draw,
    int column,
    Vector2 center,
    float fontSize,
    uint color,
    float scale)
  {
    switch (column)
    {
      case 0:
        DrawInverseX(draw, center, fontSize, color, scale);
        break;
      case 1:
        DrawSqrtX(draw, center, fontSize, color, scale);
        break;
      case 2:
        DrawRUp(draw, center, fontSize, color, scale);
        break;
      case 3:
        DrawRDown(draw, center, fontSize, color, scale);
        break;
      case 4:
        DrawXExchangeY(draw, center, fontSize, color, scale);
        break;
    }
  }

  private const float CardSlotMathXScale = 1.0f;
  private const float CardSlotArialRunScale = 0.82f;
  private const float CardSlotSlashScale = 0.82f;
  private const float CardSlotMathXBandHeight = 1.1f;
  private const float SansCapHeightRatio = 0.72f;

  public static LabelSize MeasureCardSlotLabel(int column, float fontSize)
  {
    float xSize = fontSize * CardSlotMathXScale;
    return column switch
    {
      0 => new(
        PlainGlyphWidth("1", fontSize * CardSlotArialRunScale)
        + PlainGlyphWidth("/", fontSize * CardSlotArialRunScale)
        + MathGlyphWidth("x", xSize)
        + fontSize * 0.08f,
        fontSize),
      1 => new(fontSize * 1.38f, fontSize * 1.1f),
      2 => new(ArialBoldGlyphWidth("R", fontSize) + fontSize * 0.28f, fontSize),
      3 => new(ArialBoldGlyphWidth("R", fontSize) + fontSize * 0.28f, fontSize),
      4 => new(
        (MathGlyphWidth("x", xSize) + CardSlotExchangeArt.MeasureWidth(fontSize) + MathGlyphWidth("y", xSize) + fontSize * 0.28f) * 1.08f,
        fontSize),
      _ => new(fontSize, fontSize),
    };
  }

  private static float SansGlyphWidth(string text, float size) =>
    CalcFaceplateFonts.IsReady ? CalcFaceplateFonts.SansWidth(text, size) : size * 0.55f;

  private static float MathGlyphWidth(string text, float size) =>
    CalcFaceplateFonts.IsMathReady ? CalcFaceplateFonts.MathWidth(text, size) : size * 0.48f;

  private static Vector2 SansGlyphSize(string text, float size) =>
    CalcFaceplateFonts.IsReady ? CalcFaceplateFonts.MeasureSans(text, size) : new Vector2(size * 0.55f, size);

  private static Vector2 MathGlyphSize(string text, float size) =>
    CalcFaceplateFonts.IsMathReady ? CalcFaceplateFonts.MeasureMath(text, size) : new Vector2(size * 0.48f, size * 0.72f);

  public static LabelSize Measure(string text, float fontSize)
  {
    if (IsRegisterRowLetter(text))
    {
      Vector2 size = CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
        ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
        : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
      return new(size.X, size.Y);
    }

    return new(MeasureWidth(text, fontSize), fontSize * 1.05f);
  }

  public static LabelSize MeasureKeyFaceLabel(string text, float fontSize) =>
    new(MeasureWidth(text, fontSize, keyFaceArialBold: true), MeasureKeyFaceHeight(text, fontSize));

  public static LabelSize MeasureSkirtLabel(string text, float fontSize)
  {
    if (IsCardSlotSkirtLabel(text))
    {
      int column = text switch
      {
        "x\u2194y" => 4,
        "R\u2193" => 3,
        "R\u2191" => 2,
        _ => 2,
      };
      return MeasureCardSlotLabel(column, fontSize * 0.9f);
    }

    if (IsSkirtComparisonLabel(text))
    {
      float compareSize = fontSize * 0.96f;
      return new(MeasureSkirtComparisonWidth(text, compareSize), compareSize * 0.92f);
    }

    return new(MeasureWidth(text, fontSize, skirtArial: true), MeasureSkirtHeight(text, fontSize));
  }

  public static LabelSize MeasureBodyLabel(string text, float fontSize) =>
    new(MeasureWidth(text, fontSize, skirtArial: true), fontSize * 0.92f);

  public static void DrawBodyLabel(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string text,
    float fontSize,
    uint color,
    float scale) =>
    Draw(draw, topLeft, text, fontSize, color, scale, bold: false, skirtArial: true, bandAlign: true);

  public static void DrawSkirtLabel(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string text,
    float fontSize,
    uint color,
    float scale)
  {
    if (IsSkirtComparisonLabel(text))
    {
      float compareSize = fontSize * 0.96f;
      Draw(draw, topLeft, text, compareSize, color, scale, bold: false, skirtArial: true, skirtBand: true);
      return;
    }

    Draw(draw, topLeft, text, fontSize, color, scale, bold: true, skirtArial: true, skirtBand: true);
  }

  public static bool TryDrawSkirtCardSlotLabel(
    ImDrawListPtr draw,
    string text,
    Vector2 skirtCenter,
    float fontSize,
    uint color,
    float scale)
  {
    int? column = text switch
    {
      "x\u2194y" => 4,
      "R\u2193" => 3,
      "R\u2191" => 2,
      _ => null,
    };

    if (column is null)
    {
      return false;
    }

    float slotFont = fontSize * 0.9f;
    LabelSize size = MeasureCardSlotLabel(column.Value, slotFont);
    float left = skirtCenter.X - size.Width * 0.5f;
    float top = skirtCenter.Y - size.Height * 0.5f;

    if (column == 4)
    {
      DrawXExchangeYAt(draw, left, top, slotFont, color, scale, widen: true);
      return true;
    }

    if (column == 3)
    {
      DrawRWithInlineDownArrow(draw, left, top, slotFont, color, scale);
      return true;
    }

    DrawRWithInlineUpArrow(draw, left, top, slotFont, color, scale);
    return true;
  }

  public static bool IsCardSlotSkirtLabel(string text) =>
    text is "x\u2194y" or "R\u2193" or "R\u2191";

  public static void DrawKeyFaceLabel(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string text,
    float fontSize,
    uint color,
    float scale) =>
    Draw(draw, topLeft, text, fontSize, color, scale, bold: false, keyFaceArialBold: true);

  public static void Draw(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string text,
    float fontSize,
    uint color,
    float scale,
    bool bold = true,
    bool keyFaceArialBold = false,
    bool skirtArial = false,
    bool skirtBand = false,
    bool bandAlign = false,
    float mathYOffset = 0f)
  {
    float x = topLeft.X;
    float y = topLeft.Y;
    bool bandMode = skirtBand || bandAlign;
    int i = 0;
    while (i < text.Length)
    {
      if (TryConsume(text, ref i, "x\u2194y", out _))
      {
        x += DrawXExchangeYAt(draw, x, y + mathYOffset, fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "LST X", out _))
      {
        x += DrawLstMathX(draw, x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "x\u2260y", out _))
      {
        if (skirtBand)
        {
          x += DrawSkirtComparison(draw, x, y, fontSize, color, scale, SkirtCompareOp.NotEqual);
        }
        else
        {
          x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawNotEqual(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        }

        continue;
      }

      if (TryConsume(text, ref i, "x\u2264y", out _))
      {
        if (skirtBand)
        {
          x += DrawSkirtComparison(draw, x, y, fontSize, color, scale, SkirtCompareOp.LessEqual);
        }
        else
        {
          x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawLessEqual(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        }

        continue;
      }

      if (TryConsume(text, ref i, "x=y", out _))
      {
        if (skirtBand)
        {
          x += DrawSkirtComparison(draw, x, y, fontSize, color, scale, SkirtCompareOp.Equal);
        }
        else
        {
          x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawPlainRun(draw, "=", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
          x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        }

        continue;
      }

      if (TryConsume(text, ref i, "x>y", out _))
      {
        if (skirtBand)
        {
          x += DrawSkirtComparison(draw, x, y, fontSize, color, scale, SkirtCompareOp.Greater);
        }
        else
        {
          x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
          x += DrawPlainRun(draw, ">", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
          x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        }

        continue;
      }

      if (TryConsume(text, ref i, "x!/y", out _))
      {
        x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawPlainRun(draw, "!/", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
        x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        continue;
      }

      if (TryConsume(text, ref i, "\u221ax", out _) || TryConsume(text, ref i, "√x", out _))
      {
        x += DrawSqrtXAt(draw, x, y + (bandMode ? fontSize * 0.04f : 0f), fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "y^x", out _))
      {
        x += DrawYToTheXAt(draw, x, y + (bandMode ? fontSize * 0.04f : 0f), fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "1/x", out _))
      {
        x += DrawInverseXAt(draw, x, y + (bandMode ? fontSize * 0.04f : 0f), fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "R\u2192P", out _))
      {
        x += DrawPlainRun(draw, "R", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += DrawPlainRun(draw, "P", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "R\u2191", out _))
      {
        if (skirtBand || skirtArial)
        {
          x += DrawRWithInlineUpArrowWidth(draw, x, y, fontSize, color, scale);
        }
        else
        {
          x += DrawPlainRun(draw, "R", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
          x += DrawArrowUp(draw, x, y, fontSize, color, scale, bandMode);
        }

        continue;
      }

      if (TryConsume(text, ref i, "R\u2193", out _))
      {
        x += DrawRWithInlineDownArrowWidth(draw, x, y, fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "\u2192D.MS", out _))
      {
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += DrawPlainRun(draw, "D.MS", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "\u2192OCT", out _))
      {
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += DrawPlainRun(draw, "OCT", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "f\u207b\u00b9", out _) || TryConsume(text, ref i, "f⁻¹", out _))
      {
        x += DrawPlainRun(draw, "f", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        x += DrawSuperscriptMinusOne(
          draw,
          x,
          y,
          fontSize,
          color,
          scale,
          bold,
          keyFaceArialBold,
          skirtArial,
          skirtBand,
          nudgeRight: keyFaceArialBold && !skirtBand ? fontSize * 0.08f + MathF.Max(scale, 1.5f) : 0f,
          nudgeDown: keyFaceArialBold && !skirtBand ? fontSize * 0.14f : 0f);
        continue;
      }

      if (TryConsume(text, ref i, "\u03c0", out _) || TryConsume(text, ref i, "π", out _))
      {
        x += DrawPiLabel(draw, x, y, fontSize, color, scale, skirtBand);
        continue;
      }

      if (text[i] == 'x')
      {
        i++;
        x += DrawMathX(draw, x, y + mathYOffset, fontSize, color, scale);
        continue;
      }

      if (text[i] == 'y')
      {
        i++;
        x += DrawMathY(draw, x, y + mathYOffset, fontSize, color, scale);
        continue;
      }

      int start = i;
      while (i < text.Length && text[i] is not ('x' or 'y') && !IsPatternStart(text, i))
      {
        i++;
      }

      string run = text[start..i];
      if (run.Length > 0)
      {
        x += DrawPlainRun(draw, run, x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
      }
    }
  }

  private static void DrawInverseX(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(0, fontSize);
    DrawInverseFraction(draw, center, size, fontSize, color, scale);
  }

  private static void DrawSqrtX(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(1, fontSize);
    DrawSqrtRadicalX(draw, center, size, fontSize, color, scale);
  }

  private static void DrawYToTheX(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(2, fontSize);
    DrawExponentYx(draw, center, size, fontSize, color, scale);
  }

  private static void DrawRUp(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(2, fontSize);
    float left = center.X - size.Width * 0.5f;
    float top = center.Y - size.Height * 0.5f;
    DrawRWithInlineUpArrow(draw, left, top, fontSize, color, scale);
  }

  private static void DrawRDown(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(3, fontSize);
    float left = center.X - size.Width * 0.5f;
    float top = center.Y - size.Height * 0.5f;
    DrawRWithInlineDownArrow(draw, left, top, fontSize, color, scale);
  }

  private static void DrawXExchangeY(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    LabelSize size = MeasureCardSlotLabel(4, fontSize);
    float left = center.X - size.Width * 0.5f;
    float top = center.Y - size.Height * 0.5f;
    DrawXExchangeYAt(draw, left, top, fontSize, color, scale);
  }

  private static float CardSlotMathXTop(Vector2 center, float fontSize, float xHeight) =>
    center.Y + fontSize * CardSlotMathXBandHeight * 0.5f - xHeight;

  private static void DrawInverseFraction(
    ImDrawListPtr draw,
    Vector2 center,
    LabelSize box,
    float fontSize,
    uint color,
    float scale)
  {
    float xSize = fontSize * CardSlotMathXScale;
    float arialSize = fontSize * CardSlotArialRunScale;
    Vector2 xDim = MathGlyphSize("x", xSize);
    float xTop = CardSlotMathXTop(center, fontSize, xDim.Y);
    float gap = fontSize * 0.02f;
    float rowBottom = xTop + xDim.Y;

    float totalW =
      PlainGlyphWidth("1", arialSize)
      + PlainGlyphWidth("/", arialSize)
      + MathGlyphWidth("x", xSize)
      + gap * 2f;
    float drawX = center.X - totalW * 0.5f;

    drawX += DrawPlainGlyph(draw, "1", drawX, rowBottom - PlainGlyphHeight("1", arialSize), arialSize, color) + gap;
    drawX += DrawPlainGlyph(draw, "/", drawX, rowBottom - PlainGlyphHeight("/", arialSize), arialSize, color) + gap;
    DrawMathGlyph(draw, "x", drawX, xTop, xSize, color);
  }

  private static void DrawSqrtRadicalX(
    ImDrawListPtr draw,
    Vector2 center,
    LabelSize box,
    float fontSize,
    uint color,
    float scale)
  {
    float left = center.X - box.Width * 0.5f;
    float top = center.Y - box.Height * 0.5f;
    float xSize = fontSize * CardSlotMathXScale;
    Vector2 xDim = MathGlyphSize("x", xSize);
    float xTop = CardSlotMathXTop(center, fontSize, xDim.Y);
    CardSlotSqrtArt.RadicalLayout layout = CardSlotSqrtArt.LayoutForX(center.X, xDim.X, xDim.Y, fontSize);
    CardSlotSqrtArt.Draw(draw, layout.SqrtLeft, xTop, xDim.Y, scale, color);
    DrawCardSlotMathX(draw, layout.XLeft, xTop, xSize, color);
  }

  private static void DrawExponentYx(
    ImDrawListPtr draw,
    Vector2 center,
    LabelSize box,
    float fontSize,
    uint color,
    float scale)
  {
    float left = center.X - box.Width * 0.5f;
    float top = center.Y - box.Height * 0.5f;
    Vector2 yDim = MathGlyphSize("y", fontSize);
    float yTop = top + box.Height - yDim.Y;

    float yW = DrawCardSlotMathY(draw, left, yTop, fontSize, color);
    float xX = left + yW + fontSize * 0.04f;
    float xY = yTop - fontSize * 0.2f;
    DrawCardSlotMathX(draw, xX, xY, fontSize, color);
  }

  private static void DrawRWithInlineDownArrow(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale)
  {
    float rW = DrawArialBoldGlyph(draw, "R", x, y, fontSize, color);
    float arrowX = x + rW + fontSize * 0.04f;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    DrawInlineDownArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
  }

  private static float DrawInverseXAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    LabelSize box = MeasureCardSlotLabel(0, fontSize);
    DrawInverseFraction(draw, new Vector2(x + box.Width * 0.5f, y + box.Height * 0.5f), box, fontSize, color, scale);
    return box.Width;
  }

  private static float DrawSqrtXAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    LabelSize box = MeasureCardSlotLabel(1, fontSize);
    DrawSqrtRadicalX(draw, new Vector2(x + box.Width * 0.5f, y + box.Height * 0.5f), box, fontSize, color, scale);
    return box.Width;
  }

  private static float DrawYToTheXAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    LabelSize box = MeasureCardSlotLabel(2, fontSize);
    DrawExponentYx(draw, new Vector2(x + box.Width * 0.5f, y + box.Height * 0.5f), box, fontSize, color, scale);
    return box.Width;
  }

  private static float DrawXExchangeYAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool widen = false)
  {
    float xSize = fontSize * CardSlotMathXScale;
    Vector2 xDim = MathGlyphSize("x", xSize);
    float xW = DrawCardSlotMathX(draw, x, y, xSize, color);
    float gap = fontSize * (widen ? 0.14f : 0.06f);
    float centerY = y + xDim.Y * 0.58f;
    float symW = CardSlotExchangeArt.Draw(draw, x + xW + gap, centerY, xDim.Y, scale, color);
    DrawCardSlotMathY(draw, x + xW + gap + symW + gap, y, xSize, color);
    return xW + gap + symW + gap + MathGlyphWidth("y", xSize);
  }

  private static float PlainGlyphWidth(string text, float size) =>
    PlainGlyphSize(text, size).X;

  private static float PlainGlyphHeight(string text, float size) =>
    PlainGlyphSize(text, size).Y;

  private static Vector2 PlainGlyphSize(string text, float size) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, size)
      : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text);

  private static float DrawPlainGlyph(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
  {
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      return CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, y, size, color);
    }

    ImFontPtr font = ImGui.GetFont();
    draw.AddText(font, size, new Vector2(x, y), color, text);
    return font.CalcTextSizeA(size, float.MaxValue, 0f, text).X;
  }

  private static float ArialBoldGlyphWidth(string text, float size) =>
    CalcFaceplateFonts.ArialBoldWidth(text, size);

  private static float DrawArialBoldGlyph(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
  {
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      return CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, y, size, color);
    }

    return DrawTextRun(draw, text, x, y, size, color, bold: true);
  }

  private static bool IsRegisterRowLetter(string text) =>
    text is "A" or "B" or "C" or "D" or "E";

  private static float DrawPlainGlyphBold(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
  {
    float stroke = MathF.Max(0.55f, size * 0.04f);
    DrawPlainGlyph(draw, text, x, y, size, color);
    DrawPlainGlyph(draw, text, x + stroke, y, size, color);
    return PlainGlyphWidth(text, size) + stroke;
  }

  private static float DrawCardSlotMathX(ImDrawListPtr draw, float x, float y, float size, uint color) =>
    DrawMathGlyph(draw, "x", x, y, size, color);

  private static float DrawCardSlotMathY(ImDrawListPtr draw, float x, float y, float size, uint color) =>
    DrawMathGlyph(draw, "y", x, y, size, color);

  private static float DrawSansGlyph(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
  {
    if (CalcFaceplateFonts.IsReady)
    {
      return CalcFaceplateFonts.DrawSansTop(draw, text, x, y, size, color);
    }

    return DrawTextRun(draw, text, x, y, size, color, bold: true);
  }

  private static float DrawMathGlyph(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
  {
    if (CalcFaceplateFonts.IsMathReady)
    {
      return CalcFaceplateFonts.DrawMathTop(draw, text, x, y, size, color);
    }

    return text switch
    {
      "x" => DrawMathXVector(draw, x, y, size, color, 1f),
      "y" => DrawMathYVector(draw, x, y, size, color, 1f),
      _ => DrawTextRun(draw, text, x, y, size, color, bold: true),
    };
  }

  private static float DrawInlineDownArrow(
    ImDrawListPtr draw,
    float x,
    float capTop,
    float capBottom,
    float fontSize,
    uint color,
    float scale)
  {
    float capHeight = capBottom - capTop;
    float w = fontSize * 0.3f;
    float thickness = MathF.Max(3.2f, scale * 2.25f);
    float cx = x + w * 0.5f;
    float arrowHeight = capHeight * 0.88f;
    float arrowTop = capTop + (capHeight - arrowHeight) * 0.5f;
    float tipY = arrowTop + arrowHeight;
    float headH = arrowHeight * 0.38f;
    float headTop = tipY - headH;

    draw.AddLine(new Vector2(cx, arrowTop), new Vector2(cx, headTop), color, thickness);
    draw.AddTriangleFilled(new Vector2(cx, tipY), new Vector2(x, headTop), new Vector2(x + w, headTop), color);
    return w + fontSize * 0.03f;
  }

  private static float DrawRWithInlineDownArrowWidth(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale)
  {
    float rW = DrawArialBoldGlyph(draw, "R", x, y, fontSize, color);
    float arrowX = x + rW + fontSize * 0.04f;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    float arrowW = DrawInlineDownArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
    return rW + fontSize * 0.04f + arrowW;
  }

  private static float DrawRWithInlineUpArrowWidth(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale)
  {
    float rW = DrawArialBoldGlyph(draw, "R", x, y, fontSize, color);
    float arrowX = x + rW + fontSize * 0.04f;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    float arrowW = DrawInlineUpArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
    return rW + fontSize * 0.04f + arrowW;
  }

  private static float DrawExchangeSymbol(ImDrawListPtr draw, float x, float midY, float fontSize, uint color, float scale) =>
    CardSlotExchangeArt.Draw(draw, x, midY, fontSize * 0.72f, scale, color);

  private static float DrawMathX(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    if (CalcFaceplateFonts.IsMathReady)
    {
      return CalcFaceplateFonts.DrawMathTop(draw, "x", x, y, fontSize, color);
    }

    return DrawMathXVector(draw, x, y, fontSize, color, scale);
  }

  private static float DrawMathY(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    if (CalcFaceplateFonts.IsMathReady)
    {
      return CalcFaceplateFonts.DrawMathTop(draw, "y", x, y, fontSize, color);
    }

    return DrawMathYVector(draw, x, y, fontSize, color, scale);
  }

  private static float DrawMathXVector(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    float thickness = MathF.Max(1.1f, scale * 1.1f);
    float w = fontSize * 0.42f;
    float h = fontSize * 0.72f;
    float skew = fontSize * 0.12f;
    Vector2 bl = new(x + skew * 0.2f, y + h);
    Vector2 br = new(x + w, y + h);
    Vector2 tl = new(x + skew, y);
    Vector2 tr = new(x + w + skew, y);
    draw.AddLine(bl, tr, color, thickness);
    draw.AddLine(tl, br, color, thickness);
    return w + skew * 0.35f;
  }

  private static float DrawMathYVector(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    float thickness = MathF.Max(1.1f, scale * 1.1f);
    float w = fontSize * 0.38f;
    float h = fontSize * 0.72f;
    float skew = fontSize * 0.12f;
    Vector2 top = new(x + w * 0.5f + skew, y);
    Vector2 left = new(x + skew * 0.2f, y + h * 0.58f);
    Vector2 right = new(x + w, y + h * 0.58f);
    Vector2 tail = new(x + w * 0.62f + skew, y + h);
    draw.AddLine(top, left, color, thickness);
    draw.AddLine(top, right, color, thickness);
    draw.AddLine(right, tail, color, thickness);
    return w + skew * 0.35f;
  }

  private static float DrawArrowDown(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    float shaftW = MathF.Max(1.1f, scale * 1.05f);
    float w = fontSize * 0.28f;
    float h = fontSize * 0.55f;
    float top = y - h * 0.75f;
    draw.AddRectFilled(new Vector2(x + w * 0.35f, top), new Vector2(x + w * 0.65f, y - h * 0.2f), color);
    Vector2 tip = new(x + w * 0.5f, y);
    draw.AddTriangleFilled(tip, new(x, y - h * 0.22f), new(x + w, y - h * 0.22f), color);
    return w + fontSize * 0.06f;
  }

  private static float DrawArrowUp(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool bandMode = false)
  {
    float w = fontSize * 0.28f;
    float h = fontSize * 0.55f;
    float mid = bandMode ? y + fontSize * 0.42f : y - fontSize * 0.36f;
    Vector2 tip = new(x + w * 0.5f, mid - h * 0.42f);
    draw.AddTriangleFilled(tip, new(x, mid - h * 0.18f), new(x + w, mid - h * 0.18f), color);
    draw.AddRectFilled(new Vector2(x + w * 0.35f, mid - h * 0.16f), new Vector2(x + w * 0.65f, mid + h * 0.28f), color);
    return w + fontSize * 0.06f;
  }

  private static float DrawArrowRight(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool bandMode = false)
  {
    float w = fontSize * 0.42f;
    float h = fontSize * 0.28f;
    float midY = bandMode ? y + fontSize * 0.42f : y - fontSize * 0.36f;
    draw.AddRectFilled(new Vector2(x, midY - h * 0.35f), new Vector2(x + w * 0.62f, midY + h * 0.35f), color);
    draw.AddTriangleFilled(
      new(x + w, midY),
      new(x + w * 0.55f, midY - h * 0.55f),
      new(x + w * 0.55f, midY + h * 0.55f),
      color);
    return w + fontSize * 0.04f;
  }

  private static void DrawTinyArrowRight(ImDrawListPtr draw, Vector2 tip, float size, uint color, float thickness)
  {
    draw.AddLine(new Vector2(tip.X - size, tip.Y), tip, color, thickness);
    draw.AddTriangleFilled(tip, new(tip.X - size * 0.55f, tip.Y - size * 0.42f), new(tip.X - size * 0.55f, tip.Y + size * 0.42f), color);
  }

  private static void DrawTinyArrowLeft(ImDrawListPtr draw, Vector2 tip, float size, uint color, float thickness)
  {
    draw.AddLine(tip, new(tip.X + size, tip.Y), color, thickness);
    draw.AddTriangleFilled(tip, new(tip.X + size * 0.55f, tip.Y - size * 0.42f), new(tip.X + size * 0.55f, tip.Y + size * 0.42f), color);
  }

  private static float SkirtBandBaseline(float y, float fontSize) => y + fontSize * 0.62f;

  private enum SkirtCompareOp
  {
    NotEqual,
    LessEqual,
    Equal,
    Greater,
  }

  private static float SkirtOperatorMid(float baseline, float fontSize)
  {
    float h = PlainGlyphHeight("=", fontSize);
    float top = baseline - h * 0.72f;
    return top + h * 0.45f;
  }

  private static float DrawSkirtComparison(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale,
    SkirtCompareOp op)
  {
    float gap = fontSize * 0.16f;
    float baseline = SkirtBandBaseline(y, fontSize);
    float opBaseline = baseline - fontSize * 0.08f;
    float opMid = SkirtOperatorMid(opBaseline, fontSize);
    float mathBaseline = baseline + fontSize * 0.06f;
    float glyph = fontSize * 0.98f;
    float xTop = mathBaseline - MathGlyphSize("x", glyph).Y;
    float yTop = mathBaseline - MathGlyphSize("y", glyph).Y;

    float cx = x;
    cx += DrawMathX(draw, cx, xTop, glyph, color, scale) + gap;
    cx += op switch
    {
      SkirtCompareOp.NotEqual => DrawNotEqualOp(draw, cx, opMid, fontSize, color, scale),
      SkirtCompareOp.LessEqual => DrawLessEqualOp(draw, cx, opMid, fontSize, color, scale),
      SkirtCompareOp.Equal => DrawEqualOp(draw, cx, opBaseline, fontSize, color, arialBold: false),
      SkirtCompareOp.Greater => DrawGreaterOp(draw, cx, opBaseline, fontSize, color, arialBold: false),
      _ => 0f,
    } + gap;
    cx += DrawMathY(draw, cx, yTop, glyph, color, scale);
    return cx - x;
  }

  private static bool IsSkirtComparisonLabel(string text) =>
    text is "x\u2260y" or "x\u2264y" or "x=y" or "x>y";

  private static float MeasureSkirtComparisonWidth(string text, float fontSize)
  {
    float gap = fontSize * 0.16f;
    float glyph = fontSize * 0.98f;
    float opW = text switch
    {
      "x\u2260y" or "x\u2264y" => fontSize * 0.4f,
      _ => fontSize * 0.34f,
    } + fontSize * 0.04f;
    return MathGlyphWidth("x", glyph) + gap + opW + gap + MathGlyphWidth("y", glyph);
  }

  private static float DrawLstMathX(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    bool bold,
    bool keyFaceArialBold,
    bool skirtArial,
    bool skirtBand)
  {
    float prefixW = DrawPlainRun(draw, "LST ", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
    float xSize = fontSize;
    Vector2 xDim = MathGlyphSize("X", xSize);
    float lstH = PlainGlyphHeight("LST ", fontSize);
    float xTop = y + (lstH - xDim.Y) * 0.55f;
    float xW = DrawMathGlyph(draw, "X", x + prefixW, xTop, xSize, color);
    return prefixW + xW;
  }

  private static float DrawPiLabel(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale,
    bool skirtBand)
  {
    float piSize = fontSize * 0.92f;
    if (CalcFaceplateFonts.HasPiGlyph(piSize))
    {
      Vector2 dim = CalcFaceplateFonts.MeasurePi(piSize);
      float top = skirtBand ? y + (fontSize * 0.92f - dim.Y) * 0.5f : y;
      return CalcFaceplateFonts.DrawPiTop(draw, x, top, piSize, color);
    }

    return DrawPiGlyphFallback(draw, x, y, fontSize, color, scale, skirtBand);
  }

  private static float DrawPiGlyphFallback(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale,
    bool skirtBand)
  {
    float w = fontSize * 0.58f;
    float h = fontSize * 0.46f;
    float thickness = MathF.Max(2.2f, scale * 1.55f);
    float barY = skirtBand ? y + (fontSize * 0.92f - h) * 0.5f + h * 0.08f : y + h * 0.08f;
    float legBottom = barY + h * 0.9f;
    float leftLeg = x + w * 0.2f;
    float rightLeg = x + w * 0.8f;
    draw.AddLine(new Vector2(leftLeg, barY), new Vector2(leftLeg, legBottom), color, thickness);
    draw.AddLine(new Vector2(rightLeg, barY), new Vector2(rightLeg, legBottom), color, thickness);
    draw.AddLine(new Vector2(x + w * 0.08f, barY), new Vector2(x + w * 0.92f, barY), color, thickness);
    draw.AddLine(new Vector2(leftLeg, barY), new Vector2(leftLeg, legBottom), color, thickness * 0.55f);
    draw.AddLine(new Vector2(rightLeg, barY), new Vector2(rightLeg, legBottom), color, thickness * 0.55f);
    draw.AddLine(new Vector2(x + w * 0.08f, barY), new Vector2(x + w * 0.92f, barY), color, thickness * 0.55f);
    return w;
  }

  private static float DrawNotEqualOp(ImDrawListPtr draw, float x, float mid, float fontSize, uint color, float scale)
  {
    float thickness = MathF.Max(1f, scale * 0.95f);
    float w = fontSize * 0.36f;
    draw.AddLine(new Vector2(x, mid - fontSize * 0.1f), new Vector2(x + w, mid - fontSize * 0.1f), color, thickness);
    draw.AddLine(new Vector2(x, mid + fontSize * 0.1f), new Vector2(x + w, mid + fontSize * 0.1f), color, thickness);
    draw.AddLine(new Vector2(x + w * 0.15f, mid + fontSize * 0.22f), new Vector2(x + w * 0.85f, mid - fontSize * 0.22f), color, thickness);
    return w + fontSize * 0.04f;
  }

  private static float DrawLessEqualOp(ImDrawListPtr draw, float x, float mid, float fontSize, uint color, float scale)
  {
    float thickness = MathF.Max(1f, scale * 0.95f);
    float w = fontSize * 0.36f;
    draw.AddLine(new Vector2(x + w, mid - fontSize * 0.18f), new Vector2(x, mid), color, thickness);
    draw.AddLine(new Vector2(x, mid), new Vector2(x + w, mid + fontSize * 0.18f), color, thickness);
    draw.AddLine(new Vector2(x, mid + fontSize * 0.2f), new Vector2(x + w, mid + fontSize * 0.2f), color, thickness);
    return w + fontSize * 0.04f;
  }

  private static float DrawEqualOp(ImDrawListPtr draw, float x, float baseline, float fontSize, uint color, bool arialBold = true)
  {
    float h = PlainGlyphHeight("=", fontSize);
    float top = baseline - h * 0.72f;
  return arialBold
      ? DrawArialBoldGlyph(draw, "=", x, top, fontSize, color) + fontSize * 0.04f
      : DrawPlainGlyph(draw, "=", x, top, fontSize, color) + fontSize * 0.04f;
  }

  private static float DrawGreaterOp(ImDrawListPtr draw, float x, float baseline, float fontSize, uint color, bool arialBold = true)
  {
    float h = PlainGlyphHeight(">", fontSize);
    float top = baseline - h * 0.72f;
    return arialBold
      ? DrawArialBoldGlyph(draw, ">", x, top, fontSize, color) + fontSize * 0.04f
      : DrawPlainGlyph(draw, ">", x, top, fontSize, color) + fontSize * 0.04f;
  }

  private static float BodyBandBaseline(float y, float fontSize) => y + fontSize * 0.55f;

  private static float DrawBandMathX(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand, bool bodyBand)
  {
    float xSize = fontSize * 0.95f;
    float top = skirtBand
      ? SkirtBandBaseline(y, fontSize) - MathGlyphSize("x", xSize).Y
      : bodyBand
        ? BodyBandBaseline(y, fontSize) - MathGlyphSize("x", xSize).Y
        : y;
    return DrawMathX(draw, x, top, xSize, color, scale);
  }

  private static float DrawBandMathY(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand, bool bodyBand)
  {
    float ySize = fontSize * 0.95f;
    float top = skirtBand
      ? SkirtBandBaseline(y, fontSize) - MathGlyphSize("y", ySize).Y
      : bodyBand
        ? BodyBandBaseline(y, fontSize) - MathGlyphSize("y", ySize).Y
        : y;
    return DrawMathY(draw, x, top, ySize, color, scale);
  }

  private static float DrawBandMathUpperX(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand, bool bodyBand)
  {
    float xSize = fontSize * 0.95f;
    float top = skirtBand
      ? SkirtBandBaseline(y, fontSize) - MathGlyphSize("X", xSize).Y
      : bodyBand
        ? BodyBandBaseline(y, fontSize) - MathGlyphSize("X", xSize).Y
        : y;
    return DrawMathGlyph(draw, "X", x, top, xSize, color);
  }

  private static float DrawSkirtMathX(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand) =>
    DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bodyBand: false);

  private static float DrawSkirtMathY(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand) =>
    DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bodyBand: false);

  private static float DrawNotEqual(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand = false, bool bodyBand = false)
  {
    float thickness = MathF.Max(1f, scale * 0.95f);
    float w = fontSize * 0.34f;
    float mid = skirtBand
      ? SkirtBandBaseline(y, fontSize) - fontSize * 0.18f
      : bodyBand
        ? BodyBandBaseline(y, fontSize) - fontSize * 0.18f
        : y - fontSize * 0.36f;
    draw.AddLine(new(x, mid - fontSize * 0.1f), new(x + w, mid - fontSize * 0.1f), color, thickness);
    draw.AddLine(new(x, mid + fontSize * 0.1f), new(x + w, mid + fontSize * 0.1f), color, thickness);
    draw.AddLine(new(x + w * 0.15f, mid + fontSize * 0.22f), new(x + w * 0.85f, mid - fontSize * 0.22f), color, thickness);
    return w + fontSize * 0.06f;
  }

  private static float DrawLessEqual(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool skirtBand = false, bool bodyBand = false)
  {
    float thickness = MathF.Max(1f, scale * 0.95f);
    float w = fontSize * 0.34f;
    float mid = skirtBand
      ? SkirtBandBaseline(y, fontSize) - fontSize * 0.18f
      : bodyBand
        ? BodyBandBaseline(y, fontSize) - fontSize * 0.18f
        : y - fontSize * 0.36f;
    draw.AddLine(new(x + w, mid - fontSize * 0.18f), new(x, mid), color, thickness);
    draw.AddLine(new(x, mid), new(x + w, mid + fontSize * 0.18f), color, thickness);
    draw.AddLine(new(x, mid + fontSize * 0.2f), new(x + w, mid + fontSize * 0.2f), color, thickness);
    return w + fontSize * 0.06f;
  }

  private static float DrawSuperscriptMinusOne(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale,
    bool bold,
    bool keyFaceArialBold = false,
    bool skirtArial = false,
    bool skirtBand = false,
    float nudgeRight = 0f,
    float nudgeDown = 0f)
  {
    float super = fontSize * 0.68f;
    float superY = y - fontSize * 0.22f + nudgeDown;
    float startX = x + nudgeRight;
    float w = DrawPlainRun(draw, "-", startX, superY, super, color, bold, keyFaceArialBold, skirtArial);
    w += DrawPlainRun(draw, "1", startX + w, superY, super, color, bold, keyFaceArialBold, skirtArial);
    return w + nudgeRight;
  }

  private static void DrawRWithInlineUpArrow(
    ImDrawListPtr draw,
    float x,
    float y,
    float fontSize,
    uint color,
    float scale)
  {
    float rW = DrawArialBoldGlyph(draw, "R", x, y, fontSize, color);
    float arrowX = x + rW + fontSize * 0.04f;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    DrawInlineUpArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
  }

  private static float DrawInlineUpArrow(
    ImDrawListPtr draw,
    float x,
    float capTop,
    float capBottom,
    float fontSize,
    uint color,
    float scale)
  {
    float capHeight = capBottom - capTop;
    float w = fontSize * 0.3f;
    float thickness = MathF.Max(3.2f, scale * 2.25f);
    float cx = x + w * 0.5f;
    float arrowHeight = capHeight * 0.88f;
    float arrowBottom = capBottom - (capHeight - arrowHeight) * 0.5f;
    float tipY = arrowBottom - arrowHeight;
    float headBottom = tipY + arrowHeight * 0.38f;

    draw.AddLine(new Vector2(cx, arrowBottom), new Vector2(cx, headBottom), color, thickness);
    draw.AddTriangleFilled(new Vector2(cx, tipY), new Vector2(x, headBottom), new Vector2(x + w, headBottom), color);
    return w + fontSize * 0.03f;
  }

  private static float DrawPlainRun(
    ImDrawListPtr draw,
    string text,
    float x,
    float y,
    float fontSize,
    uint color,
    bool bold,
    bool keyFaceArialBold = false,
    bool skirtArial = false,
    bool skirtBand = false,
    bool bodyBand = false)
  {
    float drawY = bodyBand
      ? y + fontSize * 0.08f
      : skirtBand && text is "=" or ">"
        ? SkirtBandBaseline(y, fontSize) - PlainGlyphHeight(text, fontSize) * 0.72f
        : y;
    if (skirtArial)
    {
      return DrawArialBoldGlyph(draw, text, x, drawY, fontSize, color);
    }

    if (keyFaceArialBold || IsRegisterRowLetter(text))
    {
      return DrawArialBoldGlyph(draw, text, x, drawY, fontSize, color);
    }

    return DrawTextRun(draw, text, x, drawY, fontSize, color, bold);
  }

  private static float DrawTextRun(
    ImDrawListPtr draw,
    string text,
    float x,
    float y,
    float fontSize,
    uint color,
    bool bold)
  {
    if (bold && CalcFaceplateFonts.IsReady)
    {
      return CalcFaceplateFonts.DrawSansTop(draw, text, x, y, fontSize, color);
    }

    if (bold)
    {
      CalcFaceplateTypography.DrawBoldText(draw, text, new Vector2(x, y), fontSize, color);
    }
    else
    {
      draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), color, text);
    }

    return ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text).X;
  }

  private static float MeasureWidth(string text, float fontSize, bool keyFaceArialBold = false, bool skirtArial = false)
  {
    bool useArial = keyFaceArialBold || skirtArial;
    float width = 0f;
    int i = 0;
    while (i < text.Length)
    {
      if (TryConsume(text, ref i, "x\u2194y", out _)) { width += fontSize * 2.05f; continue; }
      if (TryConsume(text, ref i, "x\u2260y", out _)) { width += fontSize * 2.05f; continue; }
      if (TryConsume(text, ref i, "x\u2264y", out _)) { width += fontSize * 2.05f; continue; }
      if (TryConsume(text, ref i, "x=y", out _)) { width += fontSize * 1.95f; continue; }
      if (TryConsume(text, ref i, "x>y", out _)) { width += fontSize * 1.95f; continue; }
      if (TryConsume(text, ref i, "x!/y", out _)) { width += fontSize * 1.8f; continue; }
      if (TryConsume(text, ref i, "\u221ax", out _) || TryConsume(text, ref i, "√x", out _)) { width += fontSize * 1.15f; continue; }
      if (TryConsume(text, ref i, "\u03c0", out _) || TryConsume(text, ref i, "π", out _))
      {
        float piSize = fontSize * 0.92f;
        width += CalcFaceplateFonts.HasPiGlyph(piSize)
          ? CalcFaceplateFonts.MeasurePi(piSize).X
          : fontSize * 0.58f;
        continue;
      }
      if (TryConsume(text, ref i, "LST X", out _)) { width += PlainGlyphWidth("LST ", fontSize) + MathGlyphWidth("X", fontSize); continue; }
      if (TryConsume(text, ref i, "y^x", out _)) { width += fontSize * 1.25f; continue; }
      if (TryConsume(text, ref i, "1/x", out _)) { width += fontSize * 1.35f; continue; }
      if (TryConsume(text, ref i, "R\u2192P", out _)) { width += fontSize * 1.55f; continue; }
      if (TryConsume(text, ref i, "R\u2191", out _)) { width += fontSize * 1.2f; continue; }
      if (TryConsume(text, ref i, "R\u2193", out _)) { width += fontSize * 1.2f; continue; }
      if (TryConsume(text, ref i, "\u2192D.MS", out _)) { width += fontSize * 2.1f; continue; }
      if (TryConsume(text, ref i, "\u2192OCT", out _)) { width += fontSize * 2.0f; continue; }
      if (TryConsume(text, ref i, "f\u207b\u00b9", out _) || TryConsume(text, ref i, "f⁻¹", out _)) { width += fontSize * 1.05f; continue; }
      if (text[i] == 'x' || text[i] == 'y')
      {
        width += CalcFaceplateFonts.IsMathReady
          ? CalcFaceplateFonts.MathWidth(text[i].ToString(), fontSize)
          : fontSize * 0.48f;
        i++;
        continue;
      }
      int start = i;
      while (i < text.Length && text[i] is not ('x' or 'y') && !IsPatternStart(text, i))
      {
        i++;
      }

      string run = text[start..i];
      if (run.Length > 0)
      {
        width += useArial || IsRegisterRowLetter(run)
          ? ArialBoldGlyphWidth(run, fontSize)
          : CalcFaceplateFonts.IsReady
            ? CalcFaceplateFonts.SansWidth(run, fontSize)
            : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, run).X;
      }
    }

    return width;
  }

  private static float MeasureSkirtHeight(string text, float fontSize) =>
    fontSize * 0.96f;

  private static float MeasureKeyFaceHeight(string text, float fontSize)
  {
    if (text is "f\u207b\u00b9" or "f⁻¹")
    {
      return fontSize * 0.92f;
    }

    if (text == "g")
    {
      return fontSize * 0.82f;
    }

    if (!ContainsKeyFaceGlyphPattern(text))
    {
      return PlainArialBoldSize(text, fontSize).Y;
    }

    return fontSize * 1.05f;
  }

  private static Vector2 PlainArialBoldSize(string text, float size) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, size)
      : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text);

  private static bool ContainsKeyFaceGlyphPattern(string text) =>
    text.Contains('x')
    || text.Contains('y')
    || text.Contains('\u221a')
    || text.Contains('√')
    || text.Contains('\u2192')
    || text.Contains('\u2191')
    || text.Contains('\u2193')
    || text.Contains('\u2194')
    || text.Contains('\u2260')
    || text.Contains('\u2264')
    || text.Contains('=')
    || text.Contains('>')
    || text.Contains("!/");

  private static bool IsPatternStart(string text, int index)
  {
    ReadOnlySpan<char> tail = text.AsSpan(index);
    return tail.StartsWith("x\u2194y")
      || tail.StartsWith("x\u2260y")
      || tail.StartsWith("x\u2264y")
      || tail.StartsWith("x=y")
      || tail.StartsWith("x>y")
      || tail.StartsWith("x!/y")
      || tail.StartsWith("\u221ax")
      || tail.StartsWith("√x")
      || tail.StartsWith("y^x")
      || tail.StartsWith("1/x")
      || tail.StartsWith("R\u2192P")
      || tail.StartsWith("R\u2191")
      || tail.StartsWith("R\u2193")
      || tail.StartsWith("\u2192D.MS")
      || tail.StartsWith("\u2192OCT")
      || tail.StartsWith("LST X")
      || tail.StartsWith("f\u207b\u00b9")
      || tail.StartsWith("f⁻¹")
      || tail.StartsWith("\u03c0")
      || tail.StartsWith("π");
  }

  private static bool TryConsume(string text, ref int index, string pattern, out string _)
  {
    if (text.AsSpan(index).StartsWith(pattern))
    {
      index += pattern.Length;
      _ = pattern;
      return true;
    }

    _ = pattern;
    return false;
  }
}
