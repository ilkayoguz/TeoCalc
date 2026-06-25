using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

namespace TeoCalc.Core;

public static class ClassicCpuFactory
{
  public static ClassicCpu Create(TeoCalcModelDefinition model, string engineRoot)
  {
    string modelDir = Path.Combine(engineRoot, model.Model);
    string romPath = Path.Combine(modelDir, model.Firmware.RomBinary.Replace('/', Path.DirectorySeparatorChar));
    string handlerPath = Path.Combine(modelDir, model.Firmware.HandlerCatalog.Replace('/', Path.DirectorySeparatorChar));

    ClassicMicrocodeRom rom = ClassicMicrocodeRom.LoadBinary(romPath);
    MicrocodeHandlerCatalog handlers = MicrocodeHandlerCatalog.Load(handlerPath);

    ProgramVocabulary? vocabulary = null;
    if (model.Program is not null && !string.IsNullOrWhiteSpace(model.Program.Vocabulary))
    {
      string vocabularyPath = Path.Combine(modelDir, model.Program.Vocabulary.Replace('/', Path.DirectorySeparatorChar));
      vocabulary = ProgramVocabulary.Load(vocabularyPath);
    }

    ClassicCpu cpu = new(rom, handlers, vocabulary);
    cpu.State.ProgramRamBase = (byte)model.Hardware.ProgramRamBase;
    cpu.Reset();
    return cpu;
  }
}
