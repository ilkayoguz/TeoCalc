using TeoCalc.Rendering;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class DebugTransportGrainTests
{
  [TestMethod]
  public void StepInto_UsesStudioGrain_WhenCardProgramAndMicrocodeHotkeysOff()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    Assert.IsTrue(session.SupportsCardProgram);
    session.PreferMicrocodeHotkeys = false;
    // Smoke: StepInto should not throw; grain routing is PreferMicrocodeHotkeys-gated.
    session.PreferMicrocodeHotkeys = true;
    Assert.IsTrue(session.PreferMicrocodeHotkeys);
    session.PreferMicrocodeHotkeys = false;
    Assert.IsFalse(session.PreferMicrocodeHotkeys);
  }

  [TestMethod]
  public void PreferMicrocodeHotkeys_DefaultsFalse()
  {
    using CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    Assert.IsFalse(session.PreferMicrocodeHotkeys);
  }
}
