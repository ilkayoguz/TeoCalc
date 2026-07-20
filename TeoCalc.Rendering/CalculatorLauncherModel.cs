using TeoCalc.Game.Launcher;
using TeoGame.Presentation.Components;

namespace TeoCalc.Rendering;

/// <summary>Rendering adapter over <see cref="CalculatorLauncherPresenter"/> + TeoGame icon grid.</summary>
public sealed class CalculatorLauncherModel
{
  public const int ColumnCount = 5;

  private readonly CalculatorLauncherPresenter _presenter;
  private readonly CalculatorLauncherEntry[] _entries;
  private readonly IconGridComponent _iconGrid;

  private CalculatorLauncherModel(
    CalculatorLauncherPresenter presenter,
    CalculatorLauncherEntry[] entries,
    IconGridComponent iconGrid)
  {
    _presenter = presenter;
    _entries = entries;
    _iconGrid = iconGrid;
  }

  public IReadOnlyList<CalculatorLauncherEntry> Entries => _entries;

  public IconGridComponent IconGrid => _iconGrid;

  /// <summary>Keep keyboard navigation columns in sync with the drawn grid.</summary>
  public void EnsureColumnCount(int columns)
  {
    columns = Math.Max(1, columns);
    if (_iconGrid.ColumnCount != columns)
    {
      _iconGrid.ColumnCount = columns;
    }
  }

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
          item.ProductLabel,
          item.EngineModelId,
          item.Status,
          item.CanOpen,
          reference);
      })
      .ToArray();

    IconGridComponent iconGrid = new("teo-calc-launcher");
    iconGrid.Style = IconGridStyle.Tile;
    iconGrid.SetItems(
      entries
        .Select(e => new IconGridItem(e.ModelId, e.LauncherLabel, iconGlyph: null, enabled: true))
        .ToArray(),
      ColumnCount);
    iconGrid.SetFocusedIndexSilent(Math.Max(0, presenter.ViewModel.SelectedIndex));

    CalculatorLauncherModel model = new(presenter, entries, iconGrid);
    iconGrid.FocusedIndexChanged += index => model.SelectFromGrid(index);
    return model;
  }

  public void Select(int index)
  {
    _presenter.Select(index);
    _iconGrid.SetFocusedIndexSilent(SelectedIndex);
  }

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

  private void SelectFromGrid(int index)
  {
    if (index == _presenter.ViewModel.SelectedIndex)
    {
      return;
    }

    _presenter.Select(index);
  }
}
