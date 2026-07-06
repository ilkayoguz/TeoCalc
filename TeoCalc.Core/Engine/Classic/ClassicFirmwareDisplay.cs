namespace TeoCalc.Core.Engine.Classic;

/// <summary>
/// Panamatik <c>ShowDisplay</c> on firmware <c>act_a</c>/<c>act_b</c>.
/// </summary>
public static class ClassicFirmwareDisplay
{
  public static string BuildLedText(
    ClassicCpuState state,
    bool programMode = false,
    byte programEndState = 0)
  {
    bool displayOn = (state.Flags & ClassicCpuFlags.DisplayOn) != 0;
    return ClassicDisplayFormatter.ToLedFontText(
      state.Registers,
      displayOn,
      programMode,
      programEndState);
  }
}
