using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>SVG card-slot labels above keys A–E (face-below-switch band).</summary>
public static class CardSlotLabelArt
{
  private const float RefHeight = 24f;

  private static readonly (string File, float RefWidth, float HeightMul)[] Labels =
  [
    ("label-1-over-x.svg", 60f, 1.0f),
    ("label-sqrt-x.svg", 60f, 1.05f),
    ("label-y-pow-x.svg", 60f, 1.05f),
    ("label-r-down.svg", 60f, 1.0f),
    ("label-x-exchange-y.svg", 72f, 1.05f),
  ];

  private static string AssetsRoot =>
    Path.Combine(FaceplateAssetPaths.ResolveAssetsRoot("HP-65"), "CardSlot");

  public static bool IsReady =>
    ClassicFaceplateSvgAssets.CanDrawCardSlotLabels && Labels.All(label => File.Exists(PathFor(label.File)));

  public static ClassicFaceplateGlyphs.LabelSize Measure(int column, float fontSize)
  {
    if ((uint)column >= Labels.Length)
    {
      return new(fontSize, fontSize);
    }

    (string _, float refWidth, float heightMul) = Labels[column];
    float height = fontSize * heightMul;
    float width = height * (refWidth / RefHeight);
    return new(width, height);
  }

  public static bool TryDraw(ImDrawListPtr draw, int column, Vector2 center, float fontSize, float scale)
  {
    if ((uint)column >= Labels.Length || !ClassicFaceplateSvgAssets.CanDrawCardSlotLabels)
    {
      return false;
    }

    (string file, _, float heightMul) = Labels[column];
    string path = PathFor(file);
    if (!File.Exists(path))
    {
      return false;
    }

    ClassicFaceplateGlyphs.LabelSize size = Measure(column, fontSize);
    Vector2 min = new(center.X - size.Width * 0.5f, center.Y - size.Height * 0.5f);
    Vector2 max = new(center.X + size.Width * 0.5f, center.Y + size.Height * 0.5f);

    int rasterW = Math.Max(1, (int)MathF.Ceiling(size.Width * 4f * scale));
    int rasterH = Math.Max(1, (int)MathF.Ceiling(size.Height * 4f * scale));
    return ClassicFaceplateSvgAssets.TryDrawSvg(draw, min, max, path, rasterW, rasterH);
  }

  private static string PathFor(string file) => Path.Combine(AssetsRoot, file);
}
