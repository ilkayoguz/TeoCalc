using TeoCalc.ReferenceEmulator;

namespace TeoCalc.Core.Tests;

[TestClass]
public static class FirmwareBootstrap
{
  [AssemblyInitialize]
  public static void UseEmulatorAdapter(TestContext context) =>
    CalcFirmwareBootstrap.UseEmulatorAdapter();
}
