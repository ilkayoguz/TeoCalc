using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace TeoCalc.Rendering;

/// <summary>Rasterizes bitmap files (PNG) to OpenGL textures for ImGui.</summary>
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

  public bool TryDraw(
    ImDrawListPtr draw,
    Vector2 min,
    Vector2 max,
    string path,
    int revision = 0)
  {
    if (_gl is null || !File.Exists(path))
    {
      return false;
    }

    int rasterWidth = Math.Max(1, (int)MathF.Ceiling(max.X - min.X));
    int rasterHeight = Math.Max(1, (int)MathF.Ceiling(max.Y - min.Y));
    Slot? slot = EnsureSlot(path, rasterWidth, rasterHeight, revision);
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

  private Slot? EnsureSlot(string path, int rasterWidth, int rasterHeight, int revision)
  {
    if (_gl is null)
    {
      return null;
    }

    DateTime stamp = File.GetLastWriteTimeUtc(path);
    string key = $"{path}|{rasterWidth}x{rasterHeight}|v{revision}";
    if (_slots.TryGetValue(key, out Slot? existing) && existing.SourceStamp == stamp)
    {
      return existing;
    }

    if (existing is not null)
    {
      _gl.DeleteTexture(existing.Texture);
      _slots.Remove(key);
    }

    uint texture = _gl.GenTexture();
    Slot slot = new(texture, stamp, rasterWidth, rasterHeight);
    _slots[key] = slot;
    return slot;
  }

  private static bool TryRasterize(Slot slot, string path, out byte[] rgba)
  {
    rgba = [];
    using SKBitmap? bitmap = SKBitmap.Decode(path);
    if (bitmap is null)
    {
      return false;
    }

    using SKBitmap scaled = bitmap.Resize(new SKImageInfo(slot.RasterWidth, slot.RasterHeight), SKSamplingOptions.Default);
    if (scaled is null)
    {
      return false;
    }

    rgba = new byte[slot.RasterWidth * slot.RasterHeight * 4];
    System.Runtime.InteropServices.Marshal.Copy(scaled.GetPixels(), rgba, 0, rgba.Length);
    return true;
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
