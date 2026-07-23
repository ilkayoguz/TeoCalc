using System.Numerics;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

public static class Hp65CalcBodyLayout
{
  public const string LayoutId = "hp65";

  private static CalcBodyLayout? _cached;

  public static CalcBodyLayout Instance => _cached ??= Create();

  public static CalcBodyLayout Create()
  {
    BodyFaceplateLayout.EnsureLoaded();

    Dictionary<int, RectF> keySlots = new();
    foreach (FaceplateCell cell in CalcFaceplateLayout.GetPhysicalCells("Classic", "T-65"))
    {
      if (BodyFaceplateLayout.TryGetKeyRect(cell.KeyChartIndex, out RectF rect))
      {
        keySlots[cell.KeyChartIndex] = rect;
      }
    }

    return new CalcBodyLayout
    {
      Id = LayoutId,
      ReferenceWidth = BodyFaceplateLayout.ReferenceWidth,
      ReferenceHeight = BodyFaceplateLayout.ReferenceHeight,
      DisplaySlot = BodyFaceplateLayout.DisplayWindow,
      SwitchSlot = BodyFaceplateLayout.SwitchTrack,
      KeypadSlot = BodyFaceplateLayout.KeypadPanel,
      LogoSlot = BodyFaceplateLayout.BrandPlate,
      CardSlotBand = BodyFaceplateLayout.CardSlotBand,
      SwitchRowLift = BodyFaceplateLayout.SwitchRowLift,
      SwitchLabelY = BodyFaceplateLayout.SwitchLabelY,
      OnOffSwitchCenter = BodyFaceplateLayout.OnOffSwitchCenter,
      PrgmRunSwitchCenter = BodyFaceplateLayout.PrgmRunSwitchCenter,
      KeySlots = keySlots,
    };
  }
}
