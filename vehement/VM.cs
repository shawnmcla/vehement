//#define VERBOSE
using System.Runtime.CompilerServices;
using Vehement.Common;

namespace Vehement
{
    public partial class VM
    {
        public class ProfilingData
        {
            public int OperationsExecuted = 0;
            public DateTime ExecutionStart = DateTime.MinValue;
            public DateTime ExecutionEnd = DateTime.MinValue;

            public void MarkExecutionStart()
            {
                ExecutionStart = DateTime.Now;
            }

            public void MarkExecutionEnd()
            {
                ExecutionEnd = DateTime.Now;
            }

            public double OpsPerSecond => OperationsExecuted / (ExecutionEnd - ExecutionStart).TotalSeconds;
            public ProfilingData() { }
        }

        private ProfilingData profilingData = new();
        public ProfilingData Profiling => profilingData;

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
        private const bool disallowExecuteAfterBoundaries = true;
        public bool DisallowExecutingOutsideProgramSegment { get => disallowExecuteAfterBoundaries; }

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

        // Note: Do not use these internally. These exist solely to expose the memory to the outside.
        public Span<ushort> Registers => _regs;
        public Span<byte> Program => _mem.AsSpan(ProgramOffsetBytes, ProgramSizeBytes);
        public Span<byte> Heap => _mem.AsSpan(HeapOffsetBytes, HeapSizeBytes);
        public Span<byte> Stack => _mem.AsSpan(StackOffsetBytes, StackSizeBytes);

