namespace TeoCalc.Rendering;

public sealed class ReferenceCalculatorCatalog
{
  private ReferenceCalculatorCatalog()
  {
  }

  public IReadOnlyCollection<ReferenceCalculatorEntry> Entries => [];

  public ReferenceCalculatorEntry? TryGet(string modelId) => null;

  public static ReferenceCalculatorCatalog CreateDefault() => new();
}
