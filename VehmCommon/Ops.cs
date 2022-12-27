namespace Vehement.Common
{
    public static class OpsMapper
    {
        public static Dictionary<Op, Mnemonic> mapping = new Dictionary<Op, Mnemonic>()
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

            { Op.JUMP, Mnemonic.JUMP },
            { Op.JUMP_ZERO, Mnemonic.JUMPZ },
            { Op.JUMP_NOT_ZERO, Mnemonic.JUMPNZ },

            { Op.JUMP_EQ, Mnemonic.JUMPEQ },
            { Op.JUMP_NEQ, Mnemonic.JUMPNEQ },
            { Op.JUMP_GT, Mnemonic.JUMPGT },
            { Op.JUMP_LT, Mnemonic.JUMPLT },
            { Op.JUMP_GEQ, Mnemonic.JUMPGEQ },
            { Op.JUMP_LEQ, Mnemonic.JUMPLEQ },

            { Op.PUSH, Mnemonic.PUSH },
            { Op.POP, Mnemonic.POP },

            { Op.CALL, Mnemonic.CALL },
            { Op.RET, Mnemonic.RET },

            { Op.PRINT, Mnemonic.PRINT },
            { Op.HALT, Mnemonic.HALT },
        };

        public static Mnemonic GetMnemonic(Op op)
        {
            return mapping[op];
        }
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
        JUMPZ,
        JUMPNZ,

        JUMPEQ,
        JUMPNEQ,
        JUMPGT,
        JUMPLT,
        JUMPGEQ,
        JUMPLEQ,

        PUSH,
        POP,
        CALL,
        RET,
        PRINT,
        HALT
    }

    public enum Op : byte
    {
        NOOP = 0,

        MOV_REG_IMM, // [reg8], [imm16] (Mov imm into register)
        MOV_REG_REG, // [reg8], [reg8]  (Mov value of second reg to first)
        MOV_REG_MEM, // [reg8], [adr16] (Mov value of memory address to reg)
        MOV_MEM_REG, // [adr16], [reg8] (Mov value of register into mem at address)

        DUP,

        ADD,        // [reg8], [reg8], [reg8] (unsigned i16, Add regs 2 and 3, store in reg 1)
        SUB,        // [reg8], [reg8], [reg8] (unsigned i16, Sub ..
        MUL,        // [reg8], [reg8], [reg8] (unsigned i16, Mul ..
        DIV,        // [reg8], [reg8], [reg8] (unsigned i16, Div ..

        ADDS,        // [reg8], [reg8], [reg8] (Signed i16, Add regs 2 and 3, store in reg 1)
        SUBS,        // [reg8], [reg8], [reg8] (Signed i16, Sub ..
        MULS,        // [reg8], [reg8], [reg8] (Signed i16, Mul ..
        DIVS,        // [reg8], [reg8], [reg8] (Signed i16, Div ..

        ADDF,        // [reg8], [reg8], [reg8] (float16, Add regs 2 and 3, store in reg 1)
        SUBF,        // [reg8], [reg8], [reg8] (float16, Sub ..
        MULF,        // [reg8], [reg8], [reg8] (float16, Mul ..
        DIVF,        // [reg8], [reg8], [reg8] (float16, Div ..

        CMP,        // (Compare unsigned i16 value of word at sp-4 with word at sp-2)
        CMP_REG_REG,// [reg8], [reg8] (Compare value of first and second register)
        CMP_REG_MEM,// [reg8], [adr16](Compare value of register with value at address)            
        CMP_MEM_REG,// [adr16], [reg8](Compare value at address with value of register)

        CMPF,        // (Compare float16 value of word at sp-4 with word at sp-2)
        CMPF_REG_REG,// [reg8], [reg8] (Compare float16 value of first and second register)
        CMPF_REG_MEM,// [reg8], [adr16](Compare float16 value of register with value at address)            
        CMPF_MEM_REG,// [adr16], [reg8](Compare float16 value at address with value of register)

        CMPS,        // (Compare signed i16 value of word at sp-4 with word at sp-2)
        CMPS_REG_REG,// [reg8], [reg8] (Compare float16 value of first and second register)
        CMPS_REG_MEM,// [reg8], [adr16](Compare float16 value of register with value at address)            
        CMPS_MEM_REG,// [adr16], [reg8](Compare float16 value at address with value of register)

        JUMP,       // [off16] (Unconditional)
        JUMP_ZERO,      // [off16] (Jump to program+off16 IF top of stack is 0)
        JUMP_NOT_ZERO,     // [off16] ( ... not zero)

        JUMP_EQ,     // [off16] (... if EQUALS Comparison flag is 1)
        JUMP_NEQ,    // [off16] (... if EQUALS Comparison flag is 0)
        JUMP_GT,     // [off16] (... if GT Comparison flag is 1)
        JUMP_LT,     // [off16] (... if LT Comparison flag is 1)

        JUMP_GEQ,     // [off16] (... if LT Comparison flag is 0)
        JUMP_LEQ,     // [off16] (... if GT Comparison flag is 0)

        PUSH,       // [reg8] Push value of reg to top of stack
        POP,        // [reg8] Pop value off top of stack, move into reg
        CALL,       // [off16] Like a jump, but also sets up some registers:
        RET,        // Restore SP and SB from registers 6 and 7.
                    // Jump back to address stored in reg8
        PRINT,      // [adr16] Prints a VString to the Console pointed to by adr16.
        HALT,       // Terminate
    }
}