        // Note: This method assumes that the program ends least 3 bytes away from the end of the _mem array.
        unsafe public void Step()
        {
            if (_halted) return;
            int pc = Pc;

            if (disallowExecuteAfterBoundaries && pc < StaticSegmentSizeBytes || pc >= ProgramSizeBytes)
                throw new Exception($"Pc is pointing to non-executable memory (0x{pc:X2})");

            var op = (Op)_mem[ProgramOffsetBytes + pc];
#if VERBOSE
            Console.WriteLine($"Before: {Pc} ({op})");
#endif
            int comparison;
            int pcAdjustment = OpEncodingHelpers.OpMayModifyPC(op) ? 0 : OpEncodingHelpers.GetInstructionSize(op);

            fixed (ushort* regsPtr = &_regs[0])
            fixed (byte* memPtr = &_mem[0])
            {
                byte* programPtr = memPtr + ProgramOffsetBytes;
                byte* stackPtr = memPtr + StackOffsetBytes;

                int flags = 0;
                Half f16_x, f16_y;
                short i16_x, i16_y;
                ushort* dst = null;
                byte* memDst = null;
                byte n1 = *(programPtr + pc + 1), n2 = *(programPtr + pc + 2), n3 = *(programPtr + pc + 3);
                ushort src = 0, addr = (ushort)(n1 | (n2 << 8));

                switch ((OpEncoding)((int)op & 0b111_00000))
                {
                    case OpEncoding.REG_IMM:
                        dst = regsPtr + n1;
                        src = (ushort)(n2 | (n3 << 8));
                        break;
                    case OpEncoding.REG_REG:
                        dst = regsPtr + n1;
                        src = *(regsPtr + n2);
                        break;
                    case OpEncoding.REG_MEM:
                        dst = regsPtr + n1;
                        src = *(programPtr + (n2 | (n3 << 8)));
                        break;
                    case OpEncoding.MEM_REG:
                        memDst = memPtr + (n2 | (n3 << 8));
                        src = *(regsPtr + n3);
                        break;
                    case OpEncoding.NONE:
                    case OpEncoding.IMM:
                    case OpEncoding.REG:
                    default:
                        break;
                }

                switch (op)
                {
                    case Op.PUSH:
                        *(stackPtr + Sp) = (byte)(*(regsPtr + n1) & 0xFF);
                        *(stackPtr + Sp + 1) = (byte)(*(regsPtr + n1) >> 8);
                        Sp += 2;
#if VERBOSE
                        Console.WriteLine($"  PUSH value of reg {n1} ({*(stackPtr + Sp - 2) | (*(stackPtr + Sp - 1) << 8)}) at SP {Sp-2}");
#endif
                        break;
                    case Op.PUSH_IMM:
                        *(stackPtr + Sp) = (byte)(n1 & 0xFF);
                        *(stackPtr + Sp + 1) = (byte)(n2 >> 8);
                        Sp += 2;
#if VERBOSE
                        Console.WriteLine($"  PUSH value of imm ({n1 | (n2>>8)}) at SP {Sp-2}");
#endif
                        break;
                    case Op.POP:
                        Sp -= 2;
                        *(regsPtr + *(programPtr + pc + 1)) = (ushort)(*(stackPtr + Sp) | (*(stackPtr + Sp + 1) << 8));
#if VERBOSE
                        Console.WriteLine($"  POP value ({*(stackPtr + Sp) | (*(stackPtr + Sp + 1) << 8)}) from SP {Sp - 2} into register {*(programPtr + Pc + 1)}");
#endif
                        break;
                    case Op.DUP:
                        *(stackPtr + Sp) = *(stackPtr + Sp - 2);
                        *(stackPtr + Sp + 1) = *(stackPtr + Sp - 1);
                        Sp += 2;
                        break;
                    case Op.ADD:
                        *dst = (ushort)(*dst + src);
#if VERBOSE
                        Console.WriteLine($"  ADD value ({src}) to value of reg {n1} ({*(regsPtr + n1)})");
#endif
                        break;
                    case Op.SUB:
                        *dst = (ushort)(*dst - src);
                        break;
                    case Op.MUL:
                        *dst = (ushort)(*dst * src);
                        break;
                    case Op.DIV:
                        *dst = (ushort)(*dst / src);
                        break;
                    case Op.ADDF:
                        f16_x = UnsafeCast<ushort, Half>(*dst);
                        f16_y = UnsafeCast<ushort, Half>(src);
                        *dst = UnsafeCast<Half, ushort>(f16_x + f16_y);
                        break;
                    case Op.SUBF:
                        f16_x = UnsafeCast<ushort, Half>(*dst);
                        f16_y = UnsafeCast<ushort, Half>(src);
                        *dst = UnsafeCast<Half, ushort>(f16_x - f16_y);
                        break;
                    case Op.MULF:
                        f16_x = UnsafeCast<ushort, Half>(*dst);
                        f16_y = UnsafeCast<ushort, Half>(src);
                        *dst = UnsafeCast<Half, ushort>(f16_x * f16_y);
                        break;
                    case Op.DIVF:
                        f16_x = UnsafeCast<ushort, Half>(*dst);
                        f16_y = UnsafeCast<ushort, Half>(src);
                        *dst = UnsafeCast<Half, ushort>(f16_x / f16_y);
                        break;
                    case Op.ADDS:
                        i16_x = UnsafeCast<ushort, short>(*dst);
                        i16_y = UnsafeCast<ushort, short>(src);
                        *dst = UnsafeCast<short, ushort>((short)(i16_x + i16_y));
                        break;
                    case Op.SUBS:
                        i16_x = UnsafeCast<ushort, short>(*dst);
                        i16_y = UnsafeCast<ushort, short>(src);
                        *dst = UnsafeCast<short, ushort>((short)(i16_x - i16_y));
                        break;
                    case Op.MULS:
                        i16_x = UnsafeCast<ushort, short>(*dst);
                        i16_y = UnsafeCast<ushort, short>(src);
                        *dst = UnsafeCast<short, ushort>((short)(i16_x * i16_y));
                        break;
                    case Op.DIVS:
                        i16_x = UnsafeCast<ushort, short>(*dst);
                        i16_y = UnsafeCast<ushort, short>(src);
                        *dst = UnsafeCast<short, ushort>((short)(i16_x / i16_y));
                        break;
                    case Op.MOV_MEM_REG:
                        *memDst = (byte)(src & 0xFF);
                        *(memDst + 1) = (byte)(src >> 8);
                        break;
                    case Op.MOV_REG_IMM:
                    case Op.MOV_REG_MEM:
                    case Op.MOV_REG_REG:
                        *dst = src;
                        break;
                    case Op.CMP_MEM_REG:
                        comparison = (ushort)(*memDst | (*(memDst + 1) << 8)).CompareTo(src);
                        if (comparison == 0) flags |= (byte)FlagsValue.CmpEqual;
                        if (comparison > 0) flags |= (byte)FlagsValue.CmpGreaterThan;
                        if (comparison < 0) flags |= (byte)FlagsValue.CmpLessThan;
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) & 0b11111_000);
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) | flags);
                        break;
                    case Op.CMP_REG_REG:
                    case Op.CMP_REG_MEM:
                        comparison = (*dst).CompareTo(src);
                        if (comparison == 0) flags |= (byte)FlagsValue.CmpEqual;
                        if (comparison > 0) flags |= (byte)FlagsValue.CmpGreaterThan;
                        if (comparison < 0) flags |= (byte)FlagsValue.CmpLessThan;
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) & 0b11111_000);
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) | flags);
#if VERBOSE
                        Console.WriteLine($"  CMP_REG_(REG|MEM) {*dst} <=> {src} --> {comparison}");
                        Console.WriteLine($"  FLAGS: {Flags}");
