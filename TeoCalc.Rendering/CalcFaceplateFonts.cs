using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>
/// Faceplate fonts loaded as <b>additional</b> ImGui fonts after the default UI font.
/// Font pointers are stored per ImGui context so launcher + calculator windows can coexist.
/// </summary>
public static class CalcFaceplateFonts
{
  private const float FaceplateLoadPx = 18f;

  /// <summary>Baked size for <c>LEDcharset_class.TTF</c> (Classic LED charset).</summary>
  public const float LedDisplayLoadPx = 48f;

  private readonly record struct FontSet(
    ImFontPtr SansBold,
    ImFontPtr MathItalic,
    ImFontPtr Arial,
    ImFontPtr ArialBold,
    ImFontPtr LedDisplay,
    bool SansReady,
    bool MathReady,
    bool ArialReady,
    bool ArialBoldReady,
    bool LedReady);

  private static readonly Dictionary<nint, FontSet> ByContext = new();

  private static GCHandle _glyphRangeHandle;

  private static GCHandle _ledGlyphRangeHandle;

  private static FontSet Current =>
    ByContext.TryGetValue(ImGui.GetCurrentContext(), out FontSet set) ? set : default;

  private static nint FaceplateGlyphRanges
  {
    get
    {
      if (!_glyphRangeHandle.IsAllocated)
      {
        ushort[] ranges = [0x0020, 0x00FF, 0x03C0, 0x03C0, 0];
        _glyphRangeHandle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
      }

      return _glyphRangeHandle.AddrOfPinnedObject();
    }
  }

  private static nint LedDisplayGlyphRanges
  {
    get
    {
      if (!_ledGlyphRangeHandle.IsAllocated)
      {
        // Space, '-', '0'..'9', ';' (LED decimal). Avoid PUA range — crashes ImGui atlas build.
        ushort[] ranges = [0x0020, 0x003B, 0];
        _ledGlyphRangeHandle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
      }

      return _ledGlyphRangeHandle.AddrOfPinnedObject();
    }
  }

  public static bool IsReady => Current.SansReady;

  public static bool IsMathReady => Current.MathReady;

  public static bool IsArialReady => Current.ArialReady;

  public static bool IsArialBoldReady => Current.ArialBoldReady;

  public static bool IsLedDisplayReady => Current.LedReady;

  public const string PiGlyph = "\u03c0";

  public static bool CanDrawPiGlyph => HasPiGlyph();

  public static bool HasPiGlyph(float probeSize = FaceplateLoadPx)
  {
    FontSet fonts = Current;
    if (!fonts.ArialReady && !fonts.SansReady)
    {
      return false;
    }

    Vector2 pi = MeasurePi(probeSize);
    Vector2 reference = fonts.ArialReady
      ? MeasureArial("n", probeSize)
      : MeasureSans("n", probeSize);
    return pi.X >= reference.X * 0.42f && pi.Y >= reference.Y * 0.55f;
  }

  public static ImFontPtr SansBold => Current.SansReady ? Current.SansBold : ImGui.GetFont();

  public static ImFontPtr MathItalic => Current.MathReady ? Current.MathItalic : ImGui.GetFont();

  public static ImFontPtr Arial => Current.ArialReady ? Current.Arial : ImGui.GetFont();

  public static ImFontPtr ArialBold => Current.ArialBoldReady ? Current.ArialBold : Arial;

  public static ImFontPtr LedDisplay => Current.LedReady ? Current.LedDisplay : ImGui.GetFont();

  /// <summary>Silk ImGuiController onConfigureIO hook. Keeps default font first.</summary>
  public static void Configure()
  {
    ImGuiIOPtr io = ImGui.GetIO();
    io.Fonts.Clear();
    io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

    _ = io.Fonts.AddFontDefault();

    ImFontPtr sansBold = default;
    ImFontPtr mathItalic = default;
    ImFontPtr arial = default;
    ImFontPtr arialBold = default;
    ImFontPtr ledDisplay = default;
    bool sansReady = false;
    bool mathReady = false;
    bool arialReady = false;
    bool arialBoldReady = false;
    bool ledReady = false;

    string? sansPath = ResolveFont("LiberationSans-Bold.ttf");
    string? mathPath = ResolveFont("STIXTwoText-BoldItalic.ttf")
      ?? ResolveFont("LiberationSerif-BoldItalic.ttf");

    if (sansPath is not null)
    {
      sansBold = io.Fonts.AddFontFromFileTTF(sansPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        sansReady = sansBold.NativePtr != null;
      }
    }

    if (mathPath is not null)
    {
      mathItalic = io.Fonts.AddFontFromFileTTF(mathPath, FaceplateLoadPx);
      unsafe
      {
        mathReady = mathItalic.NativePtr != null;
      }
    }

    string? arialPath = ResolveArial();
    if (arialPath is not null)
    {
      arial = io.Fonts.AddFontFromFileTTF(arialPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        arialReady = arial.NativePtr != null;
      }
    }

    string? arialBoldPath = ResolveArialBold();
    if (arialBoldPath is not null)
    {
      arialBold = io.Fonts.AddFontFromFileTTF(arialBoldPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        arialBoldReady = arialBold.NativePtr != null;
      }
    }

    string? ledPath = ResolveFont("LEDcharset_class.TTF");
    if (ledPath is not null)
    {
      ledDisplay = io.Fonts.AddFontFromFileTTF(ledPath, LedDisplayLoadPx, null, LedDisplayGlyphRanges);
      unsafe
      {
        ledReady = ledDisplay.NativePtr != null;
      }
    }

    ByContext[ImGui.GetCurrentContext()] = new FontSet(
      sansBold,
      mathItalic,
      arial,
      arialBold,
      ledDisplay,
      sansReady,
      mathReady,
      arialReady,
      arialBoldReady,
      ledReady);
  }

