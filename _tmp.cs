using TeoCalc.Core.Engine.Classic;
var r = new ClassicRegisterFile();
r.B[12] = 2;
var slots = ClassicLedDisplayMapper.Map(r, true);
Console.WriteLine(ClassicLedDisplayMapper.ToFontText(slots));
for (int i=0;i<slots.Length;i++) Console.WriteLine($"{i+1}: {slots[i].Kind} {slots[i].Digit}");
