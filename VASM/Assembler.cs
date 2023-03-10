using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Vehement;
using Vehement.Common;

namespace Vehement.Assembler
{
    public partial class Assembler
    {
        private class CompilationSection
        {
            private string name = "";
            private static int next = 0;
            private List<CompilationResult> results = new();
            private int index = 0;

            public List<CompilationResult> Results => results;
            public string Name => name;
            public int Index => index;

            public CompilationSection(string name, List<CompilationResult> results)
            {
                if (name.ToUpper() == "_START")
                {
                    index = -1;
                }
                else
                {
                    index = next++;
                }
                this.name = name.ToUpper();
                this.results = results;
            }
        }

        private List<string> sourceLines;
        private int lineIndex = 0;

        private StaticData? staticData;
        private Dictionary<string, List<string>> sourceSections = new();
        private Dictionary<string, CompilationSection> compiledSections = new();

        public List<byte> EmitCompiledProgram()
        {
            int staticSegmentSize = 0;
            int offset = 0;
            Dictionary<string, int> sectionOffsets = new();
            List<byte> bytes = new();

            if (staticData is not null)
            {
                bytes.AddRange(staticData.AllBytes);
                staticSegmentSize = bytes.Count;
                offset = bytes.Count;
            }
            var orderedSections = compiledSections.Values.OrderBy(s => s.Index);
            // First pass, simply track sections and their offsets.
            foreach (var section in orderedSections)
            {
                sectionOffsets.Add(section.Name, offset);
                foreach (var result in section.Results)
                {
                    offset += result.Bytecode.Length;
                }
            }

            // Second pass, emit bytecode and patch jump targets
            foreach (var section in orderedSections)
            {
                Console.WriteLine("EMITTING FOR SECTION: " + section.Name);
                List<byte> sectionBytes = new();
                foreach (var result in section.Results)
                {
                    byte[] bytecode = result.Bytecode;
                    if (result.LabelToResolve.HasValue)
                    {
                        string label = result.LabelToResolve.Value.label;
                        int localOffset = result.LabelToResolve.Value.offset;

                        if (!sectionOffsets.ContainsKey(label))
                            throw new Exception($"Label `{label}` not found!");
                        var (lo, hi) = SplitToBytes((ushort)sectionOffsets[label]);
                        bytecode[localOffset] = lo;
                        bytecode[localOffset + 1] = hi;
                    }
                    sectionBytes.AddRange(bytecode);
                }
                bytes.AddRange(sectionBytes);
            }

            ProgramHeader header = new()
            {
                Version = ProgramHeader.VERSION,
                Endianness = 0,
                Options = 0,
                StaticSegmentSize = (ushort)staticSegmentSize
            };

            bytes.InsertRange(0, header.ToBytes());
            return bytes;
        }

        private void CompileSection(string name, List<string> lines)
        {
            List<CompilationResult> compiledSectionResults = new();

            foreach (var line in lines)
            {
                var result = AsmToBytecode(line);
                compiledSectionResults.Add(result);
            }

            compiledSections.Add(name, new(name, compiledSectionResults));
        }

        private void CompileSections()
        {
            foreach (var (sectionName, sectionSource) in sourceSections)
            {
                Console.WriteLine($"Compiling section `{sectionName}`");
                CompileSection(sectionName, sectionSource);
            }
        }

        private void ParseStaticSegment()
        {
            staticData = new StaticData();

            while (lineIndex < sourceLines.Count && sourceLines[lineIndex].StartsWith("$"))
            {
                List<byte> bytes = new();
                var line = sourceLines[lineIndex];
                var parts = line.Split(':');
                var identifier = parts[0].TrimStart('$').Trim();
                var value = String.Join(':', parts[1..]).Trim();
                if (IsStringLiteral(value))
                {
                    string str = value.Trim('"');
                    var strBytes = Encoding.ASCII.GetBytes(str);
                    int strLen = strBytes.Length;

                    bytes.Add((byte)strLen);
                    bytes.AddRange(strBytes);
                }
                else
                {
                    throw new Exception("Unsupported static data type.");
                }

                staticData.Add(identifier, bytes);
                lineIndex++;
            }
        }

        private void ParseSections()
        {
            while (lineIndex < sourceLines.Count)
            {
                string line = sourceLines[lineIndex];
                Debug.WriteLine($"Parsing line #{lineIndex}:");
                Debug.WriteLine($"  {line}");

                if (line.ToUpper() == ".STATIC")
                {
                    lineIndex++;
                    ParseStaticSegment();
                    continue;
                }

                string sectionName;
                if (IsLabel(line))
                {
                    lineIndex++;
                    sectionName = line.TrimEnd(':').ToUpper();
                    Debug.WriteLine($"Start of section `{sectionName}`");
                }
                else
                {
                    sectionName = "_START";
                    Debug.WriteLine("Start of implicit _START section");
                }

                if (sourceSections.ContainsKey(sectionName))
                {
                    throw new Exception($"Duplicate section: `{sectionName}`");
                }

                sourceSections.Add(sectionName, ParseSection());
            }
        }