  public static void UnregisterCurrentContext() =>
    ByContext.Remove(ImGui.GetCurrentContext());

  public static float DrawPiTop(ImDrawListPtr draw, float x, float topY, float size, uint color)
  {
    if (IsArialBoldReady)
    {
      return DrawArialBoldTop(draw, PiGlyph, x, topY, size, color);
    }

    if (IsArialReady)
    {
      return DrawArialTop(draw, PiGlyph, x, topY, size, color);
    }

    return DrawSansTop(draw, PiGlyph, x, topY, size, color);
  }

  public static Vector2 MeasurePi(float size) =>
    IsArialBoldReady ? MeasureArialBold(PiGlyph, size)
    : IsArialReady ? MeasureArial(PiGlyph, size)
    : MeasureSans(PiGlyph, size);

  public static Vector2 MeasureSans(string text, float size) =>
    SansBold.CalcTextSizeA(size, float.MaxValue, 0f, text);

  public static Vector2 MeasureMath(string text, float size) =>
    MathItalic.CalcTextSizeA(size, float.MaxValue, 0f, text);

  public static float DrawSansTop(ImDrawListPtr draw, string text, float x, float topY, float size, uint color)
  {
    draw.AddText(SansBold, size, new Vector2(x, topY), color, text);
    return MeasureSans(text, size).X;
  }

  public static float DrawMathTop(ImDrawListPtr draw, string text, float x, float topY, float size, uint color)
  {
    draw.AddText(MathItalic, size, new Vector2(x, topY), color, text);
    return MeasureMath(text, size).X;
  }

