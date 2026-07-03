using System.Numerics;
using ImGuiNET;

namespace TeoCalc.Rendering;

/// <summary>Vector-drawn HP Classic faceplate symbols (math italic x/y, arrows, radicals).</summary>
public static class HpClassicFaceplateGlyphs
{
  public readonly record struct LabelSize(float Width, float Height);

  public static Vector2 CardSlotLabelDrawCenter(int column, Vector2 slotCenter, float fontSize, float scale)
  {
    float y = slotCenter.Y + (column is 1 or 3 ? fontSize * 0.055f : 0f);
    float x = slotCenter.X;
    float xSize = fontSize * CardSlotMathXScale;
    Vector2 xDim = MathGlyphSize("x", xSize);

    switch (column)
    {
      case 1:
      {
        CardSlotSqrtArt.RadicalLayout layout = CardSlotSqrtArt.LayoutForX(x, xDim.X, xDim.Y, fontSize);
        float visualMid = (layout.SqrtLeft + layout.XLeft + xDim.X) * 0.5f;
        x = slotCenter.X + (slotCenter.X - visualMid);
        break;
      }
    }

    return new Vector2(x, y);
  }

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
        DrawYToTheX(draw, center, fontSize, color, scale);
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
      2 => new(fontSize * 1.25f, fontSize),
      3 => new(ArialBoldGlyphWidth("R", fontSize * 0.82f) + fontSize * 0.24f, fontSize * 0.82f),
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

  public static LabelSize MeasureKeyFaceLabel(string text, float fontSize)
  {
    if (text == "\u00d7")
    {
      return MeasureKeyFaceMultiply(fontSize * 1.22f);
    }

    if (text == "CLX")
    {
      return new(
        PlainGlyphWidth("CL", fontSize) + MathGlyphWidth("X", fontSize),
        fontSize * 0.92f);
    }

    if (!ContainsKeyFaceGlyphPattern(text))
    {
      if (IsPlainArialKeyFaceLabel(text))
      {
        CalcFaceplateFonts.FontInkBounds ink = CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize);
        return new(ink.Width, ink.Height);
      }

      return new(
        MeasureWidth(text, fontSize, keyFaceArialBold: true),
        MeasureKeyFaceHeight(text, fontSize));
    }

