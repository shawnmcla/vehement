#define VERBOSE
using System.Text;
using Vehement.Common;

namespace Vehement
{
    public class VM
    {
        internal const int REGISTER_RETURN_VALUE = 0;
        internal const int REGISTER_SB_BACKUP = (int)Reg.Reg6;
        internal const int REGISTER_SP_BACKUP = (int)Reg.Reg7;
        internal const int REGISTER_RETURN_ADDRESS = (int)Reg.Reg8;

        internal const int TOTAL_REGISTER_COUNT = 11;
        internal const int GENERAL_REGISTER_COUNT = 8;
        internal const int DEFAULT_MEMORY_SIZE = ushort.MaxValue;

        internal const int DEFAULT_PROGRAM_SIZE = 4096;
        internal const int DEFAULT_STACK_SIZE = 4096;
        internal const int DEFAULT_HEAP_SIZE = 2048;

        internal const int DEFAULT_PROGRAM_OFFSET = 0;
        internal const int DEFAULT_STACK_OFFSET = DEFAULT_PROGRAM_OFFSET + DEFAULT_PROGRAM_SIZE;
        internal const int DEFAULT_HEAP_OFFSET = DEFAULT_STACK_OFFSET + DEFAULT_STACK_SIZE;

        private ProgramHeader _programHeader;

        public int MemorySize { get; private set; } = DEFAULT_MEMORY_SIZE;

        public int StaticSegmentSizeBytes { get; private set; } = 0;
        public int ProgramOffsetBytes { get; private set; } = DEFAULT_PROGRAM_OFFSET;
        public int ProgramSizeBytes { get; private set; } = DEFAULT_PROGRAM_SIZE;

        public int StackOffsetBytes { get; private set; } = DEFAULT_STACK_OFFSET;
        public int StackSizeBytes { get; private set; } = DEFAULT_STACK_SIZE;

        public int HeapOffsetBytes { get; private set; } = DEFAULT_HEAP_OFFSET;
        public int HeapSizeBytes { get; private set; } = DEFAULT_HEAP_SIZE;

        public bool ProgramSegmentReadonly { get; private set; } = true;
        public bool DisallowExecutingOutsideProgramSegment { get; private set; } = true;

        private ushort[] _regs = new ushort[TOTAL_REGISTER_COUNT];
        private byte[] _mem;

        private bool _halted = false;
        public bool Halted => _halted;

        public ushort Pc { get => _regs[(int)Reg.Pc]; set => _regs[(int)Reg.Pc] = value; }
        public ushort Sp { get => _regs[(int)Reg.Sp]; set => _regs[(int)Reg.Sp] = value; }
        public ushort Sb { get => _regs[(int)Reg.Sb]; set => _regs[(int)Reg.Sb] = value; }

        public Span<ushort> Registers => _regs;
        public Span<byte> Program => _mem.AsSpan(ProgramOffsetBytes, ProgramSizeBytes);
        public Span<byte> Heap => _mem.AsSpan(HeapOffsetBytes, HeapSizeBytes);
        public Span<byte> Stack => _mem.AsSpan(StackOffsetBytes, StackSizeBytes);

        public void Write16(int addr, ushort value)
        {
            if (ProgramSegmentReadonly && addr >= ProgramOffsetBytes && addr < ProgramOffsetBytes + ProgramSizeBytes)
            {
                _halted = true;
                Console.WriteLine(">> Illegal execution, halted.");
            }

            var low = (byte)(value & 0xFF);
            var high = (byte)(value >> 8);
            _mem[addr] = low;
            _mem[addr + 1] = high;
        }

        public ushort Read16(int addr)
        {
            var low = _mem[addr];
            var high = _mem[addr + 1];
            return (ushort)(high << 8 | low);
        }

        public void Write8(int addr, byte value)
        {
            _mem[addr] = value;
        }

        public byte Read8(int addr)
        {
            return _mem[addr];
        }

        public void HeapWrite8(ushort offset, byte value)
        {
            Write8(HeapOffsetBytes + offset, value);
        }

        public byte HeapRead8(ushort offset)
        {
            return Read8(HeapOffsetBytes + offset);
        }

        public void StackWrite8(ushort offset, byte value)
        {
            Write8(StackOffsetBytes + offset, value);
        }

        public byte StackRead8(ushort offset)
        {
            return Read8(StackOffsetBytes + offset);
        }

