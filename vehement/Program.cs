#define VERBOSE
using Vehement.Debug;
using Vehement;
using Vehement.Assembler;
using Vehement.Common;

List<byte> program = new();
Half x = (Half)5.25;
Half y = (Half)6.52;

ushort x2 = ToUshort(x);
byte x2lo = (byte)(x2 & 0xFF);
byte x2hi = (byte)(x2 >> 8);
ushort y2 = ToUshort(y);
byte y2lo = (byte)(y2 & 0xFF);
byte y2hi = (byte)(y2 >> 8);

Half hx = ToHalf((ushort)(x2lo + (x2hi << 8)));
Half hy = ToHalf((ushort)(y2lo + (y2hi << 8)));

Console.WriteLine($"{hx} + {hy} = {hx + hy}");
Console.WriteLine($"{hx} - {hy} = {hx - hy}");
Console.WriteLine($"{hx} * {hy} = {hx * hy}");
Console.WriteLine($"{hx} / {hy} = {hx / hy}");


program.AddRange(new byte[]
{
    (byte)Op.MOV_REG_IMM, 0x00, x2lo, x2hi,
    (byte)Op.MOV_REG_IMM, 0x01, y2lo, y2hi, 
    (byte)Op.ADDF, 0x02, 0x00, 0x01,
    (byte)Op.SUBF, 0x03, 0x00, 0x01,
    (byte)Op.MULF, 0x04, 0x00, 0x01,
    (byte)Op.DIVF, 0x05, 0x00, 0x01,
    (byte)Op.HALT
});

VM vm = new(program.ToArray());

Debugger dbg = new(vm);
vm.RunUntilHalt();
Console.WriteLine(dbg.Dump());

static Half ToHalf(ushort value) => ReinterpretCast<ushort, Half>(value);
static ushort ToUshort(Half value) => ReinterpretCast<Half, ushort>(value);

static unsafe TDest ReinterpretCast<TSource, TDest>(TSource source)
{
    var tr = __makeref(source);
    TDest w = default(TDest)!;
    var trw = __makeref(w);
    *((IntPtr*)&trw) = *((IntPtr*)&tr);
    return __refvalue(trw, TDest);
}