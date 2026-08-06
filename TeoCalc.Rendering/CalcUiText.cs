using Teo.Locale;

namespace TeoCalc.Rendering;

/// <summary>Minimal Calc UI copy for Settings (System / English / Turkish).</summary>
public static class CalcUiText
{
  public static string SettingsTitle(LanguagePreference language) => Pick(language, "Settings", "Ayarlar");

  public static string Language(LanguagePreference language) => Pick(language, "Language", "Dil");

  public static string LanguageSystem(LanguagePreference language) => Pick(language, "System", "Sistem");

  public static string LanguageEnglish(LanguagePreference language) => Pick(language, "English", "İngilizce");

  public static string LanguageTurkish(LanguagePreference language) => Pick(language, "Turkish", "Türkçe");

  public static string Appearance(LanguagePreference language) => Pick(language, "Appearance", "Görünüm");

  public static string ThemeSystem(LanguagePreference language) => Pick(language, "System", "Sistem");

  public static string ThemeLight(LanguagePreference language) => Pick(language, "Light", "Açık");

  public static string ThemeDark(LanguagePreference language) => Pick(language, "Dark", "Koyu");

  public static string SessionProfile(LanguagePreference language) =>
    Pick(language, "Session Profile", "Oturum Profili");

  public static string Features(LanguagePreference language) => Pick(language, "Features", "Özellikler");

  public static string ExecutionSpeed(LanguagePreference language) =>
    Pick(language, "Execution Speed", "Çalıştırma Hızı");

  public static string SpeedHint(LanguagePreference language) =>
    Pick(
      language,
      "Speed toggles apply when a calculator session is open.",
      "Hız ayarları açık bir hesap makinesi oturumunda uygulanır.");

  public static string SaveAs(LanguagePreference language) => Pick(language, "Save As", "Farklı Kaydet");

  public static string SaveAsTooltip(LanguagePreference language) =>
    Pick(
      language,
      "Save current speed and feature toggles as a new profile",
      "Geçerli hız ve özellikleri yeni profil olarak kaydet");

  public static string Create(LanguagePreference language) => Pick(language, "Create", "Oluştur");

  public static string Default(LanguagePreference language) => Pick(language, "Default", "Varsayılan");

  public static string Cancel(LanguagePreference language) => Pick(language, "Cancel", "İptal");

  public static string Ok(LanguagePreference language) => Pick(language, "OK", "Tamam");

  public static string About(LanguagePreference language) => Pick(language, "About TeoCalc", "TeoCalc Hakkında");

  public static string Family(LanguagePreference language) => Pick(language, "Family", "Aile");

  public static string RomWords(LanguagePreference language) => Pick(language, "ROM words", "ROM sözcüğü");

  public static string Build(LanguagePreference language) => Pick(language, "Build", "Sürüm");

  public static string Close(LanguagePreference language) => Pick(language, "Close", "Kapat");

  private static string Pick(LanguagePreference language, string english, string turkish)
  {
    LanguagePreference resolved = language;
    if (resolved is LanguagePreference.System or LanguagePreference.Unknown)
      resolved = TextKeyword.ParsePreference(LanguagePreferenceGate.ResolveLanguageCode(LanguagePreference.System));

    return resolved is LanguagePreference.Turkish ? turkish : english;
  }
}
