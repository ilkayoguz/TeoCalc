namespace TeoCalc.Core.Firmware;

/// <summary>
/// Host wires the active firmware backend (emulator adapter or ClassicCpu) at startup.
/// Rendering/session depend only on this locator + <see cref="ICalcFirmwareGateway"/>.
/// </summary>
public static class CalcFirmwareGatewayLocator
{
  public static Func<string, ICalcFirmwareGateway>? Create { get; set; }

  public static Func<string, bool>? IsSupported { get; set; }

  public static Func<string, IReadOnlyList<string>>? GetAssetWarnings { get; set; }

  public static ICalcFirmwareGateway CreateGateway(string engineModelId)
  {
    if (Create is null)
    {
      throw new InvalidOperationException(
        "Firmware gateway is not wired. Call CalcFirmwareBootstrap.UseEmulatorAdapter() from the host.");
    }

    return Create(engineModelId);
  }

  public static bool Supports(string engineModelId) =>
    IsSupported?.Invoke(engineModelId) ?? false;

  public static IReadOnlyList<string> AssetWarnings(string engineModelId) =>
    GetAssetWarnings?.Invoke(engineModelId) ?? [];
}
