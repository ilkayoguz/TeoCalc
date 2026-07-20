namespace TeoCalc.Rendering.Faceplate;

/// <summary>Resolves <see cref="TeoCalcModelFaceplate.AnnotationStyleId"/> to modifier packs.</summary>
public static class CalcAnnotationStyleCatalog
{
  public const string ClassicFgId = "ClassicFg";
  public const string Hp35WhiteCapAboveId = "Hp35WhiteCapAbove";
  public const string ClassicGoldOnlyId = "ClassicGoldOnly";
  public const string ClassicDualCapAboveId = "ClassicDualCapAbove";
  public const string SpiceFghId = "SpiceFgh";
  public const string ClassicHp67FghId = "ClassicHp67Fgh";
  public const string NoneId = "None";

  public static bool TryResolve(
    string? annotationStyleId,
    out IReadOnlyList<CalcModifierAnnotationStyle> styles,
    out IReadOnlyList<CalcModifierKey> modifierKeys)
  {
    styles = [];
    modifierKeys = [];
    if (string.IsNullOrWhiteSpace(annotationStyleId))
    {
      return false;
    }

    switch (annotationStyleId.Trim())
    {
      case ClassicFgId:
        styles = CalcModifierPlacement.ClassicFg;
        modifierKeys = [CalcModifierKey.F, CalcModifierKey.G];
        return true;
      case Hp35WhiteCapAboveId:
        styles = CalcModifierPlacement.Hp35WhiteCapAbove;
        modifierKeys = [CalcModifierKey.F];
        return true;
      case ClassicGoldOnlyId:
        styles = CalcModifierPlacement.ClassicGoldOnly;
        modifierKeys = [CalcModifierKey.F];
        return true;
      case ClassicDualCapAboveId:
        styles = CalcModifierPlacement.ClassicDualCapAbove;
        modifierKeys = [CalcModifierKey.F, CalcModifierKey.G];
        return true;
      case SpiceFghId:
        styles = CalcModifierPlacement.SpiceFgh;
        modifierKeys = [CalcModifierKey.F, CalcModifierKey.G, CalcModifierKey.H];
        return true;
      case ClassicHp67FghId:
        styles = CalcModifierPlacement.ClassicHp67Fgh;
        modifierKeys = [CalcModifierKey.F, CalcModifierKey.G, CalcModifierKey.H];
        return true;
      case NoneId:
        styles = CalcModifierPlacement.None;
        modifierKeys = [];
        return true;
      default:
        return false;
    }
  }

  /// <summary>
  /// Migration-only fallback when rewriting Model.json without AnnotationStyleId.
  /// Runtime catalog resolve expects AnnotationStyleId from Model.json (M1).
  /// </summary>
  internal static string HeuristicId(string engineId, string catalogId)
  {
    bool hp34 = string.Equals(engineId, "HP-34", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-34C", StringComparison.OrdinalIgnoreCase);
    bool hp35 = string.Equals(engineId, "HP-35", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-35", StringComparison.OrdinalIgnoreCase);
    bool hp45 = string.Equals(engineId, "HP-45", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-45", StringComparison.OrdinalIgnoreCase);
    bool hp55 = string.Equals(engineId, "HP-55", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-55", StringComparison.OrdinalIgnoreCase);
    bool hp67 = string.Equals(engineId, "HP-67", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-67", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-67BE", StringComparison.OrdinalIgnoreCase);
    bool hp70 = string.Equals(engineId, "HP-70", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-70", StringComparison.OrdinalIgnoreCase);
    bool hp80 = string.Equals(engineId, "HP-80", StringComparison.OrdinalIgnoreCase)
      || string.Equals(catalogId, "HP-80", StringComparison.OrdinalIgnoreCase);

    if (hp70)
    {
      return NoneId;
    }

    if (hp67)
    {
      return ClassicHp67FghId;
    }

    if (hp34)
    {
      return SpiceFghId;
    }

    if (hp35)
    {
      return Hp35WhiteCapAboveId;
    }

    if (hp45 || hp80)
    {
      return ClassicGoldOnlyId;
    }

    if (hp55)
    {
      return ClassicDualCapAboveId;
    }

    return ClassicFgId;
  }
}
