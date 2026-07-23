using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;
using TeoTheme;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class AppThemeSettingsTests
{
  [TestMethod]
  public void AppThemePack_LoadsFromResource()
  {
    string dir = TeoCalcPaths.ResourcePath(Path.Combine("Config", "AppTheme"));
    Assert.IsTrue(Directory.Exists(dir), dir);
    ThemePack pack = ThemeCatalog.LoadDefault(dir);
    Assert.AreEqual("TeoTheme", pack.Id);
    Assert.IsTrue(pack.Light.TryGet(ThemeTokens.TooltipTextColor, out ThemeColor lightTip));
    Assert.IsTrue(pack.Dark.TryGet(ThemeTokens.TooltipTextColor, out ThemeColor darkTip));
    Assert.IsTrue(lightTip.R + lightTip.G + lightTip.B < 0.5f, "Light tooltip text should be dark.");
    Assert.IsTrue(darkTip.R + darkTip.G + darkTip.B > 1.5f, "Dark tooltip text should be light.");
  }

  [TestMethod]
  public void Preference_RoundTripsThroughFormatter()
  {
    Assert.AreEqual(AppThemePreference.Light, CalcUserSettingsStore.ParseAppThemePreference("Light"));
    Assert.AreEqual(AppThemePreference.Dark, CalcUserSettingsStore.ParseAppThemePreference("Dark"));
    Assert.AreEqual(AppThemePreference.System, CalcUserSettingsStore.ParseAppThemePreference("System"));
    Assert.AreEqual(AppThemePreference.System, CalcUserSettingsStore.ParseAppThemePreference(null));
    Assert.AreEqual("Light", CalcUserSettingsStore.FormatAppThemePreference(AppThemePreference.Light));
    Assert.AreEqual("Dark", CalcUserSettingsStore.FormatAppThemePreference(AppThemePreference.Dark));
    Assert.AreEqual("System", CalcUserSettingsStore.FormatAppThemePreference(AppThemePreference.System));
  }

  [TestMethod]
  public void TitleBar_IncludesSettingsBeforeWindowControls()
  {
    Assert.AreEqual(4, CalcWindowTitlePanelComponent.ButtonCount);
    Assert.AreEqual(
      CalcWindowTitlePanelComponent.ButtonWidth * 4f,
      CalcWindowTitlePanelComponent.ButtonsWidth);
  }

  [TestMethod]
  public void CalcAppTheme_InitializeResolvesPreference()
  {
    CalcUserSettingsStore.Initialize();
    AppThemePreference prior = CalcUserSettingsStore.LoadAppThemePreference();
    try
    {
      CalcAppTheme.Initialize(AppThemePreference.Dark);
      Assert.AreEqual(AppThemePreference.Dark, CalcAppTheme.Preference);
      Assert.AreEqual(ThemeAppearance.Dark, CalcAppTheme.Appearance);
      CalcAppTheme.SetPreference(AppThemePreference.Light);
      Assert.AreEqual(AppThemePreference.Light, CalcAppTheme.Preference);
      Assert.AreEqual(ThemeAppearance.Light, CalcAppTheme.Appearance);
    }
    finally
    {
      CalcAppTheme.SetPreference(prior);
    }
  }
}
