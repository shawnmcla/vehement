#define VERBOSE
using Vehement.Debug;
using Vehement;

Assembler vasm = new Assembler(@"C:\Users\Shawn\source\repos\vehement\VASM\test.vasm");
var program = vasm.EmitCompiledProgram();
VM vm = new(program.ToArray());
vm.RunUntilHalt();

Debugger dbg = new(vm);
Console.WriteLine();
Console.WriteLine(dbg.Dump());
