using System.Numerics;
using ImGuiNET;
using TeoCalc.Formats;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>
/// Studio mnemonic paint: HP-style keycap widgets + shift legends on a dark code pane.
/// Cap face/ink come from the same <see cref="CalcKeyLabelPalette.PrimaryOnCap"/> source
/// as the physical faceplate.
/// </summary>
public static class StudioMnemonicPaint
{
  /// <summary>
  /// Single readability scale for Studio listing chrome (keycaps, LED digits, pads, gaps).
  /// Apply via <see cref="PushListingScale"/> around a pane, or rely on draw helpers that scale geometry.
  /// </summary>
  public const float StudioListingScale = 1.30f;

  /// <summary>Dark charcoal code pane (faceplate-adjacent; keycaps need contrast).</summary>
  public const uint CodePaneBg = 0xFF2C3034u;

  /// <summary>Default ink on dark code background (non-keycap UI).</summary>
  public const uint DefaultInk = 0xFFE8E6E2u;

  /// <summary>Live program step / PTR marker (▶ in # column) — amber, not a row wash.</summary>
  public const uint PointerMarkerInk = 0xFF3AD4FFu;

  /// <summary>Studio breakpoint disc in the # column (VS-like crimson).</summary>
  public const uint BreakpointMarkerInk = 0xFF2D2DE8u;

  /// <summary>Selected listing row (cursor / click) — soft cool blue-gray, distinct from PC arrow.</summary>
  public const uint SelectionRowBg = 0x50786848u;

  /// <summary>Selected row hover.</summary>
  public const uint SelectionRowHoveredBg = 0x62887858u;

  /// <summary>Selected row active / pressed.</summary>
  public const uint SelectionRowActiveBg = 0x74988868u;

  public const uint KeycapBezel = 0xFF141618u;

  /// <summary>Fixed listing keycap width (ref units; multiply by <see cref="StudioListingScale"/>).</summary>
  public const float KeycapWidthRef = 30f;

  /// <summary>Gap between paired listing keycaps (ref units).</summary>
  public const float KeycapGapRef = 4f;

  /// <summary>
  /// Vertical pad inside listing keycaps (ref units × <see cref="StudioListingScale"/>).
  /// Slightly above the old 2 px-ref so descenders clear the bezel after ink centering.
  /// </summary>
  public const float KeycapPadYRef = 3.25f;

  public static void PushListingScale() => ImGui.SetWindowFontScale(StudioListingScale);

  public static void PopListingScale() => ImGui.SetWindowFontScale(1f);

  /// <summary>
  /// Horizontal align within the current table cell / content region.
  /// <paramref name="align"/>: 0 = left, 0.5 = center, 1 = right.
  /// </summary>
  public static void AlignCursorForContent(float contentWidth, float align)
  {
    if (align <= 0f || contentWidth <= 0f)
    {
      return;
    }

    float avail = ImGui.GetContentRegionAvail().X;
    float pad = avail - contentWidth;
    if (pad > 0f)
    {
      ImGui.SetCursorPosX(ImGui.GetCursorPosX() + pad * Math.Clamp(align, 0f, 1f));
    }
  }

  /// <summary>Width of N fixed-size listing keycaps including gaps — ref units.</summary>
  public static float MeasureKeycapsWidthRef(int tokenCount)
  {
    if (tokenCount <= 0)
    {
      return 0f;
    }

    return tokenCount * KeycapWidthRef + Math.Max(0, tokenCount - 1) * KeycapGapRef;
  }

  /// <summary>
  /// Listing row / keycap content height under the current font + <see cref="StudioListingScale"/>.
  /// Uses typographic ascent−descent (em-box), not glyph ink.
  /// </summary>
  public static float ListingRowContentHeight()
  {
    float s = StudioListingScale;
    return TypographicEmHeight() + KeycapPadYRef * s * 2f;
  }

