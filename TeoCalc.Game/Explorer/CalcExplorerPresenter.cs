using TeoCalc.Core.Catalog;

namespace TeoCalc.Game.Explorer;

/// <summary>Keeps a passive <see cref="CalcExplorerViewModel"/> in sync with <see cref="ICalcExplorerSession"/>.</summary>
public sealed class CalcExplorerPresenter
{
  private readonly ICalcExplorerSession _session;

  public CalcExplorerPresenter(ICalcExplorerSession session)
  {
    _session = session;
    ViewModel = new CalcExplorerViewModel();
    SyncFromSession();
  }

  public CalcExplorerViewModel ViewModel { get; }

  public void Tick(float deltaSeconds)
  {
    _session.Tick(deltaSeconds);
    SyncFromSession();
  }

  public void SelectModel(int index)
  {
    _session.LoadModel(index);
    SyncFromSession();
  }

  public void SetPowerOn(bool on)
  {
    if (on)
    {
      _session.PowerOnResume();
    }
    else
    {
      _session.PowerOff();
    }

    SyncFromSession();
  }

  public void SyncFromSession()
  {
    ViewModel.DisplayName = _session.DisplayName;
    ViewModel.EngineModelId = _session.EngineModelId;
    ViewModel.ProductLabel = CalcModelIds.ToProductLabel(_session.EngineModelId);
    ViewModel.PowerOn = _session.PowerOn;
    ViewModel.ProgramMode = _session.ProgramMode;
    ViewModel.DisplayText = _session.DisplayText;
    ViewModel.IsDisplayVisible = _session.IsDisplayVisible();
    ViewModel.StatusLine =
      $"{ViewModel.ProductLabel}  power={(ViewModel.PowerOn ? "on" : "off")}  " +
      $"display={(ViewModel.IsDisplayVisible ? ViewModel.DisplayText : "off")}";
  }
}
