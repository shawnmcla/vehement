using System.Diagnostics;
using Vehement.Assembler;

Assembler vasm = Assembler.FromFile(@"C:\Users\Shawn\source\repos\vehement\VASM\test.vasm");
var program = vasm.EmitCompiledProgram();
Disassembler dis = new(program.ToArray());
var disStr = dis.Disassemble();
Console.WriteLine(disStr);

//if (args is null || args.Length == 0)
//    Environment.Exit(0);