  /// <summary>Scaled em-box height from font ascent/descent (fallback: text line height).</summary>
  public static float TypographicEmHeight()
  {
    ImFontPtr font = ImGui.GetFont();
    float fontSize = ImGui.GetFontSize();
    float ascent = font.Ascent;
    float descent = font.Descent;
    float baseSize = font.FontSize;
    if (baseSize > 0.01f && (ascent - descent) > 0.01f)
    {
      return (ascent - descent) * (fontSize / baseSize);
    }

    return MathF.Max(fontSize, ImGui.GetTextLineHeight());
  }

  public static uint ColorForToken(string token)
  {
    ChromeForToken(token, null, out _, out uint ink);
    return ink;
  }

  public static void ChromeForLabelKey(out uint face, out uint ink) =>
    ApplyStyle(CalcButtonStyle.Black, out face, out ink);

  public static void ChromeForToken(string token, string? modelId, out uint face, out uint ink)
  {
    ApplyStyle(CalcButtonStyle.Black, out face, out ink);

    if (string.IsNullOrEmpty(token))
    {
      return;
    }

    string t = token.Trim();
    if (t.Length == 0)
    {
      return;
    }

    // A–E strip label keys: fixed black cap, white ink (never faceplate style overrides).
    if (ClassicCardStripLabels.TryGetStripColumn(t, out _))
    {
      ChromeForLabelKey(out face, out ink);
      return;
    }

    if (!string.IsNullOrWhiteSpace(modelId)
        && StudioShiftLegend.TryFindFaceplateEntry(modelId, t, out _, out ClassicKeyFaceplateLegend.KeyFaceplateEntry entry)
        && !string.IsNullOrWhiteSpace(entry.Style)
        && CalcKeyStyleResolver.TryParse(entry.Style, out CalcButtonStyle fromFaceplate))
    {
      ApplyStyle(fromFaceplate, out face, out ink);
      return;
    }

    if (string.Equals(t, "f", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "f-1", StringComparison.OrdinalIgnoreCase))
    {
      ApplyStyle(CalcButtonStyle.Orange, out face, out ink);
      return;
    }

    if (string.Equals(t, "g", StringComparison.OrdinalIgnoreCase))
    {
      ApplyStyle(CalcButtonStyle.Blue, out face, out ink);
      return;
    }

    if (string.Equals(t, "h", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "LBL", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "GSB", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "GTO", StringComparison.OrdinalIgnoreCase))
    {
      ApplyStyle(CalcButtonStyle.Olive, out face, out ink);
      return;
    }

    if (IsDigitToken(t))
    {
      ApplyStyle(CalcButtonStyle.White, out face, out ink);
      return;
    }

    if (t.EndsWith("-1", StringComparison.Ordinal) && t.Length > 2)
    {
      ApplyStyle(CalcButtonStyle.Orange, out face, out ink);
      return;
    }

    // Default: dark keycap, light CapFace ink (museum key surface).
    ApplyStyle(CalcButtonStyle.Black, out face, out ink);
  }

  /// <summary>Colored mnemonic as keycaps only (legends live in the Legend column).</summary>
  public static void DrawMnemonicKeycaps(
    string mnemonic,
    string? modelId,
    string? previousMnemonic = null,
    float align = 0f,
    StudioListingView.MergeKind? rowKind = null)
  {
    _ = previousMnemonic;
    if (string.IsNullOrEmpty(mnemonic))
    {
      ImGui.TextUnformatted(string.Empty);
      return;
    }

    List<string> tokens = Tokenize(mnemonic);
    if (tokens.Count == 0)
    {
      ImGui.TextUnformatted(mnemonic);
      return;
    }

    float s = StudioListingScale;
    float gap = KeycapGapRef * s;
    AlignCursorForContent(MeasureKeycapsWidthRef(tokens.Count) * s, align);
    float startX = ImGui.GetCursorPosX();
    float y = ImGui.GetCursorPosY();
    bool wrote = false;

    for (int i = 0; i < tokens.Count; i++)
    {
      string token = tokens[i];
      if (wrote)
      {
        ImGui.SameLine(0f, gap);
      }
      else
      {
        ImGui.SetCursorPos(new Vector2(startX, y));
      }

      if (IsLabelTargetKeycap(token, i, tokens, rowKind))
      {
        ChromeForLabelKey(out uint face, out uint ink);
        DrawKeycap(token, face, ink);
      }
      else
      {
        ChromeForToken(token, modelId, out uint face, out uint ink);
        DrawKeycap(token, face, ink);
      }

      wrote = true;
    }
  }