  public static float MathWidth(string text, float size) =>
    IsMathReady ? MeasureMath(text, size).X : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text).X;

  public static float SansWidth(string text, float size) =>
    IsReady ? MeasureSans(text, size).X : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text).X;

  public static Vector2 MeasureArial(string text, float size) =>
    Arial.CalcTextSizeA(size, float.MaxValue, 0f, text);

  public static float DrawArialTop(ImDrawListPtr draw, string text, float x, float topY, float size, uint color)
  {
    draw.AddText(Arial, size, new Vector2(x, topY), color, text);
    return MeasureArial(text, size).X;
  }

  public static Vector2 MeasureArialBold(string text, float size) =>
    ArialBold.CalcTextSizeA(size, float.MaxValue, 0f, text);

  public static float ArialBoldWidth(string text, float size) =>
    IsArialBoldReady || IsArialReady ? MeasureArialBold(text, size).X : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text).X;

  public static float DrawArialBoldTop(ImDrawListPtr draw, string text, float x, float topY, float size, uint color)
  {
    draw.AddText(ArialBold, size, new Vector2(x, topY), color, text);
    return MeasureArialBold(text, size).X;
  }

  /// <summary>Tight painted bounds for a string relative to the ImGui <see cref="DrawArialBoldTop"/> origin.</summary>
  public readonly record struct FontInkBounds(float Width, float Left, float Top, float Height)
  {
    public float InkMidX => Left + Width * 0.5f;

    public float InkMidY => Top + Height * 0.5f;
  }

  public static FontInkBounds MeasureArialBoldInk(string text, float size) =>
    MeasureInkBounds(ArialBold, size, IsArialBoldReady || IsArialReady, text);

  /// <summary>Tight painted bounds for math-italic text relative to the <see cref="DrawMathTop"/> origin.</summary>
  public static FontInkBounds MeasureMathInk(string text, float size) =>
    MeasureInkBounds(MathItalic, size, IsMathReady, text);

  /// <summary>Tight painted bounds of LED-font text relative to the draw origin (top-left pen).</summary>
  public static FontInkBounds MeasureLedInk(string text, float size) =>
    MeasureInkBounds(LedDisplay, size, IsLedDisplayReady, text);

  public static Vector2 ArialBoldTopLeftForBandCenter(Vector2 bandCenter, string text, float size) =>
    ArialBoldTopLeftForBandCenter(bandCenter, text, size, verticalBiasRatio: 0f);

  public static Vector2 ArialBoldTopLeftForBandCenter(Vector2 bandCenter, string text, float size, float verticalBiasRatio)
  {
    FontInkBounds ink = MeasureArialBoldInk(text, size);
    if (ink.Height > 0.01f)
    {
      return new(
        bandCenter.X - ink.InkMidX,
        bandCenter.Y - ink.InkMidY + size * verticalBiasRatio);
    }

    Vector2 box = MeasureArialBold(text, size);
    return new(bandCenter.X - box.X * 0.5f, bandCenter.Y - box.Y * 0.5f + size * verticalBiasRatio);
  }

  public static Vector2 ArialBoldTopLeftForBand(Vector2 bandMin, Vector2 bandMax, string text, float size, float verticalBiasRatio)
  {
    Vector2 center = (bandMin + bandMax) * 0.5f;
    return ArialBoldTopLeftForBandCenter(center, text, size, verticalBiasRatio);
  }

  private static FontInkBounds MeasureInkBounds(ImFontPtr font, float size, bool ready, string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return default;
    }

    if (!ready)
    {
      Vector2 box = ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text);
      return new FontInkBounds(box.X, 0f, 0f, box.Y);
    }

    unsafe
    {
      ImFont* native = (ImFont*)font.NativePtr;
      if (native == null)
      {
        Vector2 box = font.CalcTextSizeA(size, float.MaxValue, 0f, text);
        return new FontInkBounds(box.X, 0f, 0f, box.Y);
      }

      float scale = size / native->FontSize;
      float minY = float.MaxValue;
      float maxY = float.MinValue;
      float minX = float.MaxValue;
      float maxX = float.MinValue;
      float penX = 0f;
      foreach (char c in text)
      {
        ImFontGlyph* glyph = ImGuiNative.ImFont_FindGlyph(native, c);
        if (glyph == null)
        {
          glyph = ImGuiNative.ImFont_FindGlyphNoFallback(native, c);
        }

        if (glyph == null)
        {
          continue;
        }

        float x0 = penX + glyph->X0 * scale;
        float x1 = penX + glyph->X1 * scale;
        float y0 = glyph->Y0 * scale;
        float y1 = glyph->Y1 * scale;
        minX = MathF.Min(minX, x0);
        maxX = MathF.Max(maxX, x1);
        minY = MathF.Min(minY, y0);
        maxY = MathF.Max(maxY, y1);
        penX += glyph->AdvanceX * scale;
      }

      if (minY > maxY || minX > maxX)
      {
        Vector2 box = font.CalcTextSizeA(size, float.MaxValue, 0f, text);
        return new FontInkBounds(box.X, 0f, 0f, box.Y);
      }

      return new FontInkBounds(maxX - minX, minX, minY, maxY - minY);
    }
  }

  /// <summary>Draws text with extra letter-spacing so glyphs span <paramref name="leftX"/>..<paramref name="rightX"/>.</summary>
  public static float DrawArialBoldStretchedToWidth(
    ImDrawListPtr draw,
    string text,
    float leftX,
    float rightX,
    float topY,
    float size,
    uint color)
  {
    float targetWidth = MathF.Max(rightX - leftX, 1f);
    if (string.IsNullOrEmpty(text))
    {
      return 0f;
    }

    ImFontPtr font = IsArialBoldReady || IsArialReady ? ArialBold : ImGui.GetFont();
    if (text.Length == 1)
    {
      draw.AddText(font, size, new Vector2(leftX, topY), color, text);
      return font.CalcTextSizeA(size, float.MaxValue, 0f, text).X;
    }

    float totalNatural = 0f;
    for (int i = 0; i < text.Length; i++)
    {
      totalNatural += font.CalcTextSizeA(size, float.MaxValue, 0f, text.AsSpan(i, 1)).X;
    }

    float spacing = (targetWidth - totalNatural) / (text.Length - 1);
    float x = leftX;
    for (int i = 0; i < text.Length; i++)
    {
      ReadOnlySpan<char> glyph = text.AsSpan(i, 1);
      draw.AddText(font, size, new Vector2(x, topY), color, glyph);
      x += font.CalcTextSizeA(size, float.MaxValue, 0f, glyph).X + spacing;
    }

    return targetWidth;
  }

  private static string? ResolveArial() =>
    ResolveFont("arial.ttf")
    ?? ResolveFont("Arial.ttf");

  private static string? ResolveArialBold() =>
    ResolveFont("arialbd.ttf")
    ?? ResolveFont("Arial Bold.ttf")
    ?? ResolveFont("ARIALBD.TTF");

  private static string? ResolveFont(string fileName)
  {
    string bundled = TeoCalcPaths.ResourcePath(Path.Combine("Font", fileName));
    if (File.Exists(bundled))
    {
      return bundled;
    }

    string windows = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fileName);
    return File.Exists(windows) ? windows : null;
  }
}
