using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>
/// Faceplate fonts loaded as <b>additional</b> ImGui fonts after the default UI font.
/// <see cref="ImGui.GetFont"/> stays the small default; use <see cref="SansBold"/> only on the faceplate.
/// </summary>
public static class CalcFaceplateFonts
{
  private const float FaceplateLoadPx = 18f;

  private static ImFontPtr _sansBold;
  private static ImFontPtr _mathItalic;
  private static ImFontPtr _arial;
  private static ImFontPtr _arialBold;
  private static bool _sansReady;
  private static bool _mathReady;
  private static bool _arialReady;
  private static bool _arialBoldReady;
  private static GCHandle _glyphRangeHandle;

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

  public static bool IsReady => _sansReady;

  public static bool IsMathReady => _mathReady;

  public static bool IsArialReady => _arialReady;

  public static bool IsArialBoldReady => _arialBoldReady;

  public const string PiGlyph = "\u03c0";

  public static bool CanDrawPiGlyph => HasPiGlyph();

  public static bool HasPiGlyph(float probeSize = FaceplateLoadPx)
  {
    if (!_arialReady && !_sansReady)
    {
      return false;
    }

    Vector2 pi = MeasurePi(probeSize);
    Vector2 reference = _arialReady
      ? MeasureArial("n", probeSize)
      : MeasureSans("n", probeSize);
    return pi.X >= reference.X * 0.42f && pi.Y >= reference.Y * 0.55f;
  }

  public static ImFontPtr SansBold => _sansReady ? _sansBold : ImGui.GetFont();

  public static ImFontPtr MathItalic => _mathReady ? _mathItalic : ImGui.GetFont();

  public static ImFontPtr Arial => _arialReady ? _arial : ImGui.GetFont();

  public static ImFontPtr ArialBold => _arialBoldReady ? _arialBold : Arial;

  /// <summary>Silk ImGuiController onConfigureIO hook. Keeps default font first.</summary>
  public static void Configure()
  {
    ImGuiIOPtr io = ImGui.GetIO();
    io.Fonts.Clear();
    io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

    _ = io.Fonts.AddFontDefault();
    _sansReady = false;
    _mathReady = false;
    _arialReady = false;
    _arialBoldReady = false;

    string? sansPath = ResolveFont("LiberationSans-Bold.ttf");
    string? mathPath = ResolveFont("STIXTwoText-BoldItalic.ttf")
      ?? ResolveFont("LiberationSerif-BoldItalic.ttf");

    if (sansPath is not null)
    {
      _sansBold = io.Fonts.AddFontFromFileTTF(sansPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        _sansReady = _sansBold.NativePtr != null;
      }
    }

    if (mathPath is not null)
    {
      _mathItalic = io.Fonts.AddFontFromFileTTF(mathPath, FaceplateLoadPx);
      unsafe
      {
        _mathReady = _mathItalic.NativePtr != null;
      }
    }

    string? arialPath = ResolveArial();
    if (arialPath is not null)
    {
      _arial = io.Fonts.AddFontFromFileTTF(arialPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        _arialReady = _arial.NativePtr != null;
      }
    }

    string? arialBoldPath = ResolveArialBold();
    if (arialBoldPath is not null)
    {
      _arialBold = io.Fonts.AddFontFromFileTTF(arialBoldPath, FaceplateLoadPx, null, FaceplateGlyphRanges);
      unsafe
      {
        _arialBoldReady = _arialBold.NativePtr != null;
      }
    }
  }

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
    IsArialBoldReady || _arialReady ? MeasureArialBold(text, size).X : ImGui.GetFont().CalcTextSizeA(size, float.MaxValue, 0f, text).X;

  public static float DrawArialBoldTop(ImDrawListPtr draw, string text, float x, float topY, float size, uint color)
  {
    draw.AddText(ArialBold, size, new Vector2(x, topY), color, text);
    return MeasureArialBold(text, size).X;
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

    ImFontPtr font = IsArialBoldReady || _arialReady ? ArialBold : ImGui.GetFont();
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
