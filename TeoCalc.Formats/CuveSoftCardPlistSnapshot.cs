namespace TeoCalc.Formats;

/// <summary>Decoded CuveSoft RPN-65 card plist (XML).</summary>
public sealed class CuveSoftCardPlistSnapshot
{
  public string? Title { get; init; }

  public string? Description { get; init; }

  public string? Usage { get; init; }

  public int? Category { get; init; }

  public int? CardType { get; init; }

  public int? CardPac { get; init; }

  public DateTimeOffset? Created { get; init; }

  public DateTimeOffset? Modified { get; init; }

  public IReadOnlyList<string> Labels { get; init; } = [];

  public IReadOnlyList<byte> ProgramCodes { get; init; } = [];
}
