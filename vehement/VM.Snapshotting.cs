using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehement
{
    public partial class VM
    {
        public class StateSnapshot
        {
            public int MemorySize { get; private set; }
            public int StaticSegmentSizeBytes { get; private set; }
            public int ProgramOffsetBytes { get; private set; }
            public int ProgramSizeBytes { get; private set; }
            public int StackOffsetBytes { get; private set; }
            public int StackSizeBytes { get; private set; }
            public int HeapOffsetBytes { get; private set; }
            public int HeapSizeBytes { get; private set; }
            public bool ProgramSegmentReadonly { get; private set; }
            public bool DisallowExecutingOutsideProgramSegment { get; private set; }
            public ushort[] Regs = new ushort[TOTAL_REGISTER_COUNT];
            public byte[] Mem = Array.Empty<byte>();

            public bool _halted = false;

            public static StateSnapshot Create(VM vm)
            {
                StateSnapshot snapshot = new()
                {
                    MemorySize = vm.MemorySize,
                    StaticSegmentSizeBytes = vm.StaticSegmentSizeBytes,
                    ProgramOffsetBytes = vm.ProgramOffsetBytes,
                    ProgramSizeBytes = vm.ProgramSizeBytes,
                    StackOffsetBytes = vm.StackOffsetBytes,
                    StackSizeBytes = vm.StackSizeBytes,
                    HeapOffsetBytes = vm.HeapOffsetBytes,
                    HeapSizeBytes = vm.HeapSizeBytes,
                    ProgramSegmentReadonly = vm.ProgramSegmentReadonly,
                    DisallowExecutingOutsideProgramSegment = vm.DisallowExecutingOutsideProgramSegment,
                    Regs = (ushort[])(vm._regs.Clone()),
                    Mem = (byte[])(vm._mem.Clone())
                };
                return snapshot;
            }

            private StateSnapshot() { }
        }

        public StateSnapshot GetSnapshot()
        {
            return StateSnapshot.Create(this);
        }

        public void RestoreSnapshot(StateSnapshot snapshot)
        {
            MemorySize = snapshot.MemorySize;
            StaticSegmentSizeBytes = snapshot.StaticSegmentSizeBytes;
            ProgramOffsetBytes = snapshot.ProgramOffsetBytes;
            ProgramSizeBytes = snapshot.ProgramSizeBytes;
            StackOffsetBytes = snapshot.StackOffsetBytes;
            StackSizeBytes = snapshot.StackSizeBytes;
            HeapOffsetBytes = snapshot.HeapOffsetBytes;
            HeapSizeBytes = snapshot.HeapSizeBytes;
            ProgramSegmentReadonly = snapshot.ProgramSegmentReadonly;
            //DisallowExecutingOutsideProgramSegment = snapshot.DisallowExecutingOutsideProgramSegment;
            _regs = (ushort[])(snapshot.Regs.Clone());
            _mem = (byte[])(snapshot.Mem.Clone());
        }

    }
}
