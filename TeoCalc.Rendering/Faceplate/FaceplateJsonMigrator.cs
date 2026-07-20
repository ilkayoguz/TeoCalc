using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Rendering;

/// <summary>
/// One-shot migrator: writes CapFace/Style into key.faceplate.json and Faceplate policy into Model.json.
/// </summary>
public static class FaceplateJsonMigrator
{
  private static readonly JsonSerializerOptions WriteOptions = new()
  {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
  };

  public static void ExportAll()
  {
    foreach (string catalogId in TeoCalcModelCatalog.SupportedModels)
    {
      ExportModel(catalogId);
    }

    ClassicKeyFaceplateLegend.ClearCache();
  }

  public static void ExportModel(string catalogId)
  {
    CalcModelIdentity identity = CalcModelIds.Resolve(catalogId);
    string engineId = identity.EngineId;
    string engineDir = TeoCalcPaths.ResourcePath(Path.Combine("Engine", engineId));
    string modelPath = Path.Combine(engineDir, "Model.json");
    string faceplatePath = Path.Combine(engineDir, "Program", "key.faceplate.json");
    string vocabPath = Path.Combine(engineDir, "Program", "program.vocabulary.json");
    if (!File.Exists(modelPath) || !File.Exists(vocabPath))
    {
      throw new FileNotFoundException($"Missing model or vocabulary for {catalogId}", modelPath);
    }

    TeoCalcModelDefinition model = TeoCalcModelDefinition.Load(modelPath);
    string family = string.IsNullOrWhiteSpace(model.Family) ? identity.Family : model.Family;
    ProgramVocabulary vocabulary = ProgramVocabulary.Load(vocabPath);

    MergeKeyFaceplate(faceplatePath, catalogId, engineId, family, vocabulary);
    MergeModelFaceplate(modelPath, catalogId, engineId, model, identity);
  }

  private static void MergeKeyFaceplate(
    string faceplatePath,
    string catalogId,
    string engineId,
    string family,
    ProgramVocabulary vocabulary)
  {
    JsonObject root;
    if (File.Exists(faceplatePath))
    {
      root = JsonNode.Parse(File.ReadAllText(faceplatePath)) as JsonObject
        ?? new JsonObject();
    }
    else
    {
      root = new JsonObject
      {
        ["Format"] = "TeoCalc.KeyFaceplate",
        ["Model"] = engineId,
      };
    }

    root["Format"] = "TeoCalc.KeyFaceplate";
    root["SchemaVersion"] = 2;
    root["Model"] = engineId;

    JsonObject keys = root["Keys"] as JsonObject ?? new JsonObject();
    root["Keys"] = keys;

    for (int index = 0; index < vocabulary.KeyChart.Count; index++)
    {
      ProgramKeyEntry key = vocabulary.KeyChart[index];
      JsonObject entry = keys[index.ToString()] as JsonObject ?? new JsonObject();
      // Do not clobber CapFace/Style once migrated — legacy ladders are gone.
      if (entry["CapFace"] is null)
      {
        entry["CapFace"] = CalcFaceplateLayout.LabelForKey(key, vocabulary, family, catalogId);
      }

      if (entry["Style"] is null)
      {
        entry["Style"] = CalcKeyStyleResolver.Format(CalcButtonStyle.White);
      }

      keys[index.ToString()] = entry;
    }

    File.WriteAllText(faceplatePath, root.ToJsonString(WriteOptions) + Environment.NewLine);
  }

  private static void MergeModelFaceplate(
    string modelPath,
    string catalogId,
    string engineId,
    TeoCalcModelDefinition model,
    CalcModelIdentity identity)
  {
    JsonObject root = JsonNode.Parse(File.ReadAllText(modelPath)) as JsonObject
      ?? throw new InvalidOperationException($"Invalid Model.json: {modelPath}");

    JsonObject face = root["Faceplate"] as JsonObject ?? new JsonObject();
    root["Faceplate"] = face;

    string shortId = model.Faceplate?.ShortId is { Length: > 0 } sid
      ? sid
      : identity.ShortId;

    face["BodyLayoutId"] = model.Faceplate?.BodyLayoutId is { Length: > 0 } bl
      ? bl
      : "00d";
    face["ThemeId"] = model.Faceplate?.ThemeId is { Length: > 0 } th
      ? th
      : "Modern";
    face["ShortId"] = shortId;
    face["AnnotationStyleId"] = model.Faceplate?.AnnotationStyleId is { Length: > 0 } ann
      ? ann
      : CalcAnnotationStyleCatalog.HeuristicId(engineId, catalogId);
    face["SwitchBankId"] = model.Faceplate?.SwitchBankId is { Length: > 0 } sw
      ? sw
      : CalcSwitchCatalog.HeuristicBankId(shortId, catalogId);
    face["HasCardSlot"] = model.Faceplate?.HasCardSlot
      ?? CalcCardSlotComponent.HeuristicHasCardSlot(shortId);
    face["HasPrinter"] = model.Faceplate?.HasPrinter ?? false;

    File.WriteAllText(modelPath, root.ToJsonString(WriteOptions) + Environment.NewLine);
  }
}