    return new(MeasureWidth(text, fontSize, keyFaceArialBold: true), MeasureKeyFaceHeight(text, fontSize));
  }

  public static LabelSize MeasureSkirtLabel(string text, float fontSize)
  {
    CalcFaceplateBandLabel.LayoutBox box = MeasureSkirtLayoutBox(text, fontSize);
    return new(box.Width, box.Height);
  }

  public static CalcFaceplateBandLabel.LayoutBox MeasureSkirtLayoutBox(string text, float fontSize)
  {
    if (IsSkirtComparisonLabel(text))
    {
      return MeasureComparisonLayoutBox(text, fontSize);
    }

    if (IsPlainArialSkirtLabel(text))
    {
      return CalcFaceplateBandLabel.FromInk(CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize));
    }

    return MeasureCompositeLayoutBox(text, fontSize, skirtArial: true);
  }

  public static CalcFaceplateBandLabel.LayoutBox MeasureFaceLayoutBox(string text, float fontSize)
  {
    if (text == "\u00d7")
    {
      LabelSize size = MeasureKeyFaceMultiply(fontSize * 1.22f);
      return CalcFaceplateBandLabel.BoxAt(0f, 0f, size.Width, size.Height);
    }

    if (!ContainsKeyFaceGlyphPattern(text) && IsPlainArialKeyFaceLabel(text))
    {
      return CalcFaceplateBandLabel.FromInk(CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize));
    }

    return MeasureCompositeLayoutBox(text, fontSize, skirtArial: false, keyFaceArialBold: true);
  }

  private static CalcFaceplateBandLabel.LayoutBox MeasureComparisonLayoutBox(string text, float fontSize)
  {
    float gap = fontSize * 0.19f;
    float glyphSize = fontSize * 1.24f;
    Vector2 xDim = MathGlyphSize("x", glyphSize);
    Vector2 yDim = MathGlyphSize("y", glyphSize);
    float opW = text is "x\u2260y" or "x\u2264y" ? fontSize * 0.56f : fontSize * 0.5f;
    float opH = fontSize * 0.46f;
    float rowH = MathF.Max(xDim.Y, MathF.Max(opH, yDim.Y));
    float width = xDim.X + gap + opW + gap + yDim.X;
    return CalcFaceplateBandLabel.BoxAt(0f, 0f, width, rowH);
  }

  private static CalcFaceplateBandLabel.LayoutBox MeasureCompositeLayoutBox(
    string text,
    float fontSize,
    bool skirtArial,
    bool keyFaceArialBold = false)
  {
    float rowMidY = fontSize * 0.5f;
    float x = 0f;
    CalcFaceplateBandLabel.LayoutBox union = default;
    int i = 0;
    while (i < text.Length)
    {
      if (TryConsume(text, ref i, "CLX", out _))
      {
        float w = PlainGlyphWidth("CL", fontSize) + MathGlyphWidth("X", fontSize);
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.46f, w, fontSize * 0.92f));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "LST X", out _))
      {
        float w = PlainGlyphWidth("LST ", fontSize) + MathGlyphWidth("X", fontSize);
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.5f, w, fontSize));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "x\u2194y", out _))
      {
        float xSize = fontSize * 1.08f;
        float w = MathGlyphWidth("x", xSize) + CardSlotExchangeArt.MeasureWidth(fontSize) + MathGlyphWidth("y", xSize) + fontSize * 0.28f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.55f, w, fontSize * 1.1f));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "1/x", out _))
      {
        float w = fontSize * 1.72f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.6f, w, fontSize * 1.2f));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "y^x", out _))
      {
        float w = fontSize * 1.62f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.6f, w, fontSize * 1.2f));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "R\u2191", out _) || TryConsume(text, ref i, "R\u2193", out _))
      {
        float w = fontSize * 1.47f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.52f, w, fontSize * 1.04f));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "\u03c0", out _) || TryConsume(text, ref i, "π", out _))
      {
        float piSize = fontSize * 0.92f;
        Vector2 dim = CalcFaceplateFonts.HasPiGlyph(piSize)
          ? CalcFaceplateFonts.MeasurePi(piSize)
          : new Vector2(fontSize * 0.58f, fontSize * 0.46f);
        union = CalcFaceplateBandLabel.Union(
          union,
          CalcFaceplateBandLabel.BoxAt(x, rowMidY - dim.Y * 0.5f, dim.X, dim.Y));
        x += dim.X;
        continue;
      }

      if (TryConsume(text, ref i, "\u221ax", out _) || TryConsume(text, ref i, "√x", out _))
      {
        float w = fontSize * 1.15f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.5f, w, fontSize));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "R\u2192P", out _))
      {
        float w = fontSize * 1.75f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.5f, w, fontSize));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "\u2192D.MS", out _) || TryConsume(text, ref i, "\u2192OCT", out _))
      {
        float w = fontSize * 2.15f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.5f, w, fontSize));
        x += w;
        continue;
      }

      if (TryConsume(text, ref i, "f\u207b\u00b9", out _) || TryConsume(text, ref i, "f⁻¹", out _))
      {
        float w = fontSize * 1.05f;
        union = CalcFaceplateBandLabel.Union(union, CalcFaceplateBandLabel.BoxAt(x, rowMidY - fontSize * 0.5f, w, fontSize));
        x += w;
        continue;
      }

      int start = i;
      while (i < text.Length && text[i] is not ('x' or 'y') && !IsPatternStart(text, i))
      {
        i++;
      }

      string run = text[start..i];
      if (run.Length == 0)
      {
        if (i < text.Length && text[i] is 'x' or 'y')
        {
          string glyph = text[i].ToString();
          i++;
          Vector2 dim = MathGlyphSize(glyph, fontSize);
          union = CalcFaceplateBandLabel.Union(
            union,
            CalcFaceplateBandLabel.BoxAt(x, rowMidY - dim.Y * 0.5f, dim.X, dim.Y));
          x += dim.X;
        }
        else if (i < text.Length)
        {
          i++;
        }

        continue;
      }

      CalcFaceplateFonts.FontInkBounds ink = CalcFaceplateFonts.MeasureArialBoldInk(run, fontSize);
      union = CalcFaceplateBandLabel.Union(
        union,
        CalcFaceplateBandLabel.BoxAt(x + ink.Left, rowMidY - ink.InkMidY + ink.Top, ink.Width, ink.Height));
      x += skirtArial || keyFaceArialBold
        ? ArialBoldGlyphWidth(run, fontSize)
        : SansGlyphWidth(run, fontSize);
    }

    if (union.Width <= 0f)
    {
      return CalcFaceplateBandLabel.BoxAt(0f, 0f, MeasureWidth(text, fontSize, keyFaceArialBold, skirtArial), fontSize * 0.96f);
    }

    return union;
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
      Draw(draw, topLeft, text, fontSize, color, scale, bold: false, skirtArial: true, skirtBand: true);
      return;
    }

    Draw(draw, topLeft, text, fontSize, color, scale, bold: true, skirtArial: true, skirtBand: true);
  }

  public static Vector2 SkirtLabelTopLeft(Vector2 skirtMin, Vector2 skirtMax, string text, float fontSize) =>
    CalcFaceplateBandLabel.TopLeftForBand(skirtMin, skirtMax, MeasureSkirtLayoutBox(text, fontSize));

  public static void DrawKeyFaceLabelInRect(
    ImDrawListPtr draw,
    Vector2 bandMin,
    Vector2 bandMax,
    string text,
    float fontSize,
    uint color,
    float scale)
  {
    Vector2 bandCenter = KeyCapGeometry.BandCenter(bandMin, bandMax);
    if (text == "\u00d7")
    {
      DrawKeyFaceMultiply(draw, bandCenter, fontSize, color, scale);
      return;
    }

    if (!ContainsKeyFaceGlyphPattern(text) && IsPlainArialKeyFaceLabel(text))
    {
      float bias = ContainsDescender(text) ? -0.1f : 0f;
      Vector2 topLeft = CalcFaceplateFonts.ArialBoldTopLeftForBand(bandMin, bandMax, text, fontSize, verticalBiasRatio: bias);
      DrawArialBoldGlyph(draw, text, topLeft.X, topLeft.Y, fontSize, color);
      return;
    }

    CalcFaceplateBandLabel.LayoutBox box = MeasureFaceLayoutBox(text, fontSize);
    Vector2 topLeftComposite = CalcFaceplateBandLabel.TopLeftForBand(bandMin, bandMax, box);
    Draw(
      draw,
      topLeftComposite,
      text,
      fontSize,
      color,
      scale,
      bold: false,
      keyFaceArialBold: true,
      rowMidY: bandCenter.Y,
      useRowMid: true);
  }

  public static void DrawSkirtLabelInRect(
    ImDrawListPtr draw,
    Vector2 bandMin,
    Vector2 bandMax,
    string text,
    float fontSize,
    uint color,
    float scale)
  {
    Vector2 bandCenter = KeyCapGeometry.BandCenter(bandMin, bandMax);
    if (IsSkirtComparisonLabel(text))
    {
      DrawComparisonRowAtCenter(draw, bandCenter, text, fontSize, color, scale);
      return;
    }

    if (IsPlainArialSkirtLabel(text))
    {
      Vector2 topLeft = CalcFaceplateFonts.ArialBoldTopLeftForBand(bandMin, bandMax, text, fontSize, verticalBiasRatio: 0f);
      DrawArialBoldGlyph(draw, text, topLeft.X, topLeft.Y, fontSize, color);
      return;
    }

    CalcFaceplateBandLabel.LayoutBox box = MeasureSkirtLayoutBox(text, fontSize);
    Vector2 topLeftComposite = CalcFaceplateBandLabel.TopLeftForBand(bandMin, bandMax, box);
    Draw(
      draw,
      topLeftComposite,
      text,
      fontSize,
      color,
      scale,
      bold: true,
      skirtArial: true,
      skirtBand: true,
      rowMidY: bandCenter.Y,
      useRowMid: true);
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
    float scale)
  {
    if (text == "\u00d7")
    {
      LabelSize box = MeasureKeyFaceMultiply(fontSize);
      Vector2 center = topLeft + new Vector2(box.Width * 0.5f, box.Height * 0.5f);
      DrawKeyFaceMultiply(draw, center, fontSize, color, scale);
      return;
    }

    Draw(draw, topLeft, text, fontSize, color, scale, bold: false, keyFaceArialBold: true);
  }

  public static void DrawKeyFaceMultiply(ImDrawListPtr draw, Vector2 center, float size, uint color, float scale)
  {
    float span = size * 0.56f;
    float stroke = MathF.Max(2.5f, scale * 2.3f);
    draw.AddLine(center + new Vector2(-span * 0.5f, -span * 0.5f), center + new Vector2(span * 0.5f, span * 0.5f), color, stroke);
    draw.AddLine(center + new Vector2(-span * 0.5f, span * 0.5f), center + new Vector2(span * 0.5f, -span * 0.5f), color, stroke);
  }

  public static LabelSize MeasureKeyFaceMultiply(float size) => new(size * 0.82f, size * 0.82f);

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
    float mathYOffset = 0f,
    float rowMidY = 0f,
    bool useRowMid = false)
  {
    float x = topLeft.X;
    float y = topLeft.Y;
    bool bandMode = skirtBand || bandAlign;
    int i = 0;
    while (i < text.Length)
    {
      if (TryConsume(text, ref i, "CLX", out _))
      {
        x += DrawPlainRun(draw, "CL", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
        float xSize = fontSize;
        Vector2 xDim = MathGlyphSize("X", xSize);
        float xTop = y + (fontSize * 0.92f - xDim.Y) * 0.5f;
        x += DrawMathGlyph(draw, "X", x, xTop, xSize, color);
        continue;
      }

      if (TryConsume(text, ref i, "x\u2194y", out _))
      {
        float top = useRowMid ? rowMidY - fontSize * 0.55f : y + mathYOffset;
        x += DrawXExchangeYAt(draw, x, top, fontSize, color, scale, widen: useRowMid);
        continue;
      }

      if (TryConsume(text, ref i, "LST X", out _))
      {
        x += DrawLstMathX(draw, x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "x\u2260y", out _))
      {
        x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawNotEqual(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        continue;
      }

      if (TryConsume(text, ref i, "x\u2264y", out _))
      {
        x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawLessEqual(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        continue;
      }

      if (TryConsume(text, ref i, "x=y", out _))
      {
        x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawPlainRun(draw, "=", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
        x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        continue;
      }

      if (TryConsume(text, ref i, "x>y", out _))
      {
        x += DrawBandMathX(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
        x += DrawPlainRun(draw, ">", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign);
        x += DrawBandMathY(draw, x, y, fontSize, color, scale, skirtBand, bandAlign);
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
        float h = MeasureCardSlotLabel(1, fontSize).Height;
        float top = useRowMid ? rowMidY - h * 0.5f : y + (bandMode ? fontSize * 0.04f : 0f);
        x += DrawSqrtXAt(draw, x, top, fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "y^x", out _))
      {
        float drawFont = useRowMid ? fontSize * 1.12f : fontSize;
        float h = MeasureCardSlotLabel(2, drawFont).Height;
        float top = useRowMid ? rowMidY - h * 0.5f : y;
        x += DrawYToTheXAt(draw, x, top, drawFont, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "1/x", out _))
      {
        float drawFont = useRowMid ? fontSize * 1.12f : fontSize;
        float h = MeasureCardSlotLabel(0, drawFont).Height;
        float top = useRowMid ? rowMidY - h * 0.5f : y;
        x += DrawInverseXAt(draw, x, top, drawFont, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "R\u2192P", out _))
      {
        x += DrawPlainRun(draw, "R", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        x += fontSize * 0.1f;
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += fontSize * 0.1f;
        x += DrawPlainRun(draw, "P", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "R\u2191", out _))
      {
        if (skirtBand || skirtArial)
        {
          float left = x;
          float top = useRowMid ? rowMidY - fontSize * 0.5f : y;
          x += DrawRWithInlineUpArrowWidth(draw, left, top, fontSize, color, scale);
        }
        else
        {
          x += DrawPlainRun(draw, "R", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign, rowMidY, useRowMid);
          x += DrawArrowUp(draw, x, y, fontSize, color, scale, bandMode);
        }

        continue;
      }

      if (TryConsume(text, ref i, "R\u2193", out _))
      {
        float left = x;
        float top = useRowMid ? rowMidY - fontSize * 0.5f : y;
        x += DrawRWithInlineDownArrowWidth(draw, left, top, fontSize, color, scale);
        continue;
      }

      if (TryConsume(text, ref i, "\u2192D.MS", out _))
      {
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += fontSize * 0.1f;
        x += DrawPlainRun(draw, "D.MS", x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand);
        continue;
      }

      if (TryConsume(text, ref i, "\u2192OCT", out _))
      {
        x += DrawArrowRight(draw, x, y, fontSize, color, scale, bandMode);
        x += fontSize * 0.1f;
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
        float piSize = fontSize * 0.92f;
        Vector2 dim = CalcFaceplateFonts.HasPiGlyph(piSize)
          ? CalcFaceplateFonts.MeasurePi(piSize)
          : new Vector2(fontSize * 0.58f, fontSize * 0.46f);
        float top = useRowMid ? rowMidY - dim.Y * 0.5f : y;
        x += DrawPiLabel(draw, x, top, fontSize, color, scale, skirtBand);
        continue;
      }

      if (text[i] == 'x')
      {
        i++;
        Vector2 dim = MathGlyphSize("x", fontSize);
        float top = useRowMid ? rowMidY - dim.Y * 0.5f : y + mathYOffset;
        x += DrawMathX(draw, x, top, fontSize, color, scale);
        continue;
      }

      if (text[i] == 'y')
      {
        i++;
        Vector2 dim = MathGlyphSize("y", fontSize);
        float top = useRowMid ? rowMidY - dim.Y * 0.5f : y + mathYOffset;
        x += DrawMathY(draw, x, top, fontSize, color, scale);
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
        x += DrawPlainRun(draw, run, x, y, fontSize, color, bold, keyFaceArialBold, skirtArial, skirtBand, bandAlign, rowMidY, useRowMid);
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
    fontSize *= 0.86f;
    float width = ArialBoldGlyphWidth("R", fontSize) + fontSize * 0.38f;
    float left = center.X - width * 0.5f;
    float top = center.Y - fontSize * 0.5f;
    DrawRWithInlineDownArrow(draw, left, top, fontSize, color, scale);
  }

  private static void DrawXExchangeY(ImDrawListPtr draw, Vector2 center, float fontSize, uint color, float scale)
  {
    float xSize = fontSize * CardSlotMathXScale;
    Vector2 xDim = MathGlyphSize("x", xSize);
    float gap = fontSize * 0.26f;
    float width = MathGlyphWidth("x", xSize) + gap + CardSlotExchangeArt.MeasureWidth(fontSize) + gap + MathGlyphWidth("y", xSize);
    float left = center.X - width * 0.5f;
    float top = center.Y - xDim.Y * 0.5f;
    DrawXExchangeYAt(draw, left, top, fontSize, color, scale, widen: true);
  }

  private static float CardSlotMathXTop(Vector2 center, float fontSize, float xHeight) =>
    center.Y - xHeight * 0.5f;

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
    float xSize = fontSize * CardSlotMathXScale;
    Vector2 xDim = MathGlyphSize("x", xSize);
    Vector2 yDim = MathGlyphSize("y", fontSize);
    float xTop = CardSlotMathXTop(center, fontSize, xDim.Y);
    float rowBottom = xTop + xDim.Y;
    float yTop = rowBottom - yDim.Y;
    float totalW = MathGlyphWidth("y", fontSize) + fontSize * 0.04f + MathGlyphWidth("x", xSize);
    float left = center.X - totalW * 0.5f;

    float yW = DrawCardSlotMathY(draw, left, yTop, fontSize, color);
    float xX = left + yW + fontSize * 0.04f;
    float xY = xTop;
    DrawCardSlotMathX(draw, xX, xY, xSize, color);
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
    float arrowX = x + rW + fontSize * 0.20f;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    DrawInlineDownArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
  }

  private static float DrawInverseXAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale)
  {
    LabelSize box = MeasureCardSlotLabel(0, fontSize);
    box = new LabelSize(fontSize * 1.52f, fontSize * 1.1f);
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
    box = new LabelSize(fontSize * 1.42f, fontSize * 1.1f);
    DrawExponentYx(draw, new Vector2(x + box.Width * 0.5f, y + box.Height * 0.5f), box, fontSize, color, scale);
    return box.Width;
  }

  private static float DrawXExchangeYAt(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool widen = false)
  {
    float xSize = fontSize * CardSlotMathXScale * (widen ? 1.08f : 1f);
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

  public static float DrawArialBoldGlyph(ImDrawListPtr draw, string text, float x, float y, float size, uint color)
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
    float w = fontSize * 0.28f;
    DrawSvgArrow(draw, new Vector2(x + w * 0.5f, (capTop + capBottom) * 0.5f), MathF.Min(capHeight, fontSize * 0.58f), ArrowDirection.Down, color);
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
    float gap = fontSize * 0.26f;
    float arrowX = x + rW + gap;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    float arrowW = DrawInlineDownArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
    return rW + gap + arrowW;
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
    float gap = fontSize * 0.14f;
    float arrowX = x + rW + gap;
    float capTop = y + fontSize * 0.15f;
    float capBottom = y + fontSize * (SansCapHeightRatio + 0.12f);
    float arrowW = DrawInlineUpArrow(draw, arrowX, capTop, capBottom, fontSize, color, scale);
    return rW + gap + arrowW;
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
    float w = fontSize * 0.42f;
    DrawSvgArrow(draw, new Vector2(x + w * 0.5f, y - fontSize * 0.2f), fontSize * 0.62f, ArrowDirection.Down, color);
    return w + fontSize * 0.06f;
  }

  private static float DrawArrowUp(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool bandMode = false)
  {
    float w = fontSize * 0.42f;
    float mid = bandMode ? y + fontSize * 0.5f : y - fontSize * 0.2f;
    DrawSvgArrow(draw, new Vector2(x + w * 0.5f, mid), fontSize * 0.62f, ArrowDirection.Up, color);
    return w + fontSize * 0.06f;
  }

  private static float DrawArrowRight(ImDrawListPtr draw, float x, float y, float fontSize, uint color, float scale, bool bandMode = false)
  {
    float w = fontSize * 0.58f;
    float midY = bandMode ? y + fontSize * 0.5f : y - fontSize * 0.2f;
    DrawSvgArrow(draw, new Vector2(x + w * 0.5f, midY), fontSize * 0.62f, ArrowDirection.Right, color);
    return w + fontSize * 0.04f;
  }

  private enum ArrowDirection
  {
    Up,
    Right,
    Down,
    Left,
  }

  private static void DrawSvgArrow(ImDrawListPtr draw, Vector2 center, float size, ArrowDirection direction, uint color)
  {
    float s = size / 32f;
    Vector2 p(float x, float y)
    {
      Vector2 q = new((x - 16f) * s, (y - 16f) * s);
      return direction switch
      {
        ArrowDirection.Right => center + new Vector2(-q.Y, q.X),
        ArrowDirection.Down => center + new Vector2(-q.X, -q.Y),
        ArrowDirection.Left => center + new Vector2(q.Y, -q.X),
        _ => center + q,
      };
    }

    draw.AddTriangleFilled(p(16f, 0f), p(4f, 10f), p(28f, 10f), color);
    draw.AddQuadFilled(p(12f, 10f), p(20f, 10f), p(20f, 32f), p(12f, 32f), color);
  }

  private static void DrawTinyArrowRight(ImDrawListPtr draw, Vector2 tip, float size, uint color, float thickness)
  {
    draw.AddLine(new Vector2(tip.X - size, tip.Y), tip, color, thickness);
    float halfH = size * 0.46f;
    float baseX = tip.X - size * 0.62f;
    draw.AddTriangleFilled(tip, new(baseX, tip.Y - halfH), new(baseX, tip.Y + halfH), color);
  }

  private static void DrawTinyArrowLeft(ImDrawListPtr draw, Vector2 tip, float size, uint color, float thickness)
  {
    draw.AddLine(tip, new(tip.X + size, tip.Y), color, thickness);
    float halfH = size * 0.46f;
    float baseX = tip.X + size * 0.62f;
    draw.AddTriangleFilled(tip, new(baseX, tip.Y - halfH), new(baseX, tip.Y + halfH), color);
  }

  private static float SkirtBandBaseline(float y, float fontSize) => y + fontSize * 0.92f * 0.5f;

  private static void DrawComparisonRowAtCenter(
    ImDrawListPtr draw,
    Vector2 bandCenter,
    string text,
    float fontSize,
    uint color,
    float scale)
  {
    CalcFaceplateBandLabel.LayoutBox box = MeasureComparisonLayoutBox(text, fontSize);
    float x = bandCenter.X - box.Width * 0.5f;
    float rowMidY = bandCenter.Y;
    float gap = fontSize * 0.17f;
    float glyphSize = fontSize * 1.12f;
    Vector2 xDim = MathGlyphSize("x", glyphSize);
    Vector2 yDim = MathGlyphSize("y", glyphSize);

    x += DrawMathX(draw, x, rowMidY - xDim.Y * 0.5f, glyphSize, color, scale) + gap;
    float opMidY = rowMidY + fontSize * 0.055f;
    x += text switch
    {
      "x\u2260y" => DrawNotEqualOp(draw, x, opMidY, fontSize, color, scale),
      "x\u2264y" => DrawLessEqualOp(draw, x, opMidY, fontSize, color, scale),
      "x=y" => DrawPlainInkAtRowMid(draw, "=", x, opMidY, fontSize, color),
      "x>y" => DrawPlainInkAtRowMid(draw, ">", x, opMidY, fontSize, color),
      _ => 0f,
    } + gap;
    DrawMathY(draw, x, rowMidY - yDim.Y * 0.5f, glyphSize, color, scale);
  }

  private static float DrawPlainInkAtRowMid(
    ImDrawListPtr draw,
    string text,
    float x,
    float rowMidY,
    float fontSize,
    uint color)
  {
    CalcFaceplateFonts.FontInkBounds ink = CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize);
    return DrawArialBoldGlyph(draw, text, x, rowMidY - ink.InkMidY, fontSize, color);
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
    float arrowX = x + rW + fontSize * 0.20f;
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
    float w = fontSize * 0.28f;
    DrawSvgArrow(draw, new Vector2(x + w * 0.5f, (capTop + capBottom) * 0.5f), MathF.Min(capHeight, fontSize * 0.58f), ArrowDirection.Up, color);
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
    bool bodyBand = false,
    float rowMidY = 0f,
    bool useRowMid = false)
  {
    if (useRowMid && (skirtArial || keyFaceArialBold || IsRegisterRowLetter(text)))
    {
      return DrawPlainInkAtRowMid(draw, text, x, rowMidY, fontSize, color);
    }

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
      if (TryConsume(text, ref i, "CLX", out _)) { width += PlainGlyphWidth("CL", fontSize) + MathGlyphWidth("X", fontSize); continue; }
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
      if (TryConsume(text, ref i, "R\u2192P", out _)) { width += fontSize * 1.75f; continue; }
      if (TryConsume(text, ref i, "R\u2191", out _)) { width += fontSize * 1.2f; continue; }
      if (TryConsume(text, ref i, "R\u2193", out _)) { width += fontSize * 1.2f; continue; }
      if (TryConsume(text, ref i, "\u2192D.MS", out _)) { width += fontSize * 2.2f; continue; }
      if (TryConsume(text, ref i, "\u2192OCT", out _)) { width += fontSize * 2.1f; continue; }
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

  private static float MeasureSkirtHeight(string text, float fontSize)
  {
    if (IsSkirtComparisonLabel(text))
    {
      return fontSize * 0.96f;
    }

    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      if (IsPlainArialSkirtLabel(text))
      {
        return CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize).Height;
      }

      return PlainArialBoldSize(text, fontSize).Y;
    }

    return fontSize * 0.96f;
  }

  private static float MeasureKeyFaceHeight(string text, float fontSize)
  {
    if (text is "f\u207b\u00b9" or "f⁻¹")
    {
      return fontSize * 0.92f;
    }

    if (!ContainsKeyFaceGlyphPattern(text))
    {
      if (IsPlainArialKeyFaceLabel(text))
      {
        return CalcFaceplateFonts.MeasureArialBoldInk(text, fontSize).Height;
      }

      return PlainArialBoldSize(text, fontSize).Y;
    }

    return fontSize * 1.05f;
  }

  private static bool IsPlainArialKeyFaceLabel(string text) =>
    text is not "\u00d7"
    and not "CLX"
    and not "f\u207b\u00b9"
    and not "f⁻¹"
    && !ContainsKeyFaceGlyphPattern(text);

  private static bool ContainsDescender(string text) =>
    text.Contains('g')
    || text.Contains('j')
    || text.Contains('p')
    || text.Contains('q')
    || text.Contains('y');

  public static bool IsPlainArialSkirtLabel(string text) =>
    !IsCardSlotSkirtLabel(text) && !IsSkirtComparisonLabel(text);

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
