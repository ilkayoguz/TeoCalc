using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImGuiNET;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Gold rule-label bracket spanning a CapAbove key range (CLEAR, COMPUTE, …).
/// Configured per model via <c>ClearBracket</c> in <c>key.faceplate.json</c>.
/// Lives in an expanded inter-row gutter — CapFace height stays PreferredCapHeight.
/// Vertical legs drop beside CapAbove legends with rounded tips.
/// </summary>
public static class CalcBracketLegendComponent
{
  /// <summary>
  /// Extra inter-row gap (ref px) inserted above the bracket row, on top of normal GutterRef.
  /// Gives the label room without overlapping the previous row or shrinking CapFace.
  /// </summary>
  public const float GutterExtraAboveRef = 14f;

  /// <summary>
  /// How far above the key-slot top the rule line center sits (ref px).
  /// Placed mid/low in the expanded gutter so ~6–8px margin remains from the row above at scale 1.
  /// </summary>
  public const float LiftAboveSlotRef = 9f;

  /// <summary>Stroke width multiplier vs chassis scale.</summary>
  public const float RuleThicknessScale = 2.75f;

  /// <summary>Vertical leg length multiplier vs chassis scale.</summary>
  public const float RuleDropScale = 7.0f;

  private const string DefaultText = "CLEAR";

  private static readonly Dictionary<string, Spec?> SpecCache =
    new(StringComparer.OrdinalIgnoreCase);

  /// <summary>Where the left vertical leg / horizontal rule starts on <see cref="Spec.LeftKey"/>.</summary>
  public enum LeftEdgeAlign
  {
    /// <summary>Near the left edge of the left key (default).</summary>
    KeyLeft = 0,

    /// <summary>
    /// At the horizontal mid of the left key — used for wide ENTER on HP-31E / HP-34C
    /// so the CLEAR rule begins over the cap center, not the left column edge.
    /// </summary>
    MidKey = 1,
  }

  /// <summary>JSON-configured span and label for one faceplate bracket.</summary>
  public readonly record struct Spec(
    int LeftKey,
    int RightKey,
    int TextCenterKey,
    string Text,
    LeftEdgeAlign LeftEdge = LeftEdgeAlign.KeyLeft);

  public static bool TryResolve(string? modelId, out Spec spec)
  {
    spec = default;
    if (string.IsNullOrWhiteSpace(modelId))
    {
      return false;
    }

    Spec? resolved = ResolveCached(modelId);
    if (resolved is null)
    {
      return false;
    }

    spec = resolved.Value;
    return true;
  }

  public static bool CoversKey(Spec spec, int keyChartIndex) =>
    keyChartIndex >= spec.LeftKey && keyChartIndex <= spec.RightKey;

  /// <summary>Visual row index of the bracketed key row, or -1 when absent.</summary>
  public static int FindBracketRow(IReadOnlyList<FaceplateCell> cells, string? modelId)
  {
    if (!TryResolve(modelId, out Spec spec))
    {
      return -1;
    }

    foreach (FaceplateCell cell in cells)
    {
      if (cell.KeyChartIndex == spec.LeftKey)
      {
        return cell.Row;
      }
    }

    return -1;
  }

  /// <summary>Rule line center sits in the expanded gutter above the key slot.</summary>
  public static float CenterY(float slotTopY, float scale) =>
    slotTopY - LiftAboveSlotRef * scale;

  /// <summary>Bottom of bracket word ink (legs may extend lower beside CapAbove).</summary>
  public static float TextBottom(float centerY, float scale)
  {
    const float fontRef = 10.5f;
    float textCenterY = centerY - scale * 2.2f;
    return textCenterY + fontRef * scale * 0.55f;
  }

  /// <summary>
  /// Approximate bottom of label ink + vertical legs (layout diagnostics).
  /// Uses Body.svg reference sizes so unit tests need no ImGui context.
  /// </summary>
  public static float ExtentBottom(float centerY, float scale)
  {
    float textBottom = TextBottom(centerY, scale);
    float textCenterY = centerY - scale * 2.2f;
    float legBottom = textCenterY + scale * RuleDropScale + scale * RuleThicknessScale * 0.5f;
    return MathF.Max(textBottom, legBottom);
  }

  /// <summary>
  /// Top of CapAbove legend ink (mirrors CapAbove placement just above the cap).
  /// Uses Body.svg reference sizes so unit tests need no ImGui context.
  /// </summary>
  public static float CapAboveLegendTop(float capMinY, float scale)
  {
    const float goldShiftRef = 12.25f;
    return capMinY - goldShiftRef * scale - scale * 1.2f;
  }

