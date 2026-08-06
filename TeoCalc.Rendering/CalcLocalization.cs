using System.Globalization;
using Teo.Locale;
using Teo.Locale.Windows;

namespace TeoCalc.Rendering;

/// <summary>Applies Teo.Locale language preference for Calc UI (preview / persist).</summary>
public static class CalcLocalization
{
  private static LanguagePreference _preference = LanguagePreference.System;
  private static bool _initialized;

  public static LanguagePreference Preference => _preference;

  public static void EnsureInitialized()
  {
    if (_initialized)
      return;

    CalcUserSettingsStore.Initialize();
    _preference = CalcUserSettingsStore.LoadLanguagePreference();
    LanguagePreferenceGate.HostResolver = OperatingSystem.IsWindows()
      ? WindowsHostCultureResolver.Instance
      : null;

    try
    {
      LanguagePreferenceGate.Apply(_preference);
    }
    catch (CultureNotFoundException)
    {
      // TeoCalc may run with InvariantGlobalization; preference is still tracked in-process.
    }

    _initialized = true;
  }

  public static void SetPreference(LanguagePreference preference, bool persist)
  {
    EnsureInitialized();
    _preference = preference;
    try
    {
      LanguagePreferenceGate.Apply(preference);
    }
    catch (CultureNotFoundException)
    {
      // keep in-process preference even when ICU cultures are unavailable
    }

    if (persist)
      CalcUserSettingsStore.SaveLanguagePreference(preference);
  }
}
