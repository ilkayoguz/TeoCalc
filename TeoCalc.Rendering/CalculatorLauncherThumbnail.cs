using System.Numerics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// Cached launcher thumbnails: real faceplate vector draw (same path as live windows),
/// baked once into GL textures via framebuffer readback.
/// </summary>
public static class CalculatorLauncherThumbnail
{
  private static readonly Dictionary<string, uint> Textures = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, PendingBake> Pending = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<string, PreviewModel> Previews = new(StringComparer.OrdinalIgnoreCase);
  private static GL? _gl;

  private readonly record struct PendingBake(Vector2 Min, Vector2 Max, int FrameDelay);

  private sealed class PreviewModel
  {
    public required TeoCalcModelDefinition Model { get; init; }
    public required CalcModelDefinition Faceplate { get; init; }
    public required ProgramVocabulary? Vocabulary { get; init; }
    public required CalcBodyLayout Layout { get; init; }
  }

  public static void Initialize(GL gl)
  {
    if (ReferenceEquals(_gl, gl))
    {
      return;
    }

    Dispose();
    _gl = gl;
  }

  public static void Dispose()
  {
    if (_gl is not null)
    {
      foreach (uint texture in Textures.Values)
      {
        _gl.DeleteTexture(texture);
      }
    }

    Textures.Clear();
    Pending.Clear();
    Previews.Clear();
    _gl = null;
  }

  public static void Draw(ImDrawListPtr draw, Vector2 min, Vector2 max, CalculatorLauncherEntry entry)
  {
    string key = entry.ModelId;
    if (Textures.TryGetValue(key, out uint texture))
    {
      draw.AddImage((nint)texture, min, max);
      return;
    }

    DrawLiveFaceplate(draw, min, max, entry);
    if (_gl is not null && !Pending.ContainsKey(key))
    {
      // Delay one frame so the live draw is in the framebuffer before readback.
      Pending[key] = new PendingBake(min, max, FrameDelay: 1);
    }
  }

  /// <summary>Call after <c>ImGuiController.Render</c> to bake pending thumbs from the FB.</summary>
  public static void BakePending(GL gl, Vector2D<int> framebufferSize)
  {
    if (Pending.Count == 0)
    {
      return;
    }

    List<string> ready = [];
    foreach (string key in Pending.Keys.ToArray())
    {
      PendingBake bake = Pending[key];
      if (bake.FrameDelay > 0)
      {
        Pending[key] = bake with { FrameDelay = bake.FrameDelay - 1 };
        continue;
      }

      if (TryBakeFromFramebuffer(gl, key, bake.Min, bake.Max, framebufferSize))
      {
        ready.Add(key);
      }
      else
      {
        Pending.Remove(key);
      }
    }

    foreach (string key in ready)
    {
      Pending.Remove(key);
    }
  }

