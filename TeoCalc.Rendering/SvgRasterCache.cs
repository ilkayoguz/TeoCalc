using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ImGuiNET;
using Silk.NET.OpenGL;
using SkiaSharp;
using Svg.Skia;

namespace TeoCalc.Rendering;

/// <summary>Rasterizes SVG files to OpenGL textures for ImGui <see cref="ImDrawListPtr.AddImage"/>.</summary>
internal sealed class SvgRasterCache : IDisposable
{
  private readonly Dictionary<string, Slot> _slots = new(StringComparer.OrdinalIgnoreCase);
  private GL? _gl;

  public bool IsInitialized => _gl is not null;

  public void Initialize(GL gl)
  {
    if (ReferenceEquals(_gl, gl))
    {
      return;
    }

    Dispose();
    _gl = gl;
  }

  public bool TryDraw(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int rasterWidth,
    int rasterHeight,
    float alpha = 1f,
    Vector2? uv0 = null,
    Vector2? uv1 = null,
    int revision = 0) =>
    TryDraw(draw, min, max, path, rasterWidth, rasterHeight, 0f, 0u, alpha, uv0, uv1, revision);

  public bool TryDraw(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int rasterWidth,
    int rasterHeight,
    float rotateDegrees,
    uint tintColor,
    float alpha = 1f,
    Vector2? uv0 = null,
    Vector2? uv1 = null,
    int revision = 0)
  {
    if (_gl is null || !File.Exists(path))
    {
      return false;
    }

    Slot? slot = EnsureSlot(path, rasterWidth, rasterHeight, rotateDegrees, revision);
    if (slot is null || !TryRasterize(slot, path, rotateDegrees, out byte[] rgba))
    {
      return false;
    }

    Upload(slot, rgba);
    DrawImage(draw, min, max, slot.Texture, alpha, uv0 ?? Vector2.Zero, uv1 ?? Vector2.One, tintColor);
    return true;
  }

  public void Dispose()
  {
    if (_gl is not null)
    {
      foreach (Slot slot in _slots.Values)
      {
        _gl.DeleteTexture(slot.Texture);
        slot.Svg.Dispose();
      }
    }

    _slots.Clear();
    _gl = null;
  }

  private Slot? EnsureSlot(string path, int rasterWidth, int rasterHeight, float rotateDegrees = 0f, int revision = 0)
  {
    if (_gl is null)
    {
      return null;
    }

    DateTime stamp = File.GetLastWriteTimeUtc(path);
    string key = $"{path}|{rasterWidth}x{rasterHeight}|r{rotateDegrees:0.##}|v{revision}";
    if (_slots.TryGetValue(key, out Slot? existing) && existing.SourceStamp == stamp)
    {
      return existing;
    }

    if (existing is not null)
    {
      _gl.DeleteTexture(existing.Texture);
      existing.Svg.Dispose();
      _slots.Remove(key);
    }

    SKSvg svg = new();
    try
    {
      if (svg.Load(path) is null)
      {
        svg.Dispose();
        return null;
      }
    }
    catch
    {
      svg.Dispose();
      return null;
    }

    uint texture = _gl.GenTexture();
    ConfigureTexture(texture);
    Slot slot = new(svg, texture, stamp, rasterWidth, rasterHeight);
    _slots[key] = slot;
    return slot;
  }

  private static void ConfigureTexture(uint texture)
  {
    // Bound by caller via Upload.
  }

  private void Upload(Slot slot, byte[] rgba)
  {
    if (_gl is null)
    {
      return;
    }

    _gl.BindTexture(TextureTarget.Texture2D, slot.Texture);
    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    unsafe
    {
      fixed (byte* pixels = rgba)
      {
        _gl.TexImage2D(
          TextureTarget.Texture2D,
          0,
          InternalFormat.Rgba,
          (uint)slot.RasterWidth,
          (uint)slot.RasterHeight,
          0,
          PixelFormat.Rgba,
          PixelType.UnsignedByte,
          pixels);
      }
    }
  }

  private static readonly Regex ViewBoxRegex = new(
    @"viewBox\s*=\s*""\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s*""",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  private static bool TryRasterize(Slot slot, string path, float rotateDegrees, out byte[] rgba)
  {
    rgba = [];
    if (slot.Svg.Picture is null)
    {
      return false;
    }

    SKPicture picture = slot.Svg.Picture;
    SKRect viewBox = ResolveViewBox(path, picture.CullRect);
    if (viewBox.Width <= 0f || viewBox.Height <= 0f)
    {
      return false;
    }

    using SKBitmap bitmap = new(slot.RasterWidth, slot.RasterHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
    using SKCanvas canvas = new(bitmap);
    canvas.Clear(SKColors.Transparent);

    float scaleX = slot.RasterWidth / viewBox.Width;
    float scaleY = slot.RasterHeight / viewBox.Height;
    canvas.Scale(scaleX, scaleY);
    canvas.Translate(-viewBox.Left, -viewBox.Top);
    if (Math.Abs(rotateDegrees) > 0.01f)
    {
      canvas.RotateDegrees(rotateDegrees, viewBox.MidX, viewBox.MidY);
    }

    canvas.DrawPicture(picture);
    canvas.Flush();

    rgba = new byte[slot.RasterWidth * slot.RasterHeight * 4];
    Marshal.Copy(bitmap.GetPixels(), rgba, 0, rgba.Length);
    return true;
  }

  private static SKRect ResolveViewBox(string path, SKRect cullRect)
  {
    if (!File.Exists(path))
    {
      return cullRect;
    }

    Match match = ViewBoxRegex.Match(File.ReadAllText(path));
    if (!match.Success)
    {
      return cullRect;
    }

    float x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    float y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
    float width = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
    float height = float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
    return new SKRect(x, y, x + width, y + height);
  }

  private static void DrawImage(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    uint texture,
    float alpha,
    Vector2 uv0,
    Vector2 uv1,
    uint tintColor = 0u)
  {
    if (tintColor != 0u)
    {
      draw.AddImage((nint)texture, min, max, uv0, uv1, tintColor);
      return;
    }

    if (alpha >= 0.999f)
    {
      draw.AddImage((nint)texture, min, max, uv0, uv1);
      return;
    }

    uint tint = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha));
    draw.AddImage((nint)texture, min, max, uv0, uv1, tint);
  }

  private sealed class Slot(SKSvg svg, uint texture, DateTime sourceStamp, int rasterWidth, int rasterHeight)
  {
    public SKSvg Svg { get; } = svg;

    public uint Texture { get; } = texture;

    public DateTime SourceStamp { get; } = sourceStamp;

    public int RasterWidth { get; } = rasterWidth;

    public int RasterHeight { get; } = rasterHeight;
  }
}
