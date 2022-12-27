using System.Reflection.Emit;
using System.Text;
using Vehement.Common;

namespace Vehement.Assembler
{

    public class Disassembler
    {
        private Options _options = new();
        private int _labelsCount = 0;
        private enum ArgType
        {
            Literal8,
            Literal16,
            LabelRef,
            Reg,
        }

        private class Section
        {
            public string Name { get; set; }
            public List<Instruction> Instructions { get; set; } = new();

            public int Size => Instructions.Select(i => i.Size).Sum();

            public void Add(Instruction instruction)
            {
                Instructions.Add(instruction);
            }

            public Section(string name)
            {
                Name = name;
            }

        }

        private class Instruction
        {
            public string Asm { get; set; }
            public int Offset { get; set; }
            public int Size { get; set; }
            public Instruction(string asm, int size)
            {
                Asm = asm;
                Size = size;
            }
        }

        private class InstructionInfo
        {
            public Op OpCode { get; set; }
            public Mnemonic Mnemonic { get; set; }
            public List<ArgType> Args { get; }
            public InstructionInfo(Op opCode, params ArgType[] args)
            {
                OpCode = opCode;
                Mnemonic = OpsMapper.GetMnemonic(opCode);
                Args = new(args);
            }
        }

        private int _pc = 0;
        private int _staticStart = 0;
        private int _programStart = 0;
        private int _staticSize = 0;
        private Dictionary<string, int> _staticRefs = new();
        private Dictionary<string, int> _labelRefs = new();
        private Dictionary<string, Section> _sections = new();
        private Section _mainSection = new("MAIN");

        private readonly string[] _registerLabels = { "$reg1", "$reg2", "$reg3", "$reg4", "$reg5", "$reg6", "$reg7", "$reg8", "$rbp", "$rsp", "$rpc" };

        private readonly byte[] _program;
        private ProgramHeader? _header;

        private readonly Dictionary<ArgType, int> _argSizes = new()
        {
            { ArgType.Reg, 1 },
            { ArgType.Literal8, 1 },
            { ArgType.Literal16, 2 },
            { ArgType.LabelRef, 2 },
        };

        private Dictionary<Op, InstructionInfo> InstructionInfos = new()
        {
            { Op.NOOP, new(Op.NOOP) },

            { Op.MOV_MEM_REG, new(Op.MOV_MEM_REG, ArgType.Literal16, ArgType.Reg)},
            { Op.MOV_REG_MEM, new(Op.MOV_REG_MEM, ArgType.Reg, ArgType.Literal16)},
            { Op.MOV_REG_IMM, new(Op.MOV_REG_IMM, ArgType.Reg,ArgType.Literal16)},
            { Op.MOV_REG_REG, new(Op.MOV_REG_REG, ArgType.Reg, ArgType.Reg)},

            { Op.ADD, new(Op.ADD, ArgType.Reg,ArgType.Reg,ArgType.Reg)},
            { Op.SUB, new(Op.SUB, ArgType.Reg,ArgType.Reg,ArgType.Reg)},
            { Op.MUL, new(Op.MUL, ArgType.Reg,ArgType.Reg,ArgType.Reg)},
            { Op.DIV, new(Op.DIV, ArgType.Reg,ArgType.Reg,ArgType.Reg)},

            { Op.JUMP, new(Op.JUMP, ArgType.LabelRef) },
            { Op.JUMP_ZERO, new(Op.JUMP_ZERO, ArgType.LabelRef) },
            { Op.JUMP_NOT_ZERO, new(Op.JUMP_NOT_ZERO, ArgType.LabelRef) },
            { Op.PUSH, new(Op.PUSH, ArgType.Reg) },
            { Op.POP, new(Op.POP, ArgType.Reg) },
            { Op.CALL, new(Op.CALL, ArgType.LabelRef) },
            { Op.RET, new(Op.RET) },
            { Op.PRINT, new(Op.PRINT, ArgType.Literal16) },

            { Op.HALT, new(Op.HALT)},
        };


        public void DisassembleStaticSegment(StringBuilder sb)
        {
            // TODO
            sb.AppendLine(".static");
            sb.AppendLine("\t");
        }

        public string Disassemble()
        {
            while (_pc < _program.Length)
            {
                StringBuilder sb = new();
                int instStartOffset = _pc;
                byte instruction = _program[_pc];

                if (!Enum.IsDefined(typeof(Op), instruction))
                {
                    throw new Exception("Invalid OpCode " + instruction);
                }

                Op op = (Op)instruction;
                InstructionInfo info = InstructionInfos[op];

                sb.Append($"{info.Mnemonic.ToString()} ");
                _pc += 1;

                foreach (var arg in info.Args)
                {
                    if (arg == ArgType.Reg)
                    {
                        sb.Append($"{_registerLabels[_program[_pc]]} ");
                    }
                    else if(arg == ArgType.Literal8)
                    {
                        sb.Append($"0x{_program[_pc]:X2} ");
                    }
                    else if(arg == ArgType.Literal16)
                    {
                        sb.Append($"0x{_program[_pc] + (_program[_pc + 1] << 8):X} ");
                    }
                    else if(arg == ArgType.LabelRef)
                    {
                        string label = $"_Label_{++_labelsCount}";
                        int target = _program[_pc] + (_program[_pc + 1] << 8);
                        _labelRefs.Add(label, target);
                        sb.Append($"{label} ");
                    }
                    else
                    {
                        throw new Exception("Invalid arg type " + arg);
                    }

                    _pc += _argSizes[arg];
                }

                int instSize = _pc - instStartOffset;
                _mainSection.Add(new(sb.ToString(), instSize));
            }

            foreach(var label in _labelRefs.Keys)
            {
                _sections.Add(label, new(label));
            }

            string indent = "";
            int offset = 0;
            var sortedLabels = _labelRefs.OrderBy(kvp => kvp.Value).ToList();
            int nextLabelIndex = 0;
            
            {
                StringBuilder sb = new();
                if (!_options.ImplicitStartSection)
                {
                    sb.AppendLine("_start:");
                    indent = "    ";
                }

                for (int i = 0; i < _mainSection.Instructions.Count; i++)
                {
                    if (nextLabelIndex < sortedLabels.Count && offset == sortedLabels[nextLabelIndex].Value)
                    {
                        sb.AppendLine($"{sortedLabels[nextLabelIndex].Key}:");
                        indent = "    ";
                        nextLabelIndex++;
                    }

                    var instr = _mainSection.Instructions[i];
                    offset += instr.Size;
                    sb.AppendLine($"{indent}{instr.Asm}");
                }

                return sb.ToString();
            }
        }

        public class Options
        {
            public bool ImplicitStartSection { get; set; } = false;
        }

        public Disassembler(byte[] program, Options? options = null)
        {
            if(options is not null)
            {
                _options = options;
            }

            _program = program;
            _header = ProgramHeader.FromBytes(program);
            if (_header is not null)
            {
                _staticStart = _header.Value.SizeBytes;
                _staticSize = _header.Value.StaticSegmentSize;
                _programStart = _staticStart + _staticSize;
                _pc = _programStart;
            }
        }
    }
}
