using System.Numerics;
using TeoCalc.Core.Catalog;

namespace TeoCalc.Rendering.Faceplate;

public static class Hp21CalcBodyLayout
{
  public const string LayoutId = "hp21";

  public const float ReferenceWidth = 360f;

  public const float ReferenceHeight = 683f;

  private const float KeyWidth = 58f;

  private const float KeyHeight = 38f;

  private const float KeyGapX = 6f;

  private const float KeyGapY = 10f;

  private const float PanelX = 20f;

  private const float KeypadTop = 132f;

  private static CalcBodyLayout? _cached;

  public static CalcBodyLayout Instance => _cached ??= Create();

  public static CalcBodyLayout Create()
  {
    Dictionary<int, RectF> keySlots = new();
    foreach (FaceplateCell cell in CalcFaceplateLayout.GetPhysicalCells("Woodstock", "HP-21"))
    {
      if (TryGetKeyRect(cell, out RectF rect))
      {
        keySlots[cell.KeyChartIndex] = rect;
      }
    }

    RectF switchTrack = new(PanelX, 86f, ReferenceWidth - PanelX * 2f, 34f);
    float switchRowY = switchTrack.Y + switchTrack.Height * 0.5f - KeyHeight;
    float onOffX = switchTrack.X + switchTrack.Width * 0.28f;
    float angleX = switchTrack.X + switchTrack.Width * 0.72f;

    return new CalcBodyLayout
    {
      Id = LayoutId,
      ReferenceWidth = ReferenceWidth,
      ReferenceHeight = ReferenceHeight,
      DisplaySlot = new RectF(PanelX, 20f, ReferenceWidth - PanelX * 2f, 52f),
      SwitchSlot = switchTrack,
      KeypadSlot = new RectF(PanelX, KeypadTop, ReferenceWidth - PanelX * 2f, 510f),
      LogoSlot = new RectF(PanelX, 652f, ReferenceWidth - PanelX * 2f, 24f),
      CardSlotBand = null,
      SwitchRowLift = KeyHeight,
      SwitchLabelY = switchTrack.Y - 8f,
      OnOffSwitchCenter = new Vector2(onOffX, switchRowY),
      PrgmRunSwitchCenter = new Vector2(angleX, switchRowY),
      Switches = CalcSwitchCatalog.WoodstockAngle,
      KeySlots = keySlots,
    };
  }

  private static bool TryGetKeyRect(FaceplateCell cell, out RectF rect)
  {
    float rowTop = KeypadTop + 8f + cell.Row * (KeyHeight + KeyGapY);
    float x = PanelX + 8f + cell.Column * (KeyWidth + KeyGapX);
    float width = KeyWidth * cell.ColSpan + KeyGapX * Math.Max(0, cell.ColSpan - 1);
    rect = new RectF(x, rowTop, width, KeyHeight * cell.RowSpan + KeyGapY * Math.Max(0, cell.RowSpan - 1));
    return true;
  }
}
