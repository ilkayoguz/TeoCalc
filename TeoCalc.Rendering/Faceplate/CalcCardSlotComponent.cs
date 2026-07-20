using System.Numerics;
using ImGuiNET;
using TeoCalc.Rendering;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Card-reader legend strip under the switch panel (NoCard / card captions).
/// Same height as the logo plate; outlined frame only. A–E keys sit tight below
/// so the strip reads as part of the function-key group. Labels centered per column.
/// </summary>
public static class CalcCardSlotComponent
{
  public const float GapBelowSwitchRef = 8f;

  /// <summary>
  /// Zero — combined with subtracting the keypad top gutter, A–E’s label band
  /// starts flush under the frame.
  /// </summary>
  public const float GapAboveKeypadRef = 0f;

  /// <summary>Matches the window logo band height.</summary>
  public static float HeightRef => CalcLogoPanelComponent.HeightRef;

  /// <summary>White legends inside the frame.</summary>
  public const uint LabelInk = 0xFFFFFFFF;

  /// <summary>White frame stroke.</summary>
  public const uint FrameInk = 0xFFFFFFFF;

  public const float FrameThicknessRef = 1.5f;

  public static readonly string[] NoCardLabels = CalcFaceplateLayout.CardSlotLabels;

  public static bool ModelHasCardSlot(string modelId) =>
    HeuristicHasCardSlot(modelId);

  public static bool HeuristicHasCardSlot(string modelId)
  {
    string id = modelId.Trim();
    if (id.StartsWith("HP-", StringComparison.OrdinalIgnoreCase))
    {
      id = id[3..];
    }

    return id is "65" or "67";
  }

  public static bool ModelHasCardSlot(CalcModelDefinition model) =>
    model.HasCardSlot ?? HeuristicHasCardSlot(model.Id);

  public static RectF ResolveSlotRef(float bandLeft, float bandWidth, float switchBottom) =>
    new(bandLeft, switchBottom + GapBelowSwitchRef, bandWidth, HeightRef);

  public static void Draw(
    ImDrawListPtr draw,
    RectF panel,
    CalcChassisMetrics metrics,
    Vector2 origin,
    IReadOnlyList<string>? labels = null,
    bool skipText = false)
  {
    if (panel.Width <= 0f || panel.Height <= 0f)
    {
      return;
    }

    float scale = metrics.Scale;
    float radius = Calc00dWireStyle.SwitchPanelRadiusRef * scale;
    float thickness = MathF.Max(1f, FrameThicknessRef * scale);
    draw.AddRect(
      panel.Min,
      panel.Max,
      FrameInk,
      radius,
      ImDrawFlags.RoundCornersAll,
      thickness);

    if (skipText)
    {
      return;
    }

    IReadOnlyList<string> captions = labels is { Count: > 0 } ? labels : NoCardLabels;
    float fontSize = MathF.Max(11f * scale, panel.Height * 0.42f);
    float centerY = panel.Y + panel.Height * 0.5f;

    int columns = Math.Min(CalcFaceplateLayout.Columns, captions.Count);
    for (int column = 0; column < columns; column++)
    {
      if (!TryGetColumnCenterX(metrics, origin, column, panel, out float centerX))
      {
        continue;
      }

      string caption = captions[column];
      if (IsNoCardColumn(column, caption))
      {
        Vector2 drawCenter = ClassicFaceplateGlyphs.CardSlotLabelDrawCenter(
          column,
          new Vector2(centerX, centerY),
          fontSize,
          scale);
        ClassicFaceplateGlyphs.DrawCardSlotLabel(draw, column, drawCenter, fontSize, LabelInk, scale);
        continue;
      }

      if (string.IsNullOrEmpty(caption))
      {
        continue;
      }

      ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(caption, fontSize);
      Vector2 topLeft = new(centerX - size.Width * 0.5f, centerY - size.Height * 0.5f);
      ClassicFaceplateGlyphs.DrawBodyLabel(draw, topLeft, caption, fontSize, LabelInk, scale);
    }
  }

  /// <summary>
  /// Prefer A–E key column centers (true horizontal alignment with the keys).
  /// Fall back to equal five-way split of the frame when key slots are unavailable.
  /// </summary>
  private static bool TryGetColumnCenterX(
    CalcChassisMetrics metrics,
    Vector2 origin,
    int column,
    RectF panel,
    out float centerX)
  {
    if (metrics.TryGetCardSlotColumn(origin, column, out RectF columnRect))
    {
      centerX = columnRect.X + columnRect.Width * 0.5f;
      return true;
    }

    if (metrics.Layout.TryGetKeySlot(column, out RectF key))
    {
      RectF scaled = new(
        origin.X + key.X * metrics.Scale,
        origin.Y + key.Y * metrics.Scale,
        key.Width * metrics.Scale,
        key.Height * metrics.Scale);
      centerX = scaled.X + scaled.Width * 0.5f;
      return true;
    }

    float cell = panel.Width / CalcFaceplateLayout.Columns;
    centerX = panel.X + cell * (column + 0.5f);
    return true;
  }

  private static bool IsNoCardColumn(int column, string caption)
  {
    if ((uint)column >= NoCardLabels.Length)
    {
      return false;
    }

    return string.Equals(caption, NoCardLabels[column], StringComparison.Ordinal);
  }
}
