#define VERBOSE
using Vehement.Common;

namespace Vehement
{
    public partial class VM
    {
        internal const int REGISTER_RETURN_VALUE = 0;
        internal const int REGISTER_SB_BACKUP = (int)Reg.Reg6;
        internal const int REGISTER_SP_BACKUP = (int)Reg.Reg7;
        internal const int REGISTER_RETURN_ADDRESS = (int)Reg.Reg8;

        internal const int TOTAL_REGISTER_COUNT = 12;
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

        public FlagsValue Flags { get => (FlagsValue)Registers[(int)Reg.Flags]; set => Registers[(int)Reg.Flags] = (ushort)value; }

        private Action? _onBeforeStep;
        private Action? _onAfterStep;

        private bool GetFlag(FlagsValue flag)
        {
            return Flags.HasFlag(flag);
        }

        private void SetFlag(FlagsValue flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }

        public Span<ushort> Registers => _regs;
        public Span<byte> Program => _mem.AsSpan(ProgramOffsetBytes, ProgramSizeBytes);
        public Span<byte> Heap => _mem.AsSpan(HeapOffsetBytes, HeapSizeBytes);
        public Span<byte> Stack => _mem.AsSpan(StackOffsetBytes, StackSizeBytes);

        public void WriteWord(Word addr, Word value)
        {
            if (ProgramSegmentReadonly && addr.u16 >= ProgramOffsetBytes && addr.u16 < ProgramOffsetBytes + ProgramSizeBytes)
            {
                _halted = true;
                Console.WriteLine(">> Illegal execution, halted.");
            }
            _mem[addr] = value.Low;
            _mem[addr + 1] = value.High;
        }

        public void WriteWord(int addr, Word value)
        {
            _mem[addr] = value.Low;
            _mem[addr + 1] = value.High;
        }


        public Word ReadWord(int addr)
        {
            return new Word(_mem[addr], _mem[addr + 1]);
        }

        public void StackWrite(ushort offset, Word value)
        {
            WriteWord(StackOffsetBytes + offset, value);
        }

        public Word StackRead(ushort offset)
        {
            return ReadWord(StackOffsetBytes + offset);
        }

        public Word ReadRegister(byte registerNumber)
        {
            return new Word(_regs[registerNumber]);
        }

        public void WriteRegister(byte registerNumber, Word value)
        {
            _regs[registerNumber] = value.u16;
        }

