using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CalcSessionProfileTests
{
  [TestMethod]
  public void BuiltIns_CoverStandardFastMaxSlow()
  {
    Assert.IsTrue(CalcSessionProfile.BuiltIns.Any(p => p.Id == CalcSessionProfile.StandardId));
    Assert.IsTrue(CalcSessionProfile.BuiltIns.Any(p => p.Id == CalcSessionProfile.FastId));
    Assert.IsTrue(CalcSessionProfile.BuiltIns.Any(p => p.Id == CalcSessionProfile.MaxId));
    Assert.IsTrue(CalcSessionProfile.BuiltIns.Any(p => p.Id == CalcSessionProfile.SlowId));
    Assert.AreEqual(2, CalcSessionProfile.BuiltIns.First(p => p.Id == CalcSessionProfile.StandardId).ExecutionSpeedIndex);
    Assert.AreEqual(6, CalcSessionProfile.BuiltIns.First(p => p.Id == CalcSessionProfile.MaxId).ExecutionSpeedIndex);
  }

  [TestMethod]
  public void Select_AppliesSpeedWhenControlEnabled()
  {
    CalcUserSettingsStore.Initialize();
    string prior = CalcUserSettingsStore.LoadActiveSessionProfileId();
    try
    {
      using CalcExplorerSession session = CreateSession();
      CalcSessionProfiles.Select(CalcSessionProfile.MaxId, session);
      Assert.AreEqual(CalcSessionProfile.MaxId, CalcSessionProfiles.ActiveProfileId);
      Assert.AreEqual(6, session.ExecutionSpeedIndex);
      Assert.AreEqual("16x", session.ExecutionSpeedLabel);

      CalcSessionProfiles.Select(CalcSessionProfile.SlowId, session);
      Assert.AreEqual(0, session.ExecutionSpeedIndex);
      Assert.AreEqual("0.25x", session.ExecutionSpeedLabel);
    }
    finally
    {
      CalcSessionProfiles.Select(prior);
    }
  }

  [TestMethod]
  public void FeatureToggle_SkipsSpeedWhenDisabled()
  {
    CalcUserSettingsStore.Initialize();
    string prior = CalcUserSettingsStore.LoadActiveSessionProfileId();
    try
    {
      using CalcExplorerSession session = CreateSession();
      CalcSessionProfiles.Select(CalcSessionProfile.MaxId, session);
      Assert.AreEqual(6, session.ExecutionSpeedIndex);

      CalcSessionProfiles.SetControlExecutionSpeed(false);
      session.SetExecutionSpeedIndex(3);
      CalcSessionProfiles.ApplyTo(session);
      Assert.AreEqual(3, session.ExecutionSpeedIndex);

      CalcSessionProfiles.SetControlExecutionSpeed(true);
      CalcSessionProfiles.ApplyTo(session);
      Assert.AreEqual(6, session.ExecutionSpeedIndex);
    }
    finally
    {
      CalcSessionProfiles.Select(prior);
    }
  }

  [TestMethod]
  public void SaveAs_PersistsCustomAndSelects()
  {
    CalcUserSettingsStore.Initialize();
    string prior = CalcUserSettingsStore.LoadActiveSessionProfileId();
    IReadOnlyList<CalcSessionProfile> priorCustoms = CalcUserSettingsStore.LoadCustomSessionProfiles();
    string name = "TestProf-" + Guid.NewGuid().ToString("N")[..6];
    try
    {
      using CalcExplorerSession session = CreateSession();
      CalcSessionProfiles.Select(CalcSessionProfile.StandardId, session);
      session.SetExecutionSpeedIndex(3);
      Assert.IsTrue(CalcSessionProfiles.TrySaveAs(name, session, out string? error), error);
      Assert.AreEqual(name, CalcSessionProfiles.Active.Name);
      Assert.IsFalse(CalcSessionProfiles.Active.IsBuiltIn);
      Assert.AreEqual(3, CalcSessionProfiles.Active.ExecutionSpeedIndex);
      Assert.IsTrue(
        CalcUserSettingsStore.LoadCustomSessionProfiles().Any(p => p.Name == name));
    }
    finally
    {
      CalcUserSettingsStore.SaveCustomSessionProfiles(priorCustoms);
      CalcSessionProfiles.Select(prior);
    }
  }

  private static CalcExplorerSession CreateSession() =>
    new(TeoCalcPaths.ResourcePath("Engine"));
}
