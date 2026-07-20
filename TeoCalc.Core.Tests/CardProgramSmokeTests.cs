using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Hp67;
using TeoCalc.Core.Firmware;
using TeoCalc.Formats;
using TeoCalc.Panamatik;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class CardProgramSmokeTests
{
  [TestMethod]
  public void Classic65_Gateway_CardRoundTrip_PreservesProgramAndData()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-65");
    Assert.IsTrue(gateway.SupportsCardProgram);

    byte[] codes = new byte[ClassicCardProgramIo.ProgramCapacity];
    codes[0] = ClassicProgramCodes.Start;
    codes[1] = 43;
    codes[2] = 4;
    double[] registers = [1.25, 0, 0, 0, 0, 0, 0, 0, 0, 42];
    Assert.IsTrue(gateway.TryImportCardProgram(codes, registers));

    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] exportedCodes, out double[] exportedRegs));
    Assert.AreEqual(43, exportedCodes[1]);
    Assert.AreEqual(4, exportedCodes[2]);
    Assert.AreEqual(1.25, exportedRegs[0], 1e-6);
    Assert.AreEqual(42, exportedRegs[9], 1e-6);

    ProgramVocabulary vocabulary = ProgramVocabulary.Load(
      TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card65-{Guid.NewGuid():N}.hp65");
    try
    {
      Hp65CardProgramFormat.WriteFile(
        path,
        new Hp65CardSnapshot(exportedCodes, exportedRegs),
        code => ClassicCardProgramIo.FormatMnemonic(vocabulary, code));

      Hp65CardSnapshot parsed = Hp65CardProgramFormat.ReadFile(
        path,
        mnemonic => ClassicCardProgramIo.ResolveMnemonic(vocabulary, mnemonic));
      Assert.AreEqual(43, parsed.ProgramCodes[1]);
      Assert.AreEqual(1.25, parsed.Registers[0], 1e-6);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void Act67_Gateway_CardRoundTrip_PreservesProgramAndData()
  {
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway("HP-67");
    Assert.IsInstanceOfType<Hp67FirmwareGateway>(gateway);
    Assert.IsTrue(gateway.SupportsCardProgram);

    byte[] codes = new byte[Hp67CardProgramIo.ProgramCapacity];
    codes[0] = 16; // "0"
    codes[1] = 17; // "1"
    codes[2] = 55; // "+"
    double[] registers = new double[Hp67CardProgramIo.RegisterCount];
    registers[0] = 12.5;
    registers[25] = -3;
    Assert.IsTrue(gateway.TryImportCardProgram(codes, registers));

    Assert.IsTrue(gateway.TryExportCardProgram(out byte[] exportedCodes, out double[] exportedRegs));
    Assert.AreEqual(16, exportedCodes[0]);
    Assert.AreEqual(17, exportedCodes[1]);
    Assert.AreEqual(55, exportedCodes[2]);
    Assert.AreEqual(12.5, exportedRegs[0], 1e-6);
    Assert.AreEqual(-3, exportedRegs[25], 1e-6);

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-card67-{Guid.NewGuid():N}.hp67");
    try
    {
      Hp67FirmwareGateway hp67 = (Hp67FirmwareGateway)gateway;
      Assert.IsTrue(hp67.TryExportCardMode(out Hp67CardMode mode));
      Hp67CardProgramFormat.WriteFile(
        path,
        new Hp67CardSnapshot(
          exportedCodes,
          exportedRegs,
          new Hp67CardModeSnapshot(mode.Angle, mode.Display, mode.Digits, mode.FlagsHi, mode.FlagsLo)),
        Hp67CardProgramIo.FormatMnemonic);

      Hp67CardSnapshot parsed = Hp67CardProgramFormat.ReadFile(path, Hp67CardProgramIo.ResolveMnemonic);
      Assert.AreEqual(16, parsed.ProgramCodes[0]);
      Assert.AreEqual(12.5, parsed.Registers[0], 1e-6);
      Assert.IsNotNull(parsed.Mode);

      Hp67FirmwareGateway other = (Hp67FirmwareGateway)CalcFirmwareGatewayLocator.CreateGateway("HP-67BE");
      Assert.IsTrue(other.TryImportCardProgram(parsed.ProgramCodes, parsed.Registers));
      Assert.IsTrue(other.TryExportCardProgram(out byte[] roundTrip, out double[] roundTripRegs));
      Assert.AreEqual(55, roundTrip[2]);
      Assert.AreEqual(-3, roundTripRegs[25], 1e-6);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void Hp67_SupportsCard_Hp67BE_MapsSameGateway()
  {
    Assert.IsTrue(CalcFirmwareGatewayLocator.CreateGateway("HP-67").SupportsCardProgram);
    Assert.IsTrue(CalcFirmwareGatewayLocator.CreateGateway("HP-67BE").SupportsCardProgram);
    Assert.IsTrue(CalcFirmwareBootstrap.IsNativeHp67Pilot("HP-67BE"));
  }
}