        public void Step()
        {
            if (_halted) return;

            if (DisallowExecutingOutsideProgramSegment && Pc < StaticSegmentSizeBytes || Pc >= ProgramSizeBytes)
                throw new Exception($"Pc is pointing to non-executable memory (0x{Pc:X2})");

            var instruction = (Op)Program[Pc];
            int comparison;
            byte reg, reg2, reg3;
            ushort addr, low, high;
            Word value, value2, addrWord;

            Console.WriteLine($"PC {Pc} = 0x{instruction:X}");
            switch (instruction)
            {
                case Op.NOOP:
                    Pc++;
                    break;
                case Op.PUSH:
                    reg = Program[Pc + 1];
                    value = ReadRegister(reg);
                    StackWrite(Sp, value);
                    Sp += 2;
                    Pc += 2;
#if VERBOSE
                    Console.WriteLine($"PUSH $reg{reg} (push value of register to stack)");
                    //Console.WriteLine($"  Stack offset `0x{Sp - 2:X}` = `0x{value:X}`");
#endif
                    break;
                case Op.POP:
                    reg = Program[Pc + 1];
                    Sp -= 2;
                    value = StackRead(Sp);
                    WriteRegister(reg, value);
                    Pc += 2;
#if VERBOSE
                    Console.WriteLine($"POP $reg{reg} (pop value from stack into register)");
                    //Console.WriteLine($"  Stack offset `0x{Sp - 2:X}` = `0x{value:X}`");
#endif
                    break;
                case Op.DUP:
                    value = StackRead((ushort)(Sp - 2));
                    StackWrite(Sp, value);
                    Sp += 2;
                    Pc += 1;
#if VERBOSE
                    Console.WriteLine("DUP");
                    //Console.WriteLine($"  Duplicated value from top of stack({value})");
#endif
                    break;
                case Op.ADD:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word((ushort)(ReadRegister(reg2).u16 + ReadRegister(reg3).u16));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"ADD $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.ADDF:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word(ReadRegister(reg2).f16 + ReadRegister(reg3).f16);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"ADDF $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.SUB:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word((ushort)(ReadRegister(reg2).u16 - ReadRegister(reg3).u16));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"SUB $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.SUBF:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word(ReadRegister(reg2).f16 - ReadRegister(reg3).f16);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"SUBF $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.MUL:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word((ushort)(ReadRegister(reg2).u16 * ReadRegister(reg3).u16));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MUL $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.MULF:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word(ReadRegister(reg2).f16 * ReadRegister(reg3).f16);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MULF $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.DIV:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word((ushort)(ReadRegister(reg2).u16 / ReadRegister(reg3).u16));
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"DIV $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.DIVF:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    reg3 = Program[Pc + 3];
                    value = new Word(ReadRegister(reg2).f16 / ReadRegister(reg3).f16);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"DIVF $reg{reg} $reg{reg2} $reg{reg3}");
#endif
                    break;
                case Op.CMP_MEM_REG:
                case Op.CMP_REG_MEM:
                case Op.CMP_REG_REG:
                case Op.CMP:
                    var props = GetComparisonProperties(instruction);
                    value = props.X;
                    value2 = props.Y;
                    comparison = value.u16.CompareTo(value2.u16);
                    SetFlag(FlagsValue.CmpEqual, comparison == 0);
                    SetFlag(FlagsValue.CmpGreaterThan, comparison > 0);
                    SetFlag(FlagsValue.CmpLessThan, comparison < 0);
                    Pc += (ushort)(1 + props.Offset);
#if VERBOSE
                    Console.WriteLine($"Comparison result: {comparison}");
                    //Console.WriteLine($"EQUALITY FLAG: {value} == {value2}? {Flags.HasFlag(FlagsValue.CmpEqual)}");
                    //Console.WriteLine($"GT FLAG: {value} > {value2}? {Flags.HasFlag(FlagsValue.CmpGreaterThan)}");
                    //Console.WriteLine($"LT FLAG: {value} < {value2}? {Flags.HasFlag(FlagsValue.CmpLessThan)}");
#endif
                    break;
                case Op.MOV_REG_REG:
                    reg = Program[Pc + 1];
                    reg2 = Program[Pc + 2];
                    WriteRegister(reg, ReadRegister(reg2));
                    Pc += 3;
#if VERBOSE
                    Console.WriteLine($"MOV $reg{reg} $reg{reg2}");
                    Console.WriteLine($"  reg{reg} = `0x{ReadRegister(reg2).u16:X}`");
#endif
                    break;
                case Op.MOV_REG_MEM:
                    reg = Program[Pc + 1];
                    low = Program[Pc + 2];
                    high = Program[Pc + 3];
                    addrWord = new(Program[Pc + 2], Program[Pc + 3]);
                    value = ReadWord(addrWord);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    //Console.WriteLine($"MOV $reg{reg} $0x{addr}");
                    //Console.WriteLine($"  Write value `0x{value:X}` to register $r{reg}");
#endif
                    break;
                case Op.MOV_MEM_REG:
                    addrWord = new Word(Program[Pc + 1], Program[Pc + 2]);
                    reg = Program[Pc + 3];
                    value = ReadRegister(reg);
                    WriteWord(addrWord, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MOV $0x{addrWord} $reg{reg}");
                    Console.WriteLine($"  Write value `0x{value:X}` to memory at address `0x{addrWord:X}`");
#endif
                    break;
                case Op.MOV_REG_IMM:
                    reg = Program[Pc + 1];
                    value = new Word(Program[Pc + 2], Program[Pc + 3]);
                    WriteRegister(reg, value);
                    Pc += 4;
#if VERBOSE
                    Console.WriteLine($"MOV $reg{reg} `{value}`");
#endif
                    break;
                case Op.JUMP:
                case Op.JUMP_EQ:
                case Op.JUMP_NEQ:
                case Op.JUMP_GT:
                case Op.JUMP_LT:
                case Op.JUMP_LEQ:
                case Op.JUMP_GEQ:
                    if (ShouldJump(instruction))
                    {
                        low = Program[Pc + 1];
                        high = Program[Pc + 2];
                        addr = (ushort)(high << 8 | low);
                        Pc = addr;
#if VERBOSE
                        Console.WriteLine($"JUMP (of type {instruction}) to `0x{addr:X}`");
#endif
                    }
                    else
                    {
                        Pc += 3;
#if VERBOSE
                        Console.WriteLine($"Conditional JUMP (of type {instruction}) skipped.");
#endif
                    }
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

        private bool ShouldJump(Op instruction)
        {
            if (instruction == Op.JUMP) return true;

            if (instruction == Op.JUMP_EQ && GetFlag(FlagsValue.CmpEqual)) return true;
            if (instruction == Op.JUMP_NEQ && !GetFlag(FlagsValue.CmpEqual)) return true;

            if (instruction == Op.JUMP_GT && GetFlag(FlagsValue.CmpGreaterThan)) return true;
            if (instruction == Op.JUMP_LT && GetFlag(FlagsValue.CmpLessThan)) return true;

            if (instruction == Op.JUMP_GEQ && !GetFlag(FlagsValue.CmpLessThan)) return true;
            if (instruction == Op.JUMP_LEQ && !GetFlag(FlagsValue.CmpGreaterThan)) return true;

            return false;
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

        public void OnBeforeStep(Action callback)
        {
            if (_onBeforeStep is null) _onBeforeStep = callback;
            else _onBeforeStep += callback;
        }

        public void OnAfterStep(Action callback)
        {
            if (_onAfterStep is null) _onAfterStep = callback;
            else _onAfterStep += callback;
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

        private ComparisonProperties GetComparisonProperties(Op operation)
        {
            ComparisonProperties props = new();

            if (operation == Op.CMP)
            {
                props.X = StackRead((ushort)(Sp - 4));
                props.Y = StackRead((ushort)(Sp - 2));
            }
            else if (operation == Op.CMP_MEM_REG)
            {
                props.X = ReadWord(Program[Pc + 1] + (Program[Pc + 2] << 8));
                props.Y = ReadRegister(Program[Pc + 3]);
                props.Offset = 3;
            }
            else if (operation == Op.CMP_REG_MEM)
            {
                props.X = ReadRegister(Program[Pc + 1]);
                props.Y = ReadWord(Program[Pc + 2] + (Program[Pc + 3] << 8));
                props.Offset = 3;
            }
            else if (operation == Op.CMP_REG_REG)
            {
                props.X = ReadRegister(Program[Pc + 1]);
                props.Y = ReadRegister(Program[Pc + 2]);
                props.Offset = 2;
            }

            return props;
        }

        private static Half ToHalf(ushort value) => ReinterpretCast<ushort, Half>(value);
        private static ushort ToUshort(Half value) => ReinterpretCast<Half, ushort>(value);

        private static unsafe TDest ReinterpretCast<TSource, TDest>(TSource source)
        {
            var tr = __makeref(source);
            TDest w = default!;
            var trw = __makeref(w);
            *((IntPtr*)&trw) = *((IntPtr*)&tr);
            return __refvalue(trw, TDest);
        }

        private class ComparisonProperties
        {
            public Word X;
            public Word Y;
            public int Offset;
        }
    }
}