  /// <summary>
  /// Faceplate label keys (A–E, numeric LBL/GTO/GSB targets): black cap, white ink.
  /// </summary>
  private static bool IsLabelTargetKeycap(
    string token,
    int index,
    IReadOnlyList<string> tokens,
    StudioListingView.MergeKind? rowKind)
  {
    if (!ClassicCardStripLabels.IsFaceplateLabelKey(token))
    {
      return false;
    }

    if (ClassicCardStripLabels.TryGetStripColumn(token, out _))
    {
      return true;
    }

    if (rowKind == StudioListingView.MergeKind.LabelPair)
    {
      return true;
    }

    if (index > 0)
    {
      string prev = tokens[index - 1];
      if (string.Equals(prev, "LBL", StringComparison.OrdinalIgnoreCase)
          || string.Equals(prev, "GTO", StringComparison.OrdinalIgnoreCase)
          || string.Equals(prev, "GSB", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// CapAbove / CapSkirt listing font size (larger than keycap face for readability).
  /// </summary>
  public static float LegendFontSize() => MathF.Max(14f, ImGui.GetFontSize() * 1.28f);

  /// <summary>Draw scale for <see cref="ClassicFaceplateGlyphs.DrawBodyLabel"/> in Studio chrome.</summary>
  public static float LegendDrawScale() => MathF.Max(1f, StudioListingScale);

  /// <summary>
  /// Legend string ready for <see cref="ClassicFaceplateGlyphs"/> — ASCII fallback only when
  /// neither Arial Latin-1/π nor known vector patterns can paint a leftover codepoint.
  /// </summary>
  public static string PrepareDrawableLegend(string legend)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return legend;
    }

    if (!IsAllDrawableFaceplateLegend(legend) && !StudioShiftLegend.IsAllAscii(legend))
    {
      return StudioShiftLegend.ToAsciiLegend(legend);
    }

    return legend;
  }

  /// <summary>
  /// Content width for a Legend cell (same measure path as <see cref="DrawLegend"/>).
  /// Call under <see cref="PushListingScale"/>.
  /// </summary>
  public static float MeasureLegendContentWidth(
    string legend,
    StudioShiftLegend.ShiftKind kind = StudioShiftLegend.ShiftKind.None)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return 0f;
    }

    float s = StudioListingScale;
    float fontSize = LegendFontSize();
    string drawText = PrepareDrawableLegend(legend);
    ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);

    float textW = MathF.Max(4f * s, size.Width);
    if (kind == StudioShiftLegend.ShiftKind.CardStrip)
    {
      return textW + CardStripChipPadX(s) * 2f;
    }