  /// <summary>
  /// True when the bracket word clears CapAbove legends. Legs may enter the CapAbove band beside labels.
  /// </summary>
  public static bool HasClearance(float slotTopY, float capMinY, float scale)
  {
    float centerY = CenterY(slotTopY, scale);
    float textBottom = TextBottom(centerY, scale);
    float capAboveTop = CapAboveLegendTop(capMinY, scale);
    return textBottom <= capAboveTop - scale * 0.5f;
  }

  /// <summary>
  /// Draw the configured bracket when this keypad row hosts <see cref="Spec.LeftKey"/>.
  /// Resolves left/right key rects from the row; no-op when the model has no bracket or span is incomplete.
  /// </summary>
  public static bool TryDrawForRow(
    ImDrawListPtr draw,
    string? modelId,
    IReadOnlyList<(int KeyChartIndex, RectF KeyRect)> rowKeys,
    float scale)
  {
    if (!TryResolve(modelId, out Spec spec))
    {
      return false;
    }

    RectF? left = null;
    RectF? right = null;
    bool hostsLeft = false;
    for (int i = 0; i < rowKeys.Count; i++)
    {
      (int index, RectF rect) = rowKeys[i];
      if (index == spec.LeftKey)
      {
        left = rect;
        hostsLeft = true;
      }

      if (index == spec.RightKey)
      {
        right = rect;
      }
    }

    if (!hostsLeft || left is null || right is null)
    {
      return false;
    }

    Draw(draw, in spec, left.Value, right.Value, scale);
    return true;
  }

  public static void Draw(
    ImDrawListPtr draw,
    in Spec spec,
    RectF leftKeyRect,
    RectF rightKeyRect,
    float scale)
  {
    if (leftKeyRect.Width <= 0f || rightKeyRect.Width <= 0f)
    {
      return;
    }

    string text = string.IsNullOrWhiteSpace(spec.Text) ? DefaultText : spec.Text;
    float fontSize = CalcFaceplateTypography.GoldShiftSmall(scale);
    float centerY = CenterY(leftKeyRect.Y, scale);

    Vector2 labelSize = MeasureArial(text, fontSize);
    float lineLeft = LeftRuleOuterX(in spec, leftKeyRect, scale);
    float lineRight = rightKeyRect.Max.X - scale * 3f;
    // Center the word across the effective left…right rule span (CLEAR / COMPUTE).
    float textCenterX = (lineLeft + lineRight) * 0.5f;
    float sidePad = fontSize * 0.34f + scale * 4.5f;
    uint color = CalcChassisPalette.GoldRule;
    float thickness = MathF.Max(1.5f, scale * RuleThicknessScale);
    float drop = scale * RuleDropScale;
    float cornerR = MathF.Max(thickness * 0.85f, scale * 1.8f);

    float textCenterY = centerY - scale * 2.2f;
    float textY = textCenterY - labelSize.Y * 0.5f;
    float lineY = textCenterY;
    float leftEndX = textCenterX - labelSize.X * 0.5f - sidePad;
    float rightStartX = textCenterX + labelSize.X * 0.5f + sidePad;

    DrawArialTop(
      draw,
      text,
      textCenterX - labelSize.X * 0.5f,
      textY,
      fontSize,
      CalcKeyLabelPalette.GoldOnBody);

    DrawRuleSide(draw, lineLeft, lineY, leftEndX, lineY, drop, cornerR, color, thickness, fromLeft: true);
    DrawRuleSide(draw, lineRight, lineY, rightStartX, lineY, drop, cornerR, color, thickness, fromLeft: false);
  }

  /// <summary>Outer X of the left vertical leg (KeyLeft inset or MidKey center).</summary>
  public static float LeftRuleOuterX(in Spec spec, RectF leftKeyRect, float scale) =>
    spec.LeftEdge == LeftEdgeAlign.MidKey
      ? leftKeyRect.X + leftKeyRect.Width * 0.5f
      : leftKeyRect.X + scale * 3f;

  private static Spec? ResolveCached(string modelId)
  {
    foreach (string id in CandidateModelIds(modelId))
    {
      if (SpecCache.TryGetValue(id, out Spec? cached))
      {
        if (cached is not null)
        {
          return cached;
        }

        continue;
      }

      Spec? loaded = LoadSpec(id);
      SpecCache[id] = loaded;
      if (loaded is not null)
      {
        return loaded;
      }
    }

    return null;
  }

