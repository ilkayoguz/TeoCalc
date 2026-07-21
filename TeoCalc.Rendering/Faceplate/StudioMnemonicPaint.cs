using System.Numerics;
using ImGuiNET;

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

  /// <summary>Soft pointer / PC row highlight on dark background.</summary>
  public const uint PointerRowBg = 0x6628A0FFu;

  public const uint KeycapBezel = 0xFF141618u;

  /// <summary>Fixed listing keycap width (ref units; multiply by <see cref="StudioListingScale"/>).</summary>
  public const float KeycapWidthRef = 30f;

  /// <summary>Gap between paired listing keycaps (ref units).</summary>
  public const float KeycapGapRef = 4f;

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

  public static uint ColorForToken(string token)
  {
    ChromeForToken(token, null, out _, out uint ink);
    return ink;
  }

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
    float align = 0f)
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

      ChromeForToken(token, modelId, out uint face, out uint ink);
      DrawKeycap(token, face, ink);
      wrote = true;
    }
  }

  /// <summary>
  /// Content width for a Legend cell (same measure path as <see cref="DrawLegend"/>).
  /// Call under <see cref="PushListingScale"/>.
  /// </summary>
  public static float MeasureLegendContentWidth(string legend)
  {
    if (string.IsNullOrEmpty(legend))
    {
      return 0f;
    }

    float s = StudioListingScale;
    float fontSize = MathF.Max(14f, ImGui.GetFontSize() * 1.28f);
    string drawText = legend;
    ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);
    if (!IsAllDrawableFaceplateLegend(legend) && !StudioShiftLegend.IsAllAscii(legend))
    {
      drawText = StudioShiftLegend.ToAsciiLegend(legend);
      size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);
    }

    return MathF.Max(4f * s, size.Width);
  }

  /// <summary>
  /// Shift/function legend in its own table column — same CapAbove glyph path as the faceplate
  /// (<see cref="ClassicFaceplateGlyphs.DrawBodyLabel"/>), not ImGui default-font text.
  /// </summary>
  public static void DrawLegend(string legend, StudioShiftLegend.ShiftKind kind, float align = 0.5f)
  {
    if (string.IsNullOrEmpty(legend))
    {
      ImGui.TextUnformatted(string.Empty);
      return;
    }

    uint legendColor = kind switch
    {
      StudioShiftLegend.ShiftKind.Blue => CalcChassisPalette.BlueLabel,
      StudioShiftLegend.ShiftKind.GoldInverse => CalcChassisPalette.GoldLabel,
      StudioShiftLegend.ShiftKind.Black => CalcChassisPalette.KeyCapDarkText,
      StudioShiftLegend.ShiftKind.Gold => CalcChassisPalette.GoldLabel,
      _ => DefaultInk,
    };

    float s = StudioListingScale;
    // CapAbove / CapSkirt glyphs must read clearly in the listing (larger than keycap face).
    float fontSize = MathF.Max(14f, ImGui.GetFontSize() * 1.28f);
    float scale = MathF.Max(1f, s);
    ClassicFaceplateGlyphs.LabelSize size = ClassicFaceplateGlyphs.MeasureBodyLabel(legend, fontSize);
    // Vector glyphs always available for faceplate patterns; Arial atlas covers Latin-1 + π.
    // Fall back to ASCII only when neither path can paint a non-ASCII leftover.
    string drawText = legend;
    if (!IsAllDrawableFaceplateLegend(legend) && !StudioShiftLegend.IsAllAscii(legend))
    {
      drawText = StudioShiftLegend.ToAsciiLegend(legend);
      size = ClassicFaceplateGlyphs.MeasureBodyLabel(drawText, fontSize);
    }

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
    Vector2 textSize = ImGui.CalcTextSize(label);
    float padY = 2f * s;
    // Fixed footprint so Keys column stays content-sized (pair of caps + gap).
    float w = KeycapWidthRef * s;
    float h = MathF.Max(ImGui.GetTextLineHeight() + 2f * s, textSize.Y + padY * 2f);
    Vector2 p0 = ImGui.GetCursorScreenPos();
    Vector2 p1 = p0 + new Vector2(w, h);
    ImDrawListPtr draw = ImGui.GetWindowDrawList();
    float rounding = 3f * s;
    float bezel = MathF.Max(1f, 1f * s);
    draw.AddRectFilled(p0, p1, face, rounding);
    draw.AddRect(p0, p1, KeycapBezel, rounding, ImDrawFlags.None, bezel);
    Vector2 textPos = new(
      p0.X + (w - textSize.X) * 0.5f,
      p0.Y + (h - textSize.Y) * 0.5f);
    draw.AddText(textPos, ink, label);
    ImGui.Dummy(new Vector2(w, h));
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
