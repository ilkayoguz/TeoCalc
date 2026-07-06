namespace TeoCalc.Panamatik;

public static class PanamatikEngineFactory
{
  public static bool TryCreate(string teoCalcModelId, out IPanamatikEngine? engine)
  {
    if (!TryResolveBinding(teoCalcModelId, out string? sourceFolderId, out Type? formType))
    {
      engine = null;
      return false;
    }

    string modelDirectory = ResolveModelDirectory(sourceFolderId);
    engine = new PanamatikFormEngine(teoCalcModelId, modelDirectory, formType);
    return true;
  }

  public static IPanamatikEngine Create(string teoCalcModelId)
  {
    if (!TryCreate(teoCalcModelId, out IPanamatikEngine? engine) || engine is null)
    {
      throw new NotSupportedException($"No Panamatik engine is registered for '{teoCalcModelId}'.");
    }

    return engine;
  }

  public static bool IsSupported(string teoCalcModelId) =>
    TryResolveBinding(teoCalcModelId, out _, out _);

  internal static string ResolveModelDirectory(string sourceFolderId)
  {
    string assemblyDirectory = Path.GetDirectoryName(typeof(PanamatikEngineFactory).Assembly.Location)!;
    string fromAssembly = Path.Combine(assemblyDirectory, "Sources", sourceFolderId);
    if (Directory.Exists(fromAssembly))
    {
      return fromAssembly;
    }

    return Path.Combine(AppContext.BaseDirectory, "Sources", sourceFolderId);
  }

  private static bool TryResolveBinding(string teoCalcModelId, out string sourceFolderId, out Type formType)
  {
    switch (teoCalcModelId.ToUpperInvariant())
    {
      case "HP-01":
        sourceFolderId = "HP01";
        formType = typeof(global::Panamatik.Calc.HP01.HP01);
        return true;
      case "HP-19C":
        sourceFolderId = "HP19";
        formType = typeof(global::Panamatik.Calc.HP19.HP19C);
        return true;
      case "HP-21":
        sourceFolderId = "HP21";
        formType = typeof(global::Panamatik.Calc.HP21.HP25);
        return true;
      case "HP-22":
        sourceFolderId = "HP22";
        formType = typeof(global::Panamatik.Calc.HP22.HP25);
        return true;
      case "HP-25":
        sourceFolderId = "HP25";
        formType = typeof(global::Panamatik.Calc.HP25.HP25);
        return true;
      case "HP-27":
        sourceFolderId = "HP27";
        formType = typeof(global::Panamatik.Calc.HP27.HP25);
        return true;
      case "HP-29":
        sourceFolderId = "HP29";
        formType = typeof(global::Panamatik.Calc.HP29.HP25);
        return true;
      case "HP-31":
        sourceFolderId = "HP31";
        formType = typeof(global::Panamatik.Calc.HP31.HPSpice);
        return true;
      case "HP-32":
        sourceFolderId = "HP32";
        formType = typeof(global::Panamatik.Calc.HP32.HPSpice);
        return true;
      case "HP-33":
        sourceFolderId = "HP33";
        formType = typeof(global::Panamatik.Calc.HP33.HPSpice);
        return true;
      case "HP-34":
        sourceFolderId = "HP34";
        formType = typeof(global::Panamatik.Calc.HP34.HPSpice);
        return true;
      case "HP-35":
        sourceFolderId = "HP35";
        formType = typeof(global::Panamatik.Calc.HP35.HPClassic);
        return true;
      case "HP-37":
        sourceFolderId = "HP37";
        formType = typeof(global::Panamatik.Calc.HP37.HPSpice);
        return true;
      case "HP-38":
        sourceFolderId = "HP38";
        formType = typeof(global::Panamatik.Calc.HP38.HPSpice);
        return true;
      case "HP-45":
        sourceFolderId = "HP45";
        formType = typeof(global::Panamatik.Calc.HP45.HPClassic);
        return true;
      case "HP-55":
        sourceFolderId = "HP55";
        formType = typeof(global::Panamatik.Calc.HP55.HPClassic);
        return true;
      case "HP-65":
        sourceFolderId = "HP65";
        formType = typeof(global::Panamatik.Calc.HP65.HPClassic);
        return true;
      case "HP-67":
        sourceFolderId = "HP67";
        formType = typeof(global::Panamatik.Calc.HP67.HP67);
        return true;
      case "HP-70":
        sourceFolderId = "HP70";
        formType = typeof(global::Panamatik.Calc.HP70.HPClassic);
        return true;
      case "HP-80":
        sourceFolderId = "HP80";
        formType = typeof(global::Panamatik.Calc.HP80.HPClassic);
        return true;
      default:
        sourceFolderId = string.Empty;
        formType = typeof(object);
        return false;
    }
  }
}
