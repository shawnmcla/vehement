using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Vehement.Common
{
    public static class OpsMapper
    {
        public const int x = 5;
        public static readonly Dictionary<Op, Mnemonic> mapping = new()
        {
            { Op.NOOP, Mnemonic.NOOP },

            { Op.MOV_MEM_REG, Mnemonic.MOV },
            { Op.MOV_REG_MEM, Mnemonic.MOV },
            { Op.MOV_REG_IMM, Mnemonic.MOV },
            { Op.MOV_REG_REG, Mnemonic.MOV },

            { Op.DUP, Mnemonic.DUP },

            { Op.ADD, Mnemonic.ADD },
            { Op.SUB, Mnemonic.SUB },
            { Op.MUL, Mnemonic.MUL },
            { Op.DIV, Mnemonic.DIV },

            { Op.ADDS, Mnemonic.ADD },
            { Op.SUBS, Mnemonic.SUB },
            { Op.MULS, Mnemonic.MUL },
            { Op.DIVS, Mnemonic.DIV },

            { Op.ADDF, Mnemonic.ADD },
            { Op.SUBF, Mnemonic.SUB },
            { Op.MULF, Mnemonic.MUL },
            { Op.DIVF, Mnemonic.DIV },

            { Op.CMP, Mnemonic.CMP },
            { Op.CMPF, Mnemonic.CMP },

            { Op.CMP_REG_REG, Mnemonic.CMP },
            { Op.CMP_REG_MEM, Mnemonic.CMP },
            { Op.CMP_MEM_REG, Mnemonic.CMP },

            { Op.CMPF_REG_REG, Mnemonic.CMP },
            { Op.CMPF_REG_MEM, Mnemonic.CMP },
            { Op.CMPF_MEM_REG, Mnemonic.CMP },

            { Op.CMPS_REG_REG, Mnemonic.CMP },
            { Op.CMPS_REG_MEM, Mnemonic.CMP },
            { Op.CMPS_MEM_REG, Mnemonic.CMP },

            { Op.JUMP, Mnemonic.JUMP },
            { Op.JUMPR, Mnemonic.JUMPR },
            { Op.JUMP_ZERO, Mnemonic.JUMPZ },
            { Op.JUMPR_ZERO, Mnemonic.JUMPRZ },
            { Op.JUMP_NOT_ZERO, Mnemonic.JUMPNZ },
            { Op.JUMPR_NOT_ZERO, Mnemonic.JUMPRNZ },
            { Op.JUMP_EQ, Mnemonic.JUMPEQ },
            { Op.JUMPR_EQ, Mnemonic.JUMPREQ },
            { Op.JUMP_NEQ, Mnemonic.JUMPNEQ },
            { Op.JUMPR_NEQ, Mnemonic.JUMPRNEQ },
            { Op.JUMP_GT, Mnemonic.JUMPGT },
            { Op.JUMPR_GT, Mnemonic.JUMPRGT },
            { Op.JUMP_LT, Mnemonic.JUMPLT },
            { Op.JUMPR_LT, Mnemonic.JUMPRLT },
            { Op.JUMP_GEQ, Mnemonic.JUMPGEQ },
            { Op.JUMPR_GEQ, Mnemonic.JUMPRGEQ },
            { Op.JUMP_LEQ, Mnemonic.JUMPLEQ },
            { Op.JUMPR_LEQ, Mnemonic.JUMPRLEQ },

            { Op.PUSH, Mnemonic.PUSH },
            { Op.PUSH_IMM, Mnemonic.PUSH },

            { Op.POP, Mnemonic.POP },

            { Op.CALL, Mnemonic.CALL },
            { Op.RET, Mnemonic.RET },

            { Op.HALT, Mnemonic.HALT },
        };

        public static Mnemonic GetMnemonic(Op op)
        {
            return mapping[op];
        }
    }

    public enum OperandType
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }

    public enum Mnemonic : byte
    {
        NOOP,

        MOV,
        DUP,

        ADD,
        SUB,
        MUL,
        DIV,

        CMP,
        JUMP,
        JUMPR,

        JUMPZ,
        JUMPNZ,

        JUMPRZ,
        JUMPRNZ,

        JUMPEQ,
        JUMPREQ,

        JUMPNEQ,
        JUMPRNEQ,

        JUMPGT,
        JUMPRGT,

        JUMPLT,
        JUMPRLT,

        JUMPGEQ,
        JUMPRGEQ,

        JUMPLEQ,
        JUMPRLEQ,

        PUSH,
        POP,
        CALL,
        RET,
        HALT
    }

    public static class OpEncodingHelpers
    {
        private const int firstJumpOp = (int)Op.JUMP;
        private const int lastJumpOp = (int)Op.JUMPR_LEQ;

        // None(1), IMM(3), REG(2), REG_IMM(4), REG_REG(3), REG_MEM(4), MEM_REG(4)
        private static readonly int[] sizes = { 1, 3, 2, 4, 3, 4, 4 };

        public const int EncodingMask = 0b111_00000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OpMayModifyPC(Op op)
        {
            return op == Op.CALL || op == Op.RET ||
                ((int)op >= firstJumpOp && (int)op <= (int)lastJumpOp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OpEncoding GetInstructionEncoding(Op op)
        {
            return (OpEncoding)((int)op & 0b111_00000);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FirstOperandNeedWrite(Op op)
        {
            var encoding = GetInstructionEncoding(op);

            if (encoding == OpEncoding.NONE)
                return false;
            else if (encoding == OpEncoding.IMM)
                return false;
            else if (encoding == OpEncoding.REG && op == Op.PUSH)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInstructionSize(Op op)
        {
            int idx = (int)GetInstructionEncoding(op) >> 5;
            return sizes[idx];
        }
    }

    public enum OpEncoding : byte
    {
        NONE = 0,
        IMM = 0b001_00000,
        REG = 0b010_00000,
        REG_IMM = 0b011_00000,
        REG_REG = 0b100_00000,
        REG_MEM = 0b101_00000,
        MEM_REG = 0b111_00000,
    }

    public enum Op : byte
    {
        NOOP = OpEncoding.NONE + 0,
        //#  Move operations
        MOV_REG_IMM = OpEncoding.REG_IMM | 0,        // [dst reg8],  [src imm16] (Mov imm into register)                     <4>
        MOV_REG_REG = OpEncoding.REG_REG | 0,        // [dst reg8],  [src reg8]  (Mov value of second reg to first)          <3>
        MOV_REG_MEM = OpEncoding.REG_MEM | 0,        // [dst reg8],  [src addr16] (Mov value of memory address to reg)       <4>
        MOV_MEM_REG = OpEncoding.MEM_REG | 0,        // [dst adr16], [src reg8] (Mov value of register into mem at address)  <4>

        //#  Stack operations
        PUSH = OpEncoding.REG | 0,                // [src reg8]   Push value of reg to top of stack                       <2>
        PUSH_IMM = OpEncoding.IMM | 0,            // [src imm16]  Push immediate value to top of stack                    <3>
        POP = OpEncoding.REG | 1,                // [dst reg8]   Pop value off top of stack, move into reg               <2>
        DUP = OpEncoding.NONE | 2,                //              Duplicate word at top of stack                          <1>

        //#  Arithmetic operations (Perform operation on operand1 and operand2, store result in dst reg)
        //## unsigned 16 bit integers
        ADD = OpEncoding.REG_REG | 1,               // [dst/operand1 reg8], [operand2 reg8] (as u16, $1 = $1 + $2)          <3>
        SUB = OpEncoding.REG_REG | 2,               // [dst/operand1 reg8], [operand2 reg8] (as u16, $1 = $1 - $2)          <3>
        MUL = OpEncoding.REG_REG | 3,               // [dst/operand1 reg8], [operand2 reg8] (as u16, $1 = $1 * $2)          <3>
        DIV = OpEncoding.REG_REG | 4,               // [dst/operand1 reg8], [operand2 reg8] (as u16, $1 = $1 / $2)          <3>
        //## signed 16 bit integers     
        ADDS = OpEncoding.REG_REG | 5,              // [dst/operand1 reg8], [operand2 reg8] (as i16, $1 = $1 + $2)          <3>
        SUBS = OpEncoding.REG_REG | 6,              // [dst/operand1 reg8], [operand2 reg8] (as i16, $1 = $1 - $2)          <3>
        MULS = OpEncoding.REG_REG | 7,              // [dst/operand1 reg8], [operand2 reg8] (as i16, $1 = $1 * $2)          <3>
        DIVS = OpEncoding.REG_REG | 8,              // [dst/operand1 reg8], [operand2 reg8] (as i16, $1 = $1 / $2)          <3>
        //## 16 bit floating point numbers
        ADDF = OpEncoding.REG_REG | 9,              // [dst/operand1 reg8], [operand2 reg8] (as f16, $1 = $1 + $2)          <3>
        SUBF = OpEncoding.REG_REG | 10,             // [dst/operand1 reg8], [operand2 reg8] (as f16, $1 = $1 - $2)          <3>
        MULF = OpEncoding.REG_REG | 11,             // [dst/operand1 reg8], [operand2 reg8] (as f16, $1 = $1 * $2)          <3>
        DIVF = OpEncoding.REG_REG | 12,             // [dst/operand1 reg8], [operand2 reg8] (as f16, $1 = $1 / $2)          <3>

        //#  Bitwise operations
        OR = OpEncoding.REG_REG | 13,               // [dst/operand1 reg8], [operand2 reg8] $1 = $1 | $2                    <3>
        XOR = OpEncoding.REG_REG | 14,              // [dst/operand1 reg8], [operand2 reg8] $1 = $1 ^ $2                    <3>
        AND = OpEncoding.REG_REG | 15,              // [dst/operand1 reg8], [operand2 reg8] $1 = $1 & $2                    <3>
        NOT = OpEncoding.REG | 2,                   // [dst/operand reg8]                   $1 = ~$1                        <2>

        //#  Comparison operations
        //## unsigned 16 bit integers
        CMP = OpEncoding.NONE | 3,                      // (Compare unsigned i16 value of word at sp-4 with word at sp-2)                                   <1>
        CMP_REG_REG = OpEncoding.REG_REG | 16,           // [operand1 reg8],  [operand2 reg8]    (Compare value of first and second register)                <3>
        CMP_REG_MEM = OpEncoding.REG_MEM | 1,           // [operand1 reg8],  [operand2 adr16]   (Compare value of register with value at address)           <3> 
        CMP_MEM_REG = OpEncoding.MEM_REG | 1,           // [operand1 adr16], [operand2 reg8]    (Compare value at address with value of register)           <3>
        //## signed 16 bit integers
        CMPF = OpEncoding.NONE | 4,               // (Compare float16 value of word at sp-4 with word at sp-2)                                        <1>
        CMPF_REG_REG = OpEncoding.REG_REG | 17,       // [operand1 reg8],  [operand2 reg8]    (Compare float16 value of first and second register)        <3>
        CMPF_REG_MEM = OpEncoding.REG_MEM | 2,       // [operand1 reg8],  [operand2 adr16]   (Compare float16 value of register with value at address)   <3>     
        CMPF_MEM_REG = OpEncoding.MEM_REG | 2,       // [operand1 adr16], [operand2 reg8]    (Compare float16 value at address with value of register)   <3>
        //## 16 bit floating point numbers
        CMPS = OpEncoding.NONE | 5,               // (Compare signed i16 value of word at sp-4 with word at sp-2)                                     <1>
        CMPS_REG_REG = OpEncoding.REG_REG | 18,       // [operand1 reg8],  [operand2 reg8]    (Compare float16 value of first and second register)        <3>
        CMPS_REG_MEM = OpEncoding.REG_MEM | 3,       // [operand1 reg8],  [operand2 adr16]   (Compare float16 value of register with value at address)   <3>
        CMPS_MEM_REG = OpEncoding.MEM_REG | 4,       // [operand1 adr16], [operand2 reg8]    (Compare float16 value at address with value of register)   <3>

        //#  Jump/branch operations
        JUMP = OpEncoding.IMM | 1,               // [addr16] (Unconditional jump to addr16)                                                          <3>
        JUMPR = OpEncoding.IMM | 2,              // [rel offset i16] (Unconditional jump to PC+offset)                                               <3>

        JUMP_ZERO = OpEncoding.IMM | 3,          // [addr16] (Jump to addr16 IF top of stack is zero)                                                <3>
        JUMPR_ZERO = OpEncoding.IMM | 4,         // [rel offset i16] (Jump to PC+offset IF top of stack is zero)                                     <3>

        JUMP_NOT_ZERO = OpEncoding.IMM | 5,      // [addr16] (Jump to addr16 IF top of stack is not 0)                                               <3>
        JUMPR_NOT_ZERO = OpEncoding.IMM | 6,     // [rel offset i16] (Jump to PC+offset IF top of stack is not zero)                                 <3>

        JUMP_EQ = OpEncoding.IMM | 7,            // [addr16] (Jump to addr16 IF EQUALS Comparison flag is 1)                                         <3>
        JUMPR_EQ = OpEncoding.IMM | 8,           // [rel addr16] (Jump to addr16 IF EQUALS Comparison flag is 1)                                     <3>

        JUMP_NEQ = OpEncoding.IMM | 9,           // [addr16] (Jump to addr16 IF EQUALS Comparison flag is 0)                                         <3>
        JUMPR_NEQ = OpEncoding.IMM | 10,          // [rel addr16] (Jump to PC+offset IF EQUALS Comparison flag is 0)                                  <3>

        JUMP_GT = OpEncoding.IMM | 11,            // [addr16] (Jump to addr16 IF GT Comparison flag is 1)                                             <3>
        JUMPR_GT = OpEncoding.IMM | 12,           // [rel addr16] (Jump to PC+offset IF GT Comparison flag is 1)                                      <3>

        JUMP_LT = OpEncoding.IMM | 13,            // [addr16] (Jump to addr16 IF LT Comparison flag is 1)                                             <3>
        JUMPR_LT = OpEncoding.IMM | 14,           // [rel addr16] (Jump to PC+offset IF LT Comparison flag is 1)                                      <3>

        JUMP_GEQ = OpEncoding.IMM | 15,           // [addr16] (Jump to addr16 IF LT Comparison flag is 0)                                             <3>
        JUMPR_GEQ = OpEncoding.IMM | 16,          // [rel addr16] (Jump to PC+offset IF LT Comparison flag is 0)                                      <3>

        JUMP_LEQ = OpEncoding.IMM | 17,           // [addr16] (Jump to addr16 IF GT Comparison flag is 0)                                             <3>
        JUMPR_LEQ = OpEncoding.IMM | 18,          // [rel addr16] (Jump to PC+offset IF GT Comparison flag is 0)                                      <3>

        //#  Subroutine operations
        CALL = OpEncoding.IMM | 19,               // [addr16] Store SP, SB, PC into Registers 6, 7, 8 then jump to address                            <3>
        RET = OpEncoding.NONE | 6,                // Restore SP and SB from registers 6 and 7 and jump back to address stored in Register 8           <1>

        //#  Misc operations
        HALT = OpEncoding.NONE | 7,               // Terminate                                                                                        <1>
    }
}
/* none    = 000_0000
 * imm     = 001_0000
 * reg     = 010_0000
 * reg_imm = 011_0000
 * reg_reg = 100_0000
 * reg_mem = 101_0000
 * mem_reg = 111_0000
 */ 