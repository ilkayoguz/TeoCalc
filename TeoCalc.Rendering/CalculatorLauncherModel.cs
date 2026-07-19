using TeoCalc.Game.Launcher;

namespace TeoCalc.Rendering;

/// <summary>Rendering adapter over <see cref="CalculatorLauncherPresenter"/>.</summary>
public sealed class CalculatorLauncherModel
{
  private readonly CalculatorLauncherPresenter _presenter;
  private readonly CalculatorLauncherEntry[] _entries;

  private CalculatorLauncherModel(CalculatorLauncherPresenter presenter, CalculatorLauncherEntry[] entries)
  {
    _presenter = presenter;
    _entries = entries;
  }

  public IReadOnlyList<CalculatorLauncherEntry> Entries => _entries;

  public int SelectedIndex
  {
    get => _presenter.ViewModel.SelectedIndex;
    private set => _presenter.Select(value);
  }

  public string StatusLine => _presenter.ViewModel.StatusLine;

  public CalculatorLauncherEntry? SelectedEntry =>
    SelectedIndex >= 0 && SelectedIndex < _entries.Length ? _entries[SelectedIndex] : null;

  public static CalculatorLauncherModel CreateDefault()
  {
    CalculatorLauncherPresenter presenter = CalculatorLauncherPresenter.CreateDefault(engineId =>
      CalcExplorerApp.TryOpenModelWindow(engineId, out _));

    CalculatorLauncherEntry[] entries = presenter.ViewModel.Items
      .Select(item =>
      {
        ReferenceCalculatorEntry? reference = ReferenceCalculatorCatalog.CreateDefault().TryGet(item.CatalogModelId);
        return new CalculatorLauncherEntry(
          item.CatalogModelId,
          item.DisplayName,
          item.EngineModelId,
          item.Status,
          item.CanOpen,
          reference);
      })
      .ToArray();

    return new CalculatorLauncherModel(presenter, entries);
  }

  public void Select(int index) =>
    _presenter.Select(index);

  public void MoveSelection(int delta) =>
    _presenter.MoveSelection(delta);

  public void MoveSelectionByGrid(int deltaColumn, int deltaRow, int columnCount) =>
    _presenter.MoveSelectionByGrid(deltaColumn, deltaRow, columnCount);

  public bool TryOpenSelectedTeoCalc(out CalculatorLauncherEntry entry)
  {
    entry = SelectedEntry ?? default;
    if (!_presenter.TryOpenSelected(out _))
    {
      return false;
    }

    entry = SelectedEntry ?? default;
    return true;
  }

  public void OpenReference(int index)
  {
    Select(index);
    if (SelectedEntry is not { Reference: { } reference })
    {
      return;
    }

    ReferenceCalculatorLauncher.TryLaunch(reference, out _);
  }
}
