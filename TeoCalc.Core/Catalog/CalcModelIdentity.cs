namespace TeoCalc.Core.Catalog;

/// <summary>
/// Canonical model identity: catalog / engine / short / product label / family in one place.
/// Prefer <see cref="CalcModelIds.Resolve"/> over ad-hoc ToEngineId/ToShortId/ToProductLabel chains.
/// </summary>
public sealed record CalcModelIdentity(
  string CatalogId,
  string EngineId,
  string ShortId,
  string ProductLabel,
  string Family);
