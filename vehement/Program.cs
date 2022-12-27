#define VERBOSE
using Vehement;
using Vehement.Common;
using Vehement.Debug;

List<byte> program = new();

Word x = new((Half)5.25);
Word y = new((Half)6.52);
Console.WriteLine("------------");
Console.WriteLine($"{x.ToString()} (lo {x.Low:X2} ,hi {x.High:X2} )");
Console.WriteLine($"{y.ToString()} (lo {y.Low:X2} ,hi {y.High:X2} )");
Console.WriteLine("------------");

program.AddRange(new byte[]
{
    //(byte)Op.MOV_REG_IMM, 0x00, 0xAB, 0x00,
    //(byte)Op.MOV_REG_IMM, 0x01, 0x11, 0x00,
    //(byte)Op.ADD, 0x02, 0x00, 0x01,
    (byte)Op.MOV_REG_IMM, 0x00, x.Low, x.High,
    (byte)Op.MOV_REG_IMM, 0x01, y.Low, y.High,
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
