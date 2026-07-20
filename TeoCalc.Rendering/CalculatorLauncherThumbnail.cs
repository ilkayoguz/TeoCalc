using System.Numerics;
using ImGuiNET;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// Launcher thumbnails: live primitive vector draw every frame (no GL bake).
/// Baking caused blur when icons were scaled after a small first paint.
/// </summary>
public static class CalculatorLauncherThumbnail
{
  private static readonly Dictionary<string, PreviewModel> Previews = new(StringComparer.OrdinalIgnoreCase);

  private sealed class PreviewModel
  {
    public required TeoCalcModelDefinition Model { get; init; }
    public required CalcModelDefinition Faceplate { get; init; }
    public required ProgramVocabulary? Vocabulary { get; init; }
    public required CalcBodyLayout Layout { get; init; }
  }

  public static void Initialize(Silk.NET.OpenGL.GL gl)
  {
    // Live draw only — GL handle unused but kept for call-site compatibility.
    _ = gl;
  }

  public static void Dispose() => Previews.Clear();

  public static void Draw(ImDrawListPtr draw, Vector2 min, Vector2 max, CalculatorLauncherEntry entry)
  {
    if (!TryGetPreview(entry.ModelId, out PreviewModel? preview) || preview is null)
    {
      return;
    }

    FitRect(min, max, preview.Layout, out Vector2 fitMin, out Vector2 fitMax);
    CalcFaceplateThemeState.ApplyForModel(preview.Faceplate);
    draw.PushClipRect(min, max, true);
    CalcPrimitiveThumbnail.Draw(
      draw,
      fitMin,
      fitMax - fitMin,
      preview.Model,
      preview.Faceplate,
      preview.Vocabulary,
      preview.Layout);
    draw.PopClipRect();
  }

  /// <summary>No-op — thumbs are drawn live (keeps call sites compiling).</summary>
  public static void BakePending(Silk.NET.OpenGL.GL gl, Silk.NET.Maths.Vector2D<int> framebufferSize)
  {
    _ = gl;
    _ = framebufferSize;
  }

  private static void FitRect(
    Vector2 min,
    Vector2 max,
    CalcBodyLayout layout,
    out Vector2 fitMin,
    out Vector2 fitMax)
  {
    Vector2 available = max - min;
    float aspect = layout.ReferenceWidth / MathF.Max(1f, layout.ReferenceHeight);
    float fitW = available.X;
    float fitH = fitW / aspect;
    if (fitH > available.Y)
    {
      fitH = available.Y;
      fitW = fitH * aspect;
    }

    fitMin = min + new Vector2((available.X - fitW) * 0.5f, (available.Y - fitH) * 0.5f);
    fitMax = fitMin + new Vector2(fitW, fitH);
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

      CalcModelDefinition faceplate = CalcModelCatalog.Resolve(model, catalogId);
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