        private List<string> ParseSection()
        {
            List<string> lines = new();
            while (lineIndex < sourceLines.Count && !IsLabel(sourceLines[lineIndex]))
            {
                lines.Add(sourceLines[lineIndex++]);
            }
            return lines;
        }

        public static Assembler FromFile(string filePath)
        {
            var sourceCode = File.ReadAllText(filePath);
            return new Assembler(sourceCode);
        }

        public Assembler(string sourceCode)
        {
            sourceLines = sourceCode.Split('\n')
                            .Select(line => line.Trim().Split("//")[0])
                            .Where(line => !String.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                            .ToList();
            ParseSections();
            CompileSections();
        }

        public static CompilationResult AsmToBytecode(string asm)
        {
            List<byte> bytes = new();

            var instruction = asm.ToUpper().Split(' ').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            var mnemonic = instruction[0];
            string dst = "", src = "";
            switch (mnemonic)
            {
                case "JUMP":
                    Debug.Assert(instruction.Length == 2, "JUMP [label]");
                    return new(new byte[] { (byte)Op.JUMP, 0, 0 }, instruction[1], 1);
                case "JUMP_GEQ":
                    Debug.Assert(instruction.Length == 2, "JUMP_GEQ [label]");
                    return new(new byte[] { (byte)Op.JUMP_GEQ, 0, 0 }, instruction[1], 1);
                case "JUMP_LEQ":
                    Debug.Assert(instruction.Length == 2, "JUMP_GEQ [label]");
                    return new(new byte[] { (byte)Op.JUMP_LEQ, 0, 0 }, instruction[1], 1);
                case "ADD":
                    Debug.Assert(instruction.Length == 3, "ADD [reg (dest)] [reg (operand)]");
                    return new(new byte[] {
                    (byte)Op.ADD,
                    RegisterToByte(instruction[1]),
                    RegisterToByte(instruction[2]),
                });
                case "SUB":
                    Debug.Assert(instruction.Length == 3, "SUB [reg (dest)] [reg (operand)]");
                    return new(new byte[] {
                    (byte)Op.SUB,
                    RegisterToByte(instruction[1]),
                    RegisterToByte(instruction[2]),
                });
                case "MUL":
                    Debug.Assert(instruction.Length == 3, "MUL [reg (dest)] [reg (operand)]");
                    return new(new byte[] {
                    (byte)Op.MUL,
                    RegisterToByte(instruction[1]),
                    RegisterToByte(instruction[2]),
                });
                case "CALL":
                    Debug.Assert(instruction.Length == 2, "CALL [addr]");
                    if (IsImmediate(instruction[1]))
                    {
                        var (lo, hi) = SplitToBytes(ImmediateToUShort(instruction[1]));
                        return new(new byte[] { (byte)Op.CALL, lo, hi });
                    }
                    else if (IsLabelReference(instruction[1]))
                    {
                        return new(new byte[] { (byte)Op.CALL, 0, 0 }, instruction[1], 1);
                    }
                    break;
                case "RET":
                    Debug.Assert(instruction.Length == 1, "RET");
                    return new(new byte[] { (byte)Op.RET });
                case "PUSH":
                    Debug.Assert(instruction.Length == 2, "PUSH [reg | imm16]");
                    if (IsRegister(instruction[1]))
                    {
                        return new(new byte[] { (byte)Op.PUSH, RegisterToByte(instruction[1]) });
                    }
                    else
                    {
                        var (lo, hi) = SplitToBytes(ImmediateToUShort(instruction[1]));
                        return new(new byte[] { (byte)Op.PUSH_IMM, lo, hi });
                    }
                case "POP":
                    Debug.Assert(instruction.Length == 2, "POP [reg]");
                    return new(new byte[] { (byte)Op.POP, RegisterToByte(instruction[1]) });
                case "DUP":
                    Debug.Assert(instruction.Length == 1, "DUP");
                    return new(new byte[] { (byte)Op.DUP });
                case "MOV":
                    Debug.Assert(instruction.Length == 3, "MOV [dest] [src]");
                    dst = instruction[1];
                    src = instruction[2];
                    if (IsRegister(dst) && IsRegister(src))
                    {
                        // Emit MOV_REG_REG
                        return new(new byte[] { (byte)Op.MOV_REG_REG, RegisterToByte(dst), RegisterToByte(src) });
                    }
                    else if (IsRegister(dst) && IsImmediate(src))
                    {
                        // Emit MOV_REG_IMM
                        var (lo, hi) = SplitToBytes(ImmediateToUShort(src));
                        return new(new byte[] { (byte)Op.MOV_REG_IMM, RegisterToByte(dst), lo, hi });
                    }
                    else if (IsRegister(dst) && IsMemory(src))
                    {
                        // Emit MOV_REG_MEM
                        var (lo, hi) = SplitToBytes(MemoryAddressToUShort(src));
                        return new(new byte[] { (byte)Op.MOV_REG_MEM, RegisterToByte(dst), lo, hi });
                    }
                    else if (IsRegister(dst) && IsImmediate(src))
                    {
                        // Emit MOV_REG_MEM
                        var (lo, hi) = SplitToBytes(MemoryAddressToUShort(src));
                        return new(new byte[] { (byte)Op.MOV_REG_IMM, RegisterToByte(dst), lo, hi });
                    }
                    else if (IsMemory(dst) && IsRegister(src))
                    {
                        // EMIT MOV_MEM_REG
                        var (lo, hi) = SplitToBytes(MemoryAddressToUShort(dst));
                        return new(new byte[] { (byte)Op.MOV_MEM_REG, lo, hi, RegisterToByte(dst) });
                    }
                    else
                    {
                        throw new Exception($"Invalid operands for MOV: `{dst}`, `{src}`");
                    }
                case "CMP":
                    if (instruction.Length == 1)
                    {
                        return new(new byte[] { (byte)Op.CMP });
                    }
                    else
                    {
                        Debug.Assert(instruction.Length == 3, "CMP [reg|mem operand 1] [reg|mem operand 2]");
                        dst = instruction[1];
                        src = instruction[2];
                        if (IsRegister(dst) && IsRegister(src))
                        {
                            return new(new byte[] { (byte)Op.CMP_REG_REG, RegisterToByte(dst), RegisterToByte(src) });
                        }
                        else if (IsRegister(dst) && IsMemory(src))
                        {
                            var (lo, hi) = SplitToBytes(MemoryAddressToUShort(src));
                            return new(new byte[] { (byte)Op.CMP_REG_MEM, RegisterToByte(dst), lo, hi });
                        }
                        else if (IsMemory(dst) && IsRegister(src))
                        {
                            var (lo, hi) = SplitToBytes(MemoryAddressToUShort(dst));
                            return new(new byte[] { (byte)Op.CMP_REG_MEM, RegisterToByte(src), lo, hi });
                        }
                        else
                        {
                            Debug.Assert(false, $"Invalid operands for CMP: `{dst}`, `{src}`");
                        }
                    }
                    break;
                case "HALT":
                    Debug.Assert(instruction.Length == 1, "HALT");
                    return new(new byte[] { (byte)Op.HALT });
                default:
                    Debug.Assert(false, "Unrecognized mnemonic: " + mnemonic);
                    break;
            }

            return new(""u8.ToArray());
        }

        #region UTIL
        public static (byte lo, byte hi) SplitToBytes(ushort value) => (lo: (byte)(value & 0xFF), hi: (byte)(value >> 8));
        public static byte RegisterToByte(string str) => IsSpecialRegister(str) ? SpecialRegisterToByte(str) : (byte)(Convert.ToByte(str[4..], 16) - 1);

        public static byte SpecialRegisterToByte(string str)
        {
            return str switch
            {
                "$RBP" => (byte)Reg.Sb,
                "$RSP" => (byte)Reg.Sp,
                "$RPC" => (byte)Reg.Pc,
                _ => throw new Exception($"Invalid special register `{str}`"),
            };
        }

        public static ushort ImmediateToUShort(string str) => Convert.ToUInt16(str, 16);
        public static ushort MemoryAddressToUShort(string str) => Convert.ToUInt16(str[1..], 16);
        public static bool IsImmediate(string str) => Regex.IsMatch(str, @"^0X[0-9A-F]{1,4}$");
        public static bool IsSpecialRegister(string str) => str == "$RSP" || str == $"RBP" || str == "$RPC";
        public static bool IsRegister(string str) => IsSpecialRegister(str) || Regex.IsMatch(str, @"^\$REG\d$");
        public static bool IsMemory(string str) => Regex.IsMatch(str, @"^\$0X[0-9A-F]{1,4}$");
        public static bool IsLabelReference(string str) => Regex.IsMatch(str, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        static bool IsLabel(string str) => Regex.IsMatch(str, @"^\.?[a-zA-Z_][a-zA-Z0-9_]*:$");
        static bool IsStringLiteral(string str) => Regex.IsMatch(str, "^\".*\"$");
        #endregion
    }
}
