//#define VERBOSE

//using Vehement;
//using Vehement.Common;
//using Vehement.Debug;

//List<byte> program = new();

//program.AddRange(new byte[]
//{
//    (byte)Op.MOV_REG_IMM, 0x06, 0x01, 0x00,     // value to increment i with
//    (byte)Op.MOV_REG_IMM, 0x07, 0x04, 0x00,     // value to sub from Sp after popping two
//    (byte)Op.MOV_REG_IMM, 0x00, 0x00, 0x00,     // i
//    (byte)Op.MOV_REG_IMM, 0x01,  35,  0x00,    // while i < 25
//    (byte)Op.PUSH_IMM, 0x00, 0x00,
//    (byte)Op.PUSH_IMM, 0x01, 0x00,
//    (byte)Op.PUSH_IMM, 0x01, 0x00,
//    (byte)Op.CMP_REG_REG, 0x00, 0x01, // if i >= 100
//    (byte)Op.JUMP_GEQ, 0x00, 0x00, // Todo: compute address of end
//});
//int endAddrSpace = program.Count;
//program.AddRange(new byte[]
//{
//    // If not GEQ, do the fibbonacci
//    (byte)Op.POP, 0x02, // pop top
//    (byte)Op.POP, 0x03, // pop top - 1
//    (byte)Op.ADD, (byte)Reg.Sp, 0x07, // move sp back up 4 so we don't lose the values on stack :)
//    (byte)Op.ADD, 0x02, 0x03, // add n-1 + n-2
//    (byte)Op.PUSH, 0x02, // push n
//    (byte)Op.ADD, 0x00, 0x06,
//    (byte)Op.JUMP, (byte)(endAddrSpace - 6), 0x00
//});
//int endAddr = program.Count;
//program[endAddrSpace - 2] = (byte)(endAddr);
//program.AddRange(new byte[] {
//    // END
//    (byte)Op.HALT
//});

//VM vm = new(program.ToArray());

//Debugger dbg = new(vm);
////vm.RunUntilHalt();
//for (int i = 0; i < 1000; i++)
//    vm.Step();
//Console.WriteLine(dbg.Dump());
