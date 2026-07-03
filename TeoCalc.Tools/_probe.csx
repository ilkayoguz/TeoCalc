using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

ClassicCpu cpu = Create();
cpu.Reset();
cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
ClassicProgramInput.TryResolveKeyCode(ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json")), '7', out byte key);
cpu.PressKey(key);
for (int i = 0; i < 200; i++) cpu.Step();
string lit = ClassicDisplayFormatter.ToLedFontText(cpu.State.Registers, true);
bool on = (cpu.State.Flags & ClassicCpuFlags.DisplayOn) != 0;
Console.WriteLine($"after 7: displayOn={on} text='{lit}' keyBuf={cpu.State.KeyBuffer}");

ClassicCpu Create() {
  var rom = ClassicMicrocodeRom.LoadBinary(TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
  var cat = MicrocodeHandlerCatalog.Load(TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
  var voc = ProgramVocabulary.Load(TeoCalcPaths.ResourcePath("Engine/HP-65/Program/program.vocabulary.json"));
  return new ClassicCpu(rom, cat, voc);
}
