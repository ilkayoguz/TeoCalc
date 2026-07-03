using TeoCalc.Core;
using TeoCalc.Core.Catalog;
using TeoCalc.Core.Engine.Classic;

var rom = ClassicMicrocodeRom.LoadBinary(TeoCalcPaths.ResourcePath("Engine/HP-65/Firmware/hp65.microcode.bin"));
var cat = MicrocodeHandlerCatalog.Load(TeoCalcPaths.ResourcePath("Engine/Classic/microcode.handlers.json"));
var cpu = new ClassicCpu(rom, cat);
cpu.Reset();
cpu.State.Flags |= ClassicCpuFlags.DisplayOn;
int fmt = 61 * 14 / 2;
cpu.State.Ram[fmt+3]=0x21; cpu.State.Ram[fmt+4]=0x22;
int found = 0;
for (int s=0; s<200000 && found<5; s++) {
  cpu.Step();
  var b = cpu.State.Registers.B;
  if (b.Any(x => x == 9)) {
    var a = cpu.State.Registers.A;
    var text = ClassicDisplayFormatter.ToLedFontText(cpu.State.Registers, true);
    Console.WriteLine($"step {s} PC={cpu.State.ProgramCounter:X4} B9count={b.Count(x=>x==9)} text=[{text.Replace(';','.')}]");
    Console.WriteLine("A="+string.Join(',',a)+" B="+string.Join(',',b));
    found++;
  }
}
Console.WriteLine("done found="+found);