  private static IEnumerable<string> CandidateModelIds(string modelId)
  {
    yield return modelId;
    string engineId = CalcModelIds.ToEngineId(modelId);
    if (!string.Equals(engineId, modelId, StringComparison.OrdinalIgnoreCase))
    {
      yield return engineId;
    }

    string shortId = CalcModelIds.ToShortId(modelId);
    if (!string.Equals(shortId, modelId, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(shortId, engineId, StringComparison.OrdinalIgnoreCase))
    {
      yield return shortId;
    }
  }

  private static Spec? LoadSpec(string modelId)
  {
    string path = Path.Combine(
      TeoCalcPaths.ResourcePath("Engine"),
      CalcModelIds.ToEngineId(modelId),
      "Program",
      "key.faceplate.json");
    if (!File.Exists(path))
    {
      return null;
    }

    string json = File.ReadAllText(path);
    Document? document = JsonSerializer.Deserialize<Document>(json, JsonOptions);
    BracketJson? bracket = document?.ClearBracket;
    if (bracket is null)
    {
      return null;
    }

    int left = bracket.LeftKey;
    int right = bracket.RightKey;
    int center = bracket.TextCenterKey ?? ((left + right) / 2);
    if (left < 0 || right <= left)
    {
      return null;
    }

    string text = string.IsNullOrWhiteSpace(bracket.Text) ? DefaultText : bracket.Text.Trim();
    LeftEdgeAlign leftEdge = ParseLeftEdge(bracket.LeftEdge);
    return new Spec(left, right, center, text, leftEdge);
  }

  private static LeftEdgeAlign ParseLeftEdge(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return LeftEdgeAlign.KeyLeft;
    }

    return value.Trim() switch
    {
      "Mid" or "MidKey" or "Center" or "MidEnter" => LeftEdgeAlign.MidKey,
      _ => LeftEdgeAlign.KeyLeft,
    };
  }

  private static void DrawRuleSide(
    ImDrawListPtr draw,
    float outerX,
    float lineY,
    float innerX,
    float innerY,
    float drop,
    float cornerR,
    uint color,
    float thickness,
    bool fromLeft)
  {
    float vertBottom = lineY + drop;
    draw.PathClear();
    if (fromLeft)
    {
      draw.PathLineTo(new Vector2(outerX, vertBottom));
      draw.PathLineTo(new Vector2(outerX, lineY + cornerR));
      draw.PathBezierQuadraticCurveTo(new Vector2(outerX, lineY), new Vector2(outerX + cornerR, lineY));
      draw.PathLineTo(new Vector2(innerX, innerY));
    }
    else
    {
      draw.PathLineTo(new Vector2(innerX, innerY));
      draw.PathLineTo(new Vector2(outerX - cornerR, lineY));
      draw.PathBezierQuadraticCurveTo(new Vector2(outerX, lineY), new Vector2(outerX, lineY + cornerR));
      draw.PathLineTo(new Vector2(outerX, vertBottom));
    }

    // Open path — RoundCorners* flags are for AddRect, not PathStroke (were no-ops here).
    draw.PathStroke(color, ImDrawFlags.None, thickness);
    // Explicit round tip on the downward leg (ImGui AA lines alone read thin at mid alpha).
    float tipR = thickness * 0.5f;
    draw.AddCircleFilled(new Vector2(outerX, vertBottom), tipR, color, 12);
  }

  private static Vector2 MeasureArial(string text, float fontSize) =>
    CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady
      ? CalcFaceplateFonts.MeasureArialBold(text, fontSize)
      : ImGui.GetFont().CalcTextSizeA(fontSize, float.MaxValue, 0f, text);

  private static void DrawArialTop(ImDrawListPtr draw, string text, float x, float y, float fontSize, uint color)
  {
    if (CalcFaceplateFonts.IsArialBoldReady || CalcFaceplateFonts.IsArialReady)
    {
      CalcFaceplateFonts.DrawArialBoldTop(draw, text, x, y, fontSize, color);
      return;
    }

    draw.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), color, text);
  }

  private static JsonSerializerOptions JsonOptions => new()
  {
    PropertyNameCaseInsensitive = true,
  };

  private sealed class Document
  {
    [JsonPropertyName("ClearBracket")]
    public BracketJson? ClearBracket { get; init; }
  }

  private sealed class BracketJson
  {
    [JsonPropertyName("LeftKey")]
    public int LeftKey { get; init; }

    [JsonPropertyName("RightKey")]
    public int RightKey { get; init; }

    [JsonPropertyName("TextCenterKey")]
    public int? TextCenterKey { get; init; }

    /// <summary>Bracket label; default CLEAR (HP-80 uses COMPUTE).</summary>
    [JsonPropertyName("Text")]
    public string? Text { get; init; }

    /// <summary>Left rule align: omit/Left = key left; Mid/MidKey = mid of LeftKey (wide ENTER).</summary>
    [JsonPropertyName("LeftEdge")]
    public string? LeftEdge { get; init; }
  }
}