#endif
                        break;
                    case Op.CMP:
                        comparison = (ushort)(_mem[Sp - 4] | (_mem[Sp - 3] << 8)).CompareTo((ushort)(_mem[Sp - 2] | (_mem[Sp - 1] << 8)));
                        if (comparison == 0) flags |= (byte)FlagsValue.CmpEqual;
                        if (comparison > 0) flags |= (byte)FlagsValue.CmpGreaterThan;
                        if (comparison < 0) flags |= (byte)FlagsValue.CmpLessThan;
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) & 0b11111_000);
                        *(regsPtr + 11) = (ushort)(*(regsPtr + 11) | flags);
                        break;
                    case Op.JUMP:
                        *(regsPtr + 8) = addr;
                        break;
                    case Op.JUMP_EQ:
                        *(regsPtr + 8) = (ushort)(Flags.HasFlag(FlagsValue.CmpEqual) ? addr : pc + 3);
                        break;
                    case Op.JUMP_NEQ:
                        *(regsPtr + 8) = (ushort)(!Flags.HasFlag(FlagsValue.CmpEqual) ? addr : pc + 3);
                        break;
                    case Op.JUMP_GT:
                        *(regsPtr + 8) = (ushort)(Flags.HasFlag(FlagsValue.CmpGreaterThan) ? addr : pc + 3);
                        break;
                    case Op.JUMP_LT:
                        *(regsPtr + 8) = (ushort)(Flags.HasFlag(FlagsValue.CmpLessThan) ? addr : pc + 3);
                        break;
                    case Op.JUMP_LEQ:
                        *(regsPtr + 8) = (ushort)(!Flags.HasFlag(FlagsValue.CmpGreaterThan) ? addr : pc + 3);
                        break;
                    case Op.JUMP_GEQ:
                        *(regsPtr + 8) = (ushort)(!Flags.HasFlag(FlagsValue.CmpLessThan) ? addr : pc + 3);
#if VERBOSE
                        Console.WriteLine($"  JUMP_GEQ to {addr} ? {(!Flags.HasFlag(FlagsValue.CmpLessThan) ? "Yes" : "No")}");
#endif
                        break;
                    case Op.CALL:
                        Registers[REGISTER_SB_BACKUP] = Sb;
                        Registers[REGISTER_SP_BACKUP] = Sp;
                        Registers[REGISTER_RETURN_ADDRESS] = (ushort)(pc + 3);
                        Sb = Sp;
                        *(regsPtr + 8) = addr;
                        break;
                    case Op.RET:
                        Sb = Registers[REGISTER_SB_BACKUP];
                        Sp = Registers[REGISTER_SP_BACKUP];
                        *(regsPtr + 8) = Registers[REGISTER_RETURN_ADDRESS];
                        break;
                    case Op.HALT:
                        _halted = true;
                        break;
                    case Op.NOOP:
                        break;
                    default:
                        throw new Exception($"Unrecognized instruction: `0x{(int)op:X2}` at PC={pc}");
                }

                profilingData.OperationsExecuted++;
                *(regsPtr + 8) += (ushort)pcAdjustment;
#if VERBOSE
                Console.WriteLine($"After: {Pc}\n");
#endif
            }
        }

        private bool ShouldJump(Op instruction)
        {
            if (instruction == Op.JUMP) return true;

            if (instruction == Op.JUMP_EQ && Flags.HasFlag(FlagsValue.CmpEqual)) return true;
            if (instruction == Op.JUMP_NEQ && Flags.HasFlag(FlagsValue.CmpEqual)) return true;

            if (instruction == Op.JUMP_GT && Flags.HasFlag(FlagsValue.CmpGreaterThan)) return true;
            if (instruction == Op.JUMP_LT && Flags.HasFlag(FlagsValue.CmpLessThan)) return true;

            if (instruction == Op.JUMP_GEQ && !Flags.HasFlag(FlagsValue.CmpLessThan)) return true;
            if (instruction == Op.JUMP_LEQ && !Flags.HasFlag(FlagsValue.CmpGreaterThan)) return true;

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
            profilingData.MarkExecutionStart();
            while (!_halted)
            {
                Step();
            }
            profilingData.MarkExecutionEnd();
            Console.WriteLine($"Total Ops: {profilingData.OperationsExecuted} | Total time: {(profilingData.ExecutionEnd - profilingData.ExecutionStart).TotalMilliseconds}ms |Ops per second: {profilingData.OpsPerSecond:0}");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        private static unsafe TTo UnsafeCast<TFrom, TTo>(TFrom value) where TFrom : struct where TTo : struct => *(TTo*)(void*)&value;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    }
}