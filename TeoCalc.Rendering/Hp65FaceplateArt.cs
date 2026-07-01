using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.OpenGL;
using StbImageSharp;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

  /// <summary>
  /// HP-65 outer-frame chrome from hp65_470.png. Key wells and interior panels are
  /// transparent in the asset; legacy rectangular mask is skipped when alpha is present.
  /// </summary>
  public static class Hp65FaceplateArt
  {
    // Legacy fallback mask when asset is a solid photo without per-key alpha.
    private const int MaskLeft = 14;
  private const int MaskTop = 48;
  private const int MaskRight = 456;
  private const int MaskBottom = 862;

  private static GL? _gl;
  private static uint _texture;
  private static int _width;
  private static int _height;
  private static bool _ready;

  public static bool IsReady => _ready;

  public static int Width => _width;

  public static int Height => _height;

  public static void TryInitialize(GL gl)
  {
    if (_ready && ReferenceEquals(_gl, gl))
    {
      return;
    }

    Dispose();
    string path = TeoCalcPaths.ResourcePath("Engine/HP-65/Assets/hp65_470.png");
    if (!File.Exists(path))
    {
      return;
    }

    try
    {
      using FileStream stream = File.OpenRead(path);
      ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
      if (image.Data.Length != image.Width * image.Height * 4)
      {
        return;
      }

      MaskInteriorPixels(image.Data, image.Width, image.Height);

      _gl = gl;
      _width = image.Width;
      _height = image.Height;
      _texture = gl.GenTexture();
      gl.BindTexture(TextureTarget.Texture2D, _texture);
      gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
      gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
      unsafe
      {
        fixed (byte* pixels = image.Data)
        {
          gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba,
            (uint)image.Width,
            (uint)image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixels);
        }
      }

      _ready = true;
    }
    catch
    {
      Dispose();
    }
  }

  /// <summary>Draws only the outer frame chrome on top of procedural faceplate content.</summary>
  public static void DrawFrameOverlay(ImDrawListPtr draw, Vector2 origin, CalcChassisMetrics metrics)
  {
    if (!_ready)
    {
      return;
    }

    Vector2 max = origin + new Vector2(metrics.Width, metrics.Height);
    draw.AddImage((nint)_texture, origin, max);
  }

  private static void MaskInteriorPixels(byte[] pixels, int width, int height)
  {
    if (AssetHasInteriorAlpha(pixels, width, height))
    {
      return;
    }

    int x0 = Math.Clamp(MaskLeft, 0, width - 1);
    int y0 = Math.Clamp(MaskTop, 0, height - 1);
    int x1 = Math.Clamp(MaskRight, 0, width - 1);
    int y1 = Math.Clamp(MaskBottom, 0, height - 1);

    for (int y = y0; y <= y1; y++)
    {
      int row = y * width;
      for (int x = x0; x <= x1; x++)
      {
        int index = (row + x) * 4;
        pixels[index + 3] = 0;
      }
    }
  }

  private static bool AssetHasInteriorAlpha(byte[] pixels, int width, int height)
  {
    int x0 = Math.Clamp(MaskLeft, 0, width - 1);
    int y0 = Math.Clamp(MaskTop, 0, height - 1);
    int x1 = Math.Clamp(MaskRight, 0, width - 1);
    int y1 = Math.Clamp(MaskBottom, 0, height - 1);
    int transparent = 0;
    int sampled = 0;
    for (int y = y0; y <= y1; y += 4)
    {
      int row = y * width;
      for (int x = x0; x <= x1; x += 4)
      {
        sampled++;
        if (pixels[(row + x) * 4 + 3] < 16)
        {
          transparent++;
        }
      }
    }

    return sampled > 0 && transparent > sampled / 8;
  }

  public static void Dispose()
  {
    if (_ready && _gl is not null)
    {
      _gl.DeleteTexture(_texture);
    }

    _gl = null;
    _texture = 0;
    _width = 0;
    _height = 0;
    _ready = false;
  }
}