        public void HeapWrite16(ushort offset, ushort value)
        {
            Write16(HeapOffsetBytes + offset, value);
        }

        public ushort HeapRead16(ushort offset)
        {
            return Read16(HeapOffsetBytes + offset);
        }

        public void StackWrite16(ushort offset, ushort value)
        {
            Write16(StackOffsetBytes + offset, value);
        }

        public ushort StackRead16(ushort offset)
        {
            return Read16(StackOffsetBytes + offset);
        }

        public ushort ReadRegister(byte registerNumber)
        {
            return _regs[registerNumber];
        }

        public void WriteRegister(byte registerNumber, ushort value)
        {
            _regs[registerNumber] = value;
        }

        public void Step()
        {
            if (_halted) return;

            if (DisallowExecutingOutsideProgramSegment && Pc < StaticSegmentSizeBytes || Pc > ProgramSizeBytes)
                throw new Exception($"Pc is pointing to non-executable memory (0x{Pc:X2})");

            var instruction = (Op)Program[Pc];
            byte reg, reg2, reg3;
            ushort value, addr, low, high;

            Console.WriteLine($"PC {Pc} = 0x{instruction:X}");
            switch (instruction)
            {
                case Op.NOOP:
                    Pc++;
                    break;
                case Op.PUSH:
                    reg = Program[Pc + 1];
                    value = ReadRegister(reg);
                    StackWrite16(Sp, value);
                    Sp += 2;
                    Pc += 2;
#if VERBOSE
                    Console.WriteLine($"PUSH $reg{reg} (push value of register to stack)");
                    Console.WriteLine($"  Stack offset `0x{Sp - 2:X}` = `0x{value:X}`");
#endif
                    break;
                case Op.POP:
                    reg = Program[Pc + 1];
                    Sp -= 2;
                    value = StackRead16(Sp);
                    WriteRegister(reg, value);
                    Pc += 2;
#if VERBOSE
                    Console.WriteLine($"POP $reg{reg} (pop value from stack into register)");
                    Console.WriteLine($"  Stack offset `0x{Sp - 2:X}` = `0x{value:X}`");
#endif
                    break;
                case Op.ADD:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = (ushort)(ReadRegister(reg2) + ReadRegister(reg3));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"ADD $reg{reg} $reg{reg2} $reg{reg3}");
                    Console.WriteLine($"  reg{reg3} = `0x{ReadRegister(reg2):X}` + `0x{ReadRegister(reg3)}` (`0x{value:X}`)");
#endif
                    break;
                case Op.SUB:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = (ushort)(ReadRegister(reg2) - ReadRegister(reg3));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"SUB $reg{reg} $reg{reg2} $reg{reg3}");
                    Console.WriteLine($"  reg{reg3} = `0x{ReadRegister(reg2):X}` - `0x{ReadRegister(reg3)}` (`0x{value:X}`)");
#endif
                    break;
                case Op.MUL:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = (ushort)(ReadRegister(reg2) * ReadRegister(reg3));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MUL $reg{reg} $reg{reg2} $reg{reg3}");
                    Console.WriteLine($"  reg{reg3} = `0x{ReadRegister(reg2):X}` * `0x{ReadRegister(reg3)}` (`0x{value:X}`)");
#endif
                    break;
                case Op.DIV:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = (ushort)(ReadRegister(reg2) / ReadRegister(reg3));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"DIV $reg{reg} $reg{reg2} $reg{reg3}");
                    Console.WriteLine($"  reg{reg3} = `0x{ReadRegister(reg2):X}` / `0x{ReadRegister(reg3)}` (`0x{value:X}`)");
#endif
                    break;
                case Op.MOV_REG_REG:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    WriteRegister(reg, ReadRegister(reg2));
                    Pc += 3;
#if VERBOSE
                    Console.WriteLine($"MOV $reg{reg} $reg{reg2}");
                    Console.WriteLine($"  reg{reg} = `0x{ReadRegister(reg2):X}`");
#endif
                    break;
                case Op.MOV_REG_MEM:
                    reg = Program[Pc + 1];
                    low = Program[Pc + 2];
                    high = Program[Pc + 3];
                    addr = (ushort)(high << 8 | low);
                    value = Read16(addr);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MOV $reg{reg} $0x{addr}");
                    Console.WriteLine($"  Write value `0x{value:X}` to register $r{reg}");
#endif
                    break;
                case Op.MOV_MEM_REG:
                    low = Program[Pc + 1];
                    high = Program[Pc + 2];
                    reg = Program[Pc + 3];
                    addr = (ushort)(high << 8 | low);
                    value = ReadRegister(reg);
                    Write16(addr, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MOV $0x{addr} $reg{reg}");
                    Console.WriteLine($"  Write value `0x{value:X}` to memory at address `0x{addr:X}`");
#endif
                    break;
                case Op.MOV_REG_IMM:
                    reg = Program[Pc + 1];
                    low = Program[Pc + 2];
                    high = Program[Pc + 3];
                    value = (ushort)(high << 8 | low);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MOV $reg{reg} `{value}`");
#endif
                    break;
                case Op.JUMP:
                    low = Program[Pc + 1];
                    high = Program[Pc + 2];
                    addr = (ushort)(high << 8 | low);
                    Pc = addr;
#if VERBOSE
                    Console.WriteLine($"Uncoditional JUMP to `0x{addr:X}`");
#endif
                    break;
                case Op.CALL:
                    low = Program[Pc + 1];
                    high = Program[Pc + 2];
                    addr = (ushort)(high << 8 | low);
                    Registers[REGISTER_SB_BACKUP] = Sb;
                    Registers[REGISTER_SP_BACKUP] = Sp;
                    Registers[REGISTER_RETURN_ADDRESS] = (ushort)(Pc + 3); // this inst + size of addr + 1
                    Sb = Sp;
                    Pc = addr;
#if VERBOSE
                    Console.WriteLine($"CALL `0x{addr:X}`");
                    Console.WriteLine($"  Stored Sb(0x{Sb:X}), Sp(0x{Sp:X}), Return address({Pc + 1:X}) into registers 6, 7, 8");
#endif
                    break;
                case Op.RET:
                    Sb = Registers[REGISTER_SB_BACKUP];
                    Sp = Registers[REGISTER_SP_BACKUP];
                    Pc = Registers[REGISTER_RETURN_ADDRESS];
#if VERBOSE
                    Console.WriteLine($"RET");
                    Console.WriteLine($"  Restored Sb(0x{Sb:X}), Sp(0x{Sp:X}), Return address(0x{Pc + 1:X}) from registers 6, 7, 8");
#endif
                    break;
                case Op.HALT:
                    _halted = true;
                    break;
                default:
                    throw new Exception($"Unrecognized or unimplemented instruction: 0x{instruction:x}");
            }

#if VERBOSE
            Console.WriteLine();
#endif

        }

        public void RunInstructions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Halted) break;
                Step();
            }
        }

        public void RunUntilHalt()
        {
            while (!Halted)
            {
                Step();
            }
        }

        private void InitializeMemoryLayout(byte[] program)
        {
            /** Memory layout:
             * Program:     0 -> program size (read only)
             * Stack:       Program size -> +stack size
             * Heap:        Stack offset+size -> end
             */

            // TODO: Allow customizing these options
            MemorySize = DEFAULT_MEMORY_SIZE;


            StaticSegmentSizeBytes = _programHeader.StaticSegmentSize;
            ProgramOffsetBytes = 0;
            ProgramSizeBytes = program.Length;

            StackOffsetBytes = ProgramSizeBytes;
            StackSizeBytes = DEFAULT_STACK_SIZE;

            HeapOffsetBytes = StackOffsetBytes + StackSizeBytes;
            HeapSizeBytes = MemorySize - HeapOffsetBytes;
        }

        public VM(byte[] program)
        {
            var header = ProgramHeader.FromBytes(program);

            if (header is null)
            {
                _programHeader = ProgramHeader.Default();
            }
            else
            {
                _programHeader = header.Value;
            }

            InitializeMemoryLayout(program);
            _mem = new byte[MemorySize];
            Pc = _programHeader.StaticSegmentSize;
            Array.Copy(program, 0 + _programHeader.SizeBytes, _mem, 0, ProgramSizeBytes - _programHeader.SizeBytes);
        }

        public void PrintDebug()
        {
            for (int i = 0; i < 16; i++)
            {
                Console.Write($"{$"R{i + 1}",-5}|");
            }
            Console.WriteLine();
            for (int i = 0; i < 16; i++)
            {
                Console.Write($"{ReadRegister((byte)i),5}|");
            }
            Console.WriteLine();
            Console.WriteLine($"PC={Pc} | SP={Sp} | Sb={Sb}");
        }
    }
}
