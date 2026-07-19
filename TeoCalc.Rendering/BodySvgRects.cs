using System.Globalization;
using System.Text.RegularExpressions;
using TeoCalc.Core;

namespace TeoCalc.Rendering;

/// <summary>Reads named SVG rects from Body.svg (single source for overlay alignment).</summary>
internal static class BodySvgRects
{
  private static readonly Regex RectRegex = new(
    @"<rect\s+[^>]*\bid\s*=\s*""([^""]+)""[^>]*>",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  private static readonly Regex AttributeRegex = new(
    @"\b(x|y|width|height)\s*=\s*""([^""]+)""",
    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

  public static bool TryGetRect(string elementId, out RectF rect)
  {
    rect = default;
    string path = Faceplate.FaceplateAssetPaths.ResolveFile("HP-65", "Body.svg");
    if (!File.Exists(path))
    {
      return false;
    }

    string svg = File.ReadAllText(path);
    foreach (Match tag in RectRegex.Matches(svg))
    {
      if (!string.Equals(tag.Groups[1].Value, elementId, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (!TryParseAttributes(tag.Value, out float x, out float y, out float w, out float h))
      {
        return false;
      }

      rect = new RectF(x, y, w, h);
      return true;
    }

    return false;
  }

  private static bool TryParseAttributes(string tag, out float x, out float y, out float w, out float h)
  {
    x = y = w = h = 0f;
    bool hasX = false;
    bool hasY = false;
    bool hasW = false;
    bool hasH = false;

    foreach (Match attr in AttributeRegex.Matches(tag))
    {
      float value = float.Parse(attr.Groups[2].Value, CultureInfo.InvariantCulture);
      switch (attr.Groups[1].Value.ToLowerInvariant())
      {
        case "x":
          x = value;
          hasX = true;
          break;
        case "y":
          y = value;
          hasY = true;
          break;
        case "width":
          w = value;
          hasW = true;
          break;
        case "height":
          h = value;
          hasH = true;
          break;
      }
    }

    return hasX && hasY && hasW && hasH && w > 0f && h > 0f;
  }
}
