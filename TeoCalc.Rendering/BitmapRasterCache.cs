using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace TeoCalc.Rendering;

/// <summary>Rasterizes bitmap files (PNG/ICO) to OpenGL textures for ImGui.</summary>
internal sealed class BitmapRasterCache : IDisposable
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

  /// <summary>
  /// Draws <paramref name="path"/> into <paramref name="min"/>..<paramref name="max"/>.
  /// When <paramref name="useSourceResolution"/> is true, the texture keeps the file's
  /// native pixel size (better for small ICO/PNG marks scaled by ImGui).
  /// </summary>
  public bool TryDraw(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int revision = 0,
    bool useSourceResolution = false)
  {
    if (_gl is null || !File.Exists(path))
    {
      return false;
    }

    Slot? slot = EnsureSlot(path, revision, useSourceResolution, displayMin: min, displayMax: max);
    if (slot is null || !TryRasterize(slot, path, out byte[] rgba))
    {
      return false;
    }

    Upload(slot, rgba);
    draw.AddImage((nint)slot.Texture, min, max);
    return true;
  }

  public void Dispose()
  {
    if (_gl is not null)
    {
      foreach (Slot slot in _slots.Values)
      {
        _gl.DeleteTexture(slot.Texture);
      }
    }

    _slots.Clear();
    _gl = null;
  }

  private Slot? EnsureSlot(
    string path,
    int revision,
    bool useSourceResolution,
    Vector2 displayMin,
    Vector2 displayMax)
  {
    if (_gl is null)
    {
      return null;
    }

    DateTime stamp = File.GetLastWriteTimeUtc(path);
    string key = useSourceResolution
      ? $"{path}|native|v{revision}"
      : $"{path}|{MathF.Ceiling(displayMax.X - displayMin.X)}x{MathF.Ceiling(displayMax.Y - displayMin.Y)}|v{revision}";

    if (_slots.TryGetValue(key, out Slot? existing) && existing.SourceStamp == stamp)
    {
      return existing;
    }

    if (existing is not null)
    {
      _gl.DeleteTexture(existing.Texture);
      _slots.Remove(key);
    }

    int rasterWidth;
    int rasterHeight;
    if (useSourceResolution)
    {
      using SKBitmap? probe = SKBitmap.Decode(path);
      if (probe is null)
      {
        return null;
      }

      rasterWidth = Math.Max(1, probe.Width);
      rasterHeight = Math.Max(1, probe.Height);
    }
    else
    {
      rasterWidth = Math.Max(1, (int)MathF.Ceiling(displayMax.X - displayMin.X));
      rasterHeight = Math.Max(1, (int)MathF.Ceiling(displayMax.Y - displayMin.Y));
    }

    uint texture = _gl.GenTexture();
    Slot slot = new(texture, stamp, rasterWidth, rasterHeight);
    _slots[key] = slot;
    return slot;
  }

  private static bool TryRasterize(Slot slot, string path, out byte[] rgba)
  {
    rgba = [];
    using SKBitmap? decoded = SKBitmap.Decode(path);
    if (decoded is null)
    {
      return false;
    }

    using SKBitmap rgbaSource = decoded.ColorType == SKColorType.Rgba8888
      ? decoded.Copy()!
      : decoded.Copy(SKColorType.Rgba8888)!;
    if (rgbaSource is null)
    {
      return false;
    }

    SKBitmap upload = rgbaSource;
    SKBitmap? scaled = null;
    try
    {
      if (rgbaSource.Width != slot.RasterWidth || rgbaSource.Height != slot.RasterHeight)
      {
        scaled = rgbaSource.Resize(
          new SKImageInfo(slot.RasterWidth, slot.RasterHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
          new SKSamplingOptions(SKCubicResampler.Mitchell));
        if (scaled is null)
        {
          return false;
        }

        upload = scaled;
      }

      rgba = new byte[slot.RasterWidth * slot.RasterHeight * 4];
      Marshal.Copy(upload.GetPixels(), rgba, 0, rgba.Length);
      return true;
    }
    finally
    {
      scaled?.Dispose();
    }
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

  private sealed class Slot(uint texture, DateTime sourceStamp, int rasterWidth, int rasterHeight)
  {
    public uint Texture { get; } = texture;

    public DateTime SourceStamp { get; } = sourceStamp;

    public int RasterWidth { get; } = rasterWidth;

    public int RasterHeight { get; } = rasterHeight;
  }
}
