namespace TeoCalc.Core.Engine.Classic;

/// <summary>
/// Panamatik <c>ShowDisplay</c> on firmware <c>act_a</c>/<c>act_b</c>.
/// During idle the wait loop multiplexes the LED scan through A/B; we latch when the
/// decimal mask has a stable mantissa marker. The idle <c>B[12]==2</c> latch gets the
/// FIX blank mask while entered numbers use the firmware's own mantissa mask.
/// </summary>
public static class ClassicFirmwareDisplay
{
  public static string? TryBuildLedText(
    ClassicCpuState state,
    bool programMode = false,
    byte programEndState = 0)
  {
    if ((state.Flags & ClassicCpuFlags.DisplayOn) == 0)
    {
      return null;
    }

    if (!programMode && !IsStableDisplayLatch(state.Registers))
    {
      return null;
    }

    bool applyFixBlankMask = !programMode && state.Registers.B[12] == 2;
    ClassicRegisterFile view = SnapshotRegisters(state.Registers);
    if (applyFixBlankMask)
    {
      int decimalPlaces = ClassicDisplayFormatRam.TryGetFixDecimalPlaces(state, out int places)
        ? places
        : 2;
      ApplyFixBlankMask(view, decimalPlaces);
    }

    return ClassicDisplayFormatter.ToLedFontText(view, displayOn: true, programMode, programEndState);
  }

  /// <summary>Reject LED scan steps that have not latched a mantissa decimal marker.</summary>
  public static bool IsStableDisplayLatch(ClassicRegisterFile registers)
  {
    if (registers.B[12] == 2)
    {
      return true;
    }

    for (int index = 3; index <= 11; index++)
    {
      if (registers.B[index] == 2)
      {
        return true;
      }
    }

    return false;
  }

  private static ClassicRegisterFile SnapshotRegisters(ClassicRegisterFile source)
  {
    ClassicRegisterFile view = new();
    Array.Copy(source.A, view.A, ClassicRegisterFile.DigitCount);
    Array.Copy(source.B, view.B, ClassicRegisterFile.DigitCount);
    return view;
  }

  /// <summary>Firmware B mask: 0 = show digit, 9 = blank, 2 = decimal after prior digit.</summary>
  private static void ApplyFixBlankMask(ClassicRegisterFile view, int decimalPlaces)
  {
    decimalPlaces = Math.Clamp(decimalPlaces, 0, 9);
    for (int index = 3; index <= 11; index++)
    {
      view.B[index] = 9;
    }

    for (int place = 0; place < decimalPlaces; place++)
    {
      int digitIndex = 11 - place;
      if (digitIndex >= 3)
      {
        view.B[digitIndex] = 0;
      }
    }

    view.B[12] = 2;
    view.B[13] = 0;

    // FIX idle: exponent field blank (real HP shows 0.00 without trailing 00).
    view.B[0] = 9;
    view.B[1] = 9;
    if (view.A[2] < 8)
    {
      view.B[2] = 9;
    }
  }
}
