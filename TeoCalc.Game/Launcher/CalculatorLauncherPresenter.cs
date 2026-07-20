using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Firmware;

namespace TeoCalc.Game.Launcher;

/// <summary>Model picker presenter — renderer-agnostic.</summary>
public sealed class CalculatorLauncherPresenter
{
  private readonly CalculatorLauncherViewModel _viewModel;
  private readonly Func<string, bool>? _tryOpenEngineModel;

  private CalculatorLauncherPresenter(
    CalculatorLauncherViewModel viewModel,
    Func<string, bool>? tryOpenEngineModel)
  {
    _viewModel = viewModel;
    _tryOpenEngineModel = tryOpenEngineModel;
  }

  public CalculatorLauncherViewModel ViewModel => _viewModel;

  public static CalculatorLauncherPresenter CreateDefault(Func<string, bool>? tryOpenEngineModel = null)
  {
    List<CalculatorLauncherItem> items = [];
    foreach (string catalogId in HpCalcModelCatalog.SupportedModels)
    {
      string engineId = CalcModelIds.ToEngineId(catalogId);
      bool canOpen = CalcFirmwareGatewayLocator.Supports(engineId);
      items.Add(new CalculatorLauncherItem(
        catalogId,
        engineId,
        catalogId,
        CalcModelIds.ToProductLabel(catalogId),
        canOpen,
        canOpen ? "Emulator ready" : "Emulator pending"));
    }

    var viewModel = new CalculatorLauncherViewModel
    {
      Items = items,
      SelectedIndex = Math.Max(0, items.FindIndex(i => i.CanOpen)),
      StatusLine = items.Count == 0
        ? "No calculator models found."
        : "Select a model to open.",
    };

    var presenter = new CalculatorLauncherPresenter(viewModel, tryOpenEngineModel);
    presenter.RefreshStatus();
    return presenter;
  }

  public void Select(int index)
  {
    if (_viewModel.Items.Count == 0)
    {
      _viewModel.SelectedIndex = -1;
      return;
    }

    _viewModel.SelectedIndex = Math.Clamp(index, 0, _viewModel.Items.Count - 1);
    RefreshStatus();
  }

  public void MoveSelection(int delta)
  {
    if (_viewModel.Items.Count == 0 || delta == 0)
    {
      return;
    }

    Select((_viewModel.SelectedIndex + delta + _viewModel.Items.Count) % _viewModel.Items.Count);
  }

  public void MoveSelectionByGrid(int deltaColumn, int deltaRow, int columnCount)
  {
    if (_viewModel.Items.Count == 0)
    {
      return;
    }

    int columns = Math.Max(1, columnCount);
    int row = _viewModel.SelectedIndex / columns;
    int column = _viewModel.SelectedIndex % columns;
    int rowCount = (int)Math.Ceiling(_viewModel.Items.Count / (double)columns);
    int nextRow = Math.Clamp(row + deltaRow, 0, Math.Max(0, rowCount - 1));
    int nextColumn = Math.Clamp(column + deltaColumn, 0, columns - 1);
    int nextIndex = Math.Min(_viewModel.Items.Count - 1, (nextRow * columns) + nextColumn);
    Select(nextIndex);
  }

  public bool TryOpenSelected(out string engineModelId)
  {
    engineModelId = string.Empty;
    if (_viewModel.SelectedItem is not { } selected)
    {
      return false;
    }

    if (!selected.CanOpen)
    {
      _viewModel.StatusLine = $"{selected.ProductLabel}: {selected.Status}.";
      return false;
    }

    if (_tryOpenEngineModel is null)
    {
      engineModelId = selected.EngineModelId;
      _viewModel.StatusLine = $"Opening {selected.ProductLabel}…";
      return true;
    }

    if (_tryOpenEngineModel(selected.EngineModelId))
    {
      engineModelId = selected.EngineModelId;
      _viewModel.StatusLine = $"Opening {selected.ProductLabel}…";
      return true;
    }

    _viewModel.StatusLine = $"{selected.ProductLabel}: failed to open.";
    return false;
  }

  private void RefreshStatus()
  {
    if (_viewModel.SelectedItem is not { } selected)
    {
      return;
    }

    _viewModel.StatusLine = $"{selected.ProductLabel}: {selected.Status}.";
  }
}
