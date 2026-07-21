using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;
using TeoCalc.Core.Engine.Teo01;
using TeoCalc.Core.Engine.Teo19;
using TeoCalc.Core.Engine.Teo67;
using TeoCalc.Core.Engine.Spice;
using TeoCalc.Core.Engine.Woodstock;
using TeoCalc.Core.Firmware;
using TeoCalc.Panamatik;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

/// <summary>
/// Cross-catalog smoke: every supported model boots a native gateway, faceplate, and stable idle display.
/// </summary>
[TestClass]
public sealed class CatalogModelSmokeTests
{
  private static string EngineRoot => TeoCalcPaths.ResourcePath("Engine");

  public static IEnumerable<object[]> SupportedCatalogModels =>
    TeoCalcModelCatalog.SupportedModels.Select(id => new object[] { id });

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void ExplorerSession_LoadsNativeGateway(string catalogId)
  {
    using CalcExplorerSession session = CreateSessionForCatalog(catalogId);

    Assert.IsTrue(session.UsesFirmwareGateway, catalogId);
    Assert.AreEqual(0, session.LoadWarnings.Count, string.Join("; ", session.LoadWarnings));
    Assert.IsTrue(session.SupportsFaceplate, catalogId);
    Assert.IsFalse(
      string.Equals(session.LastBatch.LastHandlerId, "Emulator.Engine", StringComparison.Ordinal),
      catalogId);

    Type expected = ExpectedGatewayType(session.Model.Family, catalogId);
    ICalcFirmwareGateway gateway = CalcFirmwareGatewayLocator.CreateGateway(catalogId);
    Assert.IsInstanceOfType(gateway, expected, catalogId);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void ExplorerSession_PowerOn_IdleDisplayVisible(string catalogId)
  {
    using CalcExplorerSession session = CreateSessionForCatalog(catalogId);
    Warmup(session, catalogId);

    Assert.IsTrue(session.PowerOn, catalogId);
    Assert.IsTrue(session.IsDisplayVisible(), $"{catalogId}: '{session.DisplayText}'");
    Assert.IsTrue(session.DisplayText.Trim().Length > 0, session.DisplayText);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void ExplorerSession_IdleDisplay_DoesNotFlicker(string catalogId)
  {
    if (string.Equals(catalogId, "HP-01", StringComparison.OrdinalIgnoreCase))
    {
      Assert.Inconclusive("T-01 clock display updates during idle soak.");
    }

    using CalcExplorerSession session = CreateSessionForCatalog(catalogId);
    session.PowerOnResume();

    HashSet<string> texts = [];
    for (int i = 0; i < 120; i++)
    {
      session.Tick(TickSeconds(catalogId));
      if (session.DisplayText.Length > 0)
      {
        texts.Add(NormalizeDisplayText(session.DisplayText));
      }
    }

    Assert.IsLessThanOrEqualTo(2, texts.Count, $"{catalogId}: {string.Join(" | ", texts)}");
    Assert.IsTrue(texts.Count > 0, catalogId);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void Faceplate_AndBodyLayout_Resolve(string catalogId)
  {
    string engineId = CalcModelIds.ToEngineId(catalogId);
    TeoCalcModelDefinition engineModel = TeoCalcModelDefinition.Load(
      Path.Combine(EngineRoot, engineId, "Model.json"));
    CalcModelDefinition faceplateModel = CalcModelCatalog.Resolve(engineModel, catalogId);

    IReadOnlyList<FaceplateCell> cells =
      CalcFaceplateLayout.GetPhysicalCells(engineModel.Family, engineModel.Model);
    Assert.IsTrue(cells.Count > 0, catalogId);

    CalcBodyLayout body = CalcBodyLayoutCatalog.Resolve(faceplateModel);
    Assert.IsFalse(string.IsNullOrWhiteSpace(body.Id), catalogId);
    Assert.IsTrue(body.TryGetKeySlot(0, out _), catalogId);

    IReadOnlyList<CalcSwitchSpec> switches = CalcSwitchCatalog.ForModelId(CalcModelIds.ToShortId(catalogId));
    Assert.IsTrue(switches.Count >= 1, catalogId);
    Assert.IsTrue(switches[0].PositionCount >= 2, catalogId);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void ProductLabel_IsTeoNeutral(string catalogId)
  {
    string label = CalcModelIds.ToProductLabel(catalogId);
    StringAssert.StartsWith(label, "T-", catalogId);
    Assert.IsFalse(label.Contains("HP", StringComparison.OrdinalIgnoreCase), label);
    Assert.IsFalse(label.Contains("Panamatik", StringComparison.OrdinalIgnoreCase), label);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void NativePilotFlags_AreTrue(string catalogId)
  {
    Assert.IsTrue(IsNativePilot(catalogId), catalogId);
  }

  [TestMethod]
  [DynamicData(nameof(SupportedCatalogModels))]
  public void PressDigit7_UpdatesDisplay_WhenMapped(string catalogId)
  {
    using CalcExplorerSession session = CreateSessionForCatalog(catalogId);
    Warmup(session, catalogId);

    if (session.DisplayText.Contains("Error", StringComparison.OrdinalIgnoreCase))
    {
      Assert.Inconclusive($"{catalogId} reports an idle error banner.");
    }

    ProgramVocabulary vocabulary = session.Vocabulary!;
    int chartIndex = vocabulary.KeyChart.FindIndex(k => k.Char == "7" && k.KeyCode != 0);
    if (chartIndex < 0)
    {
      Assert.Inconclusive($"{catalogId} has no mapped digit 7.");
    }

    if (!ClassicProgramInput.TryResolveKeyCode(vocabulary, '7', out byte keyCode))
    {
      Assert.Inconclusive($"{catalogId} has no mapped digit 7.");
    }

    session.PressKey(chartIndex, keyCode);
    session.SetKeyboardKeyHeld(true);
    for (int i = 0; i < 30; i++)
    {
      session.Tick(TickSeconds(catalogId));
    }

    session.SetKeyboardKeyHeld(false);
    session.ReleaseMouseKey();
    for (int i = 0; i < 30; i++)
    {
      session.Tick(TickSeconds(catalogId));
    }

    Assert.IsTrue(session.DisplayText.Contains('7'), $"{catalogId}: '{session.DisplayText}'");
  }

  private static CalcExplorerSession CreateSessionForCatalog(string catalogId)
  {
    CalcExplorerSession session = new(EngineRoot);
    int index = Array.FindIndex(session.Models, id =>
      string.Equals(CalcModelIds.ToEngineId(id), CalcModelIds.ToEngineId(catalogId), StringComparison.OrdinalIgnoreCase)
      || string.Equals(id, catalogId, StringComparison.OrdinalIgnoreCase));
    Assert.IsTrue(index >= 0, catalogId);
    session.LoadModel(index);
    return session;
  }

  private static void Warmup(CalcExplorerSession session, string catalogId)
  {
    session.PowerOnResume();
    int ticks = string.Equals(catalogId, "HP-01", StringComparison.OrdinalIgnoreCase) ? 80 : 40;
    float delta = TickSeconds(catalogId);
    for (int i = 0; i < ticks; i++)
    {
      session.Tick(delta);
    }
  }

  private static float TickSeconds(string catalogId) =>
    string.Equals(catalogId, "HP-01", StringComparison.OrdinalIgnoreCase) ? 0.01f : 0.05f;

  private static string NormalizeDisplayText(string text) =>
    string.Join(' ', text.Replace(';', '.').Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

  private static bool IsNativePilot(string catalogId) =>
    CalcFirmwareBootstrap.IsNativeClassicPilot(catalogId)
    || CalcFirmwareBootstrap.IsNativeWoodstockPilot(catalogId)
    || CalcFirmwareBootstrap.IsNativeSpicePilot(catalogId)
    || CalcFirmwareBootstrap.IsNativeTeo67Pilot(catalogId)
    || CalcFirmwareBootstrap.IsNativeTeo19Pilot(catalogId)
    || CalcFirmwareBootstrap.IsNativeTeo01Pilot(catalogId);

  private static Type ExpectedGatewayType(string family, string catalogId)
  {
    if (string.Equals(catalogId, "HP-01", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "HP01", StringComparison.OrdinalIgnoreCase))
    {
      return typeof(Teo01FirmwareGateway);
    }

    if (string.Equals(catalogId, "HP-19C", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Hp19", StringComparison.OrdinalIgnoreCase))
    {
      return typeof(Teo19FirmwareGateway);
    }

    if (string.Equals(catalogId, "HP-67", StringComparison.OrdinalIgnoreCase)
        || string.Equals(family, "Hp67", StringComparison.OrdinalIgnoreCase))
    {
      return typeof(Teo67FirmwareGateway);
    }

    return family switch
    {
      "Classic" => typeof(ClassicFirmwareGateway),
      "Woodstock" => typeof(WoodstockFirmwareGateway),
      "Spice" => typeof(SpiceFirmwareGateway),
      _ => throw new ArgumentOutOfRangeException(nameof(family), family, catalogId),
    };
  }
}
