using TeoCalc.Core;
using TeoCalc.Panamatik;

namespace TeoCalc.Rendering;

public sealed class CalculatorLauncherModel
{
  private const string TeoCalcStatus = "Panamatik engine in TeoCalc.Panamatik";
  private const string PendingStatus = "Panamatik pending";

  private readonly CalculatorLauncherEntry[] _entries;

  private CalculatorLauncherModel(IEnumerable<CalculatorLauncherEntry> entries)
  {
    _entries = entries.ToArray();
    SelectedIndex = Math.Max(0, Array.FindIndex(_entries, entry => entry.CanOpenTeoCalc || entry.CanOpenReference));
    StatusLine = _entries.Length == 0
      ? "No calculator models found."
      : "All models run from consolidated Panamatik sources inside TeoCalc.Panamatik.";
  }

  public IReadOnlyList<CalculatorLauncherEntry> Entries => _entries;

  public int SelectedIndex { get; private set; }

  public string StatusLine { get; private set; }

  public CalculatorLauncherEntry? SelectedEntry =>
    SelectedIndex >= 0 && SelectedIndex < _entries.Length ? _entries[SelectedIndex] : null;

  public static CalculatorLauncherModel CreateDefault()
  {
    ReferenceCalculatorCatalog references = ReferenceCalculatorCatalog.CreateDefault();
    return new CalculatorLauncherModel(
      HpCalcModelCatalog.SupportedModels.Select(modelId =>
      {
        string teoCalcModelId = TeoCalcModelId(modelId);
        bool canOpenTeoCalc = PanamatikEngineFactory.IsSupported(TeoCalcModelId(modelId));
        return new CalculatorLauncherEntry(
          modelId,
          modelId,
          teoCalcModelId,
          canOpenTeoCalc ? TeoCalcStatus : PendingStatus,
          canOpenTeoCalc,
          references.TryGet(modelId));
      }));
  }

  public void Select(int index)
  {
    if (_entries.Length == 0)
    {
      SelectedIndex = -1;
      return;
    }

    SelectedIndex = Math.Clamp(index, 0, _entries.Length - 1);
    CalculatorLauncherEntry entry = _entries[SelectedIndex];
    StatusLine = $"{entry.DisplayName}: {entry.TeoCalcStatus}; {entry.ReferenceStatus}.";
  }

  public void MoveSelection(int delta)
  {
    if (_entries.Length == 0 || delta == 0)
    {
      return;
    }

    Select((SelectedIndex + delta + _entries.Length) % _entries.Length);
  }

  public void MoveSelectionByGrid(int deltaColumn, int deltaRow, int columnCount)
  {
    if (_entries.Length == 0)
    {
      return;
    }

    int columns = Math.Max(1, columnCount);
    int row = SelectedIndex / columns;
    int column = SelectedIndex % columns;
    int rowCount = (int)Math.Ceiling(_entries.Length / (double)columns);
    int nextRow = Math.Clamp(row + deltaRow, 0, Math.Max(0, rowCount - 1));
    int nextColumn = Math.Clamp(column + deltaColumn, 0, columns - 1);
    int nextIndex = Math.Min(_entries.Length - 1, (nextRow * columns) + nextColumn);
    Select(nextIndex);
  }

  public bool TryOpenSelectedTeoCalc(out CalculatorLauncherEntry entry)
  {
    entry = SelectedEntry ?? default;
    if (SelectedEntry is not { } selected)
    {
      return false;
    }

    if (selected.CanOpenTeoCalc)
    {
      entry = selected;
      StatusLine = $"Opening {selected.DisplayName} TeoCalc explorer.";
      return true;
    }

    StatusLine = $"{selected.DisplayName}: {selected.TeoCalcStatus}.";
    return false;
  }

  public void OpenReference(int index)
  {
    Select(index);
    if (SelectedEntry is not { Reference: { } reference })
    {
      StatusLine = $"{SelectedEntry?.DisplayName ?? "Calculator"}: Reference pending.";
      return;
    }

    ReferenceCalculatorLauncher.TryLaunch(reference, out string status);
    StatusLine = status;
  }

  private static bool HasTeoCalcModel(string modelId)
  {
    try
    {
      return File.Exists(TeoCalcPaths.ResourcePath(Path.Combine("Engine", modelId, "Model.json")));
    }
    catch (DirectoryNotFoundException)
    {
      return false;
    }
  }

  private static string TeoCalcModelId(string modelId) =>
    modelId switch
    {
      "HP-29C" => "HP-29",
      "HP-31E" => "HP-31",
      "HP-32E" => "HP-32",
      "HP-34C" => "HP-34",
      "HP-37E" => "HP-37",
      "HP-38E" => "HP-38",
      _ => modelId,
    };
}