    return textW;
  }

  /// <summary>
  /// Shift/function legend in its own table column — same CapAbove glyph path as the faceplate
  /// (<see cref="ClassicFaceplateGlyphs.DrawBodyLabel"/>), not ImGui default-font text.
  /// Mag-card captions use near-black chip + white text; no-card built-ins use white strip ink only.
  /// </summary>
  public static void DrawLegend(string legend, StudioShiftLegend.ShiftKind kind, float align = 0.5f)
  {
    if (string.IsNullOrEmpty(legend))
    {
      ImGui.TextUnformatted(string.Empty);
      return;
    }

    float s = StudioListingScale;
    float fontSize = LegendFontSize();
    float scale = LegendDrawScale();
    string drawText = PrepareDrawableLegend(legend);
    ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);

    if (kind == StudioShiftLegend.ShiftKind.CardStrip)
    {
      DrawCardStripCaptionChip(drawText, size, fontSize, scale, s, align);
      return;
    }

    uint legendColor = kind switch
    {
      StudioShiftLegend.ShiftKind.Blue => CalcChassisPalette.BlueLabel,
      StudioShiftLegend.ShiftKind.GoldInverse => CalcChassisPalette.GoldLabel,
      StudioShiftLegend.ShiftKind.Black => CalcChassisPalette.KeyCapDarkText,
      StudioShiftLegend.ShiftKind.Gold => CalcChassisPalette.GoldLabel,
      StudioShiftLegend.ShiftKind.NoCardStrip => CalcCardSlotComponent.LabelInk,
      _ => DefaultInk,
    };

    float contentW = MathF.Max(4f * s, size.Width);
    AlignCursorForContent(contentW, align);
    Vector2 p0 = ImGui.GetCursorScreenPos();
    float lineH = ImGui.GetTextLineHeight();
    float h = MathF.Max(lineH, size.Height);
    float y = p0.Y + MathF.Max(0f, (h - size.Height) * 0.5f);
    ClassicFaceplateGlyphs.DrawBodyLabel(
      ImGui.GetWindowDrawList(),
      new Vector2(p0.X, y),
      drawText,
      fontSize,
      legendColor,
      scale);
    ImGui.Dummy(new Vector2(contentW, h));
  }

  /// <summary>
  /// Draw a CapAbove / CapSkirt legend string at an absolute screen position
  /// (flowchart symbols, overlays) — not ImGui default-font <c>AddText</c>.
  /// </summary>
  public static void DrawDrawableLegendAt(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string legend,
    uint color)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return;
    }

    string drawText = PrepareDrawableLegend(legend);
    ClassicFaceplateGlyphs.DrawBodyLabel(
      draw,
      topLeft,
      drawText,
      LegendFontSize(),
      color,
      LegendDrawScale());
  }

  /// <summary>Measure one drawable legend line (after <see cref="PrepareDrawableLegend"/>).</summary>
  public static ClassicFaceplateGlyphs.LabelSize MeasureDrawableLegend(string legend)
  {
    string drawText = PrepareDrawableLegend(legend ?? string.Empty);
    return ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, LegendFontSize());
  }

  private static float CardStripChipPadX(float listingScale) => MathF.Max(3f, 4f * listingScale);

  private static float CardStripChipPadY(float listingScale) => MathF.Max(1f, 2f * listingScale);

  /// <summary>
  /// Mag-card strip look: near-black rounded chip + white caption (same fill/ink as inserted card).
  /// </summary>
  private static void DrawCardStripCaptionChip(
    string drawText,
    ClassicFaceplateGlyphs.LabelSize size,
    float fontSize,
    float scale,
    float listingScale,
    float align)
  {
    float padX = CardStripChipPadX(listingScale);
    float padY = CardStripChipPadY(listingScale);
    float textW = MathF.Max(4f * listingScale, size.Width);
    float chipW = textW + padX * 2f;
    float lineH = ImGui.GetTextLineHeight();
    float chipH = MathF.Max(lineH, size.Height + padY * 2f);
    float radius = MathF.Max(1f, 2f * listingScale);

    AlignCursorForContent(chipW, align);
    Vector2 p0 = ImGui.GetCursorScreenPos();
    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    draw.AddRectFilled(
      p0,
      new Vector2(p0.X + chipW, p0.Y + chipH),
      CalcCardSlotComponent.CaptionChipFill,
      radius);
    float textX = p0.X + (chipW - size.Width) * 0.5f;
    float textY = p0.Y + MathF.Max(0f, (chipH - size.Height) * 0.5f);
    ClassicFaceplateGlyphs.DrawBodyLabel(
      draw,
      new Vector2(textX, textY),
      drawText,
      fontSize,
      CalcCardSlotComponent.LabelInk,
      scale);
    ImGui.Dummy(new Vector2(chipW, chipH));
  }

  /// <summary>
  /// True when every codepoint is Latin-1 / π (Arial faceplate atlas) or a known
  /// <see cref="ClassicFaceplateGlyphs"/> vector pattern (√ ↔ ↓ → ≠ ≤ …).
  /// </summary>
  private static bool IsAllDrawableFaceplateLegend(string legend)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return true;
    }

    // Faceplate Draw() consumes multi-char patterns; a short probe via measure is enough
    // for Studio — unknown BMP leftovers still measure, but Arial would show "?".
    foreach (char c in legend)
    {
      if (c <= 0xFF || c == '\u03c0')
      {
        continue;
      }

      // Common CapAbove / CapSkirt operators drawn as vectors (or composed) by ClassicFaceplateGlyphs.
      if (c is '\u2192' or '\u2190' or '\u2194' or '\u2193' or '\u2191'
          or '\u2260' or '\u2264' or '\u2265'
          or '\u221a' or '\u00b2' or '\u03a3' or '\u0394' or '\u2206'
          or '\u222b' or '\u00b0' or '\u00d7' or '\u00f7' or '\u2212'
          or '\u207b' or '\u00b9')
      {
        continue;
      }

      return false;
    }

    return true;
  }

  /// <summary>Machine-column LED chrome: dark glass + LED digit ink (LED font when loaded).</summary>
  public static void DrawMachineLedCell(string text, float align = 0f)
  {
    if (string.IsNullOrEmpty(text))
    {
      ImGui.TextUnformatted(string.Empty);
      return;
    }

    float s = StudioListingScale;
    ImFontPtr font = CalcFaceplateFonts.IsLedDisplayReady
      ? CalcFaceplateFonts.LedDisplay
      : ImGui.GetFont();
    // GetFontSize already reflects PushListingScale when active; still multiply base LED ratio.
    float fontSize = ImGui.GetFontSize() * (CalcFaceplateFonts.IsLedDisplayReady ? 0.92f : 1f);
    Vector2 textSize = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, text);
    float padX = 5f * s;
    float padY = 2f * s;
    float w = MathF.Max(28f * s, textSize.X + padX * 2f);
    float h = MathF.Max(ImGui.GetTextLineHeight() + 2f * s, textSize.Y + padY * 2f);
    AlignCursorForContent(w, align);
    Vector2 p0 = ImGui.GetCursorScreenPos();
    Vector2 p1 = p0 + new Vector2(w, h);
    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    float rounding = 2f * s;
    float bezel = MathF.Max(1f, 1f * s);
    draw.AddRectFilled(p0, p1, CalcChassisPalette.DisplayGlass, rounding);
    draw.AddRect(p0, p1, KeycapBezel, rounding, ImDrawFlags.None, bezel);
    Vector2 textPos = new(
      p0.X + (w - textSize.X) * 0.5f,
      p0.Y + (h - textSize.Y) * 0.5f);
    if (CalcFaceplateFonts.IsLedDisplayReady)
    {
      draw.AddText(
        font,
        fontSize,
        textPos + new Vector2(0.4f * s, 0.4f * s),
        CalcChassisPalette.DisplayDigitGlow,
        text);
    }

    draw.AddText(font, fontSize, textPos, CalcChassisPalette.DisplayDigit, text);
    ImGui.Dummy(new Vector2(w, h));
  }

  /// <summary>Backward-compatible flat colored text (prefer <see cref="DrawMnemonicKeycaps"/>).</summary>
  public static void DrawColoredMnemonicLine(string mnemonic) =>
    DrawMnemonicKeycaps(mnemonic, modelId: null, previousMnemonic: null);

  public static void DrawKeycap(string label, uint face, uint ink)
  {
    float s = StudioListingScale;
    float w = KeycapWidthRef * s;
    float h = ListingRowContentHeight();
    Vector2 p0 = ImGui.GetCursorScreenPos();
    DrawKeycapAt(ImGui.GetWindowDrawList(), p0, label, face, ink);
    ImGui.Dummy(new Vector2(w, h));
  }

  /// <summary>
  /// Keycap at an absolute screen position (flowchart START chrome) — no ImGui Dummy.
  /// </summary>
  public static void DrawKeycapAt(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string label,
    uint face,
    uint ink)
  {
    float s = StudioListingScale;
    ImFontPtr font = ImGui.GetFont();
    float fontSize = ImGui.GetFontSize();
    float padY = KeycapPadYRef * s;
    float w = KeycapWidthRef * s;
    float h = ListingRowContentHeight();
    Vector2 p0 = topLeft;
    Vector2 p1 = p0 + new Vector2(w, h);
    float rounding = 3f * s;
    float bezel = MathF.Max(1f, 1f * s);
    draw.AddRectFilled(p0, p1, face, rounding);
    draw.AddRect(p0, p1, KeycapBezel, rounding, ImDrawFlags.None, bezel);

    CalcFaceplateFonts.FontInkBounds inkBounds =
      CalcFaceplateFonts.MeasureFontInk(font, fontSize, label);
    Vector2 textPos;
    if (inkBounds.Height > 0.01f)
    {
      textPos = CalcFaceplateBandLabel.TopLeftForBandInk(p0, p1, inkBounds);
    }
    else
    {
      Vector2 textSize = font.CalcTextSizeA(fontSize, float.MaxValue, 0f, label);
      float emH = TypographicEmHeight();
      textPos = new(
        p0.X + (w - textSize.X) * 0.5f,
        p0.Y + padY + MathF.Max(0f, (emH - textSize.Y) * 0.5f));
    }

    draw.AddText(font, fontSize, textPos, ink, label);
  }

  /// <summary>
  /// Mag-card strip chip + white legend at an absolute position (no ImGui Dummy).
  /// </summary>
  public static void DrawCardStripLegendAt(
    ImDrawListPtr draw,
    Vector2 topLeft,
    string legend,
    float maxWidth,
    float rowHeight)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return;
    }

    float s = StudioListingScale;
    float fontSize = LegendFontSize();
    float scale = LegendDrawScale();
    string drawText = PrepareDrawableLegend(legend);
    ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);
    float padX = CardStripChipPadX(s);
    float padY = CardStripChipPadY(s);
    float textW = MathF.Min(MathF.Max(4f * s, size.Width), MathF.Max(4f, maxWidth - padX * 2f));
    float chipW = MathF.Min(maxWidth, textW + padX * 2f);
    float chipH = MathF.Max(rowHeight, size.Height + padY * 2f);
    float radius = MathF.Max(1f, 2f * s);
    draw.AddRectFilled(
      topLeft,
      new Vector2(topLeft.X + chipW, topLeft.Y + chipH),
      CalcCardSlotComponent.CaptionChipFill,
      radius);
    float textX = topLeft.X + (chipW - size.Width) * 0.5f;
    float textY = topLeft.Y + MathF.Max(0f, (chipH - size.Height) * 0.5f);
    ClassicFaceplateGlyphs.DrawBodyLabel(
      draw,
      new Vector2(textX, textY),
      drawText,
      fontSize,
      CalcCardSlotComponent.LabelInk,
      scale);
  }

  public static List<string> Tokenize(string mnemonic)
  {
    List<string> tokens = [];
    ReadOnlySpan<char> span = mnemonic.AsSpan();
    int i = 0;
    while (i < span.Length)
    {
      while (i < span.Length && char.IsWhiteSpace(span[i]))
      {
        i++;
      }

      if (i >= span.Length)
      {
        break;
      }

      int start = i;
      while (i < span.Length && !char.IsWhiteSpace(span[i]))
      {
        i++;
      }

      tokens.Add(mnemonic.Substring(start, i - start));
    }

    return tokens;
  }

  private static void ApplyStyle(CalcButtonStyle style, out uint face, out uint ink)
  {
    face = FaceForStyle(style);
    ink = CalcKeyLabelPalette.PrimaryOnCap(style);
  }

  private static uint FaceForStyle(CalcButtonStyle style) =>
    style switch
    {
      CalcButtonStyle.Orange => CalcChassisPalette.KeyOrangeFace,
      CalcButtonStyle.Blue => CalcChassisPalette.KeyBlueFace,
      CalcButtonStyle.White => CalcChassisPalette.KeyWhiteFace,
      CalcButtonStyle.Grey => CalcChassisPalette.KeyGreyFace,
      CalcButtonStyle.LightGrey => CalcChassisPalette.KeyLightGreyFace,
      CalcButtonStyle.DarkGrey => CalcChassisPalette.KeyDarkGreyFace,
      CalcButtonStyle.Olive => CalcChassisPalette.KeyOliveFace,
      CalcButtonStyle.Cement => CalcChassisPalette.KeyCementFace,
      _ => CalcChassisPalette.KeyBlackFace,
    };

  private static bool IsDigitToken(string token) =>
    token.Length == 1 && char.IsDigit(token[0]);
}