  private static bool TryBakeFromFramebuffer(
    GL gl,
    string key,
    Vector2 min,
    Vector2 max,
    Vector2D<int> framebufferSize)
  {
    int x = (int)MathF.Floor(min.X);
    int yTop = (int)MathF.Floor(min.Y);
    int w = Math.Max(1, (int)MathF.Ceiling(max.X - min.X));
    int h = Math.Max(1, (int)MathF.Ceiling(max.Y - min.Y));
    if (x < 0 || yTop < 0 || x + w > framebufferSize.X || yTop + h > framebufferSize.Y)
    {
      return false;
    }

    // OpenGL FB origin is bottom-left; ImGui is top-left.
    int y = framebufferSize.Y - (yTop + h);
    byte[] rgba = new byte[w * h * 4];
    unsafe
    {
      fixed (byte* ptr = rgba)
      {
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        gl.PixelStore(PixelStoreParameter.PackAlignment, 1);
        gl.ReadPixels(x, y, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
      }
    }

    byte[] flipped = new byte[rgba.Length];
    int stride = w * 4;
    for (int row = 0; row < h; row++)
    {
      System.Buffer.BlockCopy(rgba, (h - 1 - row) * stride, flipped, row * stride, stride);
    }

    uint texture = gl.GenTexture();
    gl.BindTexture(TextureTarget.Texture2D, texture);
    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
    gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    unsafe
    {
      fixed (byte* ptr = flipped)
      {
        gl.TexImage2D(
          TextureTarget.Texture2D,
          0,
          InternalFormat.Rgba,
          (uint)w,
          (uint)h,
          0,
          PixelFormat.Rgba,
          PixelType.UnsignedByte,
          ptr);
      }
    }

    if (Textures.TryGetValue(key, out uint old))
    {
      gl.DeleteTexture(old);
    }

    Textures[key] = texture;
    return true;
  }

  private static void DrawLiveFaceplate(ImDrawListPtr draw, Vector2 min, Vector2 max, CalculatorLauncherEntry entry)
  {
    if (!TryGetPreview(entry.ModelId, out PreviewModel? preview) || preview is null)
    {
      DrawFallback(draw, min, max);
      return;
    }

    CalcFaceplateThemeState.ApplyForModel(preview.Faceplate);
    Vector2 available = max - min;
    float aspect = preview.Layout.ReferenceWidth / MathF.Max(1f, preview.Layout.ReferenceHeight);
    float fitW = available.X;
    float fitH = fitW / aspect;
    if (fitH > available.Y)
    {
      fitH = available.Y;
      fitW = fitH * aspect;
    }

    Vector2 origin = min + new Vector2((available.X - fitW) * 0.5f, (available.Y - fitH) * 0.5f);
    draw.PushClipRect(min, max, true);
    draw.AddRectFilled(min, max, 0xFF2A2A2Cu, 4f);
    CalcFaceplateView.DrawStaticPreview(
      draw,
      origin,
      new Vector2(fitW, fitH),
      preview.Model,
      preview.Faceplate,
      preview.Vocabulary,
      preview.Layout);
    draw.PopClipRect();
  }

  private static void DrawFallback(ImDrawListPtr draw, Vector2 min, Vector2 max)
  {
    draw.AddRectFilled(min, max, 0xFF2A2A2Cu, 4f);
    draw.AddRect(min, max, 0xFF101012u, 4f, ImDrawFlags.RoundCornersAll, 1f);
  }

  private static bool TryGetPreview(string catalogId, out PreviewModel? preview)
  {
    if (Previews.TryGetValue(catalogId, out preview))
    {
      return true;
    }

    try
    {
      string engineId = CalcModelIds.ToEngineId(catalogId);
      string engineRoot = TeoCalcPaths.ResourcePath("Engine");
      string modelPath = Path.Combine(engineRoot, engineId, "Model.json");
      TeoCalcModelDefinition model = File.Exists(modelPath)
        ? TeoCalcModelDefinition.Load(modelPath)
        : new TeoCalcModelDefinition
        {
          Model = catalogId,
          DisplayName = catalogId,
          Family = CalcModelIds.InferFamily(catalogId),
        };

      ProgramVocabulary? vocabulary = null;
      if (model.Program?.Vocabulary is { Length: > 0 } vocabularyRel)
      {
        string vocabularyPath = Path.Combine(
          engineRoot,
          engineId,
          vocabularyRel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(vocabularyPath))
        {
          vocabulary = ProgramVocabulary.Load(vocabularyPath);
        }
      }

      CalcModelDefinition faceplate = CalcModelCatalog.Resolve(model, engineId);
      CalcBodyLayout layout = CalcBodyLayoutCatalog.ResolveForFaceplate(
        faceplate,
        model.Family,
        model.Model);

      preview = new PreviewModel
      {
        Model = model,
        Faceplate = faceplate,
        Vocabulary = vocabulary,
        Layout = layout,
      };
      Previews[catalogId] = preview;
      return true;
    }
    catch
    {
      preview = null;
      return false;
    }
  }
